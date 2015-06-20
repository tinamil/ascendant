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
    Window window;

    int gameProgram;
    int positionAttrib;
    int colorAttrib;

    int modelToCameraMatrixUnif;
    int cameraToClipMatrixUnif;

    int baseColorUnif;

    Matrix4 CameraToClipMatrix;

    DisplayObject diamond;

    public Game(Window window) {
      this.window = window;
      window.KeyDown += Keyboard_KeyDown;
      window.UpdateFrame += Update;
      window.FocusedChanged += FocusChange;
      window.Load += GameLoad;
      InitalizeOpenGL();
      diamond = new DisplayObject(this, Vector3.Zero, Vector3.One, Quaternion.FromAxisAngle(Vector3.UnitY, 0), @"Graphics\objects\mesh\Diamond.obj");
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
      Debug.WriteLine("Key down: " + e.Key);
      Vector3 movement = Vector3.Zero;
      if(e.Keyboard.IsKeyDown(Key.Escape))
        window.Exit();
      if(e.Keyboard.IsKeyDown(Key.W)) {
        movement.Y += .1f;
      }
      if(e.Keyboard.IsKeyDown(Key.A)) {
        movement.X += -.1f;
      }
      if(e.Keyboard.IsKeyDown(Key.S)) {
        movement.Y += -.1f;
      }
      if(e.Keyboard.IsKeyDown(Key.D)) {
        movement.X += .1f;
      }
      if(e.Keyboard.IsKeyDown(Key.LShift)) {
        movement.Z += .1f;
      }
      if(e.Keyboard.IsKeyDown(Key.LControl)) {
        movement.Z -= .1f;
      }
      Camera.Move(movement);
    }

    public void Update(object o, EventArgs e) {
      MouseMove();
    }

    public void Render() {
      GL.UseProgram(gameProgram);

      //Set the base color for all objects.
      GL.Uniform4(baseColorUnif, 1.0f, 1.0f, 1.0f, 1.0f);
      var Matrix = Matrix4.Identity;
      diamond.Render(ref Matrix, modelToCameraMatrixUnif);
      GL.UseProgram(0);
    }

    MouseState previous;
    protected void ResetCursor() {
      OpenTK.Input.Mouse.SetPosition(window.Bounds.Left + window.Bounds.Width / 2, window.Bounds.Top + window.Bounds.Height / 2);
      previous = Mouse.GetState();
    }

    protected void MouseMove() {
      var current = Mouse.GetState();
      if(current != previous ) {
        Vector2 delta = new Vector2(current.X - previous.X, current.Y - previous.Y);
        Debug.WriteLine("Mouse deltas: " + delta.ToString());
        Camera.Rotate(delta);
        ResetCursor();
      }
    }

    protected internal void FocusChange(object o, EventArgs e) {
      if(window.Focused) {
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
  
    internal void GameLoad(object o, EventArgs e){
        window.CursorVisible = false;
    }
  }
}
