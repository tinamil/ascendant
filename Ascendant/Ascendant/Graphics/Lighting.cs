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

    struct Sun : ILerpable {
        public Vector4 ambient;
        public Vector4 background;
        public Vector4 intensity;
        public float maxIntensity;

        const float cycleTime = 30;

        static public float Alpha(long millis) { return ((millis / 1000f) % cycleTime) / cycleTime; }

        static public Vector4 GetSunlightDirection(long millis) {
            float angle = 2.0f * 3.14159f * Alpha(millis);
            Vector4 sunDirection = new Vector4((float)Math.Sin(angle), (float)Math.Cos(angle), 0, 0);

            //Keep the sun from being perfectly centered overhead.
            sunDirection = Vector4.Transform(sunDirection, Matrix4.CreateRotationX(5));

            return sunDirection;
        }

        ILerpable ILerpable.multiply(float val) {
            Sun retVal = new Sun();
            retVal.ambient = ambient * val;
            retVal.background = background * val;
            retVal.intensity = intensity * val;
            retVal.maxIntensity = maxIntensity * val;
            return retVal;
        }

        ILerpable ILerpable.add(ILerpable other) {
            var rightVal = other as Sun?;
            if (rightVal.HasValue) {
                Sun retVal = new Sun();
                retVal.ambient = ambient + rightVal.Value.ambient;
                retVal.background = background + rightVal.Value.background;
                retVal.intensity = intensity + rightVal.Value.intensity;
                retVal.maxIntensity = maxIntensity + rightVal.Value.maxIntensity;
                return retVal;
            } return null;
        }
    }

    class Lighting {
        uint g_lightUniformBuffer;
        uint g_pointLightUniformBuffer;
        readonly protected List<GameObject> worldObjects;

        const float halfLightDistance = 70.0f;
        const float gamma = 2.2f;
        const float attenuation = 1 / (halfLightDistance * halfLightDistance);

        public TimedLinearInterpolator<Sun> sunTimer { get; private set; }

        public Lighting(List<GameObject> objects) {
            worldObjects = objects;
        }

        internal void setSun(TimedLinearInterpolator<Sun> _sunTimer) {
            this.sunTimer = _sunTimer;
        }

        public void InitializeOpenGL(int program) {
            int lightBlock = GL.GetUniformBlockIndex(program, "Light");
            GL.UniformBlockBinding(program, lightBlock, Window.g_lightBlockIndex);

            int pointLightBlock = GL.GetUniformBlockIndex(program, "PerLightBlock");
            GL.UniformBlockBinding(program, pointLightBlock, Window.g_pointLightBlockIndex);

            GL.GenBuffers(1, out g_lightUniformBuffer);
            GL.BindBuffer(BufferTarget.UniformBuffer, g_lightUniformBuffer);
            GL.BufferData(BufferTarget.UniformBuffer, new IntPtr(Marshal.SizeOf(typeof(LightBlockGamma))), IntPtr.Zero, BufferUsageHint.DynamicDraw);
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

        private IEnumerable<PointLight> loadObjectPointLights(GameObject obj, Matrix4 worldToCameraMat) {
            var lights = new List<PointLight>();
            foreach (Lighting.PointLight pointLight in obj.pointLights) {
                if (pointLight.lightIntensity != Vector4.Zero) {
                    Vector4 worldLightPos = new Vector4(obj.getPosition() + pointLight.cameraSpaceLightPos.Xyz, 1.0f);
                    Vector4 lightPosCameraSpace = Vector4.Transform(worldLightPos, worldToCameraMat);

                    PointLight light = new PointLight();
                    light.cameraSpaceLightPos = lightPosCameraSpace;
                    light.lightIntensity = pointLight.lightIntensity;
                    lights.Add(light);
                }
            }
            return lights;
        }

        internal PointLight[] GetPointLights(Matrix4 worldToCameraMat, long currentMillis) {
            PointLight[] lights = new PointLight[PointLight.maxLights];
            int light = 0;
            int objectIndex = 0;
            //Load all the lights from the world up to the maximum (16 by default)
            if (light < PointLight.maxLights) {
                Vector4 sunPos = new Vector4(Vector4.Transform(Sun.GetSunlightDirection(currentMillis), worldToCameraMat));
                lights[light].cameraSpaceLightPos = sunPos;
                lights[light].lightIntensity = sunTimer.Interpolate(Sun.Alpha(currentMillis)).intensity;
                light += 1;
            }
            while (light < PointLight.maxLights && objectIndex < worldObjects.Count) {
                GameObject possibleLight = worldObjects[objectIndex++];
                var allLights = loadObjectPointLights(possibleLight, worldToCameraMat);
                foreach (PointLight pointLight in allLights) {
                    lights[light++] = pointLight;
                    if (light >= PointLight.maxLights) break;
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

        internal LightBlockGamma GetLightInformation(long currentMillis) {
            LightBlockGamma lightData = new LightBlockGamma();
            Sun sun = sunTimer.Interpolate(Sun.Alpha(currentMillis));
            lightData.ambientIntensity = sun.ambient;
            lightData.attenuationMaxGamma.X = attenuation;
            lightData.attenuationMaxGamma.Y = sun.maxIntensity;
            lightData.attenuationMaxGamma.Z = 1 / gamma;
            return lightData;
        }

        static public Vector4 GammaCorrect(Vector4 input, float gamma = 2.2f) {
            return new Vector4((float)Math.Pow(input[0], 1.0f / gamma), (float)Math.Pow(input[1], 1.0f / gamma), (float)Math.Pow(input[2], 1.0f / gamma), input[3]);
        }

        internal void Render(Matrix4 worldToCamMat, long currentMillis) {
            PointLight[] lightData = GetPointLights(worldToCamMat, currentMillis);
            unsafe {
                GCHandle pinnedArray = GCHandle.Alloc(lightData, GCHandleType.Pinned);
                GL.BindBuffer(BufferTarget.UniformBuffer, g_pointLightUniformBuffer);
                GL.BufferSubData(BufferTarget.UniformBuffer, IntPtr.Zero, new IntPtr(PointLight.sizeBytes), pinnedArray.AddrOfPinnedObject());
                GL.BindBuffer(BufferTarget.UniformBuffer, 0);
                pinnedArray.Free();
            }
            LightBlockGamma data = GetLightInformation(currentMillis);
            GL.BindBuffer(BufferTarget.UniformBuffer, g_lightUniformBuffer);
            GL.BufferData(BufferTarget.UniformBuffer, new IntPtr(Marshal.SizeOf(typeof(LightBlockGamma))), ref data, BufferUsageHint.DynamicDraw);
            GL.BindBuffer(BufferTarget.UniformBuffer, 0);
        }

    }
}
