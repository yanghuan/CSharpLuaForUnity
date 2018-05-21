using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

public sealed class CSharpLuaClient : LuaClient {
  private const bool kIsRunFromLua = true;

  protected override void OpenLibs() {
    base.OpenLibs();
    OpenCJson();
    Run<TestHelloWord>();
  }

  protected override void StartMain() {
    if (kIsRunFromLua) {
      base.StartMain();
    }
  }

  private void Run<T>() where T : MonoBehaviour {
    gameObject.AddComponent<T>();
  }
}