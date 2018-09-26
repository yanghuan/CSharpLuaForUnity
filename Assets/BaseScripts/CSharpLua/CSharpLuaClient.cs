using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using LuaInterface;

namespace CSharpLua {
  [LuaAutoWrap]
  public sealed class BridgeMonoBehaviour : MonoBehaviour {
    private static readonly YieldInstruction[] updateYieldInstructions_ = new YieldInstruction[] { null, new WaitForFixedUpdate(), new WaitForEndOfFrame() };

    public LuaTable Table { get; private set; }
    public string LuaClass;
    public string SerializeData;
    public UnityEngine.Object[] SerializeObjects;

    public void Bind(LuaTable table, string luaClass) {
      Table = table;
      LuaClass = luaClass;
    }

    public void Bind(LuaTable table) {
      Table = table;
    }

    internal void Bind(string luaClass, string serializeData, UnityEngine.Object[] serializeObjects) {
      LuaClass = luaClass;
      SerializeData = serializeData;
      SerializeObjects = serializeObjects;
    }

    public Coroutine StartCoroutine(LuaTable routine) {
      return StartCoroutine(new LuaIEnumerator(routine));
    }

    public void RegisterUpdate(int instructionIndex, LuaFunction updateFn) {
      StartCoroutine(StartUpdate(updateFn, updateYieldInstructions_[instructionIndex]));
    }

    private IEnumerator StartUpdate(LuaFunction updateFn, YieldInstruction yieldInstruction) {
      while (true) {
        yield return yieldInstruction;
        updateFn.Call(Table);
      }
    }

    private void Awake() {
      if (!string.IsNullOrEmpty(LuaClass)) {
        if (Table == null) {
          Table = CSharpLuaClient.Instance.BindLua(this);
        } else {
          using (var fn = Table.GetLuaFunction("Awake")) {
            fn.Call(Table);
          }
        }
      }
    }

    private void Start() {
      using (var fn = Table.GetLuaFunction("Start")) {
        fn.Call(Table);
      }
    }
  }

  internal sealed class LuaIEnumerator : IEnumerator, IDisposable {
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
      throw new NotSupportedException();
    }
  }

  public class CSharpLuaClient : LuaClient {
    public string[] Components;
    private LuaFunction bindFn_;
    public static new CSharpLuaClient Instance { get { return (CSharpLuaClient)LuaClient.Instance; } }

    protected override void OpenLibs() {
      base.OpenLibs();
      OpenCJson();
      OpenPBC();
    }

    private void OpenPBC() {
      luaState.OpenLibs(LuaDLL.luaopen_protobuf_c);  
    }

    public override void Destroy() {
      if (bindFn_ != null) {
        bindFn_.Dispose();
        bindFn_ = null;
      }
      base.Destroy();
    }

    protected override void StartMain() {
      if (Settings.kIsRunFromLua) {
        base.StartMain();
        bindFn_ = luaState.GetFunction("UnityEngine.bind");
        if (bindFn_ == null) {
          throw new ArgumentNullException();
        }
        if (Components != null && Components.Length > 0) {
          using (var fn = luaState.GetFunction("UnityEngine.addComponent")) {
            foreach (string type in Components) {
              fn.Call(gameObject, type);
            }
          }
        }
      } else {
        if (Components != null) {
          foreach (string type in Components) {
            gameObject.AddComponent(Type.GetType(type, true, false));
          }
        }
      }
    }

    internal LuaTable BindLua(BridgeMonoBehaviour bridgeMonoBehaviour) {
      return bindFn_.Invoke<BridgeMonoBehaviour, string, string, UnityEngine.Object[], LuaTable>(
        bridgeMonoBehaviour, 
        bridgeMonoBehaviour.LuaClass, 
        bridgeMonoBehaviour.SerializeData,
        bridgeMonoBehaviour.SerializeObjects);
    }
  }
}

