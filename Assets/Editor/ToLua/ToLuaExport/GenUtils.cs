using LuaInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

public static partial class ToLuaExport
{

    static void Push(List<_MethodBase> list, _MethodBase r)
    {
        string name = GetMethodName(r.Method);
        int index = list.FindIndex((p) => { return GetMethodName(p.Method) == name && CompareMethod(p, r) >= 0; });

        if (index >= 0)
        {
            if (CompareMethod(list[index], r) == 2)
            {
                Debugger.LogWarning("{0}.{1} has been dropped as function {2} more match lua", className, list[index].GetTotalName(), r.GetTotalName());
                list.RemoveAt(index);
                list.Add(r);
                return;
            }
            else
            {
                Debugger.LogWarning("{0}.{1} has been dropped as function {2} more match lua", className, r.GetTotalName(), list[index].GetTotalName());
                return;
            }
        }

        list.Add(r);
    }


    static string GenParamTypes(ParameterInfo[] p, MethodBase mb, int offset = 0)
    {
        StringBuilder sb = new StringBuilder();
        List<Type> list = new List<Type>();

        if (!mb.IsStatic)
        {
            list.Add(type);
        }

        for (int i = 0; i < p.Length; i++)
        {
            if (IsParams(p[i]))
            {
                continue;
            }

            if (p[i].Attributes != ParameterAttributes.Out)
            {
                list.Add(GetGenericBaseType(mb, p[i].ParameterType));
            }
            else
            {
                Type genericClass = typeof(LuaOut<>);
                Type t = genericClass.MakeGenericType(p[i].ParameterType);
                list.Add(t);
            }
        }

        for (int i = offset; i < list.Count - 1; i++)
        {
            sb.Append(GetTypeOf(list[i], ", "));
        }

        if (list.Count > 0)
        {
            sb.Append(GetTypeOf(list[list.Count - 1], ""));
        }

        return sb.ToString();
    }

    static void ProcessEditorExtend(Type extendType, List<_MethodBase> list)
    {
        if (extendType != null)
        {
            List<MethodInfo> list2 = new List<MethodInfo>();
            list2.AddRange(extendType.GetMethods(BindingFlags.Instance | binding | BindingFlags.DeclaredOnly));

            for (int i = list2.Count - 1; i >= 0; i--)
            {
                if (list2[i].Name.StartsWith("op_") || list2[i].Name.StartsWith("add_") || list2[i].Name.StartsWith("remove_"))
                {
                    if (!IsNeedOp(list2[i].Name))
                    {
                        continue;
                    }
                }

                if (IsUseDefinedAttributee(list2[i]))
                {
                    list.RemoveAll((md) => { return md.Name == list2[i].Name; });
                }
                else
                {
                    int index = list.FindIndex((md) => { return IsMethodEqualExtend(md.Method, list2[i]); });

                    if (index >= 0)
                    {
                        list.RemoveAt(index);
                    }
                }

                if (!IsObsolete(list2[i]))
                {
                    list.Add(new _MethodBase(list2[i]));
                }
            }

            FieldInfo field = extendType.GetField("AdditionNameSpace");

            if (field != null)
            {
                string str = field.GetValue(null) as string;
                string[] spaces = str.Split(new char[] { ';' });

                for (int i = 0; i < spaces.Length; i++)
                {
                    usingList.Add(spaces[i]);
                }
            }
        }
    }


    static void ProcessExtendType(Type extendType, List<_MethodBase> list)
    {
        if (extendType != null)
        {
            List<MethodInfo> list2 = new List<MethodInfo>();
            list2.AddRange(extendType.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly));

            for (int i = list2.Count - 1; i >= 0; i--)
            {
                MethodInfo md = list2[i];

                if (!md.IsDefined(typeof(ExtensionAttribute), false))
                {
                    continue;
                }

                ParameterInfo[] plist = md.GetParameters();
                Type t = plist[0].ParameterType;

                if (t == type || t.IsAssignableFrom(type) || (IsGenericType(md, t) && (type == t.BaseType || type.IsSubclassOf(t.BaseType))))
                {
                    if (!IsObsolete(list2[i]))
                    {
                        _MethodBase mb = new _MethodBase(md);
                        mb.BeExtend = true;
                        list.Add(mb);
                    }
                }
            }
        }
    }

    static void ProcessExtends(List<_MethodBase> list)
    {
        extendName = "ToLua_" + className.Replace(".", "_");
        extendType = Type.GetType(extendName + ", Assembly-CSharp-Editor");
        ProcessEditorExtend(extendType, list);
        string temp = null;

        for (int i = 0; i < extendList.Count; i++)
        {
            ProcessExtendType(extendList[i], list);
            string nameSpace = GetNameSpace(extendList[i], out temp);

            if (!string.IsNullOrEmpty(nameSpace))
            {
                usingList.Add(nameSpace);
            }
        }
    }


    /*static void LuaFuncToDelegate(Type t, string head)
    {        
        MethodInfo mi = t.GetMethod("Invoke");
        ParameterInfo[] pi = mi.GetParameters();            
        int n = pi.Length;

        if (n == 0)
        {
            sb.AppendLineEx("() =>");

            if (mi.ReturnType == typeof(void))
            {
                sb.AppendFormat("{0}{{\r\n{0}\tfunc.Call();\r\n{0}}};\r\n", head);
            }
            else
            {
                sb.AppendFormat("{0}{{\r\n{0}\tfunc.BeginPCall();\r\n", head);
                sb.AppendFormat("{0}\tfunc.PCall();\r\n", head);
                GenLuaFunctionRetValue(sb, mi.ReturnType, head + "\t", "ret");
                sb.AppendFormat("{0}\tfunc.EndPCall();\r\n", head);
                sb.AppendLineEx(head + "\treturn ret;");            
                sb.AppendFormat("{0}}};\r\n", head);
            }

            return;
        }

        sb.AppendFormat("(param0");

        for (int i = 1; i < n; i++)
        {
            sb.AppendFormat(", param{0}", i);
        }

        sb.AppendFormat(") =>\r\n{0}{{\r\n{0}", head);
        sb.AppendLineEx("\tfunc.BeginPCall();");

        for (int i = 0; i < n; i++)
        {
            string push = GetPushFunction(pi[i].ParameterType);

            if (!IsParams(pi[i]))
            {
                if (pi[i].ParameterType == typeof(byte[]) && IsByteBuffer(t))
                {
                    sb.AppendFormat("{0}\tfunc.PushByteBuffer(param{1});\r\n", head, i);
                }
                else
                {
                    sb.AppendFormat("{0}\tfunc.{1}(param{2});\r\n", head, push, i);
                }
            }
            else
            {
                sb.AppendLineEx();
                sb.AppendFormat("{0}\tfor (int i = 0; i < param{1}.Length; i++)\r\n", head, i);
                sb.AppendLineEx(head + "\t{");
                sb.AppendFormat("{0}\t\tfunc.{1}(param{2}[i]);\r\n", head, push, i);
                sb.AppendLineEx(head + "\t}\r\n");
            }
        }

        sb.AppendFormat("{0}\tfunc.PCall();\r\n", head);

        if (mi.ReturnType == typeof(void))
        {
            for (int i = 0; i < pi.Length; i++)
            {
                if ((pi[i].Attributes & ParameterAttributes.Out) != ParameterAttributes.None)
                {
                    GenLuaFunctionRetValue(sb, pi[i].ParameterType, head + "\t", "param" + i, true);
                }
            }

            sb.AppendFormat("{0}\tfunc.EndPCall();\r\n", head);
        }
        else
        {
            GenLuaFunctionRetValue(sb, mi.ReturnType, head + "\t", "ret");

            for (int i = 0; i < pi.Length; i++)
            {
                if ((pi[i].Attributes & ParameterAttributes.Out) != ParameterAttributes.None)
                {
                    GenLuaFunctionRetValue(sb, pi[i].ParameterType, head + "\t", "param" + i, true);
                }
            }

            sb.AppendFormat("{0}\tfunc.EndPCall();\r\n", head);
            sb.AppendLineEx(head + "\treturn ret;");            
        }

        sb.AppendFormat("{0}}};\r\n", head);
    }*/

    //static void GenToStringFunction()
    //{                
    //    if ((op & MetaOp.ToStr) == 0)
    //    {
    //        return;
    //    }

    //    sb.AppendLineEx("\r\n\t[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]");
    //    sb.AppendLineEx("\tstatic int Lua_ToString(IntPtr L)");
    //    sb.AppendLineEx("\t{");
    //    sb.AppendLineEx("\t\tobject obj = ToLua.ToObject(L, 1);\r\n");

    //    sb.AppendLineEx("\t\tif (obj != null)");
    //    sb.AppendLineEx("\t\t{");
    //    sb.AppendLineEx("\t\t\tLuaDLL.lua_pushstring(L, obj.ToString());");
    //    sb.AppendLineEx("\t\t}");
    //    sb.AppendLineEx("\t\telse");
    //    sb.AppendLineEx("\t\t{");
    //    sb.AppendLineEx("\t\t\tLuaDLL.lua_pushnil(L);");
    //    sb.AppendLineEx("\t\t}");
    //    sb.AppendLineEx();
    //    sb.AppendLineEx("\t\treturn 1;");
    //    sb.AppendLineEx("\t}");
    //}



    static void BeginTry()
    {
        sb.AppendLineEx("\t\ttry");
        sb.AppendLineEx("\t\t{");
    }

    static void EndTry()
    {
        sb.AppendLineEx("\t\t}");
        sb.AppendLineEx("\t\tcatch (Exception e)");
        sb.AppendLineEx("\t\t{");
        sb.AppendLineEx("\t\t\treturn LuaDLL.toluaL_exception(L, e);");
        sb.AppendLineEx("\t\t}");
    }
    static void BeginCodeGen()
    {
        sb.AppendFormat("internal class {0}Wrap\r\n", wrapClassName);
        sb.AppendLineEx("{");
    }

    static void EndCodeGen(string dir)
    {
        sb.AppendLineEx("}\r\n");
        SaveFile(dir + wrapClassName + "Wrap.cs");
    }
    static void ProcessArg(Type varType, string head, string arg, int stackPos, bool beCheckTypes = false, bool beParams = false, bool beOutArg = false)
    {
        varType = GetRefBaseType(varType);
        string str = GetTypeStr(varType);
        string checkStr = beCheckTypes ? "To" : "Check";

        if (beOutArg)
        {
            if (varType.IsValueType)
            {
                sb.AppendFormat("{0}{1} {2};\r\n", head, str, arg);
            }
            else
            {
                sb.AppendFormat("{0}{1} {2} = null;\r\n", head, str, arg);
            }
        }
        else if (varType == typeof(bool))
        {
            string chkstr = beCheckTypes ? "lua_toboolean" : "luaL_checkboolean";
            sb.AppendFormat("{0}bool {1} = LuaDLL.{2}(L, {3});\r\n", head, arg, chkstr, stackPos);
        }
        else if (varType == typeof(string))
        {
            sb.AppendFormat("{0}string {1} = ToLua.{2}String(L, {3});\r\n", head, arg, checkStr, stackPos);
        }
        else if (varType == typeof(IntPtr))
        {
            sb.AppendFormat("{0}{1} {2} = ToLua.CheckIntPtr(L, {3});\r\n", head, str, arg, stackPos);
        }
        else if (varType == typeof(long))
        {
            string chkstr = beCheckTypes ? "tolua_toint64" : "tolua_checkint64";
            sb.AppendFormat("{0}{1} {2} = LuaDLL.{3}(L, {4});\r\n", head, str, arg, chkstr, stackPos);
        }
        else if (varType == typeof(ulong))
        {
            string chkstr = beCheckTypes ? "tolua_touint64" : "tolua_checkuint64";
            sb.AppendFormat("{0}{1} {2} = LuaDLL.{3}(L, {4});\r\n", head, str, arg, chkstr, stackPos);
        }
        else if (varType.IsPrimitive || IsNumberEnum(varType))
        {
            string chkstr = beCheckTypes ? "lua_tonumber" : "luaL_checknumber";
            sb.AppendFormat("{0}{1} {2} = ({1})LuaDLL.{3}(L, {4});\r\n", head, str, arg, chkstr, stackPos);
        }
        else if (varType == typeof(LuaFunction))
        {
            sb.AppendFormat("{0}LuaFunction {1} = ToLua.{2}LuaFunction(L, {3});\r\n", head, arg, checkStr, stackPos);
        }
        else if (varType.IsSubclassOf(typeof(System.MulticastDelegate)))
        {
            //if (beCheckTypes)
            //{
            //    sb.AppendFormat("{0}{1} {2} = ({1})ToLua.ToObject(L, {3});\r\n", head, str, arg, stackPos);
            //}
            //else
            {
                sb.AppendFormat("{0}{1} {2} = ({1})ToLua.CheckDelegate<{1}>(L, {3});\r\n", head, str, arg, stackPos);
            }
        }
        else if (varType == typeof(LuaTable))
        {
            sb.AppendFormat("{0}LuaTable {1} = ToLua.{2}LuaTable(L, {3});\r\n", head, arg, checkStr, stackPos);
        }
        else if (varType == typeof(Vector2))
        {
            sb.AppendFormat("{0}UnityEngine.Vector2 {1} = ToLua.ToVector2(L, {2});\r\n", head, arg, stackPos);
        }
        else if (varType == typeof(Vector3))
        {
            sb.AppendFormat("{0}UnityEngine.Vector3 {1} = ToLua.ToVector3(L, {2});\r\n", head, arg, stackPos);
        }
        else if (varType == typeof(Vector4))
        {
            sb.AppendFormat("{0}UnityEngine.Vector4 {1} = ToLua.ToVector4(L, {2});\r\n", head, arg, stackPos);
        }
        else if (varType == typeof(Quaternion))
        {
            sb.AppendFormat("{0}UnityEngine.Quaternion {1} = ToLua.ToQuaternion(L, {2});\r\n", head, arg, stackPos);
        }
        else if (varType == typeof(Color))
        {
            sb.AppendFormat("{0}UnityEngine.Color {1} = ToLua.ToColor(L, {2});\r\n", head, arg, stackPos);
        }
        else if (varType == typeof(Color32))
        {
            sb.AppendFormat("{0}UnityEngine.Color32 {1} = ToLua.ToColor32(L, {2});\r\n", head, arg, stackPos);
        }
        else if (varType == typeof(Ray))
        {
            sb.AppendFormat("{0}UnityEngine.Ray {1} = ToLua.ToRay(L, {2});\r\n", head, arg, stackPos);
        }
        else if (varType == typeof(Bounds))
        {
            sb.AppendFormat("{0}UnityEngine.Bounds {1} = ToLua.ToBounds(L, {2});\r\n", head, arg, stackPos);
        }
        else if (varType == typeof(LayerMask))
        {
            sb.AppendFormat("{0}UnityEngine.LayerMask {1} = ToLua.ToLayerMask(L, {2});\r\n", head, arg, stackPos);
        }
        else if (varType == typeof(object))
        {
            sb.AppendFormat("{0}object {1} = ToLua.ToVarObject(L, {2});\r\n", head, arg, stackPos);
        }
        else if (varType == typeof(LuaByteBuffer))
        {
            sb.AppendFormat("{0}LuaByteBuffer {1} = new LuaByteBuffer(ToLua.CheckByteBuffer(L, {2}));\r\n", head, arg, stackPos);
        }
        else if (varType == typeof(Type))
        {
            if (beCheckTypes)
            {
                sb.AppendFormat("{0}System.Type {1} = (System.Type)ToLua.ToObject(L, {2});\r\n", head, arg, stackPos);
            }
            else
            {
                sb.AppendFormat("{0}System.Type {1} = ToLua.CheckMonoType(L, {2});\r\n", head, arg, stackPos);
            }
        }
        else if (IsIEnumerator(varType))
        {
            if (beCheckTypes)
            {
                sb.AppendFormat("{0}System.Collections.IEnumerator {1} = (System.Collections.IEnumerator)ToLua.ToObject(L, {2});\r\n", head, arg, stackPos);
            }
            else
            {
                sb.AppendFormat("{0}System.Collections.IEnumerator {1} = ToLua.CheckIter(L, {2});\r\n", head, arg, stackPos);
            }
        }
        else if (varType.IsArray && varType.GetArrayRank() == 1)
        {
            Type et = varType.GetElementType();
            string atstr = GetTypeStr(et);
            string fname;
            bool flag = false;                          //是否模版函数
            bool isObject = false;

            if (et.IsPrimitive)
            {
                if (beParams)
                {
                    if (et == typeof(bool))
                    {
                        fname = beCheckTypes ? "ToParamsBool" : "CheckParamsBool";
                    }
                    else if (et == typeof(char))
                    {
                        //char用的多些，特殊处理一下减少gcalloc
                        fname = beCheckTypes ? "ToParamsChar" : "CheckParamsChar";
                    }
                    else
                    {
                        flag = true;
                        fname = beCheckTypes ? "ToParamsNumber" : "CheckParamsNumber";
                    }
                }
                else if (et == typeof(char))
                {
                    fname = "CheckCharBuffer";
                }
                else if (et == typeof(byte))
                {
                    fname = "CheckByteBuffer";
                }
                else if (et == typeof(bool))
                {
                    fname = "CheckBoolArray";
                }
                else
                {
                    fname = beCheckTypes ? "ToNumberArray" : "CheckNumberArray";
                    flag = true;
                }
            }
            else if (et == typeof(string))
            {
                if (beParams)
                {
                    fname = beCheckTypes ? "ToParamsString" : "CheckParamsString";
                }
                else
                {
                    fname = beCheckTypes ? "ToStringArray" : "CheckStringArray";
                }
            }
            else //if (et == typeof(object))
            {
                flag = true;

                if (et == typeof(object))
                {
                    isObject = true;
                    flag = false;
                }

                if (beParams)
                {
                    fname = (isObject || beCheckTypes) ? "ToParamsObject" : "CheckParamsObject";
                }
                else
                {
                    if (et.IsValueType)
                    {
                        fname = beCheckTypes ? "ToStructArray" : "CheckStructArray";
                    }
                    else
                    {
                        fname = beCheckTypes ? "ToObjectArray" : "CheckObjectArray";
                    }
                }

                if (et == typeof(UnityEngine.Object))
                {
                    ambig |= ObjAmbig.U3dObj;
                }
            }

            if (flag)
            {
                if (beParams)
                {
                    if (!isObject)
                    {
                        sb.AppendFormat("{0}{1}[] {2} = ToLua.{3}<{1}>(L, {4}, {5});\r\n", head, atstr, arg, fname, stackPos, GetCountStr(stackPos - 1));
                    }
                    else
                    {
                        sb.AppendFormat("{0}object[] {1} = ToLua.{2}(L, {3}, {4});\r\n", head, arg, fname, stackPos, GetCountStr(stackPos - 1));
                    }
                }
                else
                {
                    sb.AppendFormat("{0}{1}[] {2} = ToLua.{3}<{1}>(L, {4});\r\n", head, atstr, arg, fname, stackPos);
                }
            }
            else
            {
                if (beParams)
                {
                    sb.AppendFormat("{0}{1}[] {2} = ToLua.{3}(L, {4}, {5});\r\n", head, atstr, arg, fname, stackPos, GetCountStr(stackPos - 1));
                }
                else
                {
                    sb.AppendFormat("{0}{1}[] {2} = ToLua.{3}(L, {4});\r\n", head, atstr, arg, fname, stackPos);
                }
            }
        }
        else if (varType.IsGenericType && varType.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            Type t = TypeChecker.GetNullableType(varType);

            if (beCheckTypes)
            {
                sb.AppendFormat("{0}{1} {2} = ToLua.ToNullable<{3}>(L, {4});\r\n", head, str, arg, GetTypeStr(t), stackPos);
            }
            else
            {
                sb.AppendFormat("{0}{1} {2} = ToLua.CheckNullable<{3}>(L, {4});\r\n", head, str, arg, GetTypeStr(t), stackPos);
            }
        }
        else if (varType.IsValueType && !varType.IsEnum)
        {
            string func = beCheckTypes ? "To" : "Check";
            sb.AppendFormat("{0}{1} {2} = StackTraits<{1}>.{3}(L, {4});\r\n", head, str, arg, func, stackPos);
        }
        else //从object派生但不是object
        {
            if (beCheckTypes)
            {
                sb.AppendFormat("{0}{1} {2} = ({1})ToLua.ToObject(L, {3});\r\n", head, str, arg, stackPos);
            }
            //else if (varType == typeof(UnityEngine.TrackedReference) || typeof(UnityEngine.TrackedReference).IsAssignableFrom(varType))
            //{
            //    sb.AppendFormat("{3}{0} {1} = ({0})ToLua.CheckTrackedReference(L, {2}, typeof({0}));\r\n", str, arg, stackPos, head);
            //}
            //else if (typeof(UnityEngine.Object).IsAssignableFrom(varType))
            //{
            //    sb.AppendFormat("{3}{0} {1} = ({0})ToLua.CheckUnityObject(L, {2}, typeof({0}));\r\n", str, arg, stackPos, head);
            //}
            else
            {
                if (IsSealedType(varType))
                {
                    sb.AppendFormat("{0}{1} {2} = ({1})ToLua.CheckObject(L, {3}, typeof({1}));\r\n", head, str, arg, stackPos);
                }
                else
                {
                    sb.AppendFormat("{0}{1} {2} = ({1})ToLua.CheckObject<{1}>(L, {3});\r\n", head, str, arg, stackPos);
                }
            }
        }
    }

    static void GenPushStr(Type t, string arg, string head, bool isByteBuffer = false)
    {
        if (t == typeof(int))
        {
            sb.AppendFormat("{0}LuaDLL.lua_pushinteger(L, {1});\r\n", head, arg);
        }
        else if (t == typeof(bool))
        {
            sb.AppendFormat("{0}LuaDLL.lua_pushboolean(L, {1});\r\n", head, arg);
        }
        else if (t == typeof(string))
        {
            sb.AppendFormat("{0}LuaDLL.lua_pushstring(L, {1});\r\n", head, arg);
        }
        else if (t == typeof(IntPtr))
        {
            sb.AppendFormat("{0}LuaDLL.lua_pushlightuserdata(L, {1});\r\n", head, arg);
        }
        else if (t == typeof(long))
        {
            sb.AppendFormat("{0}LuaDLL.tolua_pushint64(L, {1});\r\n", head, arg);
        }
        else if (t == typeof(ulong))
        {
            sb.AppendFormat("{0}LuaDLL.tolua_pushuint64(L, {1});\r\n", head, arg);
        }
        else if ((t.IsPrimitive))
        {
            sb.AppendFormat("{0}LuaDLL.lua_pushnumber(L, {1});\r\n", head, arg);
        }
        else if (t.IsEnum)
        {
            sb.AppendFormat("{0}LuaDLL.lua_pushinteger(L, (int){1});\r\n", head, arg);
        }
        else
        {
            if (isByteBuffer && t == typeof(byte[]))
            {
                sb.AppendFormat("{0}LuaDLL.tolua_pushlstring(L, {1}, {1}.Length);\r\n", head, arg);
            }
            else
            {
                string str = GetPushFunction(t);
                sb.AppendFormat("{0}ToLua.{1}(L, {2});\r\n", head, str, arg);
            }
        }
    }

}
