using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

public sealed class TestHelloWord : MonoBehaviour {
  public void Awake() {
    Debug.Log("hello, word");
    Debug.Log(name);
    gameObject.name = "test";
    Debug.Log(gameObject.name);
  }
}