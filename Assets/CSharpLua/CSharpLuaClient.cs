using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

public sealed class CSharpLuaClient : LuaClient {
  protected override void OpenLibs() {
    base.OpenLibs();
    OpenCJson();
    Run<TestHelloWord>();
  }

  private void Run<T>() where T : MonoBehaviour {
    gameObject.AddComponent<T>();
  }
}