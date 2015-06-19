using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Ascendant.Graphics {
  class DisplayObject {
    Vector3 position;
    Vector3 scale;
    Quaternion orientation;

    internal void Render(Matrix4 parentMatrix, int modelToCameraMatrixUnif) {
      //Translate
      Matrix4 ModelToWorldMatrix = parentMatrix * Matrix4.CreateTranslation(position);
      //Rotate
      ModelToWorldMatrix = ModelToWorldMatrix * Matrix4.CreateFromQuaternion(orientation);
      //Scale
      ModelToWorldMatrix = ModelToWorldMatrix * Matrix4.CreateScale(scale);
      GL.UniformMatrix4(modelToCameraMatrixUnif, false, ref ModelToWorldMatrix);
    }
  }
}
