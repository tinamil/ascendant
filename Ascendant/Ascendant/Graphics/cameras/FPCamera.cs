using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using Debug = System.Diagnostics.Debug;

namespace Ascendant.Graphics {

  class FirstPersonCamera : ICamera {
    public Vector3 Position = new Vector3(0, 0, -10);
    public Vector3 Rotation = Vector3.Zero;
    public Quaternion Orientation = Quaternion.Identity;
    public Vector3 Up = Vector3.UnitY;

    protected Matrix4 WorldToCameraMatrix;
    protected bool cameraMoved = true;

    public FirstPersonCamera() {
    }

    public void RefreshMatrix() {
      Orientation =
          Quaternion.FromAxisAngle(Vector3.UnitY, Rotation.Y) *
          Quaternion.FromAxisAngle(Vector3.UnitX, Rotation.X);

      var forward = Vector3.Transform(Vector3.UnitZ, Orientation);
      WorldToCameraMatrix = Matrix4.LookAt(Position, Position + forward, Up);
    }

    public Matrix4 GetWorldToCameraMatrix() {
      if(cameraMoved) {
        RefreshMatrix();
        cameraMoved = false;
        //Debug.WriteLine("Position: {0}, {1}, {2}, Rotation: {3}, {4}, {5}", Position.X, Position.Y, Position.Z, Orientation.X, Orientation.Y, Orientation.Z);
      }
      return WorldToCameraMatrix;
    }

    public void Rotate(Vector2 newRotation) {
      TurnY(MathHelper.DegreesToRadians(-newRotation.X) * AppConfig.Default.mousespeed);
      TurnX(MathHelper.DegreesToRadians(newRotation.Y) * AppConfig.Default.mousespeed);
      cameraMoved = true;
    }
    public void Move(Vector3 translation) {
      MoveX(translation.X);
      MoveY(translation.Y);
      MoveZ(translation.Z);
      cameraMoved = true;
    }

    public void LookAt(Vector3 lookTarget) {
      throw new NotImplementedException();
    }

    internal void TurnX(float a) {
      if(Math.Abs(a) > float.Epsilon) {
        Rotation.X += a;
        Rotation.X = MathHelper.Clamp(Rotation.X, -1.57f, 1.57f);
      }
    }

    internal void TurnY(float a) {
      if(Math.Abs(a) > float.Epsilon) {
        Rotation.Y += a;
        Rotation.Y = ClampCircular(Rotation.Y, 0, MathHelper.TwoPi);
      }
    }

    internal void MoveX(float a) {
      if(Math.Abs(a) > float.Epsilon)
        Position -= Vector3.Transform(Vector3.UnitX * a, Orientation);
    }

    internal void MoveY(float a) {
      if(Math.Abs(a) > float.Epsilon)
        Position += Vector3.Transform(Vector3.UnitZ * a, Orientation);
    }

    internal void MoveZ(float a) {
      if(Math.Abs(a) > float.Epsilon)
        Position += Vector3.Transform(Vector3.UnitY * a, Orientation);
    }

    public static float ClampCircular(float n, float min, float max) {
      if(n >= max)
        n -= max;
      if(n < min)
        n += max;
      return n;
    }
  }
}
