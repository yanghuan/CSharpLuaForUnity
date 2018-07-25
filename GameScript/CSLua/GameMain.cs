/*
 * @create:  李锦俊 
 * @email: mybios@qq.com
 * 脚本入口类，入口函数是CSLua.GameCore.Startup
 * */
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CSLua
{
    public sealed class GameMain
    {
        public static GameMain Instance { get; private set; }

        TestCoroutine _testCoroutine = new TestCoroutine();
        TestHangingScript _testHangingScript = new TestHangingScript();

        void Start()
        {
            _testCoroutine.Awake();
            _testHangingScript.Awake();
        }

        // lua逻辑主入口
        public static void Startup()
        {
            Debug.Log("Lua GameMain Start");
            Instance = new GameMain();
            Instance.Start();
        }
        // lua逻辑退出
        public static void Shutdown()
        {
            Debug.Log("Lua GameMain Shutdown");
        }
    }
}


