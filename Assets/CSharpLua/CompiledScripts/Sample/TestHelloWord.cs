using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace CSharpLua.Sample {
  public sealed class TestHelloWord : MonoBehaviour {
    public void Awake() {
      Debug.Log("TestHelloWord");
      gameObject.AddComponent<TestCoroutine>();
      var c = GetComponent<TestCoroutine>();
      print(c.name);
      c.Test();

      
      var obj = (GameObject)AssetDatabase.LoadMainAssetAtPath("Assets/CSharpLua/Examples/01_HelloWorld/TestLoader.prefab");
      UserMonoBehaviourConverter.Do(obj);
      var i = obj.GetComponent<TestHangingScript>();
      string path = AssetDatabase.GetAssetPath(i.DataOfGameObject);
      string path2 = GetGameObjectPath(i.DataOfGameObject);
      print(path);
      print(path2);
    }

    public static string GetGameObjectPath(GameObject obj) {
      string path = "/" + obj.name;
      while (obj.transform.parent != null) {
        obj = obj.transform.parent.gameObject;
        path = "/" + obj.name + path;
      }
      return path;
    }
  }
}


