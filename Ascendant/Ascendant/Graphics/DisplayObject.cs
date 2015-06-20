using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Drawing;
using OpenTK.Graphics;
namespace Ascendant.Graphics {
    class DisplayObject : Obj{
        Vector3 position;
        Vector3 scale;
        Quaternion orientation;

        uint vertexBufferObject;
        uint colorBufferObject;
        uint indexBufferObject;

        uint vertexArrayObject;

        Game parent;
        List<DisplayObject> children;
        public DisplayObject(Game par, Vector3 position, Vector3 scale, Quaternion orientation) {
            parent = par;
            children = new List<DisplayObject>();
            this.position = position;
            this.scale = scale;
            this.orientation = orientation;
            LoadBuffers();
            LoadVertexArray();
        }

        private void LoadVertexArray() {
            GL.GenVertexArrays(1, out vertexArrayObject);
            GL.BindVertexArray(vertexArrayObject);

            GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferObject);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, 0);

            GL.BindBuffer(BufferTarget.ArrayBuffer, colorBufferObject);
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 0, 0);

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

            GL.BindVertexArray(0);

        }

        internal void LoadBuffers() {
            GL.GenBuffers(1, out vertexBufferObject);

            GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferObject);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(vertices.Length * sizeof(float)), vertices, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

            GL.GenBuffers(1, out indexBufferObject);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBufferObject);
            GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(indices.Length * sizeof(ushort)), indices, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

            GL.GenBuffers(1, out colorBufferObject);

            GL.BindBuffer(BufferTarget.ArrayBuffer, colorBufferObject);
            GL.BufferData<Vector4>(BufferTarget.ArrayBuffer, (IntPtr)(colors.Length * Vector4.SizeInBytes), colors, BufferUsageHint.StaticDraw);
            
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }

        internal void Render(ref Matrix4 parentMatrix, int modelToCameraMatrixUnif) {
            GL.BindVertexArray(vertexArrayObject);

            //Translate
            Matrix4 ModelToWorldMatrix = parentMatrix * Matrix4.CreateTranslation(position);
            //Rotate
            ModelToWorldMatrix = ModelToWorldMatrix * Matrix4.CreateFromQuaternion(orientation);
            //Scale
            ModelToWorldMatrix = ModelToWorldMatrix * Matrix4.CreateScale(scale);
            
            Matrix4 ModelToCameraMatrix = ModelToWorldMatrix * parent.Camera.GetWorldToCameraMatrix();
            GL.UniformMatrix4(modelToCameraMatrixUnif, false, ref ModelToCameraMatrix);

            //Draw this
            GL.DrawElements(PrimitiveType.Triangles, indices.Length, DrawElementsType.UnsignedShort, indices);

            //Draw children
            foreach (DisplayObject child in children) {
                child.Render(ref ModelToCameraMatrix, modelToCameraMatrixUnif);
            }

            GL.BindVertexArray(0);
        }
    }
}
