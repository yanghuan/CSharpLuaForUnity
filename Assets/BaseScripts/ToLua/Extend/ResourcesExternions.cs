using UnityEngine;
using System.Collections;
using LuaInterface;
using System;

namespace Generic
{
    public static class ResourcesExternions
    {
        public static UnityEngine.Object Load0T(System.String path, System.Type T)
        {
            return Resources.Load(path, T);
        }
        public static UnityEngine.Object[] FindObjectsOfTypeAll0T(System.Type T)
        {
            return Resources.FindObjectsOfTypeAll(T);
        }
        public static ResourceRequest LoadAsync0T(System.String path, System.Type T)
        {
            return Resources.LoadAsync(path, T);
        }
        public static UnityEngine.Object[] LoadAll0T(System.String path, System.Type T)
        {
            return Resources.LoadAll(path, T);
        }
        public static UnityEngine.Object GetBuiltinResource0T(System.String path, System.Type T)
        {
            return Resources.GetBuiltinResource(T, path);
        }
    }
}