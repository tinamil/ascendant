using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using Ascendant.Graphics.lighting;
using Ascendant.Graphics.objects;
using Ascendant.Graphics;

namespace Ascendant.Physics {
   public abstract class DisplayObject {

        internal List<Lighting.PointLight> pointLights = new List<Lighting.PointLight>();

        int matNumber;
        uint vertexBufferObject;
        uint vertexNormalObject;
        uint vertexTextureObject;
        uint vertexArrayObject;

        bool hasTexture { get { return mesh.texCoords.Length > 0; } }
        uint g_sampler;
        uint g_linearTexture;
        TextureTarget textureType;

        String textureFile;
        internal Mesh mesh;

        public abstract Matrix4 ModelToWorld { get; }

        public DisplayObject(int matNumber, List<Lighting.PointLight> light, Mesh mesh) {
            this.matNumber = matNumber;
            this.pointLights = light;
            this.textureFile = mesh.textureFile;
            this.mesh = mesh;
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

        internal void Render(int modelToCameraMatrixUnif, int normalModelToCameraMatrixUnif, Matrix4 WorldToCameraMatrix) {
            GL.BindVertexArray(vertexArrayObject);

            Matrix4 ModelToCameraMatrix = ModelToWorld * WorldToCameraMatrix;
            GL.UniformMatrix4(modelToCameraMatrixUnif, false, ref ModelToCameraMatrix);
            Matrix3 NormalModelToCameraMatrix = new Matrix3(ModelToCameraMatrix);
            if (NormalModelToCameraMatrix.Determinant != 0) {
                NormalModelToCameraMatrix.Invert();
            }

            GL.UniformMatrix3(normalModelToCameraMatrixUnif, false, ref NormalModelToCameraMatrix);

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
        }
    }
}
