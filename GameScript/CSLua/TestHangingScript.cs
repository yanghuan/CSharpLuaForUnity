using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace CSLua
{
    public class TestHangingScript
    {
        public string DataOfString;
        public int DataOfInt;
        public string DataOfString2 = "ddddd";
        public int a = 10;
        public GameObject DataOfGameObject;
        public UnityEngine.Object DateOfObject;

        public void Awake()
        {
            Debug.Log("Awake");
            Debug.Log(DataOfString);
            Debug.Log(DataOfInt);
            Debug.Log(DataOfString2);
            Debug.Log(a);
        }

        public void Start()
        {
            Debug.Log("Start");
        }
    }
}
