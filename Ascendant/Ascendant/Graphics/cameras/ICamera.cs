using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;

namespace Ascendant.Graphics {
  interface ICamera {
    Matrix4 GetWorldToCameraMatrix();
    void Move(Vector3 translation);
    void LookAt(Vector3 point);
    void Rotate(Vector2 rotation);
  }
}
