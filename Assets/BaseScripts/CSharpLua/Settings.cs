using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using UnityEngine;

namespace CSharpLua {
  public static partial class Settings {
    public static class Paths {
      public static readonly string CompiledScriptDir = LuaConst.rootDir + "/CompiledScripts";
      public static readonly string CompiledOutDir = LuaConst.rootDir + "/Lua/CompiledScripts";
      public static readonly string ToolsDir = Application.dataPath + "/../CSharpLuaTools";
      public const string kTempDir = "Assets/CSharpLuaTemp";
      public const string kBaseScripts = "BaseScripts";
      public const string kCompiledScripts = "CompiledScripts";
      public const string kBaseScriptsDir = "/" + kBaseScripts;
      public static readonly string SettingFilePath = LuaConst.rootDir + kBaseScriptsDir + "/CSharpLua/Settings.cs";
    }

#if UNITY_EDITOR
        public static class Menus {
      //public const string kCompile = "Lua/Compile C# To Lua";
      public const string kRunFromCSharp = "Lua/Switch to Run From C#";
      public const string kRunFromLua = "Lua/Swicth to Run From Lua";
    }
#endif
  }
}
