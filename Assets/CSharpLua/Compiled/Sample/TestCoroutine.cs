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
      StartCoroutine(T1());
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


    private IEnumerator T1() {
      print("a0");
      yield return null;
      print("a1");
      yield return T2();
      print("a2");
    }

    private IEnumerator T2() {
      print("b0");
      yield return null;
      print("b1");
      yield return null;
      print("b2");
    }
  }
}
