using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ascendant.Graphics;
using OpenTK;
using Ascendant.Graphics.objects;
using OpenTK.Graphics.OpenGL4;
using Ascendant.Graphics.lighting;

namespace Ascendant.Physics {
    class MovableObject {
        internal State display ;
        internal State current ;
        private State previous;

        internal Lighting.PointLight pointLight = new Lighting.PointLight();
        
        int matNumber;
        uint vertexBufferObject;
        uint vertexNormalObject;
        uint vertexArrayObject;

        World world;
        List<MovableObject> children;
        MovableObject hierarchichalParent;

        internal MovableObject(World world, int matNumber, Lighting.PointLight light, List<MovableObject> children, 
            float size, float mass, Vector3 position, Vector3 momentum, Quaternion orientation, Vector3 scale, 
            Vector3 angularMomentum, Mesh mesh) {
            this.world = world;
            this.children = children;
            this.matNumber = matNumber;
            this.pointLight = light;
            display = previous = current = new State(size, mass, position, momentum, orientation, scale, angularMomentum, mesh, this);
        }


        internal void setParent(MovableObject retVal) {
            this.hierarchichalParent = retVal;
        }

        internal Matrix4 getParentMatrix() {
            if (hierarchichalParent != null) {
                return hierarchichalParent.current.getModelToWorldMatrix();
            } else {
                return Matrix4.Identity;
            }
        }

        /// Update physics state.
        internal void update(float t, float dt) {
            previous = current;
            Derivative.integrate(ref current, t, dt);
        }

        internal void lerp(float blend) {
            display = State.Lerp(previous, current, blend);
        }

        internal void InitializeOpenGL(int program) {
            GL.UseProgram(program);
            LoadBuffers();
            LoadVertexArray();
            GL.UseProgram(0);
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
            GL.BufferData(BufferTarget.ArrayBuffer, new IntPtr((display.mesh.vertices.Length * Vector3.SizeInBytes)), display.mesh.vertices, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

            GL.GenBuffers(1, out vertexNormalObject);

            GL.BindBuffer(BufferTarget.ArrayBuffer, vertexNormalObject);
            GL.BufferData(BufferTarget.ArrayBuffer, new IntPtr((display.mesh.normals.Length * Vector3.SizeInBytes)), display.mesh.normals, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }

        internal void Render() {
            GL.BindVertexArray(vertexArrayObject);

            //Translate
            Matrix4 ModelToWorldMatrix = display.getModelToWorldMatrix();

            Matrix4 ModelToCameraMatrix = ModelToWorldMatrix * world.parentGame.Camera.GetWorldToCameraMatrix();
            GL.UniformMatrix4(world.modelToCameraMatrixUnif, false, ref ModelToCameraMatrix);
            Matrix3 NormalModelToCameraMatrix = new Matrix3(ModelToCameraMatrix);
            if (NormalModelToCameraMatrix.Determinant != 0) {
                NormalModelToCameraMatrix.Invert();
            }
            GL.UniformMatrix3(world.normalModelToCameraMatrixUnif, true, ref NormalModelToCameraMatrix);

            GL.BindBufferRange(BufferRangeTarget.UniformBuffer,
                Window.g_materialBlockIndex,
                MaterialLoader.g_materialUniformBuffer,
               new IntPtr((MaterialLoader.m_sizeMaterialBlock * matNumber)),
               new IntPtr((MaterialLoader.m_sizeMaterialBlock)));

            //Draw this
            GL.DrawArrays(display.mesh.type, 0, display.mesh.vertices.Length);

            GL.BindBufferBase(BufferRangeTarget.UniformBuffer, Window.g_materialBlockIndex, 0);

            GL.BindVertexArray(0);

            //Draw children
            foreach (MovableObject child in children) {
                child.Render();
            }

        }
    }
}
