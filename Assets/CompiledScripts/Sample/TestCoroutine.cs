using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

namespace Sample {
  public class TestCoroutine : MonoBehaviour {
    private List<int> list = new List<int>();

    public void Awake() {
      Debug.Log("TestCoroutine");
      StartCoroutine(OnTick());
      print(gameObject.name);
      print(list.Count);
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
