using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ascendant.Graphics.lighting;
using OpenTK.Graphics.OpenGL4;
using Ascendant.Graphics;
using OpenTK;
using OpenTK.Input;
using Ascendant.Physics;
using Ascendant.Graphics.objects;

namespace Ascendant {
    class DisplayWorld {
        readonly ICamera Camera = new FirstPersonCamera();
        internal Lighting lights { get; private set; }
        private bool g_useGammaDisplay = true;
        readonly Window window;
        readonly Game parentGame;

        readonly ISet<Key> pressedKeys = new HashSet<Key>();
        readonly ISet<MouseButton> pressedMouseButton = new HashSet<MouseButton>();
        MouseState previous; //Used for detecting if the mouse has moved from frame to frame

        int modelToCameraMatrixUnif { get; set; }
        int normalModelToCameraMatrixUnif { get; set; }

        List<DisplayObject> DisplayObjects{ get; set; }

        public DisplayWorld(Game game) {
            DisplayObjects = new List<DisplayObject>();
            lights = new Lighting(DisplayObjects);
            this.parentGame = game;
            window = game.window;
            window.KeyPress += (o, e) => {
                if (e.KeyChar == 'g') {
                    g_useGammaDisplay = !g_useGammaDisplay;
                }
            };

            window.KeyDown += (o, e) => {
                if (e.Key == Key.Escape) {
                    window.Exit();
                }
            };

            window.KeyDown += Keyboard_KeyDown;
            window.KeyUp += Keyboard_KeyUp;
            window.MouseDown += Mouse_ButtonDown;
            window.MouseUp += Mouse_ButtonUp;
        }

        internal void Render(long elapsedMillis) {
            if (g_useGammaDisplay)
                GL.Enable(EnableCap.FramebufferSrgb);
            else
                GL.Disable(EnableCap.FramebufferSrgb);

            lights.Render(Camera.GetWorldToCameraMatrix(), elapsedMillis);
            foreach (DisplayObject obj in DisplayObjects) {
                obj.Render(modelToCameraMatrixUnif, normalModelToCameraMatrixUnif, Camera.GetWorldToCameraMatrix());
            }
        }

        internal void Update(long elapsedMillis) {
            CameraMove();
            MouseMove();
        }

        protected void Keyboard_KeyDown(object sender, KeyboardKeyEventArgs e) {
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

        internal void initializeOpenGL(int program) {
            modelToCameraMatrixUnif = GL.GetUniformLocation(parentGame.gameProgram, "modelToCameraMatrix");
            normalModelToCameraMatrixUnif = GL.GetUniformLocation(parentGame.gameProgram, "normalModelToCameraMatrix");
            lights.InitializeOpenGL(program);
            MaterialLoader.LoadMaterialBufferBlock(program);
        }

        internal void add(IRigidBody newObject) {
            DisplayObjects.Add(newObject);
        }

        internal void add(MultiBodyObject newObject) {
            DisplayObjects.Add(newObject);
        }
    }
}
