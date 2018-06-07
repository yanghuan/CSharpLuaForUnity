#if UNITY_EDITOR  
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using UnityEngine;

namespace CSharpLua {
  public static class UserMonoBehaviourConverter {
    public static readonly string compiledScriptsManifestPath_ = Application.dataPath + "/Lua/CompiledScripts/manifest.lua";

    private static HashSet<string> userDefinedNames_;
    private static DateTime lastLoadNamesTime_;

    public static void Do(GameObject gameObject) {
      var childrens = GetChildrenTransform(gameObject.transform);
      foreach (var t in childrens) {
        Convert(t.gameObject);
      }
    }

    private static List<Transform> GetChildrenTransform(Transform parent) {
      List<Transform> list = new List<Transform>();
      list.Add(parent);
      GetChildrenTransform(list, parent);
      return list;
    }

    private static void GetChildrenTransform(List<Transform> list, Transform parent) {
      int count = parent.childCount;
      for (int i = 0; i < count; i++) {
        Transform child = parent.GetChild(i);
        list.Add(child);
        GetChildrenTransform(list, child);
      }
    }

    private static void LoadUserDefinedNames() {
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

    private static void CheckCompiledScriptsChanged() {
      if (userDefinedNames_ == null) {
        LoadUserDefinedNames();
      } else {
         var fileInfo = new FileInfo(compiledScriptsManifestPath_);
        if (!fileInfo.Exists) {
          throw new InvalidOperationException("please compiled scripts first");
        }

        if (fileInfo.LastWriteTime > lastLoadNamesTime_) {
          LoadUserDefinedNames();
        }
      }
    }

    private static bool IsUserDefine(Type type) {
      CheckCompiledScriptsChanged();
      return userDefinedNames_.Contains(type.FullName);
    }

    private static void Convert(GameObject gameObject) {
      var monoBehaviours = gameObject.GetComponents<MonoBehaviour>();
      foreach (MonoBehaviour i in monoBehaviours) {
        Type type = i.GetType();
        if (IsUserDefine(type)) {

        }
      }
    }
  }
}

#endif