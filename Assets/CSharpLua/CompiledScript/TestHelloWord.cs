using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

public sealed class TestHelloWord2 : MonoBehaviour {
  public void Awake() {
    Debug.Log("TestHelloWord2");
    var cc = StartCoroutine(TestCoroutine());
    print("after");
    StopCoroutine(cc);
  }

  private IEnumerator TestCoroutine() {
    print("start");
    while (true) {
      yield return new WaitForSeconds(1);
      print("tick");
    }
  }
}

public sealed class TestHelloWord : MonoBehaviour {
  public void Awake() {
    Debug.Log("TestHelloWord");
    gameObject.AddComponent<TestHelloWord2>();
  }
}