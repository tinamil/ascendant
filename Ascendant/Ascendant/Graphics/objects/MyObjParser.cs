using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using System.IO;

namespace Ascendant.Graphics {
  struct Obj {
    public BeginMode mode { get; protected set; }
    public Vector3[] vertices { get; protected set; }
    public Vector2[] texCoords { get; protected set; }
    public Vector3[] normals { get; protected set; }
    public ushort[] indices { get; protected set; }

    public Obj(Vector3[] _v, Vector2[] _t, Vector3[] _n, ushort[] _i, BeginMode _m) {
      mode = _m;
      vertices = _v;
      texCoords = _t;
      normals = _n;
      indices = _i;
    }
  }
  class MyParser {
    public static Obj parseObj(String filename) {
      StreamReader reader = File.OpenText(filename);
      string line;
      
      var vertices = new List<Vector3>();
      var texCoords = new List<Vector2>();
      var normals = new List<Vector3>();
      var indices = new List<ushort>();

      while((line = reader.ReadLine().Trim()) != null) {
        if(line.StartsWith("#")) continue;
        if(line.Equals("")) continue;
        string[] items = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
        switch(items[0]) {
          case "v":
            if(items.Length < 4)
              throw new InvalidDataException("Not enough data to complete vertex for " + filename + ", data: " + line);
            vertices.Add(new Vector3(float.Parse(items[1]), float.Parse(items[2]), float.Parse(items[3])));
            break;
          case "vt":
            if(items.Length < 3)
              throw new InvalidDataException("Not enough data to complete texture for " + filename + ", data: " + line);
            texCoords.Add(new Vector2(float.Parse(items[1]), float.Parse(items[2])));
            break;
          case "vn":
            if(items.Length < 4)
              throw new InvalidDataException("Not enough data to complete normal for " + filename + ", data: " + line);
            normals.Add(new Vector3(float.Parse(items[1]), float.Parse(items[2]), float.Parse(items[3])));
            break;
          case "f":
            if(items.Length != 4)
              throw new InvalidDataException("Face data must be specified in triangles with exactly 3 vertices in CW rotation, file: " + filename + ", data: " + line);
            for(int i = 1; i < items.Length; ++i) {
              indices.Add(ushort.Parse(items[i].Split('/')[0]));
            }
            break;
          default:
            continue;
        }
      }
      return new Obj(vertices.ToArray(), texCoords.ToArray(), normals.ToArray(), indices.ToArray<ushort>(), BeginMode.Triangles);
    }
  }
}
