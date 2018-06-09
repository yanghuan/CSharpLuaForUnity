using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Linq;

using UnityEditor;
using UnityEngine;

namespace CSharpLua {
  public static class Compiler {
    private const string kDotnet = "dotnet";
    private static readonly string compiledScriptDir_ = Application.dataPath + "/CSharpLua/CompiledScripts/";
    private static readonly string outDir_ = Application.dataPath + "/Lua/CompiledScripts/";
    private static readonly string toolsDir_ = Application.dataPath + "/../CSharpLuaTools";
    private static readonly string csharpLua_ = toolsDir_ + "/CSharp.lua/CSharp.lua.Launcher.dll";

    [MenuItem("CharpLua/Compile")]
    public static void Compile() {
      if (Directory.Exists(outDir_)) {
        string[] files = Directory.GetFiles(outDir_, "*.lua");
        foreach (string file in files) {
          File.Delete(file);
        }
      }

      List<string> libs = new List<string>();
      AssemblyName assemblyName = Assembly.GetExecutingAssembly().GetReferencedAssemblies().Single(i => i.FullName.Contains("Assembly-CSharp"));
      Assembly assembly = Assembly.Load(assemblyName);
      libs.Add(assembly.Location);
      foreach (var referenced in assembly.GetReferencedAssemblies()) {
        if(referenced.Name != "mscorlib" && !referenced.Name.StartsWith("System")) {
          string libPath = Assembly.Load(referenced).Location;
          libs.Add(libPath);
        }
      }

      string[] metas = new string[] { toolsDir_ + "/UnityEngine.xml" };
      string lib = string.Join(";", libs.ToArray());
      string meta = string.Join(";", metas);
      string args = $"{csharpLua_}  -s \"{compiledScriptDir_}\" -d \"{outDir_}\" -l \"{lib}\" -m {meta}";
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
          throw new Exception($"Compile fail, {errorString}\n{outString}\n{kDotnet} {args}");
        } else {
          UnityEngine.Debug.Log("compile success");
        }
      }
    }
  }
}

