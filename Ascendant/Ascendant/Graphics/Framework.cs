using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Graphics.OpenGL;
using System.IO;
using System.Diagnostics;

namespace Ascendant.Graphics {
  class Framework {
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

        Debug.WriteLine("Compile failure in " + eShaderType.ToString() + " shader:\n" + strInfoLog, "Error");
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
      }

      foreach(int shader in shaderList) {
        GL.DetachShader(program, shader);
        GL.DeleteShader(shader);
      }

      return program;
    }
  }
}
