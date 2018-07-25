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
        IEnumerator _iter;
        Coroutine _coroutine;
        public void Awake()
        {
            Debug.Log("TestCoroutine");
            _iter = OnTick();
            _coroutine = MonoManager.Instance.StartCoroutine(_iter);
        }

        private IEnumerator OnTick()
        {
            int count = 0;
            while (true)
            {
                yield return new WaitForSeconds(1);
                Debug.Log("TestCoroutine.OnTick");
                if(count ++ > 10)
                {
                    Debug.Log("TestCoroutine Stop!");
                    //MonoManager.Instance.StopCoroutine(_coroutine);
                    MonoManager.Instance.StopCoroutine(_iter);
                }
            }
        }

        public void Test()
        {
            Debug.Log("TestCoroutine.Test");
        }
    }
}
