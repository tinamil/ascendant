using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ascendant.Graphics;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System.Diagnostics;
namespace Ascendant {
  class Program {
    static void Main(string[] args) {
      using(var window = new Window(640, 480, GraphicsMode.Default, AppConfig.Default.title, GameWindowFlags.Default, DisplayDevice.Default, 4, 0, GraphicsContextFlags.ForwardCompatible)) {
        Debug.WriteLine(GL.GetString(StringName.Extensions));
        Debug.WriteLine(GL.GetString(StringName.Renderer));
        Debug.WriteLine(GL.GetString(StringName.ShadingLanguageVersion));
        Debug.WriteLine(GL.GetString(StringName.Vendor));
        Debug.WriteLine(GL.GetString(StringName.Version));
        window.Run(60);

      }
    }
  }
}
