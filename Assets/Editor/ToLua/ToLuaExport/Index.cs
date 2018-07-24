using LuaInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

public static partial class ToLuaExport
{

    static void GenGetFieldStr(string varName, Type varType, bool isStatic, bool isByteBuffer, bool beOverride = false)
    {
        sb.AppendLineEx("\r\n\t[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]");
        sb.AppendFormat("\tstatic int {0}_{1}(IntPtr L)\r\n", beOverride ? "_get" : "get", varName);
        sb.AppendLineEx("\t{");

        if (isStatic)
        {
            string arg = string.Format("{0}.{1}", className, varName);
            BeginTry();
            GenPushStr(varType, arg, "\t\t\t", isByteBuffer);
            sb.AppendLineEx("\t\t\treturn 1;");
            EndTry();
        }
        else
        {
            sb.AppendLineEx("\t\tobject o = null;\r\n");
            BeginTry();
            sb.AppendLineEx("\t\t\to = ToLua.ToObject(L, 1);");
            sb.AppendFormat("\t\t\t{0} obj = ({0})o;\r\n", className);
            sb.AppendFormat("\t\t\t{0} ret = obj.{1};\r\n", GetTypeStr(varType), varName);
            GenPushStr(varType, "ret", "\t\t\t", isByteBuffer);
            sb.AppendLineEx("\t\t\treturn 1;");

            sb.AppendLineEx("\t\t}");
            sb.AppendLineEx("\t\tcatch(Exception e)");
            sb.AppendLineEx("\t\t{");

            sb.AppendFormat("\t\t\treturn LuaDLL.toluaL_exception(L, e, o, \"attempt to index {0} on a nil value\");\r\n", varName);
            sb.AppendLineEx("\t\t}");
        }

        sb.AppendLineEx("\t}");
    }

    static void GenSetFieldStr(string varName, Type varType, bool isStatic, bool beOverride = false, bool csharpLike = false)
    {
        sb.AppendLineEx("\r\n\t[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]");
        sb.AppendFormat("\tstatic int {0}_{1}(IntPtr L)\r\n", beOverride ? "_set" : "set", varName + (csharpLike ? "ter" : string.Empty));
        sb.AppendLineEx("\t{");

        if (!isStatic)
        {
            sb.AppendLineEx("\t\tobject o = null;\r\n");
            BeginTry();
            sb.AppendLineEx("\t\t\to = ToLua.ToObject(L, 1);");
            sb.AppendFormat("\t\t\t{0} obj = ({0})o;\r\n", className);
            ProcessArg(varType, "\t\t\t", "arg0", 2);
            sb.AppendFormat("\t\t\tobj.{0} = arg0;\r\n", varName);

            if (type.IsValueType)
            {
                sb.AppendLineEx("\t\t\tToLua.SetBack(L, 1, obj);");
            }
            sb.AppendLineEx("\t\t\treturn 0;");
            sb.AppendLineEx("\t\t}");
            sb.AppendLineEx("\t\tcatch(Exception e)");
            sb.AppendLineEx("\t\t{");
            sb.AppendFormat("\t\t\treturn LuaDLL.toluaL_exception(L, e, o, \"attempt to index {0} on a nil value\");\r\n", varName);
            sb.AppendLineEx("\t\t}");
        }
        else
        {
            BeginTry();
            ProcessArg(varType, "\t\t\t", "arg0", csharpLike ? 1 : 2);
            sb.AppendFormat("\t\t\t{0}.{1} = arg0;\r\n", className, varName);
            sb.AppendLineEx("\t\t\treturn 0;");
            EndTry();
        }

        sb.AppendLineEx("\t}");
    }
    static void GenIndexFunc()
    {
        for (int i = 0; i < fields.Length; i++)
        {
            if (fields[i].IsLiteral && fields[i].FieldType.IsPrimitive && !fields[i].FieldType.IsEnum)
            {
                continue;
            }

            bool beBuffer = IsByteBuffer(fields[i]);
            GenGetFieldStr(fields[i].Name, fields[i].FieldType, fields[i].IsStatic, beBuffer);
        }

        for (int i = 0; i < props.Length; i++)
        {
            if (!props[i].CanRead)
            {
                continue;
            }

            bool isStatic = true;
            int index = propList.IndexOf(props[i]);

            if (index >= 0)
            {
                isStatic = false;
            }

            _MethodBase md = methods.Find((p) => { return p.Name == "get_" + props[i].Name; });
            bool beBuffer = IsByteBuffer(props[i]);

            GenGetFieldStr(props[i].Name, props[i].PropertyType, isStatic, beBuffer, md != null);
        }

        for (int i = 0; i < events.Length; i++)
        {
            GenGetEventStr(events[i].Name, events[i].EventHandlerType);
        }
    }
    static void GenNewIndexFunc()
    {
        for (int i = 0; i < fields.Length; i++)
        {
            if (fields[i].IsLiteral || fields[i].IsInitOnly || fields[i].IsPrivate)
            {
                continue;
            }

            GenSetFieldStr(fields[i].Name, fields[i].FieldType, fields[i].IsStatic);
        }

        for (int i = 0; i < props.Length; i++)
        {
            if (!props[i].CanWrite || !props[i].GetSetMethod(true).IsPublic)
            {
                continue;
            }

            bool isStatic = true;
            int index = propList.IndexOf(props[i]);

            if (index >= 0)
            {
                isStatic = false;
            }

            _MethodBase md = methods.Find((p) => { return p.Name == "set_" + props[i].Name; });
            GenSetFieldStr(props[i].Name, props[i].PropertyType, isStatic, md != null);
            // 生成CSharp.lua兼容的版本
            if (isStatic)
                GenSetFieldStr(props[i].Name, props[i].PropertyType, isStatic, md != null, true);
        }

        for (int i = 0; i < events.Length; i++)
        {
            bool isStatic = eventList.IndexOf(events[i]) < 0;
            GenSetEventStr(events[i].Name, events[i].EventHandlerType, isStatic);
        }
    }


    static void GenGetEventStr(string varName, Type varType)
    {
        sb.AppendLineEx("\r\n\t[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]");
        sb.AppendFormat("\tstatic int get_{0}(IntPtr L)\r\n", varName);
        sb.AppendLineEx("\t{");
        sb.AppendFormat("\t\tToLua.Push(L, new EventObject(typeof({0})));\r\n", GetTypeStr(varType));
        sb.AppendLineEx("\t\treturn 1;");
        sb.AppendLineEx("\t}");
    }


    static void GenSetEventStr(string varName, Type varType, bool isStatic)
    {
        sb.AppendLineEx("\r\n\t[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]");
        sb.AppendFormat("\tstatic int set_{0}(IntPtr L)\r\n", varName);
        sb.AppendLineEx("\t{");
        BeginTry();

        if (!isStatic)
        {
            sb.AppendFormat("\t\t\t{0} obj = ({0})ToLua.CheckObject(L, 1, typeof({0}));\r\n", className);
        }

        string strVarType = GetTypeStr(varType);
        string objStr = isStatic ? className : "obj";

        sb.AppendLineEx("\t\t\tEventObject arg0 = null;\r\n");
        sb.AppendLineEx("\t\t\tif (LuaDLL.lua_isuserdata(L, 2) != 0)");
        sb.AppendLineEx("\t\t\t{");
        sb.AppendLineEx("\t\t\t\targ0 = (EventObject)ToLua.ToObject(L, 2);");
        sb.AppendLineEx("\t\t\t}");
        sb.AppendLineEx("\t\t\telse");
        sb.AppendLineEx("\t\t\t{");
        sb.AppendFormat("\t\t\t\treturn LuaDLL.luaL_throw(L, \"The event '{0}.{1}' can only appear on the left hand side of += or -= when used outside of the type '{0}'\");\r\n", className, varName);
        sb.AppendLineEx("\t\t\t}\r\n");

        sb.AppendLineEx("\t\t\tif (arg0.op == EventOp.Add)");
        sb.AppendLineEx("\t\t\t{");
        sb.AppendFormat("\t\t\t\t{0} ev = ({0})arg0.func;\r\n", strVarType);
        sb.AppendFormat("\t\t\t\t{0}.{1} += ev;\r\n", objStr, varName);
        sb.AppendLineEx("\t\t\t}");
        sb.AppendLineEx("\t\t\telse if (arg0.op == EventOp.Sub)");
        sb.AppendLineEx("\t\t\t{");
        sb.AppendFormat("\t\t\t\t{0} ev = ({0})arg0.func;\r\n", strVarType);
        sb.AppendFormat("\t\t\t\t{0}.{1} -= ev;\r\n", objStr, varName);
        sb.AppendLineEx("\t\t\t}\r\n");

        sb.AppendLineEx("\t\t\treturn 0;");
        EndTry();

        sb.AppendLineEx("\t}");
    }

    //this[] 非静态函数
    static void GenItemPropertyFunction()
    {
        int flag = 0;

        if (getItems.Count > 0)
        {
            sb.AppendLineEx("\r\n\t[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]");
            sb.AppendLineEx("\tstatic int _get_this(IntPtr L)");
            sb.AppendLineEx("\t{");
            BeginTry();

            if (getItems.Count == 1)
            {
                _MethodBase m = getItems[0];
                int count = m.GetParameters().Length + 1;
                sb.AppendFormat("\t\t\tToLua.CheckArgsCount(L, {0});\r\n", count);
                m.ProcessParams(3, false, int.MaxValue);
                sb.AppendLineEx("\t\t\treturn 1;\r\n");
            }
            else
            {
                getItems.Sort(Compare);
                int[] checkTypeMap = CheckCheckTypePos(getItems);

                sb.AppendLineEx("\t\t\tint count = LuaDLL.lua_gettop(L);");
                sb.AppendLineEx();

                for (int i = 0; i < getItems.Count; i++)
                {
                    GenOverrideFuncBody(getItems[i], i == 0, checkTypeMap[i]);
                }

                sb.AppendLineEx("\t\t\telse");
                sb.AppendLineEx("\t\t\t{");
                sb.AppendFormat("\t\t\t\treturn LuaDLL.luaL_throw(L, \"invalid arguments to operator method: {0}.this\");\r\n", className);
                sb.AppendLineEx("\t\t\t}");
            }

            EndTry();
            sb.AppendLineEx("\t}");
            flag |= 1;
        }

        if (setItems.Count > 0)
        {
            sb.AppendLineEx("\r\n\t[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]");
            sb.AppendLineEx("\tstatic int _set_this(IntPtr L)");
            sb.AppendLineEx("\t{");
            BeginTry();

            if (setItems.Count == 1)
            {
                _MethodBase m = setItems[0];
                int count = m.GetParameters().Length + 1;
                sb.AppendFormat("\t\t\tToLua.CheckArgsCount(L, {0});\r\n", count);
                m.ProcessParams(3, false, int.MaxValue);
                sb.AppendLineEx("\t\t\treturn 0;\r\n");
            }
            else
            {
                setItems.Sort(Compare);
                int[] checkTypeMap = CheckCheckTypePos(setItems);

                sb.AppendLineEx("\t\t\tint count = LuaDLL.lua_gettop(L);");
                sb.AppendLineEx();

                for (int i = 0; i < setItems.Count; i++)
                {
                    GenOverrideFuncBody(setItems[i], i == 0, checkTypeMap[i]);
                }

                sb.AppendLineEx("\t\t\telse");
                sb.AppendLineEx("\t\t\t{");
                sb.AppendFormat("\t\t\t\treturn LuaDLL.luaL_throw(L, \"invalid arguments to operator method: {0}.this\");\r\n", className);
                sb.AppendLineEx("\t\t\t}");
            }


            EndTry();
            sb.AppendLineEx("\t}");
            flag |= 2;
        }

        if (flag != 0)
        {
            sb.AppendLineEx("\r\n\t[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]");
            sb.AppendLineEx("\tstatic int _this(IntPtr L)");
            sb.AppendLineEx("\t{");
            BeginTry();
            sb.AppendLineEx("\t\t\tLuaDLL.lua_pushvalue(L, 1);");
            sb.AppendFormat("\t\t\tLuaDLL.tolua_bindthis(L, {0}, {1});\r\n", (flag & 1) == 1 ? "_get_this" : "null", (flag & 2) == 2 ? "_set_this" : "null");
            sb.AppendLineEx("\t\t\treturn 1;");
            EndTry();
            sb.AppendLineEx("\t}");
        }
    }

    static bool HasGetIndex(MemberInfo md)
    {
        if (md.Name == "get_Item")
        {
            return true;
        }

        object[] attrs = type.GetCustomAttributes(true);

        for (int i = 0; i < attrs.Length; i++)
        {
            if (attrs[i] is DefaultMemberAttribute)
            {
                return md.Name == "get_ItemOf";
            }
        }

        return false;
    }

    static bool HasSetIndex(MemberInfo md)
    {
        if (md.Name == "set_Item")
        {
            return true;
        }

        object[] attrs = type.GetCustomAttributes(true);

        for (int i = 0; i < attrs.Length; i++)
        {
            if (attrs[i] is DefaultMemberAttribute)
            {
                return md.Name == "set_ItemOf";
            }
        }

        return false;
    }

    static bool IsThisArray(MethodBase md, int count)
    {
        ParameterInfo[] pis = md.GetParameters();

        if (pis.Length != count)
        {
            return false;
        }

        if (pis[0].ParameterType == typeof(int))
        {
            return true;
        }

        return false;
    }


    static bool IsItemThis(PropertyInfo info)
    {
        MethodInfo md = info.GetGetMethod();

        if (md != null)
        {
            return md.GetParameters().Length != 0;
        }

        md = info.GetSetMethod();

        if (md != null)
        {
            return md.GetParameters().Length != 1;
        }

        return true;
    }
}
