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
using Ascendant.Physics;
using System.Threading;
namespace Ascendant {
  class Program {
    static void Main(string[] args) {
        var sim = new Simulation();
      using(var window = new Window(sim)){
        Debug.WriteLine(GL.GetString(StringName.Extensions));
        Debug.WriteLine(GL.GetString(StringName.Renderer));
        Debug.WriteLine(GL.GetString(StringName.ShadingLanguageVersion));
        Debug.WriteLine(GL.GetString(StringName.Vendor));
        Debug.WriteLine(GL.GetString(StringName.Version));

        //Thread workerThread = new Thread(sim.RunSim);

        // Start the worker thread.
        //workerThread.Start();
        //Console.WriteLine("main thread: Starting worker thread...");

        // Loop until worker thread activates.
        //while (!workerThread.IsAlive) ;

        window.Run(60);

      }
    }
  }
}
