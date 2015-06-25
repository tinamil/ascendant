using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using System.Diagnostics;
using OpenTK.Graphics.OpenGL4;

namespace Ascendant.Graphics.lighting {
    interface HasLightSource {
        bool hasLight();
        Vector3 getLightPosition();
        Vector4 getLightIntensity();
    }

    class Lighting {
        uint g_lightUniformBuffer;
        List<DisplayObject> lightObjects = new List<DisplayObject>();
        static long loopDuration = 20000L;

        public const int numLights = 16;
        Ascendant.Graphics.Framework.TimedLinearInterpolator m_ambientInterpolator = new Framework.TimedLinearInterpolator();
        Ascendant.Graphics.Framework.TimedLinearInterpolator m_backgroundInterpolator = new Framework.TimedLinearInterpolator();
        Ascendant.Graphics.Framework.TimedLinearInterpolator m_sunlightInterpolator = new Framework.TimedLinearInterpolator();
        Ascendant.Graphics.Framework.TimedLinearInterpolator m_maxIntensityInterpolator = new Framework.TimedLinearInterpolator();
        Stopwatch sunTimer = new Stopwatch();

        public void AddPointLight(DisplayObject obj) {
            lightObjects.Add(obj);
        }

        public Lighting() {
            sunTimer.Start();

            Vector4 sunlight = new Vector4(6.5f, 6.5f, 6.5f, 1.0f);
            Vector4 brightAmbient = new Vector4(0.4f, 0.4f, 0.4f, 1.0f);
            Vector4 g_skyDaylightColor = new Vector4(0.65f, 0.65f, 1.0f, 1.0f);
            SunlightValueHDR[] values = new SunlightValueHDR[]	{
		        new SunlightValueHDR( 0.0f/24.0f, brightAmbient, sunlight, new Vector4(0.65f, 0.65f, 1.0f, 1.0f), 10.0f),
		        new SunlightValueHDR( 4.5f/24.0f, brightAmbient, sunlight, g_skyDaylightColor, 10.0f),
		        new SunlightValueHDR( 6.5f/24.0f, new Vector4(0.01f, 0.025f, 0.025f, 1.0f), new Vector4(2.5f, 0.2f, 0.2f, 1.0f), new Vector4(0.5f, 0.1f, 0.1f, 1.0f), 5.0f),
		        new SunlightValueHDR( 8.0f/24.0f, new Vector4(0.0f, 0.0f, 0.0f, 1.0f), new Vector4(0.0f, 0.0f, 0.0f, 1.0f), new Vector4(0.0f, 0.0f, 0.0f, 1.0f), 3.0f),
		        new SunlightValueHDR(18.0f/24.0f, new Vector4(0.0f, 0.0f, 0.0f, 1.0f), new Vector4(0.0f, 0.0f, 0.0f, 1.0f), new Vector4(0.0f, 0.0f, 0.0f, 1.0f), 3.0f),
		        new SunlightValueHDR(19.5f/24.0f, new Vector4(0.01f, 0.025f, 0.025f, 1.0f), new Vector4(2.5f, 0.2f, 0.2f, 1.0f), new Vector4(0.5f, 0.1f, 0.1f, 1.0f), 5.0f),
		        new SunlightValueHDR(20.5f/24.0f, brightAmbient, sunlight, g_skyDaylightColor, 10.0f),
            };

            LightVector ambient = new LightVector();
            LightVector light = new LightVector();
            LightVector background = new LightVector();
            LightVector maxIntensity = new LightVector();

            for (int valIx = 0; valIx < values.Length; ++valIx) {
                ambient.Add(new LightVectorData(values[valIx].ambient, values[valIx].normTime));
                light.Add(new LightVectorData(values[valIx].sunlightIntensity, values[valIx].normTime));
                background.Add(new LightVectorData(values[valIx].backgroundColor, values[valIx].normTime));
                maxIntensity.Add(new LightVectorData(new Vector4(values[valIx].maxIntensity), values[valIx].normTime));
            }

            m_ambientInterpolator.SetValues(ambient);
            m_sunlightInterpolator.SetValues(light);
            m_backgroundInterpolator.SetValues(background);
            m_maxIntensityInterpolator.SetValues(maxIntensity);
        }

        const int g_lightBlockIndex = 1;

        public void IntializeOpenGL(int program) {
            int lightBlock = GL.GetUniformBlockIndex(program, "Light");

            GL.UniformBlockBinding(program, lightBlock, g_lightBlockIndex);

            GL.GenBuffers(1, out g_lightUniformBuffer);
            GL.BindBuffer(BufferTarget.UniformBuffer, g_lightUniformBuffer);
            IntPtr lightBlockSize = (IntPtr)LightBlockGamma.getByteSize();
            GL.BufferData(BufferTarget.UniformBuffer, lightBlockSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);

            GL.BindBufferRange(BufferRangeTarget.UniformBuffer, g_lightBlockIndex, g_lightUniformBuffer, IntPtr.Zero, lightBlockSize);

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

        internal Vector4 GetBackgroundColor() {
            return m_backgroundInterpolator.Interpolate(getSunTime());
        }


        internal LightBlockGamma GetLightInformation(Matrix4 worldToCameraMat) {
            LightBlockGamma lightData = new LightBlockGamma();
            lightData.lights = new PerLight[Lighting.numLights];
            lightData.ambientIntensity = m_ambientInterpolator.Interpolate(getSunTime());
            lightData.attenuationMaxGamma.X = 1 / 5f;
            lightData.attenuationMaxGamma.Y = m_maxIntensityInterpolator.Interpolate(getSunTime()).X;
            lightData.lights[0].cameraSpaceLightPos = Vector4.Transform(GetSunlightDirection(), worldToCameraMat);
            lightData.lights[0].lightIntensity = m_sunlightInterpolator.Interpolate(getSunTime());

            for (int light = 0; light < numLights - 1 && light < lightObjects.Count; light++) {
                Vector4 worldLightPos = new Vector4(lightObjects[light].lightPosition);
                Vector4 lightPosCameraSpace = Vector4.Transform(worldLightPos, worldToCameraMat);

                lightData.lights[light + 1].cameraSpaceLightPos = lightPosCameraSpace;
                lightData.lights[light + 1].lightIntensity = lightObjects[light].lightIntensity;
            }
            Debug.WriteLine("Light info: " + getSunTime());
            return lightData;
        }
        float getSunTime() {
            return ((float)(sunTimer.ElapsedMilliseconds % loopDuration) / loopDuration);
        }
        Vector4 GetSunlightDirection() {
            float angle = 2.0f * 3.14159f * getSunTime();
            Vector4 sunDirection = new Vector4();
            sunDirection[0] = (float)Math.Sin(angle);
            sunDirection[1] = (float)Math.Cos(angle);

            //Keep the sun from being perfectly centered overhead.
            sunDirection = Vector4.Transform(sunDirection, Matrix4.CreateRotationY(5));

            return sunDirection;
        }

        static internal Vector4 GammaCorrect(Vector4 input, float gamma) {
            return new Vector4((float)Math.Pow(input[0], 1.0f / gamma), (float)Math.Pow(input[1], 1.0f / gamma), (float)Math.Pow(input[2], 1.0f / gamma), input[3]);
        }

        internal void Render(Matrix4 worldToCamMat, float gamma) {
            LightBlockGamma lightData = GetLightInformation(worldToCamMat);
            lightData.attenuationMaxGamma.Z = 1 / gamma;

            GL.BindBuffer(BufferTarget.UniformBuffer, g_lightUniformBuffer);

            IntPtr lightBlockSize = (IntPtr)LightBlockGamma.getByteSize();
            GL.BufferSubData(BufferTarget.UniformBuffer, IntPtr.Zero, lightBlockSize, ref lightData);

            GL.BindBuffer(BufferTarget.UniformBuffer, 0);
        }
    }
}
