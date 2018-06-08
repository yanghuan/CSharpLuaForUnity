#if UNITY_EDITOR  
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;

using UnityEngine;
using UnityEditor;
using LuaInterface;

namespace CSharpLua {
  public sealed class UserMonoBehaviourConverter {
    private sealed class SerializeFieldsInfo {
      public Dictionary<string, object> Normals = new Dictionary<string, object>();

      public override string ToString() {
        bool isEmpty = Normals.Count == 0;
        StringBuilder sb = new StringBuilder();
        if (!isEmpty) {
          sb.Append("return{");
          sb.Append('{');
          bool isFirst = true;
          foreach (var normal in Normals) {
            if (isFirst) {
              isFirst = false;
            } else {
              sb.Append(',');
            }
            sb.Append(normal.Key);
            sb.Append('=');
            sb.Append(ValueToString(normal.Value));
          }
          sb.Append('}');
          sb.Append('}');
        }
        return sb.ToString();
      }

      private static string ValueToString(object v) {
        if (v is string) {
          return "\"" + v + "\"";
        } else if (v is char) {
          int i = (char)v;
          return i.ToString();
        }
        return v.ToString();
      }
    }

    private static readonly string tempPrefabDir_ = "Assets/CSharpLua/Temp/prefabs";
    public static readonly string compiledScriptsManifestPath_ = Application.dataPath + "/Lua/CompiledScripts/manifest.lua";
    private static UserMonoBehaviourConverter default_;

    private HashSet<string> userDefinedNames_;
    private DateTime lastLoadNamesTime_;
    private LuaState luaState_;

    public UserMonoBehaviourConverter() {
      Load();
    }

    private void Load() {
      LoadClassNames();
      if (luaState_ != null) {
        luaState_.Dispose();
        luaState_ = null;
      }

      luaState_ = new LuaState();
      luaState_.LuaSetTop(0);
      LuaBinder.Bind(luaState_);
      luaState_.Start();
      luaState_.DoFile("Classloader.lua");
    }

    public static UserMonoBehaviourConverter Default {
      get {
        if (default_ == null) {
          default_ = new UserMonoBehaviourConverter();
        }
        return default_;
      }
    }

    private void LoadClassNames() {
      const string kBeginToken = "System.init({";
      const string kEndToken = "})";

      if (!File.Exists(compiledScriptsManifestPath_)) {
        throw new InvalidOperationException("please compiled scripts first");
      }

      string content = File.ReadAllText(compiledScriptsManifestPath_);
      int begin = content.IndexOf(kBeginToken);
      if (begin == -1) {
        throw new InvalidProgramException();
      }
      begin += kBeginToken.Length;
      int end = content.IndexOf(kEndToken, begin);
      if (end == -1) {
        throw new InvalidProgramException();
      }

      var userDefines = content.Substring(begin, end - begin).Split(',').Select(i => i.Trim().Trim('"')).ToArray();
      userDefinedNames_ = new HashSet<string>(userDefines);
      lastLoadNamesTime_ = DateTime.Now;
    }

    private static void CopyTempPrefab(ref GameObject prefab) {
      string path = Path.Combine(tempPrefabDir_, AssetDatabase.GetAssetPath(prefab));
      Directory.CreateDirectory(Path.GetDirectoryName(path));
      prefab = PrefabUtility.CreatePrefab(path, prefab);
    }

    public void Do(ref GameObject prefab) {
      CopyTempPrefab(ref prefab);
      bool hasChanged = false;
      var childrens = GetChildrenTransform(prefab.transform);
      foreach (var t in childrens) {
        hasChanged |= Convert(t.gameObject);
      }
    }

    private List<Transform> GetChildrenTransform(Transform parent) {
      List<Transform> list = new List<Transform>();
      list.Add(parent);
      GetChildrenTransform(list, parent);
      return list;
    }

    private void GetChildrenTransform(List<Transform> list, Transform parent) {
      int count = parent.childCount;
      for (int i = 0; i < count; i++) {
        Transform child = parent.GetChild(i);
        list.Add(child);
        GetChildrenTransform(list, child);
      }
    }

    private bool IsUserDefine(Type type) {
      return userDefinedNames_.Contains(type.FullName);
    }

    private bool Convert(GameObject gameObject) {
      var monoBehaviours = gameObject.GetComponents<MonoBehaviour>();
      foreach (MonoBehaviour monoBehaviour in monoBehaviours) {
        if (IsUserDefine(monoBehaviour.GetType())) {
          Convert(monoBehaviour);
        }
      }
      return false;
    }

    private bool IsSerializedField(FieldInfo field) {
      if (field.IsPublic) {
        return !field.IsDefined(typeof(HideInInspector), false);
      } else {
        return field.IsDefined(typeof(SerializeField), false);
      }
    }

    private void Convert(MonoBehaviour monoBehaviour) {
      Type type = monoBehaviour.GetType();
      string className = type.FullName;
      using (var luaClass = luaState_.GetTable(className)) {
        if (luaClass == null) {
          throw new InvalidOperationException($"{className} is not found in lua env");
        }

        SerializeFieldsInfo info = new SerializeFieldsInfo();
        FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        foreach (FieldInfo field in fields) {
          if (IsSerializedField(field)) {
            Convert(monoBehaviour, luaClass, field, info);
          }
        }

        GameObject gameObject = monoBehaviour.gameObject;
        UnityEngine.Object.DestroyImmediate(monoBehaviour, true);
        var bridgeMonoBehaviour = gameObject.AddComponent<BridgeMonoBehaviour>();
        bridgeMonoBehaviour.Bind(className, info.ToString());
      }
    }

    private void Convert(MonoBehaviour monoBehaviour, LuaTable luaClass, FieldInfo field, SerializeFieldsInfo info) {
      Type fieldType = field.FieldType;
      TypeCode fieldTypeCode = Type.GetTypeCode(fieldType);
      switch (fieldTypeCode) {
        case TypeCode.Boolean:
        case TypeCode.Char:
        case TypeCode.SByte:
        case TypeCode.Byte:
        case TypeCode.Int16:
        case TypeCode.UInt16:
        case TypeCode.Int32:
        case TypeCode.UInt32:
        case TypeCode.Int64:
        case TypeCode.UInt64:
        case TypeCode.Single:
        case TypeCode.Double:
        case TypeCode.String: {
            object x = field.GetValue(monoBehaviour);
            object y = luaClass.RawGet<string, object>(field.Name);
            if (!EqualityComparer<object>.Default.Equals(x, y)) {
              info.Normals.Add(field.Name, x);
            }
            break;
          }
      }
    }
  }
}

#endif