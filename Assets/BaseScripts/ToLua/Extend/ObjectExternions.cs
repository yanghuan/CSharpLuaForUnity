using UnityEngine;
using System.Collections;
using LuaInterface;

namespace Generic
{
    public static class ObjectExternions
    {
        public static Object Instantiate1(Object original, System.Type T)
        {
            return Object.Instantiate(original);
        }

        public static Object Instantiate3(Object original, UnityEngine.Transform parent, System.Type T)
        {
            return Object.Instantiate(original, parent);
        }

        public static Object Instantiate4(Object original, UnityEngine.Transform parent, System.Boolean worldPositionStays, System.Type T)
        {
            return Object.Instantiate(original, parent, worldPositionStays);
        }

        public static Object Instantiate5(Object original, UnityEngine.Vector3 position, UnityEngine.Quaternion rotation, System.Type T)
        {
            return Object.Instantiate(original, position, rotation);
        }

        public static Object Instantiate9(Object original, UnityEngine.Vector3 position, UnityEngine.Quaternion rotation, UnityEngine.Transform parent, System.Type T)
        {
            return Object.Instantiate(original, position, rotation, parent);
        }

        public static Object FindObjectOfType0T(System.Type T)
        {
            return Object.FindObjectOfType(T);
        }
        public static Object[] FindObjectsOfType0T(System.Type T)
        {
            return Object.FindObjectsOfType(T);
        }
    }
}