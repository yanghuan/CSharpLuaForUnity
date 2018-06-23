using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using UnityEditor;
using CSharpLua;

namespace CSharpLua {
}

[LuaAutoWrap]
public sealed class TestUtils {
  public static GameObject Load(string path) {
    GameObject prefab = (GameObject)AssetDatabase.LoadMainAssetAtPath(path);
    if (Settings.kIsRunFromLua) {
      UserMonoBehaviourConverter.Default.Do(ref prefab);
    }
    return prefab;
  }
}