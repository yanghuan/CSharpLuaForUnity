#if UNITY_EDITOR  
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
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
      public Dictionary<string, UnityEngine.Object> Objects = new Dictionary<string, UnityEngine.Object>();
      public Dictionary<string, string> MonoBehaviourFields = new Dictionary<string, string>();

      private void AppendNormals(StringBuilder sb) {
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
        sb.Append("}");
      }

      private void AppendObjects(StringBuilder sb) {
        if (Objects.Count > 0) {
          sb.Append('{');
          bool isFirst = true;
          int objectIndex = 0;
          foreach (string key in Objects.Keys) {
            if (isFirst) {
              isFirst = false;
            } else {
              sb.Append(',');
            }

            sb.Append(key);
            sb.Append('=');
            sb.Append(objectIndex);
            ++objectIndex;
          }
          sb.Append('}');
        }
      }

      public string GetSerializeData() {
        bool isEmpty = Normals.Count == 0 && Objects.Count == 0;
        StringBuilder sb = new StringBuilder();
        if (!isEmpty) {
          sb.Append("return{");
          AppendNormals(sb);
          sb.Append(',');
          AppendObjects(sb);
          sb.Append('}');
        }
        return sb.ToString();
      }

      public UnityEngine.Object[] GetSerializeObjects() {
        return Objects.Count > 0 ? Objects.Values.ToArray() : null;
      }

      private static string ValueToString(object v) {
        if (v is string) {
          return "\"" + v + "\"";
        }
        return v.ToString();
      }
    }

    private sealed class MonoBehaviourFieldLazy {
      public string ClassName;
      public SerializeFieldsInfo SerializeInfo;
      public BridgeMonoBehaviour BridgeMonoBehaviour;

      public void Bind() {
        foreach (var pair in SerializeInfo.MonoBehaviourFields) {
          var gameObject = (GameObject)SerializeInfo.Objects[pair.Key];
          var bridges = gameObject.GetComponents<BridgeMonoBehaviour>();
          var item = bridges.Single(i => i.LuaClass == pair.Value);
          SerializeInfo.Objects[pair.Key] = item;
        }
        BridgeMonoBehaviour.Bind(ClassName, SerializeInfo.GetSerializeData(), SerializeInfo.GetSerializeObjects());
      }
    }

    private static readonly string tempPrefabDir_ = Settings.Paths.kTempDir + "/prefabs";
    private static readonly string compiledScriptsManifestPath_ = Settings.Paths.CompiledOutDir + "/manifest.lua";
    private static UserMonoBehaviourConverter default_;

    private HashSet<string> userDefinedNames_;
    private LuaState luaState_;

    public UserMonoBehaviourConverter() {
      Load();
    }

    private void Load() {
      LoadClassNames();
      luaState_ = LuaClient.GetMainState();
      if (luaState_ == null) {
        throw new InvalidProgramException("not found MainState");
      }
    }

    public static UserMonoBehaviourConverter Default {
      get {
        if (default_ == null) {
          default_ = new UserMonoBehaviourConverter();
        }
        return default_;
      }
    }

    private void PauseEdit() {
      if (Application.isPlaying) {
        UnityEngine.Debug.Break();
      }
    }

    private void LoadClassNames() {
      const string kBeginToken = "System.init({";
      const string kEndToken = "})";

      if (!File.Exists(compiledScriptsManifestPath_)) {
        PauseEdit();
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
    }

    private static void CopyTempPrefab(ref GameObject prefab) {
      string oldPath = AssetDatabase.GetAssetPath(prefab);
      string path = Path.Combine(tempPrefabDir_, oldPath);
      Directory.CreateDirectory(Path.GetDirectoryName(path));
      try {
        prefab = PrefabUtility.CreatePrefab(path, prefab);
      } catch (ArgumentException e) when (e.Message == "Can't save persistent object as a Prefab asset") {
        throw new InvalidDataException("目前2018.3拷贝预设存在BUG,暂时未发现规避的方法,请使用较低版本");
      }
    }

    private bool IsUserMonoBehaviourExists(GameObject prefab) {
      var childrens = GetChildrenTransform(prefab.transform);
      foreach (var gameObject in childrens) {
        var monoBehaviours = gameObject.GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour monoBehaviour in monoBehaviours) {
          if (IsUserDefine(monoBehaviour.GetType())) {
            return true;
          }
        }
      }
      return false;
    }

    public bool Do(ref GameObject prefab) {
      if (IsUserMonoBehaviourExists(prefab)) {
        CopyTempPrefab(ref prefab);

        List<MonoBehaviourFieldLazy> monoBehaviourFieldLazys = new List<MonoBehaviourFieldLazy>();
        var childrens = GetChildrenTransform(prefab.transform);
        foreach (var t in childrens) {
          Convert(t.gameObject, monoBehaviourFieldLazys);
        }
        
        foreach (var i in monoBehaviourFieldLazys) {
          i.Bind();
        }

        return true;
      }
      return false;
    }

    private List<Transform> GetChildrenTransform(Transform parent) {
      List<Transform> list = new List<Transform> { parent };
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

    private void Convert(GameObject gameObject, List<MonoBehaviourFieldLazy> lazys) {
      var monoBehaviours = gameObject.GetComponents<MonoBehaviour>();
      foreach (MonoBehaviour monoBehaviour in monoBehaviours) {
        if (IsUserDefine(monoBehaviour.GetType())) {
          Convert(monoBehaviour, lazys);
        }
      }
    }

    private bool IsSerializedField(FieldInfo field) {
      if (field.IsPublic) {
        return !field.IsDefined(typeof(HideInInspector), false);
      } else {
        return field.IsDefined(typeof(SerializeField), false);
      }
    }

    private void Convert(MonoBehaviour monoBehaviour, List<MonoBehaviourFieldLazy> lazys) {
      Type type = monoBehaviour.GetType();
      string className = type.FullName;
      using (var luaClass = luaState_.GetTable(className)) {
        if (luaClass == null) {
          PauseEdit();
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
        if (info.MonoBehaviourFields.Count == 0) {
          bridgeMonoBehaviour.Bind(className, info.GetSerializeData(), info.GetSerializeObjects());
        } else {
          lazys.Add(new MonoBehaviourFieldLazy() {
            ClassName = className,
            SerializeInfo = info,
            BridgeMonoBehaviour = bridgeMonoBehaviour,
          });
        }
      }
    }

    private bool IsSameRootGameObject(GameObject x, GameObject y) {
      string pathX = AssetDatabase.GetAssetPath(x);
      string pathY = AssetDatabase.GetAssetPath(y);
      return pathX == pathY;
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
            if (y is double) {
              if (fieldTypeCode == TypeCode.Char) {
                x = (double)(char)x;
              } else {
                x = System.Convert.ToDouble(x);
              }
            }
            if (!EqualityComparer<object>.Default.Equals(x, y)) {
              info.Normals.Add(field.Name, x);
            }
            break;
          }
        case TypeCode.Object: {
            if (typeof(UnityEngine.Object).IsAssignableFrom(fieldType)) {
              object x = field.GetValue(monoBehaviour);
              if (x != null) {
                var obj = x as UnityEngine.Object;
                if (obj != null) {
                  if (obj is GameObject) {
                    var gameObject = (GameObject)obj;
                    if (!IsSameRootGameObject(monoBehaviour.gameObject, gameObject)) {
                      bool hasChanged = Do(ref gameObject);
                      if (hasChanged) {
                        obj = gameObject;
                      }
                    }
                  } else if (obj is MonoBehaviour) {
                    var mb = (MonoBehaviour)obj;
                    var gameObject = mb.gameObject;
                    bool isSameRoot = IsSameRootGameObject(monoBehaviour.gameObject, gameObject);
                    if (!isSameRoot) {
                      bool hasChanged = Do(ref gameObject);
                      if (hasChanged) {
                        obj = gameObject;
                      }
                    } else {
                      obj = gameObject;
                    }
                    if (IsUserDefine(mb.GetType())) {
                      if (isSameRoot) {
                        info.MonoBehaviourFields.Add(field.Name, mb.GetType().FullName);
                      } else {
                        var bridges = gameObject.GetComponents<BridgeMonoBehaviour>();
                        var mbNew = bridges.Single(i => i.LuaClass == mb.GetType().FullName);
                        Contract.Assert(mbNew != null);
                        obj = mbNew;
                      }
                    } else {
                      var mbNew = gameObject.GetComponent(mb.GetType());
                      obj = mbNew;
                    }
                  }
                  info.Objects.Add(field.Name, obj);
                } 
              }
            } else {
              if (fieldType.IsDefined(typeof(SerializableAttribute), true)) {
                PauseEdit();
                throw new NotImplementedException($"{monoBehaviour.GetType()}'s field[{field.Name}] type[{fieldType}] not implemented serialized");
              }
            }
            break;
          }
        default: {
            if (fieldType.IsEnum) {
              int x = (int)(ValueType)field.GetValue(monoBehaviour);
              int y = luaClass.RawGet<string, int>(field.Name);
              if (x != y) {
                info.Normals.Add(field.Name, x);
              }
            }
            PauseEdit();
            throw new NotSupportedException($"{monoBehaviour.GetType()}'s field[{field.Name}] type[{fieldType}] not support serialized");
          }
      }
    }
  }
}

#endif