using LuaInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

public static partial class ToLuaExport
{
    static void DefaultConstruct()
    {
        sb.AppendLineEx("\r\n\t[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]");
        sb.AppendFormat("\tstatic int _Create{0}(IntPtr L)\r\n", wrapClassName);
        sb.AppendLineEx("\t{");
        sb.AppendFormat("\t\t{0} obj = new {0}();\r\n", className);
        GenPushStr(type, "obj", "\t\t");
        sb.AppendLineEx("\t\treturn 1;");
        sb.AppendLineEx("\t}");
    }

    static void InitCtorList()
    {
        if (isStaticClass || type.IsAbstract || typeof(MonoBehaviour).IsAssignableFrom(type))
        {
            return;
        }

        ConstructorInfo[] constructors = type.GetConstructors(BindingFlags.Instance | binding);

        if (extendType != null)
        {
            ConstructorInfo[] ctorExtends = extendType.GetConstructors(BindingFlags.Instance | binding);

            if (HasAttribute(ctorExtends[0], typeof(UseDefinedAttribute)))
            {
                ctorExtList.AddRange(ctorExtends);
            }
        }

        if (constructors.Length == 0)
        {
            return;
        }

        for (int i = 0; i < constructors.Length; i++)
        {
            if (IsObsolete(constructors[i]))
            {
                continue;
            }

            int count = GetDefalutParamCount(constructors[i]);
            int length = constructors[i].GetParameters().Length;

            for (int j = 0; j < count + 1; j++)
            {
                _MethodBase r = new _MethodBase(constructors[i], length - j);
                int index = ctorList.FindIndex((p) => { return CompareMethod(p, r) >= 0; });

                if (index >= 0)
                {
                    if (CompareMethod(ctorList[index], r) == 2)
                    {
                        ctorList.RemoveAt(index);
                        ctorList.Add(r);
                    }
                }
                else
                {
                    ctorList.Add(r);
                }
            }
        }
    }

    static void GenConstructFunction()
    {
        if (ctorExtList.Count > 0)
        {
            if (HasAttribute(ctorExtList[0], typeof(UseDefinedAttribute)))
            {
                sb.AppendLineEx("\r\n\t[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]");
                sb.AppendFormat("\tstatic int _Create{0}(IntPtr L)\r\n", wrapClassName);
                sb.AppendLineEx("\t{");

                FieldInfo field = extendType.GetField(extendName + "Defined");
                string strfun = field.GetValue(null) as string;
                sb.AppendLineEx(strfun);
                sb.AppendLineEx("\t}");
                return;
            }
        }

        if (ctorList.Count == 0)
        {
            if (type.IsValueType)
            {
                DefaultConstruct();
            }

            return;
        }

        ctorList.Sort(Compare);
        int[] checkTypeMap = CheckCheckTypePos(ctorList);
        sb.AppendLineEx("\r\n\t[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]");
        sb.AppendFormat("\tstatic int _Create{0}(IntPtr L)\r\n", wrapClassName);
        sb.AppendLineEx("\t{");

        BeginTry();
        sb.AppendLineEx("\t\t\tint count = LuaDLL.lua_gettop(L);");
        sb.AppendLineEx();

        _MethodBase md = ctorList[0];
        bool hasEmptyCon = ctorList[0].GetParameters().Length == 0 ? true : false;

        //处理重载构造函数
        if (HasOptionalParam(md.GetParameters()))
        {
            ParameterInfo[] paramInfos = md.GetParameters();
            ParameterInfo param = paramInfos[paramInfos.Length - 1];
            string str = GetTypeStr(param.ParameterType.GetElementType());

            if (paramInfos.Length > 1)
            {
                string strParams = md.GenParamTypes(1);
                sb.AppendFormat("\t\t\tif (TypeChecker.CheckTypes<{0}>(L, 1) && TypeChecker.CheckParamsType<{1}>(L, {2}, {3}))\r\n", strParams, str, paramInfos.Length, GetCountStr(paramInfos.Length - 1));
            }
            else
            {
                sb.AppendFormat("\t\t\tif (TypeChecker.CheckParamsType<{0}>(L, {1}, {2}))\r\n", str, paramInfos.Length, GetCountStr(paramInfos.Length - 1));
            }
        }
        else
        {
            ParameterInfo[] paramInfos = md.GetParameters();

            if (ctorList.Count == 1 || paramInfos.Length == 0 || paramInfos.Length + 1 <= checkTypeMap[0])
            {
                sb.AppendFormat("\t\t\tif (count == {0})\r\n", paramInfos.Length);
            }
            else
            {
                string strParams = md.GenParamTypes(checkTypeMap[0]);
                sb.AppendFormat("\t\t\tif (count == {0} && TypeChecker.CheckTypes<{1}>(L, {2}))\r\n", paramInfos.Length, strParams, checkTypeMap[0]);
            }
        }

        sb.AppendLineEx("\t\t\t{");
        int rc = md.ProcessParams(4, true, checkTypeMap[0] - 1);
        sb.AppendFormat("\t\t\t\treturn {0};\r\n", rc);
        sb.AppendLineEx("\t\t\t}");

        for (int i = 1; i < ctorList.Count; i++)
        {
            hasEmptyCon = ctorList[i].GetParameters().Length == 0 ? true : hasEmptyCon;
            md = ctorList[i];
            ParameterInfo[] paramInfos = md.GetParameters();

            if (!HasOptionalParam(md.GetParameters()))
            {
                string strParams = md.GenParamTypes(checkTypeMap[i]);

                if (paramInfos.Length + 1 > checkTypeMap[i])
                {
                    sb.AppendFormat("\t\t\telse if (count == {0} && TypeChecker.CheckTypes<{1}>(L, {2}))\r\n", paramInfos.Length, strParams, checkTypeMap[i]);
                }
                else
                {
                    sb.AppendFormat("\t\t\telse if (count == {0})\r\n", paramInfos.Length);
                }
            }
            else
            {
                ParameterInfo param = paramInfos[paramInfos.Length - 1];
                string str = GetTypeStr(param.ParameterType.GetElementType());

                if (paramInfos.Length > 1)
                {
                    string strParams = md.GenParamTypes(1);
                    sb.AppendFormat("\t\t\telse if (TypeChecker.CheckTypes<{0}>(L, 1) && TypeChecker.CheckParamsType<{1}>(L, {2}, {3}))\r\n", strParams, str, paramInfos.Length, GetCountStr(paramInfos.Length - 1));
                }
                else
                {
                    sb.AppendFormat("\t\t\telse if (TypeChecker.CheckParamsType<{0}>(L, {1}, {2}))\r\n", str, paramInfos.Length, GetCountStr(paramInfos.Length - 1));
                }
            }

            sb.AppendLineEx("\t\t\t{");
            rc = md.ProcessParams(4, true, checkTypeMap[i] - 1);
            sb.AppendFormat("\t\t\t\treturn {0};\r\n", rc);
            sb.AppendLineEx("\t\t\t}");
        }

        if (type.IsValueType && !hasEmptyCon)
        {
            sb.AppendLineEx("\t\t\telse if (count == 0)");
            sb.AppendLineEx("\t\t\t{");
            sb.AppendFormat("\t\t\t\t{0} obj = new {0}();\r\n", className);
            GenPushStr(type, "obj", "\t\t\t\t");
            sb.AppendLineEx("\t\t\t\treturn 1;");
            sb.AppendLineEx("\t\t\t}");
        }

        sb.AppendLineEx("\t\t\telse");
        sb.AppendLineEx("\t\t\t{");
        sb.AppendFormat("\t\t\t\treturn LuaDLL.luaL_throw(L, \"invalid arguments to ctor method: {0}.New\");\r\n", className);
        sb.AppendLineEx("\t\t\t}");

        EndTry();
        sb.AppendLineEx("\t}");
    }


}
