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
        Mesh mesh;
        int matNumber;
        Vector3 scale;

        Physics.PhysicsObject physics;

        internal Vector3 lightPosition { get; private set; }
        internal Vector4 lightIntensity { get; private set; }

        uint vertexBufferObject;
        uint indexBufferObject;
        uint vertexNormalObject;

        uint vertexArrayObject;

        Game parent;
        List<DisplayObject> children;
        public DisplayObject(Game par, Vector3 scale, Physics.PhysicsObject physObj, Mesh mesh, int matNumber, PerLight light, List<DisplayObject> children) {
            parent = par;
            this.children = children;
            this.mesh = mesh;
            this.matNumber = matNumber;
            this.scale = scale;
            this.physics = physObj;
            this.lightIntensity = light.lightIntensity;
            this.lightPosition = light.cameraSpaceLightPos.Xyz;
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
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 0, 0);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBufferObject);

            GL.BindVertexArray(0);

        }

        internal void LoadBuffers() {
            GL.GenBuffers(1, out vertexBufferObject);

            GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferObject);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(mesh.vertices.Length * Vector3.SizeInBytes), mesh.vertices, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

            GL.GenBuffers(1, out vertexNormalObject);

            GL.BindBuffer(BufferTarget.ArrayBuffer, vertexNormalObject);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(mesh.normals.Length * Vector3.SizeInBytes), mesh.normals, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

            GL.GenBuffers(1, out indexBufferObject);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBufferObject);
            GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(mesh.indices.Length * sizeof(ushort)), mesh.indices, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
        }

        internal void Render(ref Matrix4 parentMatrix, int modelToCameraMatrixUnif, int normalModelToCameraMatrixUnif) {
            GL.BindVertexArray(vertexArrayObject);

            //Translate
            Matrix4 ModelToWorldMatrix = parentMatrix * Matrix4.CreateTranslation(physics.current.position);
            //Rotate
            ModelToWorldMatrix = ModelToWorldMatrix * Matrix4.CreateFromQuaternion(physics.current.orientation);
            //Scale
            ModelToWorldMatrix = ModelToWorldMatrix * Matrix4.CreateScale(scale);

            Matrix4 ModelToCameraMatrix = ModelToWorldMatrix * parent.Camera.GetWorldToCameraMatrix();
            GL.UniformMatrix4(modelToCameraMatrixUnif, false, ref ModelToCameraMatrix);
            Matrix3 NormalModelToCameraMatrix = new Matrix3(ModelToCameraMatrix.Inverted());
            GL.UniformMatrix3(normalModelToCameraMatrixUnif, true, ref NormalModelToCameraMatrix);

            GL.BindBufferRange(BufferRangeTarget.UniformBuffer, MyLoader.MaterialLoader.g_materialBlockIndex, MyLoader.MaterialLoader.g_materialUniformBuffer, (IntPtr)(MyLoader.MaterialLoader.m_sizeMaterialBlock * matNumber), (IntPtr)(MyLoader.MaterialLoader.m_sizeMaterialBlock));

            //Draw this
            GL.DrawElements(mesh.mode, mesh.indices.Length, DrawElementsType.UnsignedShort, 0);

            GL.BindBufferBase(BufferRangeTarget.UniformBuffer, MyLoader.MaterialLoader.g_materialBlockIndex, 0);
            //Draw children
            foreach (DisplayObject child in children) {
                child.Render(ref ModelToCameraMatrix, modelToCameraMatrixUnif, normalModelToCameraMatrixUnif);
            }

            GL.BindVertexArray(0);
        }
    }
}
