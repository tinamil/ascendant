using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.IO;

namespace Ascendant.Graphics {
    struct Mesh {
        public BeginMode mode { get; private set; }
        public Vector3[] vertices { get; private set; }
        public Vector4[] colors { get; private set; }
        public Vector2[] texCoords { get; private set; }
        public Vector3[] normals { get; private set; }
        public ushort[] indices { get; private set; }

        public Mesh(Vector3[] _v, Vector4[] _c, Vector2[] _t, Vector3[] _n, ushort[] _i, BeginMode _m)
            : this() {
            mode = _m;
            colors = _c;
            vertices = _v;
            texCoords = _t;
            normals = _n;
            indices = _i;
        }
    }
    class MyParser {
        internal protected static DisplayObject parse(Game game, string filename) {
            StreamReader reader = File.OpenText(AppConfig.Default.meshpath + filename);
            string line;
            Vector3 position = Vector3.Zero, scale = Vector3.One;
            Quaternion orientation = Quaternion.FromAxisAngle(Vector3.UnitY, 0);
            var vertices = new List<Vector3>();
            var texCoords = new List<Vector2>();
            var normals = new List<Vector3>();
            var indices = new List<ushort>();
            var colors = new List<Vector4>();
            var children = new List<DisplayObject>();
            BeginMode type = BeginMode.Triangles;
            while ((line = reader.ReadLine()) != null) {
                line = line.Trim();
                if (line.StartsWith("#")) continue;
                if (line.Equals("")) continue;
                string[] items = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                switch (items[0]) {
                    case "v":
                        if (items.Length < 4)
                            throw new InvalidDataException("Not enough data to complete vertex for " + filename + ", data: " + line);
                        vertices.Add(new Vector3(float.Parse(items[1]), float.Parse(items[2]), float.Parse(items[3])));
                        if(items.Length == 8)
                          colors.Add(new Vector4(float.Parse(items[4]), float.Parse(items[5]), float.Parse(items[6]), float.Parse(items[7])));
                        else
                          colors.Add(new Vector4(.5f, .5f, .5f, 1f));
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
                            children.Add(parse(game, items[i]));
                        }
                        break;
                    default:
                        continue;
                }
            }
            Mesh mesh = new Mesh(vertices.ToArray(), colors.ToArray(), texCoords.ToArray(), normals.ToArray(), indices.ToArray<ushort>(), type);
            DisplayObject retVal = new DisplayObject(game, position, scale, orientation, mesh, children);
            return retVal;
        }
    }
}
