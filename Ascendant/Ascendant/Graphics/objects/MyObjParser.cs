using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using System.IO;
using System.Runtime.InteropServices;
using Ascendant.Graphics.lighting;
using System.Diagnostics;

namespace Ascendant.Graphics {
    public class Mesh {
        public Vector3[] vertices { get; private set; }
        public Vector2[] texCoords { get; private set; }
        public Vector3[] normals { get; private set; }

        public Mesh(Vector3[] _v, Vector2[] _t, Vector3[] _n) {
            vertices = _v;
            texCoords = _t;
            normals = _n;
        }
    }
    [StructLayout(LayoutKind.Explicit)]
    struct PerLight {
        [FieldOffset(0)]
        internal Vector4 cameraSpaceLightPos;
        [FieldOffset(16)]
        internal Vector4 lightIntensity;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct LightBlockGamma {
        [FieldOffset(0)]
        internal Vector4 ambientIntensity;
        [FieldOffset(16)]
        internal Vector4 attenuationMaxGamma;
        [FieldOffset(32)]
        internal PerLight[] lights;

        internal static int getByteSize() {
            return Marshal.SizeOf(typeof(Vector4)) * 2 + Marshal.SizeOf(typeof(PerLight)) * Lighting.numLights;
        }

        unsafe internal Byte[] getBytes() {
            int size = getByteSize();
            IntPtr data = Marshal.AllocHGlobal(size);
            IntPtr ptr = data;
            Marshal.StructureToPtr(this.ambientIntensity, ptr, false);
            ptr += Marshal.SizeOf(typeof(Vector4));
            Marshal.StructureToPtr(this.attenuationMaxGamma, ptr, false);
            ptr += Marshal.SizeOf(typeof(Vector4));
            for (int i = 0; i < lights.Length; ++i) {
                Marshal.StructureToPtr(this.lights[i], ptr, false);
                ptr += Marshal.SizeOf(typeof(PerLight));
            }
            byte[] output = new byte[size];
            Marshal.Copy(data, output, 0, size);
            Marshal.FreeHGlobal(data);
            return output;
        }
    }

    struct Material {
        public Vector4 diffuseColor;
        public Vector4 specularColor;
        public Vector4 specularShininess;
    }

    class MyParser {
        internal static protected DisplayObject parseObject(Game game, string filename) {
            StreamReader reader = File.OpenText(AppConfig.Default.itempath + @"\base\" + filename);
            string line;
            Vector3 position = Vector3.Zero, scale = Vector3.One;
            Quaternion orientation = Quaternion.FromAxisAngle(Vector3.UnitY, 0);

            var children = new List<DisplayObject>();
            Material mat = new Material();
            Mesh mesh = null;
            PerLight perLight = new PerLight();
            float size = 0f;
            float mass = 0f;
            Vector3 momentum = Vector3.Zero;
            Vector3 angularMomentum = Vector3.Zero;
            while ((line = reader.ReadLine()) != null) {
                line = line.Trim();
                if (isSkipLine(line)) continue;
                string[] items = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                switch (items[0]) {
                    case "position":
                        if (items.Length < 4)
                            throw new InvalidDataException("Not enough data to complete position for " + filename + ", data: " + line);
                        position = new Vector3(float.Parse(items[1]), float.Parse(items[2]), float.Parse(items[3]));
                        break;
                    case "scale":
                        if (items.Length < 4)
                            throw new InvalidDataException("Not enough data to complete scale for " + filename + ", data: " + line);
                        scale = new Vector3(float.Parse(items[1]), float.Parse(items[2]), float.Parse(items[3]));
                        break;
                    case "orientation":
                        if (items.Length < 5)
                            throw new InvalidDataException("Not enough data to complete orientation for " + filename + ", data: " + line);
                        orientation = Quaternion.FromAxisAngle(new Vector3(float.Parse(items[1]), float.Parse(items[2]), float.Parse(items[3])), float.Parse(items[4]));
                        break;
                    case "size":
                        size = float.Parse(items[1]);
                        break;
                    case "mass":
                        mass = float.Parse(items[1]);
                        break;
                    case "momentum":
                        momentum = new Vector3(float.Parse(items[1]), float.Parse(items[2]), float.Parse(items[3]));
                        break;
                    case "angularMomentum":
                        angularMomentum = new Vector3(float.Parse(items[1]), float.Parse(items[2]), float.Parse(items[3]));
                        break;
                    case "children":
                        for (int i = 1; i < items.Length; ++i) {
                            children.Add(parseObject(game, items[i]));
                        }
                        break;
                    case "mesh":
                        mesh = parseMesh(items[1]);
                        break;
                    case "mtl":
                        mat = parseMaterial(items[1], items[2]);
                        break;
                    case "light":
                        perLight = parseLighting(items[1]);
                        break;
                    default:
                        continue;
                }
            }
            Physics.PhysicsObject physObj = new Physics.PhysicsObject(size, mass, position, momentum, orientation, scale, angularMomentum, mesh);
            game.window.sim.AddObject(physObj);
            DisplayObject retVal = MyLoader.loadDisplayObject(game, physObj, mat, perLight, children);
            if (perLight.lightIntensity != Vector4.Zero) game.Lights.AddPointLight(retVal);
            return retVal;
        }

        internal static protected PerLight parseLighting(string filename) {
            StreamReader reader = File.OpenText(AppConfig.Default.itempath + @"\lighting\" + filename);
            string line;
            Vector4 intensity = Vector4.Zero;
            Vector4 position = Vector4.Zero;
            while ((line = reader.ReadLine()) != null) {
                line = line.Trim();
                if (isSkipLine(line)) continue;
                string[] items = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                switch (items[0]) {
                    case "pos":
                        position = new Vector4(float.Parse(items[1]), float.Parse(items[2]), float.Parse(items[3]), 1.0f);
                        break;
                    case "intensity":
                        intensity = new Vector4(float.Parse(items[1]), float.Parse(items[2]), float.Parse(items[3]), float.Parse(items[4]));
                        break;
                    default:
                        continue;
                }
            }
            PerLight light = new PerLight();
            light.lightIntensity = intensity;
            light.cameraSpaceLightPos = position;
            return light;
        }

        static Dictionary<String, Material> matMap = new Dictionary<String, Material>();
        static private void addMaterial(String key, Vector4 dColor, Vector4 sColor, float shine) {
            if (!matMap.ContainsKey(key)) {
                Material newMat = new Material();
                newMat.diffuseColor = dColor;
                newMat.specularColor = sColor;
                newMat.specularShininess = new Vector4(shine);
                matMap.Add(key, newMat);
            }
        }
        internal static protected Material parseMaterial(string filename, string materialName) {
            Material mat;
            String key = filename + materialName;
            if (!matMap.TryGetValue(key, out mat)) {
                StreamReader reader = File.OpenText(AppConfig.Default.itempath + @"\material\" + filename);
                string line;
                String currentMat = null;
                Vector4 dcolor = Vector4.Zero;
                Vector4 scolor = Vector4.Zero;
                float shine = 0f;
                while ((line = reader.ReadLine()) != null) {
                    line = line.Trim();
                    if (isSkipLine(line)) continue;
                    string[] items = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                    switch (items[0]) {
                        case "newmtl":
                            if (currentMat != null) addMaterial(filename + currentMat, dcolor, scolor, shine);
                            currentMat = items[1];
                            break;
                        case "ka":
                        case "kd":
                        case "dcolor": dcolor = new Vector4(float.Parse(items[1]), float.Parse(items[2]), float.Parse(items[3]), float.Parse(items[4]));
                            break;
                        case "ks":
                        case "scolor": scolor = new Vector4(float.Parse(items[1]), float.Parse(items[2]), float.Parse(items[3]), float.Parse(items[4]));
                            break;
                        case "ns":
                        case "shine": shine = float.Parse(items[1]);
                            break;

                        default:
                            continue;
                    }
                }
                addMaterial(filename + currentMat, dcolor, scolor, shine);
            }
            if (matMap.TryGetValue(key, out mat)) {
                return mat;
            } else {
                return new Material();
            }
        }

        private static bool isSkipLine(string line) {
            return (line.StartsWith("#") || line.Equals(""));
        }

        static Dictionary<String, Mesh> meshMap = new Dictionary<String, Mesh>();
        internal static protected Mesh parseMesh(string filename) {
            Mesh mesh;
            String key = filename;
            if (!meshMap.TryGetValue(key, out mesh)) {
                StreamReader reader = File.OpenText(AppConfig.Default.itempath + @"mesh\" + filename);
                string line;
                var vertices = new List<Vector3>();
                var texCoords = new List<Vector2>();
                var normals = new List<Vector3>();
                var vindices = new List<int>();
                var nindices = new List<int>();
                var tindices = new List<int>();
                bool backwards = false;
                PrimitiveType type = PrimitiveType.Triangles;
                while ((line = reader.ReadLine()) != null) {
                    line = line.Trim();
                    if (isSkipLine(line)) continue;
                    string[] items = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                    switch (items[0]) {
                        case "order":
                            if (items[1] == "ccw") {
                                backwards = true;
                            }
                            break;
                        case "v":
                            if (items.Length < 4)
                                throw new InvalidDataException("Not enough data to complete vertex for " + filename + ", data: " + line);
                            if (backwards) {
                                vertices.Add(new Vector3(float.Parse(items[3]), float.Parse(items[2]), float.Parse(items[1])));
                            } else {
                                vertices.Add(new Vector3(float.Parse(items[1]), float.Parse(items[2]), float.Parse(items[3])));
                            }
                            break;
                        case "vt":
                            if (items.Length < 3)
                                throw new InvalidDataException("Not enough data to complete texture for " + filename + ", data: " + line);
                            texCoords.Add(new Vector2(float.Parse(items[1]), float.Parse(items[2])));
                            break;
                        case "vn":
                            if (items.Length < 4)
                                throw new InvalidDataException("Not enough data to complete normal for " + filename + ", data: " + line);
                            Vector3 normalVector;
                            if (backwards) {
                                normalVector = new Vector3(float.Parse(items[3]), float.Parse(items[2]), float.Parse(items[1]));
                            } else {
                                normalVector = new Vector3(float.Parse(items[1]), float.Parse(items[2]), float.Parse(items[3]));
                            }
                            normalVector.Normalize();
                            normals.Add(normalVector);
                            break;
                        case "f":
                            switch (items.Length) {
                                case 4: type = PrimitiveType.Triangles;
                                    break;
                                case 5: type = PrimitiveType.Quads;
                                    break;
                                default: throw new InvalidDataException("Face data must be specified in triangles with exactly 3 vertices or quads with 4 vertices in CW rotation, file: " + filename + ", data: " + line);
                            }
                            for (int i = 1; i < items.Length; ++i) {
                                var index = items[i].Split('/');
                                vindices.Add((ushort)(ushort.Parse(index[0]) - 1));
                                if (index.Length > 1 && index[1] != "")
                                    tindices.Add((ushort)(ushort.Parse(index[1]) - 1));
                                else
                                    tindices.Add(vindices[vindices.Count - 1]);
                                if (index.Length > 2 && index[2] != "")
                                    nindices.Add((ushort)(ushort.Parse(index[2]) - 1));
                                else
                                    nindices.Add(vindices[vindices.Count - 1]);
                            }
                            break;
                        default:
                            continue;
                    }
                }

                var indexVertices = new List<Vector3>();
                var indexTexCoords = new List<Vector2>();
                var indexNormals = new List<Vector3>();
                if (type == PrimitiveType.Triangles) {
                    foreach (int u in vindices) {
                        indexVertices.Add(vertices[u]);
                    }
                    foreach (int u in tindices) {
                        //indexTexCoords.Add(texCoords[u]);
                    }
                    foreach (int u in nindices) {
                        indexNormals.Add(normals[u]);
                    }
                } else if (type == PrimitiveType.Quads) {
                    indexVertices = convertToTriangles(vindices, vertices);
                    indexNormals = convertToTriangles(nindices, normals);
                    //indexTexCoords
                } else {
                    throw new InvalidDataException("Mesh type was not triangles or quads");
                }
               
                mesh = new Mesh(indexVertices.ToArray(), indexTexCoords.ToArray(), indexNormals.ToArray());
                meshMap.Add(key, mesh);
            }
            return mesh;
        }

        private static List<Vector3> convertToTriangles(List<int> vindices, List<Vector3> vertices) {
            var indexVertices = new List<Vector3>();
            Vector3[] quad = new Vector3[4];
            for (int i = 0; i < vindices.Count; ++i) {
                if (i % 4 == 0 && i > 0) {
                    indexVertices.Add(quad[0]);
                    indexVertices.Add(quad[1]);
                    indexVertices.Add(quad[2]);

                    indexVertices.Add(quad[2]);
                    indexVertices.Add(quad[3]);
                    indexVertices.Add(quad[0]);
                }
                quad[i % 4] = vertices[vindices[i]];
            }
            return indexVertices;
        }
    }
    class MyLoader {
        internal class MaterialLoader {
            internal static uint g_materialUniformBuffer;

            internal static List<Material> materials = new List<Material>();
            internal static int m_sizeMaterialBlock;
            internal static void LoadMaterialBufferBlock(int program) {
                //Align the size of each MaterialBlock to the uniform buffer alignment.
                int uniformBufferAlignSize = 0;
                GL.GetInteger(GetPName.UniformBufferOffsetAlignment, out uniformBufferAlignSize);
                int blockSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Material));
                m_sizeMaterialBlock = blockSize + (uniformBufferAlignSize - (blockSize % uniformBufferAlignSize));

                int sizeMaterialUniformBuffer = m_sizeMaterialBlock * materials.Count;

                unsafe {
                    IntPtr data = Marshal.AllocHGlobal(sizeMaterialUniformBuffer);
                    IntPtr inData = data;
                    for (int i = 0; i < materials.Count; ++i) {
                        Marshal.StructureToPtr(materials[i], inData, false);
                        inData = IntPtr.Add(inData, m_sizeMaterialBlock);
                    }
                    GL.GenBuffers(1, out g_materialUniformBuffer);
                    GL.BindBuffer(BufferTarget.UniformBuffer, g_materialUniformBuffer);
                    GL.BufferData(BufferTarget.UniformBuffer, (IntPtr)sizeMaterialUniformBuffer, data, BufferUsageHint.StaticDraw);
                    Marshal.FreeHGlobal(data);
                }
                int materialBlock = GL.GetUniformBlockIndex(program, "Material");
                GL.UniformBlockBinding(program, materialBlock, Window.g_materialBlockIndex);
                GL.BindBuffer(BufferTarget.UniformBuffer, 0);
            }

            internal static int AddMaterial(Material mat) {
                int matIndex;
                if (materials.Contains(mat)) {
                    matIndex = materials.FindIndex(material => material.Equals(mat));
                } else {
                    materials.Add(mat);
                    matIndex = materials.Count - 1;
                }
                return matIndex;
            }

            internal static int getMaterialCount() {
                return materials.Count;
            }
        }


        static internal DisplayObject loadDisplayObject(Game game, Physics.PhysicsObject physObj, Material mat, PerLight light, List<DisplayObject> children) {
            int matIndex = MaterialLoader.AddMaterial(mat);
            return new DisplayObject(game, physObj, matIndex, light, children);
        }

    }
}
