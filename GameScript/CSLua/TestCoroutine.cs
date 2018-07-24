using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

namespace CSLua
{
    public sealed class TestCoroutine
    {
        public void Awake()
        {
            Debug.Log("TestCoroutine");
            MonoManager.Instance.StartCoroutine(OnTick());
        }

        private IEnumerator OnTick()
        {
            while (true)
            {
                yield return new WaitForSeconds(1);
                Debug.Log("TestCoroutine.OnTick");
            }
        }

        public void Test()
        {
            Debug.Log("TestCoroutine.Test");
        }
    }
}
