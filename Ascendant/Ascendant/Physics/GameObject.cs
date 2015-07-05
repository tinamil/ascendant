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
   abstract class GameObject {
        internal Lighting.PointLight pointLight = new Lighting.PointLight();

        int matNumber;
        uint vertexBufferObject;
        uint vertexNormalObject;
        uint vertexTextureObject;
        uint vertexArrayObject;

        bool hasTexture { get { return mesh.texCoords.Length > 0; } }
        uint g_sampler; 
        uint g_linearTexture;
        TextureTarget textureType;

        World world;
        IEnumerable<GameObject> children;
        GameObject hierarchichalParent;

        String textureFile;
        internal Matrix4 cachedTransform;
        internal MovableObject parent;
        internal Mesh mesh;
        public abstract BulletSharp.RigidBody body { get; }
        protected abstract Vector3 scale { get; }
        

        internal GameObject(World world, int matNumber, Lighting.PointLight light, Mesh mesh, IEnumerable<GameObject> children) {

            this.world = world;
            this.matNumber = matNumber;
            this.pointLight = light;
            this.textureFile = mesh.textureFile;
            this.children = children;
            this.mesh = mesh;
        }

        public Matrix4 getModelToWorldMatrix() {
            //Matrix4 Translate = Matrix4.CreateTranslation(position);
            ////Rotate
            //Matrix4 Rotate = Matrix4.CreateFromQuaternion(orientation);
            ////Scale
            //Matrix4 Scale = Matrix4.CreateScale(scale);
            //// return Translate * Rotate * Scale * parentMatrix;
            //cachedTransform = getParentMatrix() * Scale * Rotate * Translate;
            //return cachedTransform;
            return Matrix4.CreateScale(scale) * body.MotionState.WorldTransform;
        }

        internal Vector3 getPosition() {
            return body.MotionState.WorldTransform.ExtractTranslation();
        }

        internal void setParent(GameObject retVal) {
            this.hierarchichalParent = retVal;
        }

        internal Matrix4 getParentMatrix() {
            if (hierarchichalParent != null) {
                return hierarchichalParent.getModelToWorldMatrix();
            } else {
                return Matrix4.Identity;
            }
        }

        internal void InitializeOpenGL(int program) {
            GL.UseProgram(program);
            LoadBuffers();
            LoadVertexArray();

            if (hasTexture) {
                int colorTextureUnif = GL.GetUniformLocation(program, "diffuseColorTex");
                GL.Uniform1(colorTextureUnif, Window.g_colorTexUnit);

                LoadTextures();
                CreateSamplers();
            }

            GL.UseProgram(0);
        }

        void LoadTextures() {
            Examples.TextureLoaders.ImageDDS.LoadFromDisk(@"Graphics\objects\textures\" + textureFile, out g_linearTexture, out textureType);
        }

        void CreateSamplers() {
            GL.GenSamplers(1, out g_sampler);
            int repeat = Convert.ToInt32(All.Repeat);
            GL.SamplerParameterI(g_sampler, SamplerParameterName.TextureWrapS, ref repeat);
            GL.SamplerParameterI(g_sampler, SamplerParameterName.TextureWrapT, ref repeat);

            int linear = Convert.ToInt32(All.Linear);
            int linearMipmapLinear = Convert.ToInt32(All.Linear);

            //Linear mipmap linear
            float maxAniso;
            GL.SamplerParameterI(g_sampler, SamplerParameterName.TextureMagFilter, ref linear);
            GL.SamplerParameterI(g_sampler, SamplerParameterName.TextureMinFilter, ref linearMipmapLinear);
            GL.GetFloat((GetPName)OpenTK.Graphics.OpenGL.ExtTextureFilterAnisotropic.MaxTextureMaxAnisotropyExt, out maxAniso);
            GL.SamplerParameter(g_sampler, SamplerParameterName.TextureMaxAnisotropyExt, maxAniso);
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

            if (hasTexture) {
                GL.BindBuffer(BufferTarget.ArrayBuffer, vertexTextureObject);
                GL.EnableVertexAttribArray(2);
                GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, 0, 0);
            }

            GL.BindVertexArray(0);
        }

        internal void LoadBuffers() {
            GL.GenBuffers(1, out vertexBufferObject);

            GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferObject);
            GL.BufferData(BufferTarget.ArrayBuffer, new IntPtr((mesh.vertices.Length * Vector3.SizeInBytes)), mesh.vertices, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

            GL.GenBuffers(1, out vertexNormalObject);

            GL.BindBuffer(BufferTarget.ArrayBuffer, vertexNormalObject);
            GL.BufferData(BufferTarget.ArrayBuffer, new IntPtr((mesh.normals.Length * Vector3.SizeInBytes)), mesh.normals, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

            GL.GenBuffers(1, out vertexTextureObject);

            GL.BindBuffer(BufferTarget.ArrayBuffer, vertexTextureObject);
            GL.BufferData(BufferTarget.ArrayBuffer, new IntPtr((mesh.texCoords.Length * Vector2.SizeInBytes)), mesh.texCoords, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

        }

        internal void Render() {
            GL.BindVertexArray(vertexArrayObject);

            //Translate
            Matrix4 ModelToWorldMatrix = getModelToWorldMatrix();

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


            if (hasTexture) {
                GL.ActiveTexture(TextureUnit.Texture0 + Window.g_colorTexUnit);
                GL.BindTexture(textureType, g_linearTexture);
                GL.BindSampler(Window.g_colorTexUnit, g_sampler);
            }

            //Draw this
            GL.DrawArrays(mesh.type, 0, mesh.vertices.Length);

            if (hasTexture) {
                GL.BindSampler(Window.g_colorTexUnit, 0);
                GL.BindTexture(textureType, 0);
            }

            GL.BindBufferBase(BufferRangeTarget.UniformBuffer, Window.g_materialBlockIndex, 0);

            GL.BindVertexArray(0);

            //Draw children
            foreach (MovableObject child in children) {
                child.Render();
            }

        }
    }
}
