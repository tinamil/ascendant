using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using OpenTK;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Input;
using System.IO;
using System.Diagnostics;
using OpenTK.Graphics;
using Ascendant.Graphics.lighting;

namespace Ascendant.Graphics {
    class Window : GameWindow {

        Game game;

        internal float GammaValue = 2.2f;
        internal Physics.Simulation sim;

        protected void LoadWindow(object sender, EventArgs e) {
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);
            GL.FrontFace(FrontFaceDirection.Cw);

            GL.Enable(EnableCap.DepthTest);
            GL.DepthMask(true);
            GL.DepthFunc(DepthFunction.Lequal);
            GL.DepthRange(0.0f, 1.0f);
        }


        protected void RenderWindow(object sender, FrameEventArgs e) {
            Vector4 bkg = Lighting.GammaCorrect(game.Lights.GetBackgroundColor(), GammaValue);

            GL.ClearColor(bkg[0], bkg[1], bkg[2], bkg[3]);
            GL.ClearDepth(1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            game.Render();

            SwapBuffers();
        }


        protected void ResizeWindow(object sender, EventArgs e) {
            game.Resize();
            GL.Viewport(0, 0, Width, Height);
        }


        private Window(int width, int height, GraphicsMode graphicsMode, String title, GameWindowFlags windowFlags,
          DisplayDevice displayDevice, int majorVersion, int minorVersion, GraphicsContextFlags graphicsContextFlags)
            : base(width, height, graphicsMode, title, windowFlags, displayDevice, majorVersion, minorVersion, graphicsContextFlags) {
            initialize();
        }

        private void initialize() {
            game = new Game(this);
            //this.Closed
            //this.Closing += (o, e) => sim.RequestStop();
            //this.Disposed
            //this.FocusedChanged += WindowFocusChange;
            //this.IconChanged
            //this.KeyDown
            //this.KeyPress
            //this.KeyUp
            this.Load += LoadWindow;
            //this.MouseDown
            //this.MouseEnter
            //this.MouseLeave
            //this.MouseMove
            //this.MouseUp
            //this.MouseWheel
            //this.Move
            this.RenderFrame += RenderWindow;
            this.Resize += ResizeWindow;
            //this.TitleChanged
            //this.Unload                  
            //this.UpdateFrame += UpdateWindowFrame;
            //this.VisibleChanged
            //this.WindowBorderChanged
            //this.WindowStateChanged
        }

        private Window() {
            initialize();
        }

        public Window(Physics.Simulation sim) {
           this.sim = sim;
           initialize();
        }
    }
}




