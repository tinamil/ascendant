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
using System.Xml.Linq;

namespace Ascendant.Graphics {
    static class MyParser {

        static Dictionary<String, Mesh> meshMap = new Dictionary<String, Mesh>();

        static public World parseWorld(Game game, string filename) {
            StreamReader reader = File.OpenText(AppConfig.Default.itempath + @"\world\" + filename);
            string line;
            var children = new List<GameObject>();
            TimedLinearInterpolator<Sun> sunTimer = null;
            World retVal = new World(game);
            while ((line = reader.ReadLine()) != null) {
                line = line.Trim();
                if (isSkipLine(line)) continue;
                string[] items = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                switch (items[0]) {
                    case "dynamic":
                        //if (items.Length < 2)
                        //    throw new InvalidDataException("Not enough data to load for " + filename + ", data: " + line);
                        //children.Add(parseDynamicObject(retVal, items[1]));
                        break;
                    case "static":
                        if (items.Length < 2)
                            throw new InvalidDataException("Not enough data to load for " + filename + ", data: " + line);
                        children.Add(parseStaticObject(retVal, items[1]));
                        break;
                    case "object":
                        if (items.Length < 2)
                            throw new InvalidDataException("Not enough data to load for " + filename + ", data: " + line);
                        children.Add(parseXMLObject(retVal, items[1], Matrix4.Identity));
                        break;
                    case "sun":
                        sunTimer = parseSun(items[1]);
                        break;
                    default:
                        continue;
                }
            }

            retVal.addRootObjects(children);
            retVal.setSun(sunTimer);
            return retVal;
        }

        private static TimedLinearInterpolator<Sun> parseSun(string path) {
            var timer = new TimedLinearInterpolator<Sun>();
            XDocument xdocument = XDocument.Load(AppConfig.Default.itempath + @"\lighting\" + path);
            IEnumerable<XElement> keys = xdocument.Descendants("key");
            var maxTime = xdocument.Root.Attribute("time");
            var data = new List<Tuple<Sun, float>>();
            foreach (var key in keys) {

                Sun sun = new Sun();

                var time = float.Parse(key.Attribute("time").Value) / 24f;

                {
                    var ambient = key.Attribute("ambient").Value;
                    var ambientStrings = ambient.Split();
                    sun.ambient = new Vector4(float.Parse(ambientStrings[0]), float.Parse(ambientStrings[1]), float.Parse(ambientStrings[2]), float.Parse(ambientStrings[3]));
                }
                {
                    var intensity = key.Attribute("intensity").Value;
                    var intensityStrings = intensity.Split();
                    sun.intensity = new Vector4(float.Parse(intensityStrings[0]), float.Parse(intensityStrings[1]), float.Parse(intensityStrings[2]), float.Parse(intensityStrings[3]));
                }
                {
                    var background = key.Attribute("background").Value;
                    var backgroundStrings = background.Split();
                    sun.background = new Vector4(float.Parse(backgroundStrings[0]), float.Parse(backgroundStrings[1]), float.Parse(backgroundStrings[2]), float.Parse(backgroundStrings[3]));
                }
                {
                    var maxIntensity = key.Attribute("max-intensity").Value;
                    sun.maxIntensity = float.Parse(maxIntensity);
                }

                data.Add(new Tuple<Sun, float>(sun, time));
            }
            timer.SetValues(data.ToArray());
            return timer;
        }

        static private StaticObject parseStaticObject(World world, string filename) {
            StreamReader reader = File.OpenText(AppConfig.Default.itempath + @"\base\" + filename);
            string line;
            Vector3 position = Vector3.Zero, scale = Vector3.One;
            Quaternion orientation = Quaternion.FromAxisAngle(Vector3.UnitY, 0);

            var children = new List<StaticObject>();
            Material mat = new Material();
            Mesh mesh = null;
            var perLight = new List<Lighting.PointLight>();
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
                    case "children":
                        for (int i = 1; i < items.Length; ++i) {
                            children.Add(parseStaticObject(world, items[i]));
                        }
                        break;
                    case "mesh":
                        mesh = parseMesh(items[1]);
                        break;
                    case "mtl":
                        mat = parseMaterial(items[1], items[2]);
                        break;
                    case "light":
                        perLight.Add(parseLighting(items[1]));
                        break;
                    default:
                        continue;
                }
            }
            int matIndex = MaterialLoader.AddMaterial(mat);
            StaticObject retVal = new StaticObject(world, matIndex, perLight, children, position, orientation, scale, mesh);
            return retVal;
        }

        static private GameObject parseXMLObject(World world, string filename, Matrix4 parentTransform) {
            XDocument xdocument = XDocument.Load(AppConfig.Default.itempath + @"\base\" + filename);
            var gObject = xdocument.Element("model");
            var modelType = (string)gObject.Attribute("type");
            var mesh = parseMesh((string)gObject.Element("mesh"));
            var materialElement = gObject.Element("material");
            Material mat;
            if (materialElement != null) {
                mat = parseMaterial((string)materialElement.Element("file"), (string)materialElement.Element("type"));
            } else {
                mat = parseMaterial(null, null);
            }
            var lightList = new List<Lighting.PointLight>();
            var pointLightElement = gObject.Element("point_lights");
            if (pointLightElement != null) {
                foreach (var lightElement in pointLightElement.Elements("light")) {
                    var light = parseLighting((string)lightElement);
                    lightList.Add(light);
                }
            }
            var scale = Vector3.One;
            var scaleElement = gObject.Element("scale");
            if (scaleElement != null) {
                scale = new Vector3(
                    float.Parse((string)scaleElement.Element("x") ?? "1"),
                    float.Parse((string)scaleElement.Element("y") ?? "1"),
                    float.Parse((string)scaleElement.Element("z") ?? "1"));
            }
            var position = new Vector3(0);
            var positionElement = gObject.Element("position");
            if (positionElement != null) {
                position = new Vector3(
                    float.Parse((string)positionElement.Element("x") ?? "0"),
                    float.Parse((string)positionElement.Element("y") ?? "0"),
                    float.Parse((string)positionElement.Element("z") ?? "0")
                );
            }
            var orientation = Quaternion.FromAxisAngle(Vector3.UnitY, 0);
            var orientationElement = gObject.Element("orientation");
            if (orientationElement != null) {
                var orientationNormal = new Vector3(
                    float.Parse((string)orientationElement.Element("x") ?? "0"),
                    float.Parse((string)orientationElement.Element("y") ?? "1"),
                    float.Parse((string)orientationElement.Element("z") ?? "0"));
                orientation = Quaternion.FromAxisAngle(
                    orientationNormal,
                    float.Parse((string)orientationElement.Element("degrees") ?? "0"));
            }
            var mass = float.Parse((string)gObject.Element("mass") ?? "0");
            var momentum = new Vector3(0);
            var momentumElement = gObject.Element("momentum");
            if (momentumElement != null) {
                momentum = new Vector3(
                    float.Parse((string)momentumElement.Element("x") ?? "0"),
                    float.Parse((string)momentumElement.Element("y") ?? "0"),
                    float.Parse((string)momentumElement.Element("z") ?? "0")
                );
            }
            var angularMomentum = new Vector3(0);
            var angularMomentumElement = gObject.Element("angularmomentum");
            if (angularMomentumElement != null) {
                angularMomentum = new Vector3(
                    float.Parse((string)angularMomentumElement.Element("x") ?? "0"),
                    float.Parse((string)angularMomentumElement.Element("y") ?? "0"),
                    float.Parse((string)angularMomentumElement.Element("z") ?? "0")
                );
            }
            var children = new Dictionary<GameObject, ConeTwist>();
            var childElementEnumerable = gObject.Element("children");
            if (childElementEnumerable != null) {
                foreach (var childElement in childElementEnumerable.Elements()) {
                    var childFile = (string)childElement.Element("model");
                    var constraintElement = childElement.Element("constraint");
                    var constraintType = (string)constraintElement.Attribute("type");
                    switch (constraintType) {
                        case "conetwist":
                            var xVal = (string)constraintElement.Element("swingX");
                            var yVal = (string)constraintElement.Element("swingY");
                            var twist = (string)constraintElement.Element("twist");
                            var softness = (string)constraintElement.Element("softness");
                            var bias = (string)constraintElement.Element("bias");
                            var relaxation = (string)constraintElement.Element("relaxation");
                            var positionParent = new Vector3(0);
                            var positionParentElement = gObject.Element("position");
                            if (positionParentElement != null) {
                                positionParent = new Vector3(
                                    float.Parse((string)positionParentElement.Element("x") ?? "0"),
                                    float.Parse((string)positionParentElement.Element("y") ?? "0"),
                                    float.Parse((string)positionParentElement.Element("z") ?? "0")
                                );
                            }
                            var orientationParent = Quaternion.FromAxisAngle(Vector3.UnitY, 0);
                            var orientationParentElement = gObject.Element("orientation");
                            if (orientationParentElement != null) {
                                var orientationParentNormal = new Vector3(
                                    float.Parse((string)orientationParentElement.Element("x") ?? "0"),
                                    float.Parse((string)orientationParentElement.Element("y") ?? "1"),
                                    float.Parse((string)orientationParentElement.Element("z") ?? "0"));
                                orientationParent = Quaternion.FromAxisAngle(
                                    orientationParentNormal,
                                    float.Parse((string)orientationParentElement.Element("degrees") ?? "0"));
                            }
                            var positionChild = new Vector3(0);
                            var positionChildElement = gObject.Element("position");
                            if (positionChildElement != null) {
                                positionChild = new Vector3(
                                    float.Parse((string)positionChildElement.Element("x") ?? "0"),
                                    float.Parse((string)positionChildElement.Element("y") ?? "0"),
                                    float.Parse((string)positionChildElement.Element("z") ?? "0")
                                );
                            }
                            var orientationChild = Quaternion.FromAxisAngle(Vector3.UnitY, 0);
                            var orientationChildElement = gObject.Element("orientation");
                            if (orientationChildElement != null) {
                                var orientationChildNormal = new Vector3(
                                    float.Parse((string)orientationChildElement.Element("x") ?? "0"),
                                    float.Parse((string)orientationChildElement.Element("y") ?? "1"),
                                    float.Parse((string)orientationChildElement.Element("z") ?? "0"));
                                orientationChild = Quaternion.FromAxisAngle(
                                    orientationChildNormal,
                                    float.Parse((string)orientationChildElement.Element("degrees") ?? "0"));
                            }
                            var constraint = new ConeTwist();
                            constraint.bias = float.Parse(bias ?? "0.3");
                            constraint.relaxation = float.Parse(relaxation ?? "1");
                            constraint.softness = float.Parse(softness ?? "1");
                            constraint.swingSpan1 = MathHelper.DegreesToRadians(float.Parse(xVal));
                            constraint.swingSpan2 = MathHelper.DegreesToRadians(float.Parse(yVal));
                            constraint.twist = MathHelper.DegreesToRadians(float.Parse(twist));
                            constraint.aFrame = Matrix4.CreateFromQuaternion(orientationParent) * Matrix4.CreateTranslation(positionParent);
                            constraint.bFrame = Matrix4.CreateFromQuaternion(orientationChild) * Matrix4.CreateTranslation(positionChild);

                            Matrix4 ScaleMatrix = Matrix4.CreateScale(scale);
                            Matrix4 TranslateMatrix = Matrix4.CreateTranslation(position);
                            Matrix4 RotateMatrix = Matrix4.CreateFromQuaternion(orientation);
                            Matrix4 MyTransform = parentTransform * ScaleMatrix * RotateMatrix * TranslateMatrix;
                            children.Add(parseXMLObject(world, childFile, MyTransform), constraint);
                            break;
                    }
                }
            }
            int matIndex = MaterialLoader.AddMaterial(mat);
            GameObject obj = new MovableObject(world, matIndex, lightList, children, mass, position, momentum, orientation, scale, angularMomentum, mesh, parentTransform);
            return obj;
        }

        //static private MovableObject parseDynamicObject(World world, string filename) {
        //    StreamReader reader = File.OpenText(AppConfig.Default.itempath + @"\base\" + filename);
        //    string line;
        //    Vector3 position = Vector3.Zero, scale = Vector3.One;
        //    Quaternion orientation = Quaternion.FromAxisAngle(Vector3.UnitY, 0);

        //    var children = new List<MovableObject>();
        //    Material mat = new Material();
        //    Mesh mesh = null;
        //    Lighting.PointLight perLight = new Lighting.PointLight();
        //    float mass = 0f;
        //    Vector3 momentum = Vector3.Zero;
        //    Vector3 angularMomentum = Vector3.Zero;
        //    while ((line = reader.ReadLine()) != null) {
        //        line = line.Trim();
        //        if (isSkipLine(line)) continue;
        //        string[] items = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
        //        switch (items[0]) {
        //            case "position":
        //                if (items.Length < 4)
        //                    throw new InvalidDataException("Not enough data to complete position for " + filename + ", data: " + line);
        //                position = new Vector3(float.Parse(items[1]), float.Parse(items[2]), float.Parse(items[3]));
        //                break;
        //            case "scale":
        //                if (items.Length < 4)
        //                    throw new InvalidDataException("Not enough data to complete scale for " + filename + ", data: " + line);
        //                scale = new Vector3(float.Parse(items[1]), float.Parse(items[2]), float.Parse(items[3]));
        //                break;
        //            case "orientation":
        //                if (items.Length < 5)
        //                    throw new InvalidDataException("Not enough data to complete orientation for " + filename + ", data: " + line);
        //                orientation = Quaternion.FromAxisAngle(new Vector3(float.Parse(items[1]), float.Parse(items[2]), float.Parse(items[3])), float.Parse(items[4]));
        //                break;
        //            case "mass":
        //                mass = float.Parse(items[1]);
        //                break;
        //            case "momentum":
        //                momentum = new Vector3(float.Parse(items[1]), float.Parse(items[2]), float.Parse(items[3]));
        //                break;
        //            case "angularMomentum":
        //                angularMomentum = new Vector3(float.Parse(items[1]), float.Parse(items[2]), float.Parse(items[3]));
        //                break;
        //            case "children":
        //                for (int i = 1; i < items.Length; ++i) {
        //                    children.Add(parseDynamicObject(world, items[i]));
        //                }
        //                break;
        //            case "mesh":
        //                mesh = parseMesh(items[1]);
        //                break;
        //            case "mtl":
        //                mat = parseMaterial(items[1], items[2]);
        //                break;
        //            case "light":
        //                perLight = parseLighting(items[1]);
        //                break;
        //            default:
        //                continue;
        //        }
        //    }
        //    int matIndex = MaterialLoader.AddMaterial(mat);
        //    MovableObject retVal = new MovableObject(world, matIndex, perLight, children, mass, position, momentum, orientation, scale, angularMomentum, mesh);
        //    foreach (MovableObject child in children) {
        //        child.setParent(retVal);
        //    }
        //    return retVal;
        //}

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
            filename = filename ?? "DefaultMaterial";
            materialName = materialName ?? "DefaultMaterial";
            String key = filename + materialName;
            if (!matMap.TryGetValue(key, out mat)) {
                StreamReader reader = File.OpenText(AppConfig.Default.itempath + @"\material\" + (filename ?? "DefaultMaterial"));
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
                var vindices = new List<uint>();
                var nindices = new List<uint>();
                var tindices = new List<uint>();
                String textureFile = null;
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
                        case "texture": textureFile = items[1];
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
                                vindices.Add((uint.Parse(index[0]) - 1));
                                if (index.Length > 1 && index[1] != "")
                                    tindices.Add((uint.Parse(index[1]) - 1));
                                else
                                    tindices.Add(vindices[vindices.Count - 1]);
                                if (index.Length > 2 && index[2] != "")
                                    nindices.Add((uint.Parse(index[2]) - 1));
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
                if (texCoords.Count > 0) {
                    foreach (int u in tindices) {
                        indexTexCoords.Add(texCoords[u]);
                    }
                }
                foreach (int u in nindices) {
                    indexNormals.Add(normals[u]);
                }
                mesh = new Mesh(indexVertices.ToArray(), indexTexCoords.ToArray(), indexNormals.ToArray(), type, textureFile);
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
