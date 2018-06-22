using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using UnityEngine;

namespace CSharpLua {
  public static class Settings {
#if UNITY_EDITOR
    public static readonly string CompiledScriptDir = Application.dataPath + "/CompiledScripts";
    public static readonly string CompiledOutDir = Application.dataPath + "/Lua/CompiledScripts";
    public static readonly string ToolsDir = Application.dataPath + "/../CSharpLuaTools";
    public static readonly string TempDir = "Assets/CSharpLuaTemp";
    public static readonly string SettingFilePath = Application.dataPath + "/CSharpLua/Settings.cs";
#endif

    public const bool kIsRunFromLua = true;
  }
}
