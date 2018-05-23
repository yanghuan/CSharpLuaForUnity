using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

[LuaAutoWrap]
public sealed class BridgeMonoBehaviour : MonoBehaviour {
}

public sealed class LaunchClient : LuaClient {
  private const bool kIsRunFromLua = true;
  public string LaunchTypeName;

  protected override void OpenLibs() {
    base.OpenLibs();
    OpenCJson();
  }

  protected override void StartMain() {
	if (string.IsNullOrEmpty(LaunchTypeName)) {
	  throw new ArgumentException("LaunchTypeName is null");
	}

    if (kIsRunFromLua) {
      base.StartMain();
	  using (var fn = luaState.GetFunction("UnityEngine.AddComponentOfType")) {
		fn.Call(gameObject, LaunchTypeName);
	  }
    } else {
	  Type componentType = Type.GetType(LaunchTypeName, true, false);
	  gameObject.AddComponent(componentType);
	}
  }
}