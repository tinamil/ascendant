using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using System.Drawing;
using OpenTK.Graphics;
namespace Ascendant.Graphics {
    class DisplayObject {
        int matNumber;

        internal Physics.PhysicsObject physics { get; private set; }

        internal Vector4 lightIntensity { get; private set; }

        uint vertexBufferObject;
        uint vertexNormalObject;

        uint vertexArrayObject;

        Game parent;
        List<DisplayObject> children;
        public DisplayObject(Game par, Physics.PhysicsObject physObj, int matNumber, PerLight light, List<DisplayObject> children) {
            parent = par;
            this.children = children;
            this.matNumber = matNumber;
            this.physics = physObj;
            this.lightIntensity = light.lightIntensity;
            LoadBuffers();
            LoadVertexArray();
        }

        private void LoadVertexArray() {
            GL.GenVertexArrays(1, out vertexArrayObject);
            GL.BindVertexArray(vertexArrayObject);

            GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferObject);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, 0);

            GL.BindBuffer(BufferTarget.ArrayBuffer, vertexNormalObject);
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, true, 0, 0);

            GL.BindVertexArray(0);

        }

        internal void LoadBuffers() {
            GL.GenBuffers(1, out vertexBufferObject);

            GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferObject);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(physics.display.mesh.vertices.Length * Vector3.SizeInBytes), physics.display.mesh.vertices, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

            GL.GenBuffers(1, out vertexNormalObject);

            GL.BindBuffer(BufferTarget.ArrayBuffer, vertexNormalObject);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(physics.display.mesh.normals.Length * Vector3.SizeInBytes), physics.display.mesh.normals, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }

        internal void Render(ref Matrix4 parentMatrix, int modelToCameraMatrixUnif, int normalModelToCameraMatrixUnif) {
            GL.BindVertexArray(vertexArrayObject);

            //Translate
            Matrix4 ModelToWorldMatrix = parentMatrix * Matrix4.CreateTranslation(physics.display.position);
            //Rotate
            ModelToWorldMatrix = ModelToWorldMatrix * Matrix4.CreateFromQuaternion(physics.display.orientation);
            //Scale
            ModelToWorldMatrix = ModelToWorldMatrix * Matrix4.CreateScale(physics.display.scale);

            Matrix4 ModelToCameraMatrix = ModelToWorldMatrix * parent.Camera.GetWorldToCameraMatrix();
            GL.UniformMatrix4(modelToCameraMatrixUnif, false, ref ModelToCameraMatrix);
            Matrix3 NormalModelToCameraMatrix = new Matrix3(ModelToCameraMatrix);
            if(NormalModelToCameraMatrix.Determinant != 0){
                NormalModelToCameraMatrix.Invert();
                NormalModelToCameraMatrix.Transpose();
            } 
            GL.UniformMatrix3(normalModelToCameraMatrixUnif, false, ref NormalModelToCameraMatrix);

            GL.BindBufferRange(BufferRangeTarget.UniformBuffer, Window.g_materialBlockIndex, MyLoader.MaterialLoader.g_materialUniformBuffer, (IntPtr)(MyLoader.MaterialLoader.m_sizeMaterialBlock * matNumber), (IntPtr)(MyLoader.MaterialLoader.m_sizeMaterialBlock));

            //Draw this
            GL.DrawArrays(PrimitiveType.Triangles, 0, physics.display.mesh.vertices.Length);

            GL.BindBufferBase(BufferRangeTarget.UniformBuffer, Window.g_materialBlockIndex, 0);
            //Draw children
            foreach (DisplayObject child in children) {
                child.Render(ref ModelToWorldMatrix, modelToCameraMatrixUnif, normalModelToCameraMatrixUnif);
            }

            GL.BindVertexArray(0);
        }
    }
}
