using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Sample {
  public class TestHangingScript : MonoBehaviour {
    public string DataOfString;
    public int DataOfInt;
    public string DataOfString2 = "ddddd";
    public int a = 10;
    public GameObject DataOfGameObject;
    public UnityEngine.Object DateOfObject;

    public void Awake() {
      print("Awake");
      print(DataOfString);
      print(DataOfInt);
      print(DataOfString2);
      print(a);
    }

    public void Start() {
      print("Start");
    }
  }
}
