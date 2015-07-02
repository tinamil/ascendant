using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using System.Runtime.InteropServices;
using Ascendant.Physics;
using Ascendant.Graphics.lighting;

namespace Ascendant.Graphics.objects {
    struct Material {
        public Vector4 diffuseColor;
        public Vector4 specularColor;
        public Vector4 specularShininess;
    }

    internal class MaterialLoader {

        readonly static List<Material> materials = new List<Material>();
        internal static uint g_materialUniformBuffer;
        internal static int m_sizeMaterialBlock;

        internal static void LoadMaterialBufferBlock(int program) {

            int materialBlock = GL.GetUniformBlockIndex(program, "Material");
            GL.UniformBlockBinding(program, materialBlock, Window.g_materialBlockIndex);

            //Align the size of each MaterialBlock to the uniform buffer alignment.
            int uniformBufferAlignSize = 0;
            GL.GetInteger(GetPName.UniformBufferOffsetAlignment, out uniformBufferAlignSize);
            int blockSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Material));
            m_sizeMaterialBlock = blockSize + (uniformBufferAlignSize - (blockSize % uniformBufferAlignSize));

            int sizeMaterialUniformBuffer = m_sizeMaterialBlock * materials.Count;

            unsafe {
                IntPtr data = Marshal.AllocHGlobal(sizeMaterialUniformBuffer);
                IntPtr inData = data;
                for (int i = 0; i < materials.Count; ++i) {
                    Marshal.StructureToPtr(materials[i], inData, false);
                    inData = IntPtr.Add(inData, m_sizeMaterialBlock);
                }
                GL.GenBuffers(1, out g_materialUniformBuffer);
                GL.BindBuffer(BufferTarget.UniformBuffer, g_materialUniformBuffer);
                GL.BufferData(BufferTarget.UniformBuffer, new IntPtr(sizeMaterialUniformBuffer), data, BufferUsageHint.StaticDraw);
                Marshal.FreeHGlobal(data);
            }
            GL.BindBuffer(BufferTarget.UniformBuffer, 0);

            GL.BindBufferRange(BufferRangeTarget.UniformBuffer, Window.g_materialBlockIndex, g_materialUniformBuffer, IntPtr.Zero, new IntPtr(sizeMaterialUniformBuffer));
            materials.Clear();
        }

        internal static int AddMaterial(Material mat) {
            int matIndex;
            if (materials.Contains(mat)) {
                matIndex = materials.FindIndex(material => material.Equals(mat));
            } else {
                materials.Add(mat);
                matIndex = materials.Count - 1;
            }
            return matIndex;
        }

        private static int getMaterialCount() {
            return materials.Count;
        }
    }
}
