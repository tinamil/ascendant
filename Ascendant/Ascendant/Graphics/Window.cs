using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System.IO;
using System.Diagnostics;

namespace Ascendant.Graphics {
  class Window : GameWindow {

    Stopwatch stopwatch = new Stopwatch();
    FirstPersonCamera camera;
    int theProgram;
    int positionAttrib;
    int colorAttrib;
    int modelToCameraMatrixUnif;
    int cameraToClipMatrixUnif;
    int baseColorUnif;

    Matrix4 cameraToClipMatrix = Matrix4.Zero;
    private int numberOfVertices = 8;

    private int vertexBufferObject;
    private int indexBufferObject;
    private int vao;

    private static readonly Vector4 GREEN_COLOR = new Vector4(0.0f, 1.0f, 0.0f, 1.0f);
    private static readonly Vector4 BLUE_COLOR = new Vector4(0.0f, 0.0f, 1.0f, 1.0f);
    private static readonly Vector4 RED_COLOR = new Vector4(1.0f, 0.0f, 0.0f, 1.0f);
    private static readonly Vector4 YELLOW_COLOR = new Vector4(1.0f, 1.0f, 0.0f, 1.0f);
    private static readonly Vector4 CYAN_COLOR = new Vector4(0.0f, 1.0f, 1.0f, 1.0f);
    private static readonly Vector4 MAGENTA_COLOR = new Vector4(1.0f, 0.0f, 1.0f, 1.0f);
    private static readonly Vector4 GREY_COLOR = new Vector4(0.8f, 0.8f, 0.8f, 1.0f);
    private static readonly Vector4 BROWN_COLOR = new Vector4(0.5f, 0.5f, 0.0f, 1.0f);

    private static readonly float[] vertexData = {
	      +1.0f, +1.0f, +1.0f,
	      -1.0f, -1.0f, +1.0f,
	      -1.0f, +1.0f, -1.0f,
	      +1.0f, -1.0f, -1.0f,

	      -1.0f, -1.0f, -1.0f,
	      +1.0f, +1.0f, -1.0f,
	      +1.0f, -1.0f, +1.0f,
	      -1.0f, +1.0f, +1.0f,
		};

    private static readonly Vector4[] vertexDataColors =	{
	    GREEN_COLOR,
	    BLUE_COLOR,
	    RED_COLOR,
	    BROWN_COLOR,

	    GREEN_COLOR,
	    BLUE_COLOR,
	    RED_COLOR,
	    BROWN_COLOR,
		};

    private short[] indexData =
		{
	    0, 1, 2,
	    1, 0, 3,
	    2, 3, 0,
	    3, 2, 1,

	    5, 4, 6,
	    4, 5, 7,
	    7, 6, 4,
	    6, 7, 5,
		};


    readonly float fFrustumScale = CalcFrustumScale(20f);

    static readonly Quaternion[] g_Orients = {
      new Quaternion(0.7071f, 0.7071f, 0.0f, 0.0f),
	    new Quaternion(0.5f, 0.5f, -0.5f, 0.5f),
	    new Quaternion(-0.4895f, -0.7892f, -0.3700f, -0.02514f),
	    new Quaternion(0.4895f, 0.7892f, 0.3700f, 0.02514f),

	    new Quaternion(0.3840f, -0.1591f, -0.7991f, -0.4344f),
	    new Quaternion(0.5537f, 0.5208f, 0.6483f, 0.0410f),
	    new Quaternion(0.0f, 0.0f, 1.0f, 0.0f),
                                             };

    static Key[] g_OrientKeys =
{
	Key.Q,
	Key.W,
	Key.E,
	Key.R,

	Key.T,
	Key.Y,
	Key.U,
};

    static float CalcFrustumScale(float fFovDeg) {
      const float degToRad = 3.14159f * 2.0f / 360.0f;
      float fFovRad = fFovDeg * degToRad;
      return (float)(1.0f / Math.Tan(fFovRad / 2.0f));
    }

    void InitializeProgram() {
      var shaderList = new List<int>();

      shaderList.Add(LoadShader(ShaderType.VertexShader, @"Graphics\data\PosColorLocalTransform.vert"));
      shaderList.Add(LoadShader(ShaderType.FragmentShader, @"Graphics\data\ColorMultUniform.frag"));

      theProgram = CreateProgram(shaderList);

      positionAttrib = GL.GetAttribLocation(theProgram, "position");
      colorAttrib = GL.GetAttribLocation(theProgram, "color");

      modelToCameraMatrixUnif = GL.GetUniformLocation(theProgram, "modelToCameraMatrix");
      cameraToClipMatrixUnif = GL.GetUniformLocation(theProgram, "cameraToClipMatrix");
      baseColorUnif = GL.GetUniformLocation(theProgram, "baseColor");


      float fzNear = 1.0f;
      float fzFar = 600.0f;
      cameraToClipMatrix.Row0.X = fFrustumScale;
      cameraToClipMatrix.Row1.Y = fFrustumScale;
      cameraToClipMatrix.Row2.Z = (fzFar + fzNear) / (fzNear - fzFar);
      cameraToClipMatrix.Row2.W = -1.0f;
      cameraToClipMatrix.Row3.Z = (2 * fzFar * fzNear) / (fzNear - fzFar);

      GL.UseProgram(theProgram);
      GL.UniformMatrix4(cameraToClipMatrixUnif, false, ref camera.Matrix);
      GL.UseProgram(0);
    }

    void InitializeVAO() {
      GL.GenBuffers(1, out vertexBufferObject);

      GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferObject);
      GL.BufferData(BufferTarget.ArrayBuffer,
        (IntPtr)((vertexData.Length * sizeof(float)) +
        (vertexDataColors.Length * Vector4.SizeInBytes)),
        vertexData,
        BufferUsageHint.StaticDraw);
      GL.BufferSubData(BufferTarget.ArrayBuffer, (IntPtr)(vertexData.Length * sizeof(float)),
        (IntPtr)(vertexDataColors.Length * Vector4.SizeInBytes),
        vertexDataColors);
      GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

      GL.GenBuffers(1, out indexBufferObject);

      GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBufferObject);
      GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(indexData.Length * sizeof(short)), indexData, BufferUsageHint.StaticDraw);
      GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

      GL.GenVertexArrays(1, out vao);
      GL.BindVertexArray(vao);

      GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferObject);
      GL.EnableVertexAttribArray(positionAttrib);
      GL.EnableVertexAttribArray(colorAttrib);

      GL.VertexAttribPointer(positionAttrib, 3, VertexAttribPointerType.Float, false, 0, 0);
      GL.VertexAttribPointer(colorAttrib, 4, VertexAttribPointerType.Float, false, 0, sizeof(float) * 3 * numberOfVertices);
      GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBufferObject);

      GL.BindVertexArray(0);
    }
    Vector4 Vectorize(ref Quaternion theQuat) {
      Vector4 ret;
      ret.X = theQuat.X;
      ret.Y = theQuat.Y;
      ret.Z = theQuat.Z;
      ret.W = theQuat.W;
      return ret;
    }
    Quaternion Lerp(ref Quaternion v0, ref Quaternion v1, float alpha) {
      Vector4 start = Vectorize(ref v0);
      Vector4 end = Vectorize(ref v1);
      Vector4 interp = Vector4.Lerp(start, end, alpha);

      Debug.WriteLine("alpha: %f, (%f, %f, %f, %f)\n", alpha, interp.W, interp.X, interp.Y, interp.Z);

      interp = Vector4.Normalize(interp);
      return new Quaternion(interp.X, interp.Y, interp.Z, interp.W);
    }


    Vector3 StationaryOffset(float fElapsedTime) {
      return new Vector3(0.0f, 0.0f, -20.0f);
    }

    Vector3 OvalOffset(float fElapsedTime) {
      const float fLoopDuration = 3.0f;
      const float fScale = 3.14159f * 2.0f / fLoopDuration;

      float fCurrTimeThroughLoop = fElapsedTime % fLoopDuration;

      return new Vector3((float)Math.Cos(fCurrTimeThroughLoop * fScale) * 4.0f,
        (float)Math.Sin(fCurrTimeThroughLoop * fScale) * 6.0f,
        -20.0f);
    }

    Vector3 BottomCircleOffset(float fElapsedTime) {
      const float fLoopDuration = 12.0f;
      const float fScale = 3.14159f * 2.0f / fLoopDuration;

      float fCurrTimeThrouhLoop = fElapsedTime % fLoopDuration;

      return new Vector3((float)Math.Cos(fCurrTimeThrouhLoop * fScale) * 5.0f,
        -3.5f,
        (float)Math.Sin(fCurrTimeThrouhLoop * fScale) * 5.0f - 20.0f);
    }

    public static int LoadShader(ShaderType eShaderType, string strShaderFilename) {
      if(!File.Exists(strShaderFilename)) {
        Debug.WriteLine("Could not find the file " + strShaderFilename, "Error");
        throw new Exception("Could not find the file " + strShaderFilename);
      }
      int shader = GL.CreateShader(eShaderType);
      using(var reader = new StreamReader(strShaderFilename)) {
        GL.ShaderSource(shader, reader.ReadToEnd());
      }
      GL.CompileShader(shader);
      int status;
      GL.GetShader(shader, ShaderParameter.CompileStatus, out status);
      if(status == 0) {
        string strInfoLog;
        GL.GetShaderInfoLog(shader, out strInfoLog);

        string strShaderType;
        switch(eShaderType) {
          case ShaderType.GeometryShader:
            strShaderType = "geometry";
            break;
          case ShaderType.FragmentShader:
            strShaderType = "fragment";
            break;
          case ShaderType.VertexShader:
            strShaderType = "vertex";
            break;
          default:
            throw new ArgumentOutOfRangeException("eShaderType");
        }

        Debug.WriteLine("Compile failure in " + strShaderType + " shader:\n" + strInfoLog, "Error");
        throw new Exception("Compile failure in " + strShaderType + " shader:\n" + strInfoLog);
      }

      return shader;
    }
    public static int CreateProgram(List<int> shaderList) {
      int program = GL.CreateProgram();

      foreach(int shader in shaderList) {
        GL.AttachShader(program, shader);
      }

      GL.LinkProgram(program);

      int status;
      GL.GetProgram(program, GetProgramParameterName.LinkStatus, out status);
      if(status == 0) {
        string strInfoLog;
        GL.GetProgramInfoLog(program, out strInfoLog);
        Debug.WriteLine("Linker failure: " + strInfoLog, "Error");
        throw new Exception("Linker failure: " + strInfoLog);
      }

      foreach(int shader in shaderList) {
        GL.DetachShader(program, shader);
      }

      return program;
    }

    protected override void OnLoad(EventArgs e) {
      base.OnLoad(e);
      camera = new FirstPersonCamera();
      camera.Position = new Vector3(0, 5, 0);


      stopwatch.Start();
      KeyDown += Keyboard_KeyDown;

      InitializeProgram();
      InitializeVAO();


      GL.Enable(EnableCap.CullFace);
      GL.CullFace(CullFaceMode.Back);
      GL.FrontFace(FrontFaceDirection.Cw);

      GL.Enable(EnableCap.DepthTest);
      GL.DepthMask(true);
      GL.DepthFunc(DepthFunction.Lequal);
      GL.DepthRange(0.0f, 1.0f);
    }

    void Keyboard_KeyDown(object sender, KeyboardKeyEventArgs e) {
      switch(e.Key) {
        case Key.Escape:
          Exit();
          break;
      }
    }

    protected override void OnRenderFrame(FrameEventArgs e) {
      base.OnRenderFrame(e);
      GL.ClearColor(System.Drawing.Color.CornflowerBlue);
      GL.ClearDepth(1.0f);
      GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

      GL.UseProgram(theProgram);
      
      GL.BindVertexArray(vao);

      float fElapsedTime = stopwatch.ElapsedMilliseconds / 1000.0f;

      var g_instanceList = new List<Matrix4>();
      Matrix4 stationaryMatrix = camera.Matrix;
      stationaryMatrix.Row3 = new Vector4(StationaryOffset(fElapsedTime), 1.0f);
      g_instanceList.Add(stationaryMatrix);
      Matrix4 ovalMatrix = camera.Matrix;
      ovalMatrix.Row3 = new Vector4(OvalOffset(fElapsedTime), 1.0f);
      g_instanceList.Add(ovalMatrix);
      Matrix4 bottomCircleMatrix = camera.Matrix;
      bottomCircleMatrix.Row3 = new Vector4(BottomCircleOffset(fElapsedTime), 1.0f);
      g_instanceList.Add(bottomCircleMatrix);

      for(int i = 0; i < g_instanceList.Count; ++i) {
        var currInst = g_instanceList.ElementAt(i);
        GL.UniformMatrix4(modelToCameraMatrixUnif, false, ref currInst);
        GL.DrawElements(BeginMode.Triangles, indexData.Length, DrawElementsType.UnsignedShort, 0);
      }

      GL.BindVertexArray(0);
      GL.UseProgram(0);
      SwapBuffers();
    }
    protected override void OnResize(EventArgs e) {
      base.OnResize(e);

      camera.Resize(Width, Height);


      GL.Viewport(0, 0, Width, Height);
    }

    MouseState current, previous = OpenTK.Input.Mouse.GetState();

    Vector2 mouseSpeed = new Vector2();
    float mouseSpeedValue = 0.7f;
    protected override void OnUpdateFrame(FrameEventArgs e) {
      base.OnUpdateFrame(e);
      Func<Key, Key, float, float> checkKeyState = (_keyA, _keyB, _value) => {
        if(Keyboard[_keyA])
          return _value;
        if(Keyboard[_keyB])
          return -_value;
        return 0f;
      };

      current = OpenTK.Input.Mouse.GetState();
      var time = (float)e.Time;
      var moveSpeed = 5 * time;
      if(current != previous) {
        mouseSpeed.X = (current.Y - previous.Y) * mouseSpeedValue * time;
        mouseSpeed.Y = (current.X - previous.X) * mouseSpeedValue * time;
        if(current[MouseButton.Left]) {
          camera.TurnX(mouseSpeed.X);
          camera.TurnY(-mouseSpeed.Y);
        }
      }
      previous = current;
      camera.MoveX(checkKeyState(Key.A, Key.D, moveSpeed));

      if(Keyboard[Key.ControlLeft]) {
        camera.MoveYLocal(checkKeyState(Key.Space, Key.C, moveSpeed));
        camera.MoveZLocal(checkKeyState(Key.W, Key.S, moveSpeed));
      } else {
        camera.MoveY(checkKeyState(Key.Space, Key.C, moveSpeed));
        camera.MoveZ(checkKeyState(Key.W, Key.S, moveSpeed));
      }

      camera.Update();
    }
  }
}




