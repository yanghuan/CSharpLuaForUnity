using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

public sealed class TestHelloWord2 : MonoBehaviour {
  public void Awake() {
    Debug.Log("TestHelloWord2");
    StartCoroutine(TestCoroutine());
    print(gameObject.name);
  }

  private IEnumerator TestCoroutine() {
    while (true) {
      yield return new WaitForSeconds(1);
      print("TestCoroutine.tick");
    }
  }

  public void Test() {
    print("Test");
  }
}

public sealed class TestHelloWord : MonoBehaviour {
  public void Awake() {
    Debug.Log("TestHelloWord");
    gameObject.AddComponent<TestHelloWord2>();
    var c = GetComponent<TestHelloWord2>();
    print(c.name);
    c.Test();
  }
}