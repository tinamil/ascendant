using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using System.Diagnostics;
using OpenTK.Graphics.OpenGL4;
using System.Runtime.InteropServices;
using Ascendant.Physics;

namespace Ascendant.Graphics.lighting {

    class Lighting {
        uint g_lightUniformBuffer;
        uint g_pointLightUniformBuffer;
        readonly protected List<GameObject> worldObjects;

        const float halfLightDistance = 70.0f;
        const float gamma = 2.2f;
        const float attenuation = 1 / (halfLightDistance * halfLightDistance);
        const float maxIntensity = 2f;

        private Vector4 ambient;
        internal Vector4 background { get; private set; }

        public Lighting(List<GameObject> objects) {
            worldObjects = objects;
        }

        public void SetGlobalLighting(Vector4 _ambient, Vector4 _background) {
            this.ambient = _ambient;
            this.background = _background;
        }

        public void InitializeOpenGL(int program) {
            int lightBlock = GL.GetUniformBlockIndex(program, "Light");
            GL.UniformBlockBinding(program, lightBlock, Window.g_lightBlockIndex);

            int pointLightBlock = GL.GetUniformBlockIndex(program, "PerLightBlock");
            GL.UniformBlockBinding(program, pointLightBlock, Window.g_pointLightBlockIndex);

            GL.GenBuffers(1, out g_lightUniformBuffer);
            GL.BindBuffer(BufferTarget.UniformBuffer, g_lightUniformBuffer);
            LightBlockGamma data = GetLightInformation();
            GL.BufferData(BufferTarget.UniformBuffer, new IntPtr(Marshal.SizeOf(typeof(LightBlockGamma))), ref data, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.UniformBuffer, 0);

            GL.BindBufferBase(BufferRangeTarget.UniformBuffer, Window.g_lightBlockIndex, g_lightUniformBuffer);

            GL.GenBuffers(1, out g_pointLightUniformBuffer);
            GL.BindBuffer(BufferTarget.UniformBuffer, g_pointLightUniformBuffer);
            GL.BufferData(BufferTarget.UniformBuffer, new IntPtr(PointLight.sizeBytes), IntPtr.Zero, BufferUsageHint.DynamicDraw);
            GL.BindBuffer(BufferTarget.UniformBuffer, 0);

            GL.BindBufferBase(BufferRangeTarget.UniformBuffer, Window.g_pointLightBlockIndex, g_pointLightUniformBuffer);
        }

        internal struct PointLight {
            public const int maxLights = 16;
            public const int sizeBytes = sizeof(float) * 4 * 2 * maxLights;
            internal Vector4 cameraSpaceLightPos;
            internal Vector4 lightIntensity;

            internal PointLight(Vector4 pos, Vector4 intensity) {
                this.cameraSpaceLightPos = pos;
                this.lightIntensity = intensity;
            }
        }

        internal struct LightBlockGamma {
            public const int sizeBytes = sizeof(float) * 4 * 2;
            internal Vector4 ambientIntensity;
            internal Vector4 attenuationMaxGamma;
        }

        internal PointLight[] GetPointLights(Matrix4 worldToCameraMat) {
            PointLight[] lights = new PointLight[PointLight.maxLights];

            int light = 0;
            int objectIndex = 0;
            //Load all the lights from the world up to the maximum (16 by default)
            while (light < PointLight.maxLights && objectIndex < worldObjects.Count) {
                GameObject possibleLight = worldObjects[objectIndex++];
                if (possibleLight.pointLight.lightIntensity != Vector4.Zero) {
                    Vector4 worldLightPos = new Vector4(possibleLight.getPosition() + possibleLight.pointLight.cameraSpaceLightPos.Xyz, 1.0f);
                    Vector4 lightPosCameraSpace = Vector4.Transform(worldLightPos, worldToCameraMat);

                    lights[light].cameraSpaceLightPos = lightPosCameraSpace;
                    lights[light].lightIntensity = possibleLight.pointLight.lightIntensity;
                    light += 1;
                }
            }
            //Fill the rest of the light array with Zero vectors to prevent garbage memory from being interpreted as lights
            while (light < PointLight.maxLights) {
                lights[light].cameraSpaceLightPos = Vector4.Zero;
                lights[light].lightIntensity = Vector4.Zero;
                light += 1;
            }
            return lights;
        }

        internal LightBlockGamma GetLightInformation() {
            LightBlockGamma lightData = new LightBlockGamma();
            lightData.ambientIntensity = ambient;
            lightData.attenuationMaxGamma.X = attenuation;
            lightData.attenuationMaxGamma.Y = maxIntensity;
            lightData.attenuationMaxGamma.Z = 1/gamma;
            return lightData;
        }

        static public Vector4 GammaCorrect(Vector4 input, float gamma = 2.2f) {
            return new Vector4((float)Math.Pow(input[0], 1.0f / gamma), (float)Math.Pow(input[1], 1.0f / gamma), (float)Math.Pow(input[2], 1.0f / gamma), input[3]);
        }

        internal void Render(Matrix4 worldToCamMat) {
            PointLight[] lightData = GetPointLights(worldToCamMat);
            unsafe {
                GCHandle pinnedArray = GCHandle.Alloc(lightData, GCHandleType.Pinned);
                GL.BindBuffer(BufferTarget.UniformBuffer, g_pointLightUniformBuffer);
                GL.BufferSubData(BufferTarget.UniformBuffer, IntPtr.Zero, new IntPtr(PointLight.sizeBytes), pinnedArray.AddrOfPinnedObject());
                GL.BindBuffer(BufferTarget.UniformBuffer, 0);
                pinnedArray.Free();
            }
        }
    }
}
