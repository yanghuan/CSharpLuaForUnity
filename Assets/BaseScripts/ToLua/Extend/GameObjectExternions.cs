using UnityEngine;
using System.Collections;
using LuaInterface;
using System;

namespace Generic
{
    public static class GameObjectExternions
    {
        public static Component AddComponent0T(this UnityEngine.GameObject self, System.Type T)
        {
            return self.AddComponent(T);
        }

        public static Component GetComponent0T(this UnityEngine.GameObject self, System.Type T)
        {
            return self.GetComponent(T);
        }

        public static Component GetComponentInChildren0T(this UnityEngine.GameObject self, System.Type T)
        {
            return self.GetComponentInChildren(T);
        }

        public static Component GetComponentInChildren1T(this UnityEngine.GameObject self, System.Boolean includeInactive, System.Type T)
        {
            return self.GetComponentInChildren(T, includeInactive);
        }

        public static Component[] GetComponentsInChildren0T(this UnityEngine.GameObject self, System.Type T)
        {
            return self.GetComponentsInChildren(T);
        }


        public static Component[] GetComponentsInChildren2T(this UnityEngine.GameObject self, System.Boolean includeInactive, System.Type T)
        {
            return self.GetComponentsInChildren(T, includeInactive);
        }

        public static Component GetComponentInParent0T(this UnityEngine.GameObject self, System.Type T)
        {
            return self.GetComponentInParent(T);
        }

        public static Component[] GetComponentsInParent0T(this UnityEngine.GameObject self, System.Type T)
        {
            return self.GetComponentsInParent(T);
        }

        public static Component[] GetComponentsInParent1T(this UnityEngine.GameObject self, System.Boolean includeInactive, System.Type T)
        {
            return self.GetComponentsInParent(T, includeInactive);
        }

        public static Component[] GetComponents0T(this UnityEngine.GameObject self, System.Type T)
        {
            return self.GetComponents(T);
        }
        
    }
}