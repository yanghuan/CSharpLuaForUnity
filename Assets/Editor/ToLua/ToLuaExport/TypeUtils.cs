using LuaInterface;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

public static partial class ToLuaExport
{

    //没有未知类型的模版类型List<int> 返回false, List<T>返回true
    static bool IsGenericConstraintType(Type t)
    {
        if (!t.IsGenericType)
        {
            return t.IsGenericParameter || t == typeof(System.ValueType);
        }

        Type[] types = t.GetGenericArguments();

        for (int i = 0; i < types.Length; i++)
        {
            Type t1 = types[i];

            if (t1.IsGenericParameter || t1 == typeof(System.ValueType))
            {
                return true;
            }

            if (IsGenericConstraintType(t1))
            {
                return true;
            }
        }

        return false;
    }
    static bool IsGenericConstraints(Type[] constraints)
    {
        for (int i = 0; i < constraints.Length; i++)
        {
            if (!IsGenericConstraintType(constraints[i]))
            {
                return false;
            }
        }

        return true;
    }
    static int GetMethodType(MethodBase md, out PropertyInfo pi)
    {
        pi = null;

        if (!md.IsSpecialName)
        {
            return 0;
        }

        int methodType = 0;
        int pos = allProps.FindIndex((p) => { return p.GetGetMethod() == md || p.GetSetMethod() == md; });

        if (pos >= 0)
        {
            methodType = 1;
            pi = allProps[pos];

            if (md == pi.GetGetMethod())
            {
                if (md.GetParameters().Length > 0)
                {
                    methodType = 2;
                }
            }
            else if (md == pi.GetSetMethod())
            {
                if (md.GetParameters().Length > 1)
                {
                    methodType = 2;
                }
            }
        }

        return methodType;
    }

    static Type GetGenericBaseType(MethodBase md, Type t)
    {
        if (!md.IsGenericMethod)
        {
            return t;
        }

        List<Type> list = new List<Type>(md.GetGenericArguments());

        if (list.Contains(t))
        {
            return t.BaseType;
        }

        return t;
    }

    static bool IsNumberEnum(Type t)
    {
        if (t.IsEnum)
        {
            return true;
        }

        if (t == typeof(BindingFlags))
        {
            return true;
        }
        return false;
    }

    static bool Is64bit(Type t)
    {
        return t == typeof(long) || t == typeof(ulong);
    }
    static bool IsParams(ParameterInfo param)
    {
        return param.GetCustomAttributes(typeof(ParamArrayAttribute), false).Length > 0;
    }


    static bool IsSealedType(Type t)
    {
        if (t.IsSealed || CustomSettings.sealedList.Contains(t))
        {
            return true;
        }

        if (t.IsGenericType && (t.GetGenericTypeDefinition() == typeof(List<>) || t.GetGenericTypeDefinition() == typeof(Dictionary<,>)))
        {
            return true;
        }

        return false;
    }

    static bool IsIEnumerator(Type t)
    {
        if (t == typeof(IEnumerator) || t == typeof(CharEnumerator)) return true;

        if (typeof(IEnumerator).IsAssignableFrom(t))
        {
            if (t.IsGenericType)
            {
                Type gt = t.GetGenericTypeDefinition();

                if (gt == typeof(List<>.Enumerator) || gt == typeof(Dictionary<,>.Enumerator))
                {
                    return true;
                }
            }
        }

        return false;
    }
    static void CheckObject(string head, Type type, string className, int pos)
    {
        if (type == typeof(object))
        {
            sb.AppendFormat("{0}var obj = ToLua.CheckObject(L, {1});\r\n", head, pos);
        }
        else if (type == typeof(Type))
        {
            sb.AppendFormat("{0}var obj = ToLua.CheckMonoType(L, {2});\r\n", head, className, pos);
        }
        else if (IsIEnumerator(type))
        {
            sb.AppendFormat("{0}var obj = ToLua.CheckIEnumerator(L, {2});\r\n", head, className, pos);
        }
        else
        {
            if (IsSealedType(type))
            {
                sb.AppendFormat("{0}var obj = ({1})ToLua.CheckObject(L, {2}, typeof({1}));\r\n", head, className, pos);
            }
            else
            {
                sb.AppendFormat("{0}var obj = ({1})ToLua.CheckObject<{1}>(L, {2});\r\n", head, className, pos);
            }
        }
    }

    static void ToObject(string head, Type type, string className, int pos)
    {
        if (type == typeof(object))
        {
            sb.AppendFormat("{0}var obj = ToLua.ToObject(L, {1});\r\n", head, pos);
        }
        else
        {
            sb.AppendFormat("{0}var obj = ({1})ToLua.ToObject(L, {2});\r\n", head, className, pos);
        }
    }
    static int[] CheckCheckTypePos<T>(List<T> list) where T : _MethodBase
    {
        int[] map = new int[list.Count];

        for (int i = 0; i < list.Count;)
        {
            if (HasOptionalParam(list[i].GetParameters()))
            {
                if (list[0].IsConstructor)
                {
                    for (int k = 0; k < map.Length; k++)
                    {
                        map[k] = 1;
                    }
                }
                else
                {
                    Array.Clear(map, 0, map.Length);
                }

                return map;
            }

            int c1 = list[i].GetParamsCount();
            int count = c1;
            map[i] = count;
            int j = i + 1;

            for (; j < list.Count; j++)
            {
                int c2 = list[j].GetParamsCount();

                if (c1 == c2)
                {
                    count = Mathf.Min(count, list[i].GetEqualParamsCount(list[j]));
                }
                else
                {
                    map[j] = c2;
                    break;
                }

                for (int m = i; m <= j; m++)
                {
                    map[m] = count;
                }
            }

            i = j;
        }

        return map;
    }


    public static string CombineTypeStr(string space, string name)
    {
        if (string.IsNullOrEmpty(space))
        {
            return name;
        }
        else
        {
            return space + "." + name;
        }
    }

    public static string GetBaseTypeStr(Type t)
    {
        if (t.IsGenericType)
        {
            return LuaMisc.GetTypeName(t);
        }
        else
        {
            return t.FullName.Replace("+", ".");
        }
    }

    //获取类型名字
    public static string GetTypeStr(Type t)
    {
        if (t.IsByRef)
        {
            t = t.GetElementType();
            return GetTypeStr(t);
        }
        else if (t.IsArray)
        {
            string str = GetTypeStr(t.GetElementType());
            str += LuaMisc.GetArrayRank(t);
            return str;
        }
        else if (t == extendType)
        {
            return GetTypeStr(type);
        }
        else if (IsIEnumerator(t))
        {
            return LuaMisc.GetTypeName(typeof(IEnumerator));
        }

        return LuaMisc.GetTypeName(t);
    }

    //获取 typeof(string) 这样的名字
    static string GetTypeOf(Type t, string sep)
    {
        string str;

        if (t.IsByRef)
        {
            t = t.GetElementType();
        }

        if (IsNumberEnum(t))
        {
            str = string.Format("uint{0}", sep);
        }
        else if (IsIEnumerator(t))
        {
            str = string.Format("{0}{1}", GetTypeStr(typeof(IEnumerator)), sep);
        }
        else
        {
            str = string.Format("{0}{1}", GetTypeStr(t), sep);
        }

        return str;
    }
    static void CheckObjectNull()
    {
        if (type.IsValueType)
        {
            sb.AppendLineEx("\t\t\tif (o == null)");
        }
        else
        {
            sb.AppendLineEx("\t\t\tif (obj == null)");
        }
    }


    public static bool IsByteBuffer(Type type)
    {
        object[] attrs = type.GetCustomAttributes(true);

        for (int j = 0; j < attrs.Length; j++)
        {
            Type t = attrs[j].GetType();

            if (t == typeof(LuaByteBufferAttribute))
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsByteBuffer(MemberInfo mb)
    {
        object[] attrs = mb.GetCustomAttributes(true);

        for (int j = 0; j < attrs.Length; j++)
        {
            Type t = attrs[j].GetType();

            if (t == typeof(LuaByteBufferAttribute))
            {
                return true;
            }
        }

        return false;
    }
    //-1 不存在替换, 1 保留左面， 2 保留右面
    static int CompareMethod(_MethodBase l, _MethodBase r)
    {
        int s = 0;

        if (!CompareParmsCount(l, r))
        {
            return -1;
        }
        else
        {
            ParameterInfo[] lp = l.GetParameters();
            ParameterInfo[] rp = r.GetParameters();

            List<Type> ll = new List<Type>();
            List<Type> lr = new List<Type>();

            if (!l.IsStatic)
            {
                ll.Add(type);
            }

            if (!r.IsStatic)
            {
                lr.Add(type);
            }

            for (int i = 0; i < lp.Length; i++)
            {
                ll.Add(GetParameterType(lp[i]));
            }

            for (int i = 0; i < rp.Length; i++)
            {
                lr.Add(GetParameterType(rp[i]));
            }

            for (int i = 0; i < ll.Count; i++)
            {
                if (ll[i] == lr[i])
                {
                    continue;
                }
                else
                {
                    return -1;
                }
            }

            if (s == 0 && l.IsStatic)
            {
                s = 2;
            }
        }

        return s;
    }


    static Type GetRefBaseType(Type argType)
    {
        if (argType.IsByRef)
        {
            return argType.GetElementType();
        }

        return argType;
    }

    static bool CompareParmsCount(_MethodBase l, _MethodBase r)
    {
        if (l == r)
        {
            return false;
        }

        int c1 = l.IsStatic ? 0 : 1;
        int c2 = r.IsStatic ? 0 : 1;

        c1 += l.GetParameters().Length;
        c2 += r.GetParameters().Length;

        return c1 == c2;
    }
    public static bool IsObsolete(MemberInfo mb)
    {
        object[] attrs = mb.GetCustomAttributes(true);

        for (int j = 0; j < attrs.Length; j++)
        {
            Type t = attrs[j].GetType();

            if (t == typeof(System.ObsoleteAttribute) || t == typeof(NoToLuaAttribute) || t == typeof(MonoPInvokeCallbackAttribute) ||
                t.Name == "MonoNotSupportedAttribute" || t.Name == "MonoTODOAttribute") // || t.ToString() == "UnityEngine.WrapperlessIcall")
            {
                return true;
            }
        }

        if (IsMemberFilter(mb))
        {
            return true;
        }

        return false;
    }

    public static bool HasAttribute(MemberInfo mb, Type atrtype)
    {
        object[] attrs = mb.GetCustomAttributes(true);

        for (int j = 0; j < attrs.Length; j++)
        {
            Type t = attrs[j].GetType();

            if (t == atrtype)
            {
                return true;
            }
        }

        return false;
    }

    static void GenEnum()
    {
        fields = type.GetFields(BindingFlags.GetField | BindingFlags.Public | BindingFlags.Static);
        List<FieldInfo> list = new List<FieldInfo>(fields);

        for (int i = list.Count - 1; i > 0; i--)
        {
            if (IsObsolete(list[i]))
            {
                list.RemoveAt(i);
            }
        }

        fields = list.ToArray();

        sb.AppendLineEx("\tpublic static void Register(LuaState L)");
        sb.AppendLineEx("\t{");
        sb.AppendFormat("\t\tL.BeginEnum(typeof({0}));\r\n", className);

        for (int i = 0; i < fields.Length; i++)
        {
            sb.AppendFormat("\t\tL.RegVar(\"{0}\", get_{0}, null);\r\n", fields[i].Name);
        }

        sb.AppendFormat("\t\tL.RegFunction(\"IntToEnum\", IntToEnum);\r\n");
        sb.AppendFormat("\t\tL.EndEnum();\r\n");
        sb.AppendFormat("\t\tTypeTraits<{0}>.Check = CheckType;\r\n", className);
        sb.AppendFormat("\t\tStackTraits<{0}>.Push = Push;\r\n", className);
        sb.AppendLineEx("\t}");
        sb.AppendLineEx();

        sb.AppendFormat("\tstatic void Push(IntPtr L, {0} arg)\r\n", className);
        sb.AppendLineEx("\t{");
        sb.AppendLineEx("\t\tToLua.Push(L, arg);");
        sb.AppendLineEx("\t}");
        sb.AppendLineEx();

        sb.AppendLineEx("\tstatic bool CheckType(IntPtr L, int pos)");
        sb.AppendLineEx("\t{");
        sb.AppendFormat("\t\treturn TypeChecker.CheckEnumType(typeof({0}), L, pos);\r\n", className);
        sb.AppendLineEx("\t}");

        for (int i = 0; i < fields.Length; i++)
        {
            sb.AppendLineEx("\r\n\t[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]");
            sb.AppendFormat("\tstatic int get_{0}(IntPtr L)\r\n", fields[i].Name);
            sb.AppendLineEx("\t{");
            sb.AppendFormat("\t\tToLua.Push(L, {0}.{1});\r\n", className, fields[i].Name);
            sb.AppendLineEx("\t\treturn 1;");
            sb.AppendLineEx("\t}");
        }

        sb.AppendLineEx("\r\n\t[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]");
        sb.AppendLineEx("\tstatic int IntToEnum(IntPtr L)");
        sb.AppendLineEx("\t{");
        sb.AppendLineEx("\t\tint arg0 = (int)LuaDLL.lua_tonumber(L, 1);");
        sb.AppendFormat("\t\t{0} o = ({0})arg0;\r\n", className);
        sb.AppendLineEx("\t\tToLua.Push(L, o);");
        sb.AppendLineEx("\t\treturn 1;");
        sb.AppendLineEx("\t}");
    }


    static string GetReturnValue(Type t)
    {
        if (t.IsPrimitive)
        {
            if (t == typeof(bool))
            {
                return "false";
            }
            else if (t == typeof(char))
            {
                return "'\\0'";
            }
            else
            {
                return "0";
            }
        }
        else if (!t.IsValueType)
        {
            return "null";
        }
        else
        {
            return string.Format("default({0})", GetTypeStr(t));
        }
    }

    static bool IsUseDefinedAttributee(MemberInfo mb)
    {
        object[] attrs = mb.GetCustomAttributes(false);

        for (int j = 0; j < attrs.Length; j++)
        {
            Type t = attrs[j].GetType();

            if (t == typeof(UseDefinedAttribute))
            {
                return true;
            }
        }

        return false;
    }

    static bool IsMethodEqualExtend(MethodBase a, MethodBase b)
    {
        if (a.Name != b.Name)
        {
            return false;
        }

        int c1 = a.IsStatic ? 0 : 1;
        int c2 = b.IsStatic ? 0 : 1;

        c1 += a.GetParameters().Length;
        c2 += b.GetParameters().Length;

        if (c1 != c2) return false;

        ParameterInfo[] lp = a.GetParameters();
        ParameterInfo[] rp = b.GetParameters();

        List<Type> ll = new List<Type>();
        List<Type> lr = new List<Type>();

        if (!a.IsStatic)
        {
            ll.Add(type);
        }

        if (!b.IsStatic)
        {
            lr.Add(type);
        }

        for (int i = 0; i < lp.Length; i++)
        {
            ll.Add(GetParameterType(lp[i]));
        }

        for (int i = 0; i < rp.Length; i++)
        {
            lr.Add(GetParameterType(rp[i]));
        }

        for (int i = 0; i < ll.Count; i++)
        {
            if (ll[i] != lr[i])
            {
                return false;
            }
        }

        return true;
    }

    static bool IsGenericType(MethodInfo md, Type t)
    {
        Type[] list = md.GetGenericArguments();

        for (int i = 0; i < list.Length; i++)
        {
            if (list[i] == t)
            {
                return true;
            }
        }

        return false;
    }
    static void GetDelegateTypeFromMethodParams(_MethodBase m)
    {
        if (m.IsGenericMethod)
        {
            return;
        }

        ParameterInfo[] pifs = m.GetParameters();

        for (int k = 0; k < pifs.Length; k++)
        {
            Type t = pifs[k].ParameterType;

            if (IsDelegateType(t))
            {
                eventSet.Add(t);
            }
        }
    }


    static string RemoveChar(string str, char c)
    {
        int index = str.IndexOf(c);

        while (index > 0)
        {
            str = str.Remove(index, 1);
            index = str.IndexOf(c);
        }

        return str;
    }

    public static string ConvertToLibSign(string str)
    {
        if (string.IsNullOrEmpty(str))
        {
            return null;
        }

        str = str.Replace('<', '_');
        str = RemoveChar(str, '>');
        str = str.Replace('[', 's');
        str = RemoveChar(str, ']');
        str = str.Replace('.', '_');
        return str.Replace(',', '_');
    }

    public static string GetNameSpace(Type t, out string libName)
    {
        if (t.IsGenericType)
        {
            return GetGenericNameSpace(t, out libName);
        }
        else
        {
            string space = t.FullName;

            if (space.Contains("+"))
            {
                space = space.Replace('+', '.');
                int index = space.LastIndexOf('.');
                libName = space.Substring(index + 1);
                return space.Substring(0, index);
            }
            else
            {
                libName = t.Namespace == null ? space : space.Substring(t.Namespace.Length + 1);
                return t.Namespace;
            }
        }
    }

    static string GetGenericNameSpace(Type t, out string libName)
    {
        Type[] gArgs = t.GetGenericArguments();
        string typeName = t.FullName;
        int count = gArgs.Length;
        int pos = typeName.IndexOf("[");
        typeName = typeName.Substring(0, pos);

        string str = null;
        string name = null;
        int offset = 0;
        pos = typeName.IndexOf("+");

        while (pos > 0)
        {
            str = typeName.Substring(0, pos);
            typeName = typeName.Substring(pos + 1);
            pos = str.IndexOf('`');

            if (pos > 0)
            {
                count = (int)(str[pos + 1] - '0');
                str = str.Substring(0, pos);
                str += "<" + string.Join(",", LuaMisc.GetGenericName(gArgs, offset, count)) + ">";
                offset += count;
            }

            name = CombineTypeStr(name, str);
            pos = typeName.IndexOf("+");
        }

        string space = name;
        str = typeName;

        if (offset < gArgs.Length)
        {
            pos = str.IndexOf('`');
            count = (int)(str[pos + 1] - '0');
            str = str.Substring(0, pos);
            str += "<" + string.Join(",", LuaMisc.GetGenericName(gArgs, offset, count)) + ">";
        }

        libName = str;

        if (string.IsNullOrEmpty(space))
        {
            space = t.Namespace;

            if (space != null)
            {
                libName = str.Substring(space.Length + 1);
            }
        }

        return space;
    }

    static Type GetParameterType(ParameterInfo info)
    {
        if (info.ParameterType == extendType)
        {
            return type;
        }

        return info.ParameterType;
    }
    public static bool IsMemberFilter(MemberInfo mi)
    {
        return memberInfoFilter.Contains(mi) || memberFilter.Contains(type.Name + "." + mi.Name);
    }

    public static bool IsMemberFilter(Type t)
    {
        string name = LuaMisc.GetTypeName(t);
        return memberInfoFilter.Contains(t) || memberFilter.Find((p) => { return name.Contains(p); }) != null;
    }

    static string GetCountStr(int count)
    {
        if (count != 0)
        {
            return string.Format("count - {0}", count);
        }

        return "count";
    }

    static int GetDefalutParamCount(MethodBase md)
    {
        int count = 0;
        ParameterInfo[] infos = md.GetParameters();

        for (int i = 0; i < infos.Length; i++)
        {
            if (!(infos[i].DefaultValue is DBNull))
            {
                ++count;
            }
        }

        return count;
    }

    static int GetOptionalParamPos(ParameterInfo[] infos)
    {
        for (int i = 0; i < infos.Length; i++)
        {
            if (IsParams(infos[i]))
            {
                return i;
            }
        }

        return -1;
    }

    static bool HasOptionalParam(ParameterInfo[] infos)
    {
        for (int i = 0; i < infos.Length; i++)
        {
            if (IsParams(infos[i]))
            {
                return true;
            }
        }

        return false;
    }

}
