using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Input;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Diagnostics;
using OpenTK.Graphics;
using System.Drawing;

namespace Ascendant.Graphics {
    class Game {

        protected internal ICamera Camera = new FirstPersonCamera();
        Vector2 lastMousePos = new Vector2();
        Window window;

        int gameProgram;
        int positionAttrib;
        int colorAttrib;

        int modelToCameraMatrixUnif;
        int cameraToClipMatrixUnif;

        int baseColorUnif;

        Matrix4 CameraToClipMatrix;

        DisplayObject diamond;
        DisplayObject floor;

        public Game(Window window) {
            this.window = window;
            window.KeyDown += Keyboard_KeyDown;
            InitalizeOpenGL();
            diamond = new DisplayObject(this, Vector3.Zero, Vector3.One, Quaternion.FromAxisAngle(Vector3.UnitZ, 0), new ushort[]{
            0, 1, 2,
	        1, 0, 3,
	        2, 3, 0,
	        3, 2, 1,

	        5, 4, 6,
	        4, 5, 7,
	        7, 6, 4,
	        6, 7, 5,
        }, new float[]{
            +1.0f, +1.0f, +1.0f,
	        -1.0f, -1.0f, +1.0f,
	        -1.0f, +1.0f, -1.0f,
	        +1.0f, -1.0f, -1.0f,

	        -1.0f, -1.0f, -1.0f,
	        +1.0f, +1.0f, -1.0f,
	        +1.0f, -1.0f, +1.0f,
	        -1.0f, +1.0f, +1.0f,

        }, new Color4[] {
            Color.Green,
            Color.Blue,
            Color.Red,
            Color.Brown,
            
            Color.Green,
            Color.Blue,
            Color.Red,
            Color.Brown,
       });
            floor = new DisplayObject(this, new Vector3(0f, 0f, -1), new Vector3(10f, 10f, 10f), Quaternion.FromAxisAngle(Vector3.UnitX, MathHelper.PiOver2), new ushort[]{
                 0, 1, 2, 
                 0, 2, 1,
                 2, 3, 0,
                 2, 0, 3,
        }, new float[]{
           
            0.5f, 0, -0.5f,
            0.5f, 0, 0.5f,
            -0.5f, 0, 0.5f,
            -0.5f, 0, -0.5f,

        }, new Color4[] {
            Color.Black,
            Color.Black,
            Color.Black,
            Color.Black,
       });
           
        }

        private void InitalizeOpenGL() {
            var shaderList = new List<int>();
            shaderList.Add(Framework.LoadShader(ShaderType.VertexShader, @"Graphics\shaders\PosColorLocalTransform.vert"));
            shaderList.Add(Framework.LoadShader(ShaderType.FragmentShader, @"Graphics\shaders\ColorMultUniform.frag"));

            gameProgram = Framework.CreateProgram(shaderList);

            positionAttrib = GL.GetAttribLocation(gameProgram, "position");
            colorAttrib = GL.GetAttribLocation(gameProgram, "color");

            modelToCameraMatrixUnif = GL.GetUniformLocation(gameProgram, "modelToCameraMatrix");
            cameraToClipMatrixUnif = GL.GetUniformLocation(gameProgram, "cameraToClipMatrix");
            baseColorUnif = GL.GetUniformLocation(gameProgram, "baseColor");

            Resize();
        }


        protected void Keyboard_KeyDown(object sender, KeyboardKeyEventArgs e) {
            Vector3 movement = Vector3.Zero;

            switch (e.Key) {
                case Key.Escape:
                    window.Exit();
                    break;
                case Key.W:
                    movement.Y += .1f;
                    break;
                case Key.A:
                    movement.X += -.1f;
                    break;
                case Key.S:
                    movement.Y += -.1f;
                    break;
                case Key.D:
                    movement.X += .1f;
                    break;
                case Key.LShift:
                    movement.Z += .1f;
                    break;
                case Key.LControl:
                    movement.Z -= .1f;
                    break;
            }
            Camera.Move(movement);
        }

        public void Update() {
            if (window.Focused) {
                MoveCursor();
            }
        }

        public void Render() {
            GL.UseProgram(gameProgram);

            //Set the base color for all objects.
            GL.Uniform4(baseColorUnif, 1.0f, 1.0f, 1.0f, 1.0f);
            var Matrix = Matrix4.Identity;
            diamond.Render(ref Matrix, modelToCameraMatrixUnif);
            floor.Render(ref Matrix, modelToCameraMatrixUnif);
            GL.UseProgram(0);
        }

        protected void ResetCursor() {
            OpenTK.Input.Mouse.SetPosition(window.Bounds.Left + window.Bounds.Width / 2, window.Bounds.Top + window.Bounds.Height / 2);
            lastMousePos = new Vector2(OpenTK.Input.Mouse.GetState().X, OpenTK.Input.Mouse.GetState().Y);
        }

        protected void MoveCursor() {
            Vector2 delta = lastMousePos - new Vector2(OpenTK.Input.Mouse.GetState().X, OpenTK.Input.Mouse.GetState().Y);
            Camera.Rotate(delta);
            ResetCursor();
        }

        protected internal void FocusChange() {
            if (window.Focused) {
                ResetCursor();
            }
        }

        internal void Resize() {
            GL.UseProgram(gameProgram);
            CameraToClipMatrix = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(AppConfig.Default.fov), window.Width / window.Height, AppConfig.Default.near, AppConfig.Default.far);
            GL.UniformMatrix4(cameraToClipMatrixUnif, false, ref CameraToClipMatrix);
            GL.UseProgram(0);
        }
    }
}
