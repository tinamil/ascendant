using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Graphics.OpenGL4;
using System.IO;
using System.Diagnostics;
using OpenTK;
using Ascendant.Graphics.lighting;
using System.Runtime.InteropServices;

namespace Ascendant {
    static class Framework {

        public static int LoadShader(ShaderType eShaderType, string strShaderFilename) {
            if (!File.Exists(strShaderFilename)) {
                Debug.WriteLine("Could not find the file " + strShaderFilename, "Error");
                throw new Exception("Could not find the file " + strShaderFilename);
            }
            int shader = GL.CreateShader(eShaderType);
            using (var reader = new StreamReader(strShaderFilename)) {
                GL.ShaderSource(shader, reader.ReadToEnd());
            }
            GL.CompileShader(shader);
            int status;
            GL.GetShader(shader, ShaderParameter.CompileStatus, out status);
            if (status == 0) {
                string strInfoLog;
                GL.GetShaderInfoLog(shader, out strInfoLog);
                Debug.WriteLine("Compile failure in " + eShaderType.ToString() + " shader:\n" + strInfoLog, "Error");
                throw new Exception();
            }

            return shader;
        }

        public static int CreateProgram(List<int> shaderList) {
            int program = GL.CreateProgram();

            foreach (int shader in shaderList) {
                GL.AttachShader(program, shader);
            }

            GL.LinkProgram(program);

            int status;
            GL.GetProgram(program, GetProgramParameterName.LinkStatus, out status);
            if (status == 0) {
                string strInfoLog;
                GL.GetProgramInfoLog(program, out strInfoLog);
                Debug.WriteLine("Linker failure: " + strInfoLog, "Error");
                throw new Exception();
            }

            foreach (int shader in shaderList) {
                GL.DetachShader(program, shader);
                GL.DeleteShader(shader);
            }

            return program;
        }

    }
    public interface ILerpable {
        ILerpable multiply(float val);
        ILerpable add(ILerpable other);
    }

    public class WeightedLinearInterpolator<T> where T : ILerpable, new() {

        protected List<Data> m_values = new List<Data>();
        int NumSegments() { return m_values.Count == 0 ? 0 : m_values.Count() - 1; }

        public T Interpolate(float fAlpha) {
            if (m_values.Count == 0)
                return new T();
            if (m_values.Count == 1)
                return m_values[0].data;

            //Find which segment we are within.
            int segment = 1;
            for (; segment < m_values.Count; ++segment) {
                if (fAlpha < m_values[segment].weight)
                    break;
            }

            if (segment == m_values.Count)
                return m_values.Last().data;

            float sectionAlpha = fAlpha - m_values[segment - 1].weight;
            sectionAlpha /= m_values[segment].weight - m_values[segment - 1].weight;

            float invSecAlpha = 1.0f - sectionAlpha;

            return (T)m_values[segment - 1].data.multiply(invSecAlpha).add(m_values[segment].data.multiply(sectionAlpha));
        }

        protected WeightedLinearInterpolator() { }

        protected struct Data {
            public T data;
            public float weight;
        };

    }

    public class TimedLinearInterpolator<T> : WeightedLinearInterpolator<T> where T : ILerpable, new() {

        internal void SetValues(Tuple<T, float>[] data, bool isLooping = true) {
            m_values.Clear();

            for (int i = 0; i < data.Length; ++i) {
                var currData = new Data();
                currData.data = data[i].Item1;
                currData.weight = data[i].Item2;

                m_values.Add(currData);
            }

            if (isLooping && m_values.Count != 0)
                m_values.Add(m_values.First());

            //Ensure first is weight 0, and last is weight 1.
            if (m_values.Count > 0) {
                Data v = m_values[0];
                v.weight = 0f;
                m_values[0] = v;
                v = m_values[m_values.Count - 1];
                v.weight = 1f;
                m_values[m_values.Count - 1] = v;
            }
        }
    }
    class TreeNode<DATA> {

        public readonly List<TreeNode<DATA>> children = new List<TreeNode<DATA>>();

        public readonly DATA data;

        public TreeNode(DATA data) {
            this.data = data;
        }

        public bool isLeaf {
            get { return children.Count == 0; }
        }
    }

}
