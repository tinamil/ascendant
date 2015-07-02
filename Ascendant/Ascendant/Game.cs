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
        protected internal ICamera Camera = new FirstPersonCamera();
        protected internal Window window;

        internal int gameProgram;

        int cameraToClipMatrixUnif;

        World world;

        ISet<Key> pressedKeys = new HashSet<Key>();
        ISet<MouseButton> pressedMouseButton = new HashSet<MouseButton>();
        MouseState previous; //Used for detecting if the mouse has moved from frame to frame

        public Game(Window window) {
            this.window = window;
            window.KeyDown += Keyboard_KeyDown;
            window.KeyUp += Keyboard_KeyUp;
            window.UpdateFrame += Update;
            window.MouseDown += Mouse_ButtonDown;
            window.MouseUp += Mouse_ButtonUp;
            window.Load += GameLoad;
            world = MyParser.parseWorld(this, "world1.wor");
            InitalizeOpenGL();
        }

        private void InitalizeOpenGL() {
            var shaderList = new List<int>();
            shaderList.Add(Framework.LoadShader(ShaderType.VertexShader, @"Graphics\shaders\PN.vert"));
            shaderList.Add(Framework.LoadShader(ShaderType.FragmentShader, @"Graphics\shaders\DiffuseSpecularMtlGamma.frag"));

            gameProgram = Framework.CreateProgram(shaderList);

            cameraToClipMatrixUnif = GL.GetUniformLocation(gameProgram, "cameraToClipMatrix");
            world.InitializeOpenGL(gameProgram);
            Resize();
        }


        protected void Keyboard_KeyDown(object sender, KeyboardKeyEventArgs e) {
            if (e.Keyboard.IsKeyDown(Key.Escape)) {
                window.Exit();
            }
            pressedKeys.Add(e.Key);
        }

        protected void Keyboard_KeyUp(object sender, KeyboardKeyEventArgs e) {
            pressedKeys.Remove(e.Key);
        }

        protected void Mouse_ButtonDown(object sender, MouseButtonEventArgs e) {
            pressedMouseButton.Add(e.Button);
            if (e.Button == MouseButton.Left) {
                window.CursorVisible = false;
                previous = Mouse.GetState();
            }
        }

        protected void Mouse_ButtonUp(object sender, MouseButtonEventArgs e) {
            pressedMouseButton.Remove(e.Button);
            if (e.Button == MouseButton.Left) {
                window.CursorVisible = true;
            }
        }

        public void Update(object o, EventArgs e) {
            MouseMove();
            CameraMove();
            Debug.WriteLine("Render frequency: " + window.RenderFrequency);
            world.Update();
        }

        private void CameraMove() {
            Vector3 movement = Vector3.Zero;
            if (pressedKeys.Contains(Key.W)) {
                movement.Y += .1f;
            }
            if (pressedKeys.Contains(Key.A)) {
                movement.X += -.1f;
            }
            if (pressedKeys.Contains(Key.S)) {
                movement.Y += -.1f;
            }
            if (pressedKeys.Contains(Key.D)) {
                movement.X += .1f;
            }
            if (pressedKeys.Contains(Key.LShift)) {
                movement.Z += .1f;
            }
            if (pressedKeys.Contains(Key.LControl)) {
                movement.Z -= .1f;
            }
            Camera.Move(movement);
        }

        public void Render() {
            GL.UseProgram(gameProgram);
            world.Render();
            GL.UseProgram(0);
        }

        protected void ResetCursor() {
            OpenTK.Input.Mouse.SetPosition(window.Bounds.Left + window.Bounds.Width / 2, window.Bounds.Top + window.Bounds.Height / 2);
            previous = Mouse.GetState();
        }

        protected void MouseMove() {
            var current = Mouse.GetState();
            if (current != previous && window.Focused && pressedMouseButton.Contains(MouseButton.Left)) {
                Vector2 delta = new Vector2(current.X - previous.X, current.Y - previous.Y);
                Camera.Rotate(delta);
                ResetCursor();
            }
        }

        internal void Resize() {
            GL.UseProgram(gameProgram);
            Matrix4 CameraToClipMatrix = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(AppConfig.Default.fov), window.Width / window.Height, AppConfig.Default.near, AppConfig.Default.far);
            GL.UniformMatrix4(cameraToClipMatrixUnif, false, ref CameraToClipMatrix);
            GL.UseProgram(0);
        }

        internal void GameLoad(object o, EventArgs e) {

        }

        internal Vector4 getBackground() {
            return world.lights.background;
        }
    }
}
