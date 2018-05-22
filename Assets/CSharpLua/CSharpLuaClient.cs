using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

public sealed class CSharpLuaClient : LuaClient {
  private const bool kIsRunFromLua = true;

  private sealed class BridgeMonoBehaviour : MonoBehaviour {
  }

  protected override void OpenLibs() {
    base.OpenLibs();
    OpenCJson();
  }

  protected override void StartMain() {
    if (kIsRunFromLua) {
      base.StartMain();
    }
    Run<TestHelloWord>();
  }

  private void Run<T>() where T : MonoBehaviour {
    if (kIsRunFromLua) {
      var bridge = gameObject.AddComponent<BridgeMonoBehaviour>();
      using (var fn = luaState.GetFunction("bindMonoBehaviour")) {
        fn.Call(bridge, typeof(T).FullName);
      }
    } else {
      gameObject.AddComponent<T>();
    }
  }
}