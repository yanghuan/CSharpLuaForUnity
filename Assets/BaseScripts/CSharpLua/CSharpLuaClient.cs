/*
 * @create:  李锦俊 
 * @email: mybios@qq.com
 * lua的入口类
 * */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using LuaInterface;

namespace CSharpLua
{
    public class CSharpLuaClient : LuaClient
    {
        public string StartupFunction;
        public string DestroyFunction;
        public static new CSharpLuaClient Instance { get { return (CSharpLuaClient)LuaClient.Instance; } }

        private void Start()
        {
            StartUp();
        }

        protected override void OpenLibs()
        {
            base.OpenLibs();
            OpenCJson();
            OpenPBC();
        }

        private void OpenPBC()
        {
            luaState.OpenLibs(LuaDLL.luaopen_protobuf_c);
        }

        public override void Destroy()
        {
            if (luaState != null)
            {
                RunFunction(DestroyFunction);
            }
            base.Destroy();
        }

        void RunFunction(string funcName)
        {
            if (Settings.kIsRunFromLua)
            {
                var bindFn_ = luaState.GetFunction(funcName);
                if (bindFn_ == null)
                {
                    throw new ArgumentNullException("找不到lua主入口函数：" + funcName);
                }
                else
                {
                    bindFn_.Call();
                }
            }
            else
            {
                var dot = funcName.LastIndexOf('.');
                var methodName = funcName.Substring(dot + 1);
                var className = funcName.Substring(0, dot);
                var t = ReflectionTools.GetType(className);
                if (t != null)
                {
                    var method = t.GetMethod(methodName);
                    method.Invoke(null, new object[0]);
                }
                else
                {
                    Debug.LogWarningFormat("找不到C#主入口函数：{0}", funcName);
                }
            }
        }

        protected override void StartMain()
        {
            if (Settings.kIsRunFromLua)
            {
                base.StartMain();
            }
            RunFunction(StartupFunction);
        }
    }
}

