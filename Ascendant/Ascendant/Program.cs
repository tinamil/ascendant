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
      using(var window = new Window()) {
        window.Run(60);
      }
    }
  }
}
