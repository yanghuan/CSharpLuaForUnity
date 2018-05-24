using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

public sealed class TestHelloWord2 : MonoBehaviour {
  public void Awake() {
	Debug.Log("TestHelloWord2");
	print("dddddd");
  }
}

public sealed class TestHelloWord : MonoBehaviour {
  public void Awake() {
    Debug.Log("TestHelloWord");
	gameObject.AddComponent<TestHelloWord2>();
  }
}