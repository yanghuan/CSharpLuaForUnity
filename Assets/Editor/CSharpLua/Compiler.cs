using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Text;

using UnityEditor;
using UnityEngine;

namespace CSharpLua {
  public static class Compiler {
    private sealed class CompiledFail : Exception {
      public CompiledFail(string message) : base(message) {
      }
    }

    private const string kDotnet = "dotnet";
    private static readonly string compiledScriptDir_ = Settings.Paths.CompiledScriptDir;
    private static readonly string outDir_ = Settings.Paths.CompiledOutDir;
    private static readonly string toolsDir_ = Settings.Paths.ToolsDir;
    private static readonly string csharpLua_ = toolsDir_ + "/CSharp.lua/CSharp.lua.Launcher.dll";
    private static readonly string settingFilePath_ = Settings.Paths.SettingFilePath;

    [MenuItem(Settings.Menus.kCompile)]
    public static void Compile() {
      if (!CheckDotnetInstall()) {
        return;
      }

      if (!File.Exists(csharpLua_)) {
        throw new InvalidProgramException($"{csharpLua_} not found");
      }

      if (Directory.Exists(outDir_)) {
        Directory.Delete(outDir_, true);
      }

      HashSet<string> libs = new HashSet<string>();
      FillUnityLibraries(libs);
      AssemblyName assemblyName = new AssemblyName(Settings.Paths.kCompiledScripts);
      Assembly assembly = Assembly.Load(assemblyName);
      foreach (var referenced in assembly.GetReferencedAssemblies()) {
        if (referenced.Name != "mscorlib" && !referenced.Name.StartsWith("System")) {
          string libPath = Assembly.Load(referenced).Location;
          libs.Add(libPath);
        }
      }

      string[] metas = new string[] { toolsDir_ + "/UnityEngine.xml" };
      string lib = string.Join(";", libs.ToArray());
      string meta = string.Join(";", metas);
      string args = $"{csharpLua_}  -s \"{compiledScriptDir_}\" -d \"{outDir_}\" -l \"{lib}\" -m {meta} -c";
      string definesString = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
      if (!string.IsNullOrEmpty(definesString)) {
        args += $" -csc -define:{definesString}";
      }

      var info = new ProcessStartInfo() {
        FileName = kDotnet,
        Arguments = args,
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        StandardOutputEncoding = Encoding.UTF8,
        StandardErrorEncoding = Encoding.UTF8,
      };
      using (var p = Process.Start(info)) {
        p.WaitForExit();
        if (p.ExitCode == 0) {
          UnityEngine.Debug.Log("compile success");
        } else {
          string outString = p.StandardOutput.ReadToEnd();
          string errorString = p.StandardError.ReadToEnd();
          throw new CompiledFail($"Compile fail, {errorString}\n{outString}\n{kDotnet} {args}");
        }
      }
    }

    private static void FillUnityLibraries(HashSet<string> libs) {
      string unityObjectPath = typeof(UnityEngine.Object).Assembly.Location;
      string baseDir = Path.GetDirectoryName(unityObjectPath);
      foreach (string path in Directory.EnumerateFiles(baseDir, "*.dll")) {
        libs.Add(path);
      }
    }

    private static bool CheckDotnetInstall() {
      bool has = InternalCheckDotnetInstall();
      if (!has) {
        UnityEngine.Debug.LogWarning("not found dotnet");
        if (EditorUtility.DisplayDialog("dotnet未安装", "未安装.NET Core 3.0+运行环境，点击确定前往安装", "确定", "取消")) {
          Application.OpenURL("https://www.microsoft.com/net/download");
        }
      }
      return has;
    }

    private static bool InternalCheckDotnetInstall() {
      var info = new ProcessStartInfo() {
        FileName = kDotnet,
        Arguments = "--version",
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        CreateNoWindow = true,
        StandardOutputEncoding = Encoding.UTF8,
        StandardErrorEncoding = Encoding.UTF8,
      };
      try {
        using (var p = Process.Start(info)) {
          p.WaitForExit();
          if (p.ExitCode == 0) {
            string version = p.StandardOutput.ReadToEnd();
            UnityEngine.Debug.LogFormat("found dotnet {0}", version);
            int major = version[0] - '0';
            if (major >= 3) {
              return true;
            } else {
              UnityEngine.Debug.LogErrorFormat("dotnet verson {0} must >= 3.0", version);
            }
          }
          return false;
        }
      } catch (Exception e) {
        UnityEngine.Debug.LogException(e);
        return false;
      }
    }

    [MenuItem(Settings.kIsRunFromLua ? Settings.Menus.kRunFromCSharp : Settings.Menus.kRunFromLua)]
    public static void Switch() {
#if UNITY_2017 || UNITY_2018
      const string kFieldName = nameof(Settings.kIsRunFromLua);
#else
      const string kFieldName = "kIsRunFromLua";
#endif

      string text = File.ReadAllText(settingFilePath_);
      int begin = text.IndexOf(kFieldName);
      if (begin != -1) {
        int end = text.IndexOf(';', begin + kFieldName.Length);
        if (end != -1) {
          string s = text.Substring(begin, end - begin);
          string[] array = s.Split('=');
          bool v = bool.Parse(array[1]);
          string replace = kFieldName + " = " + (v ? "false" : "true");
          text = text.Replace(s, replace);
          File.WriteAllText(settingFilePath_, text);
          AssetDatabase.Refresh();
        } else {
          throw new InvalidProgramException($"field {kFieldName} not found end symbol in {settingFilePath_}");
        }
      } else {
        throw new InvalidProgramException($"not found field {kFieldName} in {settingFilePath_}");
      }
    }
  }

#if UNITY_2018
  [InitializeOnLoad]
  public class EditorQuitHandler {
    static void Quit() {
      string tempDir = Settings.Paths.kTempDir;
      if (Directory.Exists(tempDir)) {
        Directory.Delete(tempDir, true);
      }
    }

    static EditorQuitHandler() {
      EditorApplication.quitting += Quit;
    }
  }
#endif
}

