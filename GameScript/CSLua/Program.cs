using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CSLua {
  public static class Program {
    public static void Main(string[] args) {
      Debug.Log("hello, world");
      Utils.MonoBehaviourInstance.StartCoroutine(Update());
    }

    public static IEnumerator Update() {
      while (true) {
        yield return new WaitForSeconds(2);
        Debug.Log("Update");
      }
    }
  }
}


