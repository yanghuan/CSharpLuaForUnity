using LuaInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

public static partial class ToLuaExport
{

    static string GetMethodName(MethodBase md)
    {
        if (md.Name.StartsWith("op_"))
        {
            return md.Name;
        }

        object[] attrs = md.GetCustomAttributes(true);

        for (int i = 0; i < attrs.Length; i++)
        {
            if (attrs[i] is LuaRenameAttribute)
            {
                LuaRenameAttribute attr = attrs[i] as LuaRenameAttribute;
                return attr.Name;
            }
        }

        return md.Name;
    }

    static bool IsGenericMethod(MethodBase md)
    {
        if (md.IsGenericMethod)
        {
            Type[] gts = md.GetGenericArguments();
            List<ParameterInfo> list = new List<ParameterInfo>(md.GetParameters());

            for (int i = 0; i < gts.Length; i++)
            {
                Type[] ts = gts[i].GetGenericParameterConstraints();

                if (ts == null || ts.Length == 0 || IsGenericConstraints(ts))
                {
                    return true;
                }

                ParameterInfo p = list.Find((iter) => { return iter.ParameterType == gts[i]; });

                if (p == null)
                {
                    return true;
                }

                list.RemoveAll((iter) => { return iter.ParameterType == gts[i]; });
            }

            for (int i = 0; i < list.Count; i++)
            {
                Type t = list[i].ParameterType;

                if (IsGenericConstraintType(t))
                {
                    return true;
                }
            }
        }

        return false;
    }
    static bool BeDropMethodType(MethodInfo md)
    {
        Type t = md.DeclaringType;

        if (t == type)
        {
            return true;
        }

        return allTypes.IndexOf(t) < 0;
    }


    static string GetPushFunction(Type t, bool isByteBuffer = false)
    {
        if (t.IsEnum || t.IsPrimitive || t == typeof(string) || t == typeof(LuaTable) || t == typeof(LuaCSFunction) || t == typeof(LuaThread) || t == typeof(LuaFunction)
            || t == typeof(Type) || t == typeof(IntPtr) || typeof(Delegate).IsAssignableFrom(t) || t == typeof(LuaByteBuffer) // || t == typeof(LuaInteger64)
            || t == typeof(Vector3) || t == typeof(Vector2) || t == typeof(Vector4) || t == typeof(Quaternion) || t == typeof(Color) || t == typeof(Color32) || t == typeof(RaycastHit)
            || t == typeof(Ray) || t == typeof(Touch) || t == typeof(Bounds) || t == typeof(object))
        {
            return "Push";
        }
        else if (t.IsArray || t == typeof(System.Array))
        {
            return "Push";
        }
        else if (IsIEnumerator(t))
        {
            return "PushIEnumerator";
        }
        else if (t == typeof(LayerMask))
        {
            return "PushLayerMask";
        }
        else if (typeof(UnityEngine.Object).IsAssignableFrom(t) || typeof(UnityEngine.TrackedReference).IsAssignableFrom(t))
        {
            return IsSealedType(t) ? "PushSealed" : "Push";
        }
        else if (t.IsValueType)
        {
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                return "PusNullable";
            }

            return "PushValue";
        }
        else if (IsSealedType(t))
        {
            return "PushSealed";
        }

        return "PushObject";
    }
    static int Compare(_MethodBase lhs, _MethodBase rhs)
    {
        int off1 = lhs.IsStatic ? 0 : 1;
        int off2 = rhs.IsStatic ? 0 : 1;

        ParameterInfo[] lp = lhs.GetParameters();
        ParameterInfo[] rp = rhs.GetParameters();

        int pos1 = GetOptionalParamPos(lp);
        int pos2 = GetOptionalParamPos(rp);

        if (pos1 >= 0 && pos2 < 0)
        {
            return 1;
        }
        else if (pos1 < 0 && pos2 >= 0)
        {
            return -1;
        }
        else if (pos1 >= 0 && pos2 >= 0)
        {
            pos1 += off1;
            pos2 += off2;

            if (pos1 != pos2)
            {
                return pos1 > pos2 ? -1 : 1;
            }
            else
            {
                pos1 -= off1;
                pos2 -= off2;

                if (lp[pos1].ParameterType.GetElementType() == typeof(object) && rp[pos2].ParameterType.GetElementType() != typeof(object))
                {
                    return 1;
                }
                else if (lp[pos1].ParameterType.GetElementType() != typeof(object) && rp[pos2].ParameterType.GetElementType() == typeof(object))
                {
                    return -1;
                }
            }
        }

        int c1 = off1 + lp.Length;
        int c2 = off2 + rp.Length;

        if (c1 > c2)
        {
            return 1;
        }
        else if (c1 == c2)
        {
            List<ParameterInfo> list1 = new List<ParameterInfo>(lp);
            List<ParameterInfo> list2 = new List<ParameterInfo>(rp);

            if (list1.Count > list2.Count)
            {
                if (list1[0].ParameterType == typeof(object))
                {
                    return 1;
                }

                list1.RemoveAt(0);
            }
            else if (list2.Count > list1.Count)
            {
                if (list2[0].ParameterType == typeof(object))
                {
                    return -1;
                }

                list2.RemoveAt(0);
            }

            for (int i = 0; i < list1.Count; i++)
            {
                if (list1[i].ParameterType == typeof(object) && list2[i].ParameterType != typeof(object))
                {
                    return 1;
                }
                else if (list1[i].ParameterType != typeof(object) && list2[i].ParameterType == typeof(object))
                {
                    return -1;
                }
                else if (list1[i].ParameterType.IsPrimitive && list2[i].ParameterType.IsPrimitive)
                {
                    if (Is64bit(list1[i].ParameterType) && !Is64bit(list2[i].ParameterType))
                    {
                        return 1;
                    }
                    else if (!Is64bit(list1[i].ParameterType) && Is64bit(list2[i].ParameterType))
                    {
                        return -1;
                    }
                    else if (Is64bit(list1[i].ParameterType) && Is64bit(list2[i].ParameterType) && list1[i].ParameterType != list2[i].ParameterType)
                    {
                        if (list1[i].ParameterType == typeof(ulong))
                        {
                            return 1;
                        }

                        return -1;
                    }
                }
            }

            return 0;
        }
        else
        {
            return -1;
        }
    }

    static void GenFunctions()
    {
        HashSet<string> set = new HashSet<string>();

        for (int i = 0; i < methods.Count; i++)
        {
            _MethodBase m = methods[i];

            if (IsGenericMethod(m.Method))
            {
                Debugger.Log("Generic Method {0}.{1} cannot be export to lua", LuaMisc.GetTypeName(type), m.GetTotalName());
                continue;
            }

            string name = GetMethodName(m.Method);

            if (nameCounter[name] > 1)
            {
                if (!set.Contains(name))
                {
                    _MethodBase mi = GenOverrideFunc(name);

                    if (mi == null)
                    {
                        set.Add(name);
                        continue;
                    }
                    else
                    {
                        m = mi;     //非重载函数，或者折叠之后只有一个函数
                    }
                }
                else
                {
                    continue;
                }
            }

            set.Add(name);
            GenFunction(m);
        }
    }
    static void GenFunction(_MethodBase m)
    {
        string name = GetMethodName(m.Method);
        sb.AppendLineEx("\r\n\t[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]");
        sb.AppendFormat("\tstatic int {0}(IntPtr L)\r\n", name == "Register" ? "_Register" : name);
        sb.AppendLineEx("\t{");

        if (HasAttribute(m.Method, typeof(UseDefinedAttribute)))
        {
            FieldInfo field = extendType.GetField(name + "Defined");
            string strfun = field.GetValue(null) as string;
            sb.AppendLineEx(strfun);
            sb.AppendLineEx("\t}");
            return;
        }

        ParameterInfo[] paramInfos = m.GetParameters();
        int offset = m.IsStatic ? 0 : 1;
        bool haveParams = HasOptionalParam(paramInfos);
        int rc = m.GetReturnType() == typeof(void) ? 0 : 1;

        BeginTry();

        if (!haveParams)
        {
            int count = paramInfos.Length + offset;
            if (m.Name == "op_UnaryNegation") count = 2;
            sb.AppendFormat("\t\t\tToLua.CheckArgsCount(L, {0});\r\n", count);
        }
        else
        {
            sb.AppendLineEx("\t\t\tint count = LuaDLL.lua_gettop(L);");
        }

        rc += m.ProcessParams(3, false, int.MaxValue);
        sb.AppendFormat("\t\t\treturn {0};\r\n", rc);
        EndTry();
        sb.AppendLineEx("\t}");
    }

    static void GenOverrideDefinedFunc(MethodBase method)
    {
        string name = GetMethodName(method);
        FieldInfo field = extendType.GetField(name + "Defined");
        string strfun = field.GetValue(null) as string;
        sb.AppendLineEx(strfun);
        return;
    }
    private static List<_MethodBase> GetOverrideMethods(string name)
    {
        return GetOverrideMethods(name, methods, false);
    }
    private static List<_MethodBase> GetOverrideMethods(string name, List<_MethodBase> sourceMethods, bool isGeneric)
    {
        List<_MethodBase> list = new List<_MethodBase>();

        for (int i = 0; i < sourceMethods.Count; i++)
        {
            if (IsGenericMethod(sourceMethods[i].Method) == isGeneric)
            {
                string curName = GetMethodName(sourceMethods[i].Method);

                if (curName == name)
                {
                    Push(list, sourceMethods[i]);
                }
            }
        }
        
        list.Sort(Compare);
        return list;
    }

    static _MethodBase GenOverrideFunc(string name)
    {
        var list = GetOverrideMethods(name);

        if (list.Count == 1)
        {
            return list[0];
        }
        else if (list.Count == 0)
        {
            return null;
        }

        int[] checkTypeMap = CheckCheckTypePos(list);

        sb.AppendLineEx("\r\n\t[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]");
        sb.AppendFormat("\tstatic int {0}(IntPtr L)\r\n", name == "Register" ? "_Register" : name);
        sb.AppendLineEx("\t{");

        BeginTry();
        sb.AppendLineEx("\t\t\tint count = LuaDLL.lua_gettop(L);");
        sb.AppendLineEx();

        for (int i = 0; i < list.Count; i++)
        {
            var genMethodName = GenMethodName(list[i], i);
            if (HasAttribute(list[i].Method, typeof(OverrideDefinedAttribute)))
            {
                sb.AppendFormat("\t\t\t\tif(true)return {0}(L);\r\n", genMethodName);
            }
            else
            {
                GenOverrideFuncCall(list[i], i == 0, checkTypeMap[i], genMethodName);
            }
        }

        sb.AppendLineEx("\t\t\telse");
        sb.AppendLineEx("\t\t\t{");
        sb.AppendFormat("\t\t\t\treturn LuaDLL.luaL_throw(L, \"invalid arguments to method: {0}.{1}\");\r\n", className, name);
        sb.AppendLineEx("\t\t\t}");

        EndTry();
        sb.AppendLineEx("\t}");

        for (int i = 0; i < list.Count; i++)
        {
            GenOverrideFunctionBody(GenMethodName(list[i], i), list[i]);
        }
        return null;
    }

    // 生成重载方法名
    static string GenMethodName(_MethodBase methodBase, int overrideIndex)
    {
        string name = GetMethodName(methodBase.Method);
        if (IsGenericMethod(methodBase.Method))
        {
            return string.Format("{0}{1}T", name, overrideIndex);
        }
        else
        {
            return string.Format("{0}{1}", name, overrideIndex);
        }
    }

    static void GenOverrideFuncCall(_MethodBase md, bool beIf, int checkTypeOffset, string genMethodName)
    {
        int offset = md.IsStatic ? 0 : 1;
        int ret = md.GetReturnType() == typeof(void) ? 0 : 1;
        string strIf = beIf ? "if " : "else if ";

        if (HasOptionalParam(md.GetParameters()))
        {
            ParameterInfo[] paramInfos = md.GetParameters();
            ParameterInfo param = paramInfos[paramInfos.Length - 1];
            string str = GetTypeStr(param.ParameterType.GetElementType());

            if (paramInfos.Length + offset > 1)
            {
                string strParams = md.GenParamTypes(0);
                sb.AppendFormat("\t\t\t{0}(TypeChecker.CheckTypes<{1}>(L, 1) && TypeChecker.CheckParamsType<{2}>(L, {3}, {4}))\r\n", strIf, strParams, str, paramInfos.Length + offset, GetCountStr(paramInfos.Length + offset - 1));
            }
            else
            {
                sb.AppendFormat("\t\t\t{0}(TypeChecker.CheckParamsType<{1}>(L, {2}, {3}))\r\n", strIf, str, paramInfos.Length + offset, GetCountStr(paramInfos.Length + offset - 1));
            }
        }
        else
        {
            ParameterInfo[] paramInfos = md.GetParameters();

            if (paramInfos.Length + offset > checkTypeOffset)
            {
                string strParams = md.GenParamTypes(checkTypeOffset);
                sb.AppendFormat("\t\t\t{0}(count == {1} && TypeChecker.CheckTypes<{2}>(L, {3}))\r\n", strIf, paramInfos.Length + offset, strParams, checkTypeOffset + 1);
            }
            else
            {
                sb.AppendFormat("\t\t\t{0}(count == {1})\r\n", strIf, paramInfos.Length + offset);
            }
        }

        sb.AppendLineEx("\t\t\t{");
        sb.AppendFormat("\t\t\t\treturn {0}(L);\r\n", genMethodName);
        sb.AppendLineEx("\t\t\t}");
    }

    private static void GenOverrideFunctionBody(string name, _MethodBase methodBase)
    {
        sb.AppendLineEx("\r\n\t[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]");
        sb.AppendFormat("\tstatic int {0}(IntPtr L)\r\n", name);
        sb.AppendLineEx("\t{");

        BeginTry();
        sb.AppendLineEx("\t\t\tint count = LuaDLL.lua_gettop(L);");
        sb.AppendLineEx();

        if (HasAttribute(methodBase.Method, typeof(OverrideDefinedAttribute)))
        {
            GenOverrideDefinedFunc(methodBase.Method);
            sb.AppendFormat("\t\t\treturn 0;\r\n");
        }
        else
        {
            int count = methodBase.ProcessParams(3, false, 0);
            int ret = methodBase.GetReturnType() == typeof(void) ? 0 : 1;
            sb.AppendFormat("\t\t\treturn {0};\r\n", ret + count);
        }

        EndTry();

        sb.AppendLineEx("\t}");
    }

    static void GenOverrideFuncBody(_MethodBase md, bool beIf, int checkTypeOffset)
    {
        int offset = md.IsStatic ? 0 : 1;
        int ret = md.GetReturnType() == typeof(void) ? 0 : 1;
        string strIf = beIf ? "if " : "else if ";

        if (HasOptionalParam(md.GetParameters()))
        {
            ParameterInfo[] paramInfos = md.GetParameters();
            ParameterInfo param = paramInfos[paramInfos.Length - 1];
            string str = GetTypeStr(param.ParameterType.GetElementType());

            if (paramInfos.Length + offset > 1)
            {
                string strParams = md.GenParamTypes(0);
                sb.AppendFormat("\t\t\t{0}(TypeChecker.CheckTypes<{1}>(L, 1) && TypeChecker.CheckParamsType<{2}>(L, {3}, {4}))\r\n", strIf, strParams, str, paramInfos.Length + offset, GetCountStr(paramInfos.Length + offset - 1));
            }
            else
            {
                sb.AppendFormat("\t\t\t{0}(TypeChecker.CheckParamsType<{1}>(L, {2}, {3}))\r\n", strIf, str, paramInfos.Length + offset, GetCountStr(paramInfos.Length + offset - 1));
            }
        }
        else
        {
            ParameterInfo[] paramInfos = md.GetParameters();

            if (paramInfos.Length + offset > checkTypeOffset)
            {
                string strParams = md.GenParamTypes(checkTypeOffset);
                sb.AppendFormat("\t\t\t{0}(count == {1} && TypeChecker.CheckTypes<{2}>(L, {3}))\r\n", strIf, paramInfos.Length + offset, strParams, checkTypeOffset + 1);
            }
            else
            {
                sb.AppendFormat("\t\t\t{0}(count == {1})\r\n", strIf, paramInfos.Length + offset);
            }
        }

        sb.AppendLineEx("\t\t\t{");
        int count = md.ProcessParams(4, false, checkTypeOffset);
        sb.AppendFormat("\t\t\t\treturn {0};\r\n", ret + count);
        sb.AppendLineEx("\t\t\t}");
    }

    static void GenOutFunction()
    {
        if (isStaticClass || CustomSettings.outList.IndexOf(type) < 0)
        {
            return;
        }

        sb.AppendLineEx("\r\n\t[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]");
        sb.AppendLineEx("\tstatic int get_out(IntPtr L)");
        sb.AppendLineEx("\t{");
        sb.AppendFormat("\t\tToLua.PushOut<{0}>(L, new LuaOut<{0}>());\r\n", className);
        sb.AppendLineEx("\t\treturn 1;");
        sb.AppendLineEx("\t}");
    }

}
