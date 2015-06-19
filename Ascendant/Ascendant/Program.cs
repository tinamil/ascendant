using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ascendant.Graphics;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
namespace Ascendant {
  class Program {
    static void Main(string[] args) {
      using(var window = new Window(640, 480, GraphicsMode.Default, AppConfig.Default.title, GameWindowFlags.Default,  DisplayDevice.Default, 4, 0, GraphicsContextFlags.Debug)) {
        window.Run(60);
        
      }
    }
  }
}
