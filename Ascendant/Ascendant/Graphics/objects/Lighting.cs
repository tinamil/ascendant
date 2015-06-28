using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using System.Diagnostics;
using OpenTK.Graphics.OpenGL4;

namespace Ascendant.Graphics.lighting {

    class Lighting {
        uint g_lightUniformBuffer;
        List<DisplayObject> lightObjects = new List<DisplayObject>();

        public const int numLights = 16;

        public void AddPointLight(DisplayObject obj) {
            lightObjects.Add(obj);
        }

        public Lighting() {

            Vector4 sunlight = new Vector4(6.5f, 6.5f, 6.5f, 1.0f);
            Vector4 brightAmbient = new Vector4(0.4f, 0.4f, 0.4f, 1.0f);
            Vector4 g_skyDaylightColor = new Vector4(0.65f, 0.65f, 1.0f, 1.0f);

        }

        public void IntializeOpenGL(int program) {
            GL.GenBuffers(1, out g_lightUniformBuffer);
            GL.BindBuffer(BufferTarget.UniformBuffer, g_lightUniformBuffer);

            GL.BufferData(BufferTarget.UniformBuffer, (IntPtr)LightBlockGamma.getByteSize(), IntPtr.Zero, BufferUsageHint.DynamicDraw);
            int lightBlock = GL.GetUniformBlockIndex(program, "Light");
            GL.UniformBlockBinding(program, lightBlock, Window.g_lightBlockIndex);

            GL.BindBuffer(BufferTarget.UniformBuffer, 0);
        }

        public struct LightVectorData {
            public Tuple<Vector4, float> data;

            public LightVectorData(Vector4 vector4, float p) {
                data = new Tuple<Vector4, float>(vector4, p);
            }
        }

        public struct LightVector {
            public List<LightVectorData> data;
            public void Add(LightVectorData rhs) {
                if (data == null) data = new List<LightVectorData>();
                data.Add(rhs);
            }
        }

        internal struct SunlightValueHDR {
            public float normTime;
            public Vector4 ambient;
            public Vector4 sunlightIntensity;
            public Vector4 backgroundColor;
            public float maxIntensity;
            public SunlightValueHDR(float _normTime, Vector4 _ambient, Vector4 _sunIntensity, Vector4 _backgroundColor, float _maxIntensity) {
                normTime = _normTime;
                ambient = _ambient;
                sunlightIntensity = _sunIntensity;
                backgroundColor = _backgroundColor;
                maxIntensity = _maxIntensity;
            }
        };

        internal LightBlockGamma GetLightInformation(Matrix4 worldToCameraMat) {
            LightBlockGamma lightData = new LightBlockGamma();
            lightData.lights = new PerLight[Lighting.numLights];
            lightData.ambientIntensity = new Vector4(.5f, .5f, .5f, 1f);
            lightData.attenuationMaxGamma.X = 1 / (70 * 70f);
            lightData.attenuationMaxGamma.Y = 2;
            int light = 0;
            for (; light < numLights && light < lightObjects.Count; light++) {
                Vector4 worldLightPos = new Vector4(lightObjects[light].physics.display.position, 1.0f);
                Vector4 lightPosCameraSpace = Vector4.Transform(worldLightPos, worldToCameraMat);

                lightData.lights[light].cameraSpaceLightPos = lightPosCameraSpace;
                lightData.lights[light].lightIntensity = lightObjects[light].lightIntensity;
            }
            while (light < numLights) {
                lightData.lights[light].cameraSpaceLightPos = Vector4.Zero;
                lightData.lights[light].lightIntensity = Vector4.Zero;
                light += 1;
            }
            return lightData;
        }

        static internal Vector4 GammaCorrect(Vector4 input, float gamma) {
            return new Vector4((float)Math.Pow(input[0], 1.0f / gamma), (float)Math.Pow(input[1], 1.0f / gamma), (float)Math.Pow(input[2], 1.0f / gamma), input[3]);
        }

        internal void Render(Matrix4 worldToCamMat, float gamma) {
            LightBlockGamma lightData = GetLightInformation(worldToCamMat);
            lightData.attenuationMaxGamma.Z = 1 / gamma;

            IntPtr lightBlockSize = (IntPtr)LightBlockGamma.getByteSize();
            GL.BindBufferRange(BufferRangeTarget.UniformBuffer, Window.g_lightBlockIndex, g_lightUniformBuffer, IntPtr.Zero, lightBlockSize);

            GL.BufferData(BufferTarget.UniformBuffer, lightBlockSize, lightData.getBytes(), BufferUsageHint.StreamDraw);


            GL.BindBuffer(BufferTarget.UniformBuffer, 0);
        }
    }
}
