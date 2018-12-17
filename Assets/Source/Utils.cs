using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[LuaAutoWrap]
public static class Utils {
  public static MonoBehaviour MonoBehaviourInstance {
    get {
      return CSharpLua.CSharpLuaClient.Instance;
    }
  }
}
