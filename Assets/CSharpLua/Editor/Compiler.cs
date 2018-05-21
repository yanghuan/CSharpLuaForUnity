using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using UnityEditor;
using UnityEngine;

public static class Compiler {
  private const string kDotnet = "dotnet"; 
  private static readonly string compiledScriptDir_ = Application.dataPath + "/CSharpLua/CompiledScript/";
  private static readonly string outDir_ = Application.dataPath + "/Lua/CompiledScript/";
  private static readonly string toolsDir_ = Application.dataPath + "/../CSharpLuaTools";
  private static readonly string csharpLua_ = toolsDir_ + "/CSharp.lua/CSharp.lua.Launcher.dll";

  [MenuItem("CharpLua/Compile", false)]
  public static void Compile() {
    string[] libs = new string[] {
      typeof(UnityEngine.Object).Assembly.Location,
    };

    string lib = string.Join(";", libs);
    string args = $"{csharpLua_}  -s \"{compiledScriptDir_}\" -d \"{outDir_}\" -l \"{lib}\"";
    var info = new ProcessStartInfo() {
      FileName = kDotnet,
      Arguments = args,
      UseShellExecute = false,
      RedirectStandardOutput = true,
      RedirectStandardError = true,
    };

    using (var p = Process.Start(info)) {
      p.WaitForExit();
      if (p.ExitCode != 0) {
        string outString = p.StandardOutput.ReadToEnd();
        string errorString = p.StandardError.ReadToEnd();
        throw new Exception($"Compile fail, {kDotnet} {args}, \n{outString}, {errorString}");
      }
    }
  }
}
