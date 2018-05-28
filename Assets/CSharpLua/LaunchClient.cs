using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using LuaInterface;

[LuaAutoWrap]
public sealed class BridgeMonoBehaviour : MonoBehaviour {
  public LuaTable Table { get; private set; }

  public void Bind(LuaTable table) {
    Table = table;
  }

  public Coroutine StartCoroutine(LuaTable routine) {
    return StartCoroutine(new LuaIEnumerator(routine));
  }

  private void Start() {
    using (var fn = Table.GetLuaFunction("Start")) {
      fn.Call(Table);
    }
  }
}

sealed class LuaIEnumerator : IEnumerator, IDisposable {
  private LuaTable table_;
  private LuaFunction current_;
  private LuaFunction moveNext_;

  public LuaIEnumerator(LuaTable table) {
    table_ = table;
    current_ = table.GetLuaFunction("getCurrent");
    if (current_ == null) {
      throw new ArgumentNullException();
    }
    moveNext_ = table.GetLuaFunction("MoveNext");
    if (moveNext_ == null) {
      throw new ArgumentNullException();
    }
  }

  public object Current {
    get {
      return current_.Invoke<LuaTable, object>(table_);
    }
  }

  public void Dispose() {
    if (current_ != null) {
      current_.Dispose();
      current_ = null;
    }

    if (moveNext_ != null) {
      moveNext_.Dispose();
      moveNext_ = null;
    }

    if (table_ != null) {
      table_.Dispose();
      table_ = null;
    }
  }

  public bool MoveNext() {
    bool hasNext = moveNext_.Invoke<LuaTable, bool>(table_);
    if (!hasNext) {
      Dispose();
    }
    return hasNext;
  }

  public void Reset() {
    throw new NotImplementedException();
  }
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
      using (var fn = luaState.DoString<LuaFunction>("return UnityEngine.GameObject.AddComponent")) {
        fn.Call(gameObject, LaunchTypeName);
      }
    } else {
      Type componentType = Type.GetType(LaunchTypeName, true, false);
      gameObject.AddComponent(componentType);
    }
  }
}