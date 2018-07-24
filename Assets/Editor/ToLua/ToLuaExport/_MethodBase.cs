/*
Copyright (c) 2015-2017 topameng(topameng@qq.com)

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
using UnityEngine;
using System;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using LuaInterface;

public static partial class ToLuaExport 
{
    class _MethodBase
    {
        public bool IsStatic
        {
            get
            {
                return method.IsStatic;
            }
        }

        public bool IsConstructor
        {
            get
            {
                return method.IsConstructor;
            }
        }

        public string Name
        {
            get
            {
                return method.Name;
            }
        }

        public MethodBase Method
        {
            get
            {
                return method;
            }
        }

        public bool IsGenericMethod
        {
            get
            {
                return method.IsGenericMethod;
            }
        }
        

        MethodBase method;
        ParameterInfo[] args;

        public _MethodBase(MethodBase m, int argCount = -1)
        {
            method = m;
            ParameterInfo[] infos = m.GetParameters();
            argCount = argCount != -1 ? argCount : infos.Length;
            args = new ParameterInfo[argCount];
            Array.Copy(infos, args, argCount);
        }

        public ParameterInfo[] GetParameters()
        {
            return args;
        }

        public int GetParamsCount()
        {
            int c = method.IsStatic ? 0 : 1;
            return args.Length + c;
        }

        public int GetEqualParamsCount(_MethodBase b)
        {
            int count = 0;
            List<Type> list1 = new List<Type>();
            List<Type> list2 = new List<Type>();            

            if (!IsStatic)
            {
                list1.Add(type);
            }

            if (!b.IsStatic)
            {
                list2.Add(type);
            }
            
            for (int i = 0; i < args.Length; i++)
            {
                list1.Add(GetParameterType(args[i]));
            }

            ParameterInfo[] p = b.args;

            for (int i = 0; i < p.Length; i++)
            {
                list2.Add(GetParameterType(p[i]));
            }

            for (int i = 0; i < list1.Count; i++)
            {
                if (list1[i] != list2[i])
                {
                    break;
                }

                ++count;
            }

            return count;
        }

        public string GenParamTypes(int offset = 0)
        {
            StringBuilder sb = new StringBuilder();
            List<Type> list = new List<Type>();

            if (!method.IsStatic)
            {
                list.Add(type);
            }

            for (int i = 0; i < args.Length; i++)
            {
                if (IsParams(args[i]))
                {
                    continue;
                }

                if (args[i].Attributes != ParameterAttributes.Out)
                {
                    list.Add(GetGenericBaseType(method, args[i].ParameterType));
                }
                else
                {
                    Type genericClass = typeof(LuaOut<>);
                    Type t = genericClass.MakeGenericType(args[i].ParameterType.GetElementType());
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

        public bool HasSetIndex()
        {
            if (method.Name == "set_Item")
            {
                return true;
            }

            object[] attrs = type.GetCustomAttributes(true);

            for (int i = 0; i < attrs.Length; i++)
            {
                if (attrs[i] is DefaultMemberAttribute)
                {
                    return method.Name == "set_ItemOf";
                }
            }

            return false;
        }

        public bool HasGetIndex()
        {
            if (method.Name == "get_Item")
            {
                return true;
            }

            object[] attrs = type.GetCustomAttributes(true);

            for (int i = 0; i < attrs.Length; i++)
            {
                if (attrs[i] is DefaultMemberAttribute)
                {
                    return method.Name == "get_ItemOf";
                }
            }

            return false;
        }

        public Type GetReturnType()
        {
            MethodInfo m = method as MethodInfo;

            if (m != null)
            {
                return m.ReturnType;
            }

            return null;
        }

        public string GetTotalName()
        {
            string[] ss = new string[args.Length];

            for (int i = 0; i < args.Length; i++)
            {
                ss[i] = GetTypeStr(args[i].GetType());
            }

            if (!ToLuaExport.IsGenericMethod(method))
            {
                return Name + "(" + string.Join(",", ss) + ")";
            }
            else
            {
                Type[] gts = method.GetGenericArguments();
                string[] ts = new string[gts.Length];

                for (int i = 0; i < gts.Length; i++)
                {
                    ts[i] = GetTypeStr(gts[i]);
                }

                return Name + "<" + string.Join(",", ts) + ">" + "(" + string.Join(",", ss) + ")";
            }
        }

        public bool BeExtend = false;

        public int ProcessParams(int tab, bool beConstruct, int checkTypePos)
        {
            ParameterInfo[] paramInfos = args;                        

            if (BeExtend)
            {
                ParameterInfo[] pt = new ParameterInfo[paramInfos.Length - 1];
                Array.Copy(paramInfos, 1, pt, 0, pt.Length);
                paramInfos = pt;
            }

            int count = paramInfos.Length;
            string head = string.Empty;
            PropertyInfo pi = null;
            int methodType = GetMethodType(method, out pi);
            int offset = ((method.IsStatic && !BeExtend) || beConstruct) ? 1 : 2;

            if (method.Name == "op_Equality")
            {
                checkTypePos = -1;
            }

            for (int i = 0; i < tab; i++)
            {
                head += "\t";
            }

            if ((!method.IsStatic && !beConstruct) || BeExtend)
            {
                if (checkTypePos > 0)
                {
                    CheckObject(head, type, className, 1);
                }
                else
                {
                    if (method.Name == "Equals")
                    {
                        if (!type.IsValueType && checkTypePos > 0)
                        {
                            CheckObject(head, type, className, 1);
                        }
                        else
                        {
                            sb.AppendFormat("{0}var obj = ({1})ToLua.ToObject(L, 1);\r\n", head, className);
                        }
                    }
                    else if (checkTypePos > 0)// && methodType == 0)
                    {
                        CheckObject(head, type, className, 1);
                    }
                    else
                    {
                        ToObject(head, type, className, 1);
                    }
                }
            }

            StringBuilder sbArgs = new StringBuilder();
            List<string> refList = new List<string>();
            List<Type> refTypes = new List<Type>();
            checkTypePos = checkTypePos - offset + 1;

            for (int j = 0; j < count; j++)
            {
                ParameterInfo param = paramInfos[j];
                string arg = "arg" + j;
                bool beOutArg = param.Attributes == ParameterAttributes.Out;
                bool beParams = IsParams(param);
                Type t = GetGenericBaseType(method, param.ParameterType);
                ProcessArg(t, head, arg, offset + j, j >= checkTypePos, beParams, beOutArg);
            }

            for (int j = 0; j < count; j++)
            {
                ParameterInfo param = paramInfos[j];

                if (!param.ParameterType.IsByRef)
                {
                    sbArgs.Append("arg");
                }
                else
                {
                    if (param.Attributes == ParameterAttributes.Out)
                    {
                        sbArgs.Append("out arg");
                    }
                    else
                    {
                        sbArgs.Append("ref arg");
                    }

                    refList.Add("arg" + j);
                    refTypes.Add(GetRefBaseType(param.ParameterType));
                }

                sbArgs.Append(j);

                if (j != count - 1)
                {
                    sbArgs.Append(", ");
                }
            }

            if (beConstruct)
            {
                sb.AppendFormat("{2}var obj = new {0}({1});\r\n", className, sbArgs.ToString(), head);
                string str = GetPushFunction(type);
                sb.AppendFormat("{0}ToLua.{1}(L, obj);\r\n", head, str);

                for (int i = 0; i < refList.Count; i++)
                {
                    GenPushStr(refTypes[i], refList[i], head);
                }

                return refList.Count + 1;
            }

            string obj = (method.IsStatic && !BeExtend) ? className : "obj";
            Type retType = GetReturnType();

            if (retType == typeof(void))
            {
                if (HasSetIndex())
                {
                    if (methodType == 2)
                    {
                        string str = sbArgs.ToString();
                        string[] ss = str.Split(',');
                        str = string.Join(",", ss, 0, ss.Length - 1);

                        sb.AppendFormat("{0}{1}[{2}] ={3};\r\n", head, obj, str, ss[ss.Length - 1]);
                    }
                    else if (methodType == 1)
                    {
                        sb.AppendFormat("{0}{1}.Item = arg0;\r\n", head, obj, pi.Name);
                    }
                    else
                    {
                        sb.AppendFormat("{0}{1}.{2}({3});\r\n", head, obj, method.Name, sbArgs.ToString());
                    }
                }
                else if (methodType == 1)
                {
                    sb.AppendFormat("{0}{1}.{2} = arg0;\r\n", head, obj, pi.Name);
                }
                else
                {
                    sb.AppendFormat("{3}{0}.{1}({2});\r\n", obj, method.Name, sbArgs.ToString(), head);
                }
            }
            else
            {
                Type genericType = GetGenericBaseType(method, retType);
                string ret = GetTypeStr(genericType);

                if (method.Name.StartsWith("op_"))
                {
                    CallOpFunction(method.Name, tab, ret);
                }
                else if (HasGetIndex())
                {
                    if (methodType == 2)
                    {
                        sb.AppendFormat("{0}var o = {2}[{3}];\r\n", head, ret, obj, sbArgs.ToString());
                    }
                    else if (methodType == 1)
                    {
                        sb.AppendFormat("{0}var o = {2}.Item;\r\n", head, ret, obj);
                    }
                    else
                    {
                        sb.AppendFormat("{0}var o = {2}.{3}({4});\r\n", head, ret, obj, method.Name, sbArgs.ToString());
                    }
                }
                else if (method.Name == "Equals")
                {
                    if (type.IsValueType || method.GetParameters().Length > 1)
                    {
                        sb.AppendFormat("{0}var o = obj.Equals({2});\r\n", head, ret, sbArgs.ToString());
                    }
                    else
                    {
                        sb.AppendFormat("{0}var o = obj != null ? obj.Equals({2}) : arg0 == null;\r\n", head, ret, sbArgs.ToString());
                    }
                }
                else if (methodType == 1)
                {
                    sb.AppendFormat("{0}var o = {2}.{3};\r\n", head, ret, obj, pi.Name);
                }
                else
                {
                    sb.AppendFormat("{0}var o = {2}.{3}({4});\r\n", head, ret, obj, method.Name, sbArgs.ToString());
                }

                bool isbuffer = IsByteBuffer();
                GenPushStr(retType, "o", head, isbuffer);
            }

            for (int i = 0; i < refList.Count; i++)
            {
                if (refTypes[i] == typeof(RaycastHit) && method.Name == "Raycast" && (type == typeof(Physics) || type == typeof(Collider)))
                {
                    sb.AppendFormat("{0}if (o) ToLua.Push(L, {1}); else LuaDLL.lua_pushnil(L);\r\n", head, refList[i]);
                }
                else
                {
                    GenPushStr(refTypes[i], refList[i], head);
                }
            }

            if (!method.IsStatic && type.IsValueType && method.Name != "ToString")
            {
                sb.Append(head + "ToLua.SetBack(L, 1, obj);\r\n");
            }

            return refList.Count;
        }

        bool IsByteBuffer()
        {
            object[] attrs = method.GetCustomAttributes(true);

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
    }
}
