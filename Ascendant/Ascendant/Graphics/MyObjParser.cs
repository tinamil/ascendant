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
using Ascendant.Physics;
using Ascendant.Graphics.objects;
using MIConvexHull;

namespace Ascendant.Graphics {
    static class MyParser {
        
        static Dictionary<String, Mesh> meshMap = new Dictionary<String, Mesh>();

        static public World parseWorld(Game game, string filename) {
            StreamReader reader = File.OpenText(AppConfig.Default.itempath + @"\world\" + filename);
            string line;
            Vector4 ambient = Vector4.Zero, background = Vector4.Zero;
            var children = new List<MovableObject>();
            World retVal = new World(game);
            while ((line = reader.ReadLine()) != null) {
                line = line.Trim();
                if (isSkipLine(line)) continue;
                string[] items = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                switch (items[0]) {
                    case "base":
                        if (items.Length < 2)
                            throw new InvalidDataException("Not enough data to load for " + filename + ", data: " + line);
                        children.Add(parseObject(retVal, items[1]));
                        break;
                    case "ambient":
                        if (items.Length < 4) throw new InvalidDataException("Not enough data to load ambient lights for " + filename + ", data: " + line);
                        ambient = new Vector4(float.Parse(items[1]), float.Parse(items[2]), float.Parse(items[3]), float.Parse(items[4]));
                        break;
                    case "background":
                        if (items.Length < 4) throw new InvalidDataException("Not enough data to load background color for " + filename + ", data: " + line);
                        background = new Vector4(float.Parse(items[1]), float.Parse(items[2]), float.Parse(items[3]), float.Parse(items[4]));
                        break;
                    default:
                        continue;
                }
            }

            retVal.addRootObjects(children);
            retVal.setLighting(ambient, background);
            return retVal;
        }

        static private MovableObject parseObject(World world, string filename) {
            StreamReader reader = File.OpenText(AppConfig.Default.itempath + @"\base\" + filename);
            string line;
            Vector3 position = Vector3.Zero, scale = Vector3.One;
            Quaternion orientation = Quaternion.FromAxisAngle(Vector3.UnitY, 0);

            var children = new List<MovableObject>();
            Material mat = new Material();
            Mesh mesh = null;
            Lighting.PointLight perLight = new Lighting.PointLight();
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
                            children.Add(parseObject(world, items[i]));
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
            int matIndex = MaterialLoader.AddMaterial(mat);
            MovableObject retVal = new MovableObject(world, matIndex, perLight, children, size, mass, position, momentum, orientation, scale, angularMomentum, mesh);
            foreach (MovableObject child in children) {
                child.setParent(retVal);
            }
            return retVal;
        }

        static private Lighting.PointLight parseLighting(string filename) {
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
            Lighting.PointLight light = new Lighting.PointLight();
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
        static private Material parseMaterial(string filename, string materialName) {
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
                    if (line.Contains('#')) {
                        int commentsBegin = line.IndexOf('#');
                        line = line.Substring(0, commentsBegin);
                    }
                    line = line.Trim().ToLower();
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

        static private Mesh parseMesh(string filename) {
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
                foreach (int u in vindices) {
                    indexVertices.Add(vertices[u]);
                }
                foreach (int u in tindices) {
                    //indexTexCoords.Add(texCoords[u]);
                }
                foreach (int u in nindices) {
                    indexNormals.Add(normals[u]);
                }
                mesh = new Mesh(indexVertices.ToArray(), indexTexCoords.ToArray(), indexNormals.ToArray(), type);
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
}
