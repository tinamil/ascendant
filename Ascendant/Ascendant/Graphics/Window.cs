using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System.IO;
using System.Diagnostics;
using OpenTK.Graphics;

namespace Ascendant.Graphics {
  class Window : GameWindow {

    Game game;
    
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
      GL.ClearColor(System.Drawing.Color.PowderBlue);
      GL.ClearDepth(1.0f);
      GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

      game.Render();

      SwapBuffers();
    }

   
    protected void ResizeWindow(object sender, EventArgs e) {
      game.Resize();
      GL.Viewport(0, 0, Width, Height);
    }

    protected void UpdateWindowFrame(object sender, FrameEventArgs e) {
      game.Update();
    }

    protected void WindowFocusChange(object sender, EventArgs e) {
      game.FocusChange();
    }

    public Window(int width, int height, GraphicsMode graphicsMode, String title, GameWindowFlags windowFlags,
      DisplayDevice displayDevice, int majorVersion, int minorVersion, GraphicsContextFlags graphicsContextFlags)
      : base(width, height, graphicsMode, title, windowFlags, displayDevice, majorVersion, minorVersion, graphicsContextFlags) {
        game = new Game(this);
      //this.Closed
      //this.Closing
      //this.Disposed
      this.FocusedChanged += WindowFocusChange;
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
      this.UpdateFrame += UpdateWindowFrame;
      //this.VisibleChanged
      //this.WindowBorderChanged
      //this.WindowStateChanged
    }
  }
}




