using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using UnityEngine;

namespace CSharpLua {
  public static class Settings {
#if UNITY_EDITOR
    public static class Paths {
      public static readonly string CompiledScriptDir = Application.dataPath + "/CSharpLua/Compiled";
      public static readonly string CompiledOutDir = Application.dataPath + "/Lua/Compiled";
      public static readonly string ToolsDir = Application.dataPath + "/../Tools";
      public const string kTempDir = "Assets/CSharpLuaTemp";
      public const string kCompiledScripts = "Compiled";
      public static readonly string SettingFilePath = Application.dataPath + "/CSharpLua/Base/CSharpLua/Settings.cs";
    }

    public static class Menus {
      public const string kCompile = "CharpLua/Compile";
      public const string kRunFromCSharp = "CharpLua/Switch to RunFromCSharp";
      public const string kRunFromLua = "CharpLua/Swicth to RunFromLua";
      public const string kGenProtobuf = "CharpLua/Gen protobuf";
    }
#endif

    public const bool kIsRunFromLua = true;
  }
}
