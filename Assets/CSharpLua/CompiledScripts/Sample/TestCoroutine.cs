using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

namespace CSharpLua.Sample {
  public sealed class TestCoroutine : MonoBehaviour {
    public void Awake() {
      Debug.Log("TestCoroutine");
      StartCoroutine(OnTick());
      print(gameObject.name);
    }

    private IEnumerator OnTick() {
      while (true) {
        yield return new WaitForSeconds(1);
        print("OnTick");
      }
    }

    public void Test() {
      print("Test");
    }
  }
}
