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
    public TestCoroutine HangingMonoBehaviour;
    public List<int> l = new List<int>();
    public Vector2 vector2;
    public Vector3 vector3;

    public void Awake() {
      print("Awake");
      print(DataOfString);
      print(DataOfInt);
      print(DataOfString2);
      print(a);
      print(HangingMonoBehaviour.name);
      print(string.Join(",", l));
      print($"{vector2}, {vector3}");
    }

    public void Start() {
      print("Start");
    }
  }
}
