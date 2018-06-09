using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

namespace Sample {
  public sealed class TestCoroutine : MonoBehaviour {
    public void Awake() {
      Debug.Log("TestCoroutine");
      StartCoroutine(OnTick());
      print(gameObject.name);
    }

    private IEnumerator OnTick() {
      while (true) {
        yield return new WaitForSeconds(1);
        print("TestCoroutine.OnTick");
      }
    }

    public void Test() {
      print("TestCoroutine.Test");
    }
  }
}
