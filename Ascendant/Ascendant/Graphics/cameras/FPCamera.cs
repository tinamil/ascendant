using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using Debug = System.Diagnostics.Debug;

namespace Ascendant.Graphics {

  class FirstPersonCamera : ICamera  {
    protected Vector3 Position = Vector3.Zero;
    protected Vector3 Rotation = Vector3.Zero;
    protected Quaternion Orientation = Quaternion.Identity;

    public FirstPersonCamera() {
    }

    public Matrix4 GetWorldToCameraMatrix() {
      Orientation =
        Quaternion.FromAxisAngle(Vector3.UnitY, Rotation.Y) *
        Quaternion.FromAxisAngle(Vector3.UnitX, Rotation.X) *
        Quaternion.FromAxisAngle(Vector3.UnitZ, Rotation.Z);

      var forward = Vector3.Transform(Vector3.UnitZ, Orientation);
      return Matrix4.LookAt(Position, Position + forward, Vector3.UnitY);
    }

    public void Rotate(Vector2 newRotation){
      TurnX(newRotation.X);
      TurnY(newRotation.Y);
    }
    public void Move(Vector3 translation) {
      MoveX(translation.X);
      MoveY(translation.Y);
      MoveZ(translation.Z);
    }

    public void LookAt(Vector3 lookTarget) {
      Rotation = lookTarget;
    }

    public void TurnX(float a) {
      Rotation.X += a;
      Rotation.X = MathHelper.Clamp(Rotation.X, -1.57f, 1.57f);
    }

    public void TurnY(float a) {
      Rotation.Y += a;
      Rotation.Y = ClampCircular(Rotation.Y, 0, MathHelper.TwoPi);
    }

    public void MoveX(float a) {
      Position += Vector3.Transform(Vector3.UnitX * a, Quaternion.FromAxisAngle(Vector3.UnitY, Rotation.Y));
    }

    public void MoveY(float a) {
      Position += Vector3.Transform(Vector3.UnitY * a, Quaternion.FromAxisAngle(Vector3.UnitY, Rotation.Y));
    }

    public void MoveZ(float a) {
      Position += Vector3.Transform(Vector3.UnitZ * a, Quaternion.FromAxisAngle(Vector3.UnitY, Rotation.Y));
    }

    public void MoveYLocal(float a) {
      Position += Vector3.Transform(Vector3.UnitY * a, Orientation);
    }

    public void MoveZLocal(float a) {
      Position += Vector3.Transform(Vector3.UnitZ * a, Orientation);
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
