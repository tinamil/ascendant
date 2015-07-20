using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Input;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using System.Diagnostics;
using OpenTK.Graphics;
using System.Drawing;
using Ascendant.Graphics.lighting;
using Ascendant.Graphics.objects;
using Ascendant.Graphics;

namespace Ascendant {
    class Game {
        protected internal Window window;

        internal int gameProgram;

        int cameraToClipMatrixUnif;

        World world;

        public Game(Window window) {
            this.window = window;
            window.UpdateFrame += Update;
            window.Load += GameLoad;
            world = MyParser.parseWorld(this, "world1.wor");
        }

        private void InitalizeOpenGL() {
            var shaderList = new List<int>();
            shaderList.Add(Framework.LoadShader(ShaderType.VertexShader, @"Graphics\shaders\PNT.vert"));
            shaderList.Add(Framework.LoadShader(ShaderType.FragmentShader, @"Graphics\shaders\DiffuseSpecularMtlGamma.frag"));

            gameProgram = Framework.CreateProgram(shaderList);

            cameraToClipMatrixUnif = GL.GetUniformLocation(gameProgram, "cameraToClipMatrix");
            world.InitializeOpenGL(gameProgram);
            Resize();
        }


        public void Update(object o, EventArgs e) {
            Debug.WriteLine("Render frequency: " + window.RenderFrequency);
            world.Update();
        }

        public void Render() {
            GL.UseProgram(gameProgram);
            world.Render();
            GL.UseProgram(0);
        }

        internal void Resize() {
            GL.UseProgram(gameProgram);
            Matrix4 CameraToClipMatrix = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(AppConfig.Default.fov), window.Width / window.Height, AppConfig.Default.near, AppConfig.Default.far);
            GL.UniformMatrix4(cameraToClipMatrixUnif, false, ref CameraToClipMatrix);
            GL.UseProgram(0);
        }

        internal void GameLoad(object o, EventArgs e) {
            InitalizeOpenGL();
        }

        internal Vector4 getBackground() {
            return world.getBackgroundColor();
        }
    }
}
