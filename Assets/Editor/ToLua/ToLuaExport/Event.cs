using LuaInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public static partial class ToLuaExport
{
    static void GenEventFunctions()
    {
        foreach (Type t in eventSet)
        {
            GenEventFunction(t, sb);
        }
    }
    public static void GenEventFunction(Type t, StringBuilder sb)
    {
        string funcName;
        string space = GetNameSpace(t, out funcName);
        funcName = CombineTypeStr(space, funcName);
        funcName = ConvertToLibSign(funcName);

        sb.AppendLineEx("\r\n\t[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]");
        sb.AppendFormat("\tstatic int {0}(IntPtr L)\r\n", funcName);
        sb.AppendLineEx("\t{");
        sb.AppendLineEx("\t\ttry");
        sb.AppendLineEx("\t\t{");
        sb.AppendLineEx("\t\t\tint count = LuaDLL.lua_gettop(L);");
        sb.AppendLineEx("\t\t\tLuaFunction func = ToLua.CheckLuaFunction(L, 1);");
        sb.AppendLineEx();
        sb.AppendLineEx("\t\t\tif (count == 1)");
        sb.AppendLineEx("\t\t\t{");
        sb.AppendFormat("\t\t\t\tDelegate arg1 = DelegateTraits<{0}>.Create(func);\r\n", GetTypeStr(t));
        sb.AppendLineEx("\t\t\t\tToLua.Push(L, arg1);");
        sb.AppendLineEx("\t\t\t}");
        sb.AppendLineEx("\t\t\telse");
        sb.AppendLineEx("\t\t\t{");
        sb.AppendLineEx("\t\t\t\tLuaTable self = ToLua.CheckLuaTable(L, 2);");
        sb.AppendFormat("\t\t\t\tDelegate arg1 = DelegateTraits<{0}>.Create(func, self);\r\n", GetTypeStr(t));
        sb.AppendFormat("\t\t\t\tToLua.Push(L, arg1);\r\n");
        sb.AppendLineEx("\t\t\t}");

        sb.AppendLineEx("\t\t\treturn 1;");
        sb.AppendLineEx("\t\t}");
        sb.AppendLineEx("\t\tcatch(Exception e)");
        sb.AppendLineEx("\t\t{");
        sb.AppendLineEx("\t\t\treturn LuaDLL.toluaL_exception(L, e);");
        sb.AppendLineEx("\t\t}");
        sb.AppendLineEx("\t}");
    }


}
