using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using System.IO;
using System.Runtime.InteropServices;
using Ascendant.Graphics.lighting;

namespace Ascendant.Graphics {
    public class Mesh {
        public BeginMode mode { get; private set; }
        public Vector3[] vertices { get; private set; }
        public Vector2[] texCoords { get; private set; }
        public Vector3[] normals { get; private set; }
        public ushort[] indices { get; private set; }

        public Mesh(Vector3[] _v, Vector2[] _t, Vector3[] _n, ushort[] _i, BeginMode _m) {
            mode = _m;
            vertices = _v;
            texCoords = _t;
            normals = _n;
            indices = _i;
        }
    }
    struct PerLight {
        internal Vector4 cameraSpaceLightPos;
        internal Vector4 lightIntensity;
    }

    internal struct LightBlockGamma {
        internal Vector4 ambientIntensity;
        internal Vector4 attenuationMaxGamma;
        internal PerLight[] lights;

        internal static int getByteSize() {
            return Marshal.SizeOf(typeof(LightBlockGamma)) + Marshal.SizeOf(typeof(PerLight)) * Lighting.numLights;
        }
    }


    [StructLayout(LayoutKind.Sequential)]
    struct Material {
        public Vector4 diffuseColor { get; private set; }
        public Vector4 specularColor { get; private set; }
        public Vector4 specularShininess { get; private set; }
        public Material(Vector4 dColor, Vector4 sColor, float shine)
            : this() {
            diffuseColor = dColor;
            specularColor = sColor;
            specularShininess = new Vector4(shine, 0f, 0f, 0f);
        }
    }

    class MyParser {
        internal static protected DisplayObject parseObject(Game game, string filename) {
            StreamReader reader = File.OpenText(AppConfig.Default.itempath + @"\base\" + filename);
            string line;
            Vector3 position = Vector3.Zero, scale = Vector3.One;
            Quaternion orientation = Quaternion.FromAxisAngle(Vector3.UnitY, 0);

            var children = new List<DisplayObject>();
            Material mat = new Material(Vector4.Zero, Vector4.Zero, 0f);
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
            Physics.PhysicsObject physObj = new Physics.PhysicsObject(size, mass, position, momentum, orientation, angularMomentum);
            game.window.sim.AddObject(physObj);
            DisplayObject retVal = MyLoader.loadDisplayObject(game, scale, physObj, mesh, mat, perLight, children);
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
        internal static protected Material parseMaterial(string filename, string materialName) {
            Material mat;
            String key = filename + materialName;
            if (!matMap.TryGetValue(key, out mat)) {
                StreamReader reader = File.OpenText(AppConfig.Default.itempath + @"\material\" + filename);
                string line;
                Vector4 dcolor = new Vector4(.5f);
                Vector4 scolor = new Vector4(.5f);
                float shine = .5f;
                while ((line = reader.ReadLine()) != null) {
                    line = line.Trim();
                    if (isSkipLine(line)) continue;
                    string[] items = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                    switch (items[0]) {
                        case "mtl":
                            if (items.Length < 2) throw new InvalidDataException("No material data available for " + filename + ", data: " + line);
                            switch (items[1]) {
                                case "dcolor": dcolor = new Vector4(float.Parse(items[2]), float.Parse(items[3]), float.Parse(items[4]), float.Parse(items[5]));
                                    break;
                                case "scolor": scolor = new Vector4(float.Parse(items[2]), float.Parse(items[3]), float.Parse(items[4]), float.Parse(items[5]));
                                    break;
                                case "shine": shine = float.Parse(items[2]);
                                    break;
                            }
                            break;
                        default:
                            continue;
                    }
                }
                mat = new Material(dcolor, scolor, shine);
                matMap.Add(key, mat);
            }
            return mat;
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
                var indices = new List<ushort>();

                BeginMode type = BeginMode.Triangles;
                while ((line = reader.ReadLine()) != null) {
                    line = line.Trim();
                    if (isSkipLine(line)) continue;
                    string[] items = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                    switch (items[0]) {
                        case "v":
                            if (items.Length < 4)
                                throw new InvalidDataException("Not enough data to complete vertex for " + filename + ", data: " + line);
                            vertices.Add(new Vector3(float.Parse(items[1]), float.Parse(items[2]), float.Parse(items[3])));
                            break;
                        case "vt":
                            if (items.Length < 3)
                                throw new InvalidDataException("Not enough data to complete texture for " + filename + ", data: " + line);
                            texCoords.Add(new Vector2(float.Parse(items[1]), float.Parse(items[2])));
                            break;
                        case "vn":
                            if (items.Length < 4)
                                throw new InvalidDataException("Not enough data to complete normal for " + filename + ", data: " + line);
                            normals.Add(new Vector3(float.Parse(items[1]), float.Parse(items[2]), float.Parse(items[3])));
                            break;
                        case "f":
                            switch (items.Length) {
                                case 4: type = BeginMode.Triangles;
                                    break;
                                case 5: type = BeginMode.Quads;
                                    break;
                                default: throw new InvalidDataException("Face data must be specified in triangles with exactly 3 vertices or quads with 4 vertices in CW rotation, file: " + filename + ", data: " + line);
                            }
                            for (int i = 1; i < items.Length; ++i) {
                                indices.Add((ushort)(ushort.Parse(items[i].Split('/')[0]) - 1));
                            }
                            break;
                        default:
                            continue;
                    }
                }
                mesh = new Mesh(vertices.ToArray(), texCoords.ToArray(), normals.ToArray(), indices.ToArray<ushort>(), type);
                meshMap.Add(key, mesh);
            }
            return mesh;
        }
    }
    class MyLoader {
        internal class MaterialLoader {
            internal static uint g_materialUniformBuffer;

            public const int g_materialBlockIndex = 0;
            internal static List<Material> materials = new List<Material>();
            internal static int m_sizeMaterialBlock;
            internal static void LoadMaterialBufferBlock(int program) {
                m_sizeMaterialBlock = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Material));

                int sizeMaterialUniformBuffer = m_sizeMaterialBlock * materials.Count;

                GL.GenBuffers(1, out g_materialUniformBuffer);
                GL.BindBuffer(BufferTarget.UniformBuffer, g_materialUniformBuffer);
                GL.BufferData(BufferTarget.UniformBuffer, (IntPtr)sizeMaterialUniformBuffer, materials.ToArray(), BufferUsageHint.StaticDraw);

                int materialBlock = GL.GetUniformBlockIndex(program, "Material");
                GL.UniformBlockBinding(program, materialBlock, g_materialBlockIndex);
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


        static internal DisplayObject loadDisplayObject(Game game, OpenTK.Vector3 scale, Physics.PhysicsObject physObj, Mesh mesh, Material mat, PerLight light, List<DisplayObject> children) {
            int matIndex = MaterialLoader.AddMaterial(mat);
            return new DisplayObject(game, scale, physObj, mesh, matIndex, light, children);
        }

    }
}
