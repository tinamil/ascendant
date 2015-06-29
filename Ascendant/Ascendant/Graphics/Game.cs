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

namespace Ascendant.Graphics {
    class Game {
        Stopwatch timer = Stopwatch.StartNew();
        protected internal Lighting Lights = new Lighting();
        protected internal ICamera Camera = new FirstPersonCamera();
        protected internal Window window;

        internal int gameProgram;

        int modelToCameraMatrixUnif;
        int normalModelToCameraMatrixUnif;
        int cameraToClipMatrixUnif;

        Matrix4 CameraToClipMatrix;

        DisplayObject floor, man, cube, diamond;
       
        public Game(Window window) {
            this.window = window;
            window.KeyDown += Keyboard_KeyDown;
            window.UpdateFrame += Update;
            window.FocusedChanged += FocusChange;
            window.Load += GameLoad;
            InitalizeOpenGL();
            floor = MyParser.parseObject(this, "Floor.base");
            man = MyParser.parseObject(this, "Man.base");
            cube = MyParser.parseObject(this, "Cube.base");
            diamond = MyParser.parseObject(this, "Diamond.base");
            
            Ascendant.Graphics.MyLoader.MaterialLoader.LoadMaterialBufferBlock(gameProgram);
        }
        
        private void InitalizeOpenGL() {
            var shaderList = new List<int>();
            shaderList.Add(Framework.LoadShader(ShaderType.VertexShader, @"Graphics\shaders\PN.vert"));
            shaderList.Add(Framework.LoadShader(ShaderType.FragmentShader, @"Graphics\shaders\DiffuseSpecularMtlGamma.frag"));

            gameProgram = Framework.CreateProgram(shaderList);

            modelToCameraMatrixUnif = GL.GetUniformLocation(gameProgram, "modelToCameraMatrix");
            normalModelToCameraMatrixUnif = GL.GetUniformLocation(gameProgram, "normalModelToCameraMatrix");
            cameraToClipMatrixUnif = GL.GetUniformLocation(gameProgram, "cameraToClipMatrix");
            Lights.IntializeOpenGL(gameProgram);
            Resize();
        }


       protected void Keyboard_KeyDown(object sender, KeyboardKeyEventArgs e) {
            Vector3 movement = Vector3.Zero;
            if (e.Keyboard.IsKeyDown(Key.Escape))
                window.Exit();
            if (e.Keyboard.IsKeyDown(Key.W)) {
                movement.Y += .1f;
            }
            if (e.Keyboard.IsKeyDown(Key.A)) {
                movement.X += -.1f;
            }
            if (e.Keyboard.IsKeyDown(Key.S)) {
                movement.Y += -.1f;
            }
            if (e.Keyboard.IsKeyDown(Key.D)) {
                movement.X += .1f;
            }
            if (e.Keyboard.IsKeyDown(Key.LShift)) {
                movement.Z += .1f;
            }
            if (e.Keyboard.IsKeyDown(Key.LControl)) {
                movement.Z -= .1f;
            }
            Camera.Move(movement);
        }

        public void Update(object o, EventArgs e) {
            MouseMove();
            Debug.WriteLine("Render frequency: " + window.RenderFrequency);
            window.sim.RunSim(timer.ElapsedMilliseconds);
        }

        public void Render() {
            GL.UseProgram(gameProgram);

            var Matrix = Matrix4.Identity;
            Lights.Render(Camera.GetWorldToCameraMatrix(), window.GammaValue);
            cube.Render(ref Matrix, modelToCameraMatrixUnif, normalModelToCameraMatrixUnif);
            floor.Render(ref Matrix, modelToCameraMatrixUnif, normalModelToCameraMatrixUnif);
            man.Render(ref Matrix, modelToCameraMatrixUnif, normalModelToCameraMatrixUnif);
            diamond.Render(ref Matrix, modelToCameraMatrixUnif, normalModelToCameraMatrixUnif);
            GL.UseProgram(0);
        }


        MouseState previous;
        protected void ResetCursor() {
            OpenTK.Input.Mouse.SetPosition(window.Bounds.Left + window.Bounds.Width / 2, window.Bounds.Top + window.Bounds.Height / 2);
            previous = Mouse.GetState();
        }

        protected void MouseMove() {
            var current = Mouse.GetState();
            if (current != previous && window.Focused) {
                Vector2 delta = new Vector2(current.X - previous.X, current.Y - previous.Y);
                Camera.Rotate(delta);
                ResetCursor();
            }
        }

        protected internal void FocusChange(object o, EventArgs e) {
            if (window.Focused) {
                window.CursorVisible = false;
                ResetCursor();
            } else {
                window.CursorVisible = true;
            }
        }

        internal void Resize() {
            GL.UseProgram(gameProgram);
            CameraToClipMatrix = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(AppConfig.Default.fov), window.Width / window.Height, AppConfig.Default.near, AppConfig.Default.far);
            GL.UniformMatrix4(cameraToClipMatrixUnif, false, ref CameraToClipMatrix);
            GL.UseProgram(0);
        }

        internal void GameLoad(object o, EventArgs e) {
            window.CursorVisible = false;
        }
    }
}
