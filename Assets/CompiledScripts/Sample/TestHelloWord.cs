using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Sample {
  public sealed class TestHelloWord : MonoBehaviour {
    public void Awake() {
      Debug.Log("TestHelloWord");
      gameObject.AddComponent<TestCoroutine>();
      var c = GetComponent<MonoBehaviour>();
      print(c.name);

      var obj1 = FindObjectOfType<MonoBehaviour>();
      Destroy(obj1);

      GameObject i = TestUtils.Load("Assets/CSharpLua/Examples/01_HelloWorld/TestLoader.prefab");
      var obj = Instantiate(i);
      obj.transform.parent = transform;
    }

    public void Start() {
      print("TestHelloWord.Start");
    }

    public void Update() {
      print("TestHelloWord.Update");
    }
  }
}


