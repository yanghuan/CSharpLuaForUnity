using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using UnityEditor;

[LuaAutoWrap]
public sealed class TestUtils {
  public static GameObject Load(string path) {
    GameObject prefab = (GameObject)AssetDatabase.LoadMainAssetAtPath(path);
    CSharpLua.BaseUtility.Provider.ConvertCustomMonoBehaviour(ref prefab);
    return prefab;
  }
}