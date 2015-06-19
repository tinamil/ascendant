using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using ObjLoader.Loader.Loaders;
using System.Runtime.InteropServices;
using System.IO;

namespace Ascendant.Graphics {
  [StructLayout(LayoutKind.Sequential)]
  struct Vertex {
    public Vector2 TexCoord;
    public Vector3 Normal;
    public Vector3 Position;
  }
  class VisibleObject {
    Vertex[] Vertices;
    uint VaoHandle;
    static ObjLoaderFactory objLoaderFactory = new ObjLoaderFactory();

    public VisibleObject(String input) {
      var objLoader = objLoaderFactory.Create();
      var fileStream = new FileStream(input, FileMode.Open, FileAccess.Read);
      var result = objLoader.Load(fileStream);
      loadVertices(result);
    }



    void loadVertices(LoadResult result) {
      Vertices = new Vertex[result.Vertices.Count];
      for(int i = 0; i < result.Vertices.Count; ++i) {
        Vertices[i] = new Vertex();
        Vertices[i].Position = new Vector3(result.Vertices[i].X, result.Vertices[i].Y, result.Vertices[i].Z);
      }
      uint[] VBOid = new uint[1];
      GL.GenVertexArrays(1, out VaoHandle);
      GL.GenBuffers(1, VBOid);

      GL.BindVertexArray(VaoHandle);
      GL.BindBuffer(BufferTarget.ArrayBuffer, VBOid[0]);
      //GL.BindBuffer(BufferTarget.ElementArrayBuffer, VBOid[1]);

      GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(Vertices.Length * 8 * +sizeof(float)), Vertices, BufferUsageHint.StaticDraw);
      //GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(Indices.Length * sizeof(ushort)), Indices, BufferUsageHint.StaticDraw);
      GL.InterleavedArrays(InterleavedArrayFormat.T2fN3fV3f, 0, IntPtr.Zero);

      //GL.EnableClientState(...);
      //GL.VertexPointer(...);
      //GL.EnableVertexAttribArray(...);

      GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
      //GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
      GL.BindVertexArray(0);
      //GL.DeleteBuffers(2, VBOid);

      GL.Enable(EnableCap.CullFace);
			GL.CullFace(CullFaceMode.Back);
			GL.FrontFace(FrontFaceDirection.Cw);

			GL.Enable(EnableCap.DepthTest);
			GL.DepthMask(true);
			GL.DepthFunc(DepthFunction.Less);
			GL.DepthRange(0.0f, 1.0f);
    }

    internal void Draw() {
      GL.BindVertexArray(VaoHandle);
      GL.DrawArrays(BeginMode.Triangles, 0, Vertices.Length);
      GL.BindVertexArray(0);
    }
  }
}
