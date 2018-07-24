using LuaInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

public static partial class ToLuaExport
{
    static void InitMethods()
    {
        bool flag = false;

        if (baseType != null || isStaticClass)
        {
            binding |= BindingFlags.DeclaredOnly;
            flag = true;
        }

        List<_MethodBase> list = new List<_MethodBase>();
        MethodInfo[] infos = type.GetMethods(BindingFlags.Instance | binding);

        for (int i = 0; i < infos.Length; i++)
        {
            list.Add(new _MethodBase(infos[i]));
        }

        for (int i = list.Count - 1; i >= 0; --i)
        {
            //去掉操作符函数
            if (list[i].Name.StartsWith("op_") || list[i].Name.StartsWith("add_") || list[i].Name.StartsWith("remove_"))
            {
                if (!IsNeedOp(list[i].Name))
                {
                    list.RemoveAt(i);
                }

                continue;
            }

            //扔掉 unity3d 废弃的函数                
            if (IsObsolete(list[i].Method))
            {
                list.RemoveAt(i);
            }
        }

        PropertyInfo[] ps = type.GetProperties();

        for (int i = 0; i < ps.Length; i++)
        {
            if (IsObsolete(ps[i]))
            {
                list.RemoveAll((p) => { return p.Method == ps[i].GetGetMethod() || p.Method == ps[i].GetSetMethod(); });
            }
            else
            {
                MethodInfo md = ps[i].GetGetMethod();

                if (md != null)
                {
                    int index = list.FindIndex((m) => { return m.Method == md; });

                    if (index >= 0)
                    {
                        if (md.GetParameters().Length == 0)
                        {
                            list.RemoveAt(index);
                        }
                        else if (list[index].HasGetIndex())
                        {
                            getItems.Add(list[index]);
                        }
                    }
                }

                md = ps[i].GetSetMethod();

                if (md != null)
                {
                    int index = list.FindIndex((m) => { return m.Method == md; });

                    if (index >= 0)
                    {
                        if (md.GetParameters().Length == 1)
                        {
                            list.RemoveAt(index);
                        }
                        else if (list[index].HasSetIndex())
                        {
                            setItems.Add(list[index]);
                        }
                    }
                }
            }
        }

        if (flag && !isStaticClass)
        {
            List<MethodInfo> baseList = new List<MethodInfo>(type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.IgnoreCase));

            for (int i = baseList.Count - 1; i >= 0; i--)
            {
                if (BeDropMethodType(baseList[i]))
                {
                    baseList.RemoveAt(i);
                }
            }

            HashSet<MethodInfo> addList = new HashSet<MethodInfo>();

            for (int i = 0; i < list.Count; i++)
            {
                List<MethodInfo> mds = baseList.FindAll((p) => { return p.Name == list[i].Name; });

                for (int j = 0; j < mds.Count; j++)
                {
                    addList.Add(mds[j]);
                    baseList.Remove(mds[j]);
                }
            }

            foreach (var iter in addList)
            {
                list.Add(new _MethodBase(iter));
            }
        }

        for (int i = 0; i < list.Count; i++)
        {
            GetDelegateTypeFromMethodParams(list[i]);
        }

        ProcessExtends(list);
        GenBaseOpFunction(list);

        metaXml.AddIncludeClass(type);
        HashSet<string> names = new HashSet<string>();
        for (int i = 0; i < list.Count; i++)
        {
            var method = list[i];
            //if (IsGenericMethod(method.Method))
            //{
            //    continue;
            //}
            if (!names.Add(method.Name))
                continue;
            var notGenericOverrideList = GetOverrideMethods(method.Name, list, false);
            if (notGenericOverrideList.Count > 1)
            {
                for (int overrideIndex = 0; overrideIndex < notGenericOverrideList.Count; overrideIndex++)
                {
                    var overrideMethod = notGenericOverrideList[overrideIndex];
                    var name = GenMethodName(overrideMethod, overrideIndex);
                    metaXml.AddMethod(overrideMethod.Method, name);
                }
            }
            else if (notGenericOverrideList.Count == 1)
            {
                var name = method.Name;
                metaXml.AddMethod(notGenericOverrideList[0].Method, name == "Register" ? "_Register" : name);
            }
            else
            {
                var name = method.Name;
                metaXml.AddMethod(method.Method, name == "Register" ? "_Register" : name);
            }

            var genericOverrideList = GetOverrideMethods(method.Name, list, true);
            for (int overrideIndex = 0; overrideIndex < genericOverrideList.Count; overrideIndex++)
            {
                var overrideMethod = genericOverrideList[overrideIndex];
                var name = GenMethodName(overrideMethod, overrideIndex);
                metaXml.AddMethod(overrideMethod.Method, name);
            }
        }

        for (int i = 0; i < list.Count; i++)
        {
            int count = GetDefalutParamCount(list[i].Method);
            int length = list[i].GetParameters().Length;

            for (int j = 0; j < count + 1; j++)
            {
                _MethodBase r = new _MethodBase(list[i].Method, length - j);
                r.BeExtend = list[i].BeExtend;
                methods.Add(r);
            }
        }
    }
    static void InitPropertyList()
    {
        props = type.GetProperties(BindingFlags.GetProperty | BindingFlags.SetProperty | BindingFlags.Instance | binding);
        propList.AddRange(type.GetProperties(BindingFlags.GetProperty | BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase));
        fields = type.GetFields(BindingFlags.GetField | BindingFlags.SetField | BindingFlags.Instance | binding);
        events = type.GetEvents(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static);
        eventList.AddRange(type.GetEvents(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public));

        List<FieldInfo> fieldList = new List<FieldInfo>();
        fieldList.AddRange(fields);

        for (int i = fieldList.Count - 1; i >= 0; i--)
        {
            if (IsObsolete(fieldList[i]))
            {
                fieldList.RemoveAt(i);
            }
            else if (IsDelegateType(fieldList[i].FieldType))
            {
                eventSet.Add(fieldList[i].FieldType);
            }
        }

        fields = fieldList.ToArray();

        List<PropertyInfo> piList = new List<PropertyInfo>();
        piList.AddRange(props);

        for (int i = piList.Count - 1; i >= 0; i--)
        {
            if (IsObsolete(piList[i]))
            {
                piList.RemoveAt(i);
            }
            else if (piList[i].Name == "Item" && IsItemThis(piList[i]))
            {
                piList.RemoveAt(i);
            }
            else if (piList[i].GetGetMethod() != null && HasGetIndex(piList[i].GetGetMethod()))
            {
                piList.RemoveAt(i);
            }
            else if (piList[i].GetSetMethod() != null && HasSetIndex(piList[i].GetSetMethod()))
            {
                piList.RemoveAt(i);
            }
            else if (IsDelegateType(piList[i].PropertyType))
            {
                eventSet.Add(piList[i].PropertyType);
            }
        }

        props = piList.ToArray();

        for (int i = propList.Count - 1; i >= 0; i--)
        {
            if (IsObsolete(propList[i]))
            {
                propList.RemoveAt(i);
            }
        }

        allProps.AddRange(props);
        allProps.AddRange(propList);

        List<EventInfo> evList = new List<EventInfo>();
        evList.AddRange(events);

        for (int i = evList.Count - 1; i >= 0; i--)
        {
            if (IsObsolete(evList[i]))
            {
                evList.RemoveAt(i);
            }
            else if (IsDelegateType(evList[i].EventHandlerType))
            {
                eventSet.Add(evList[i].EventHandlerType);
            }
        }

        events = evList.ToArray();

        for (int i = eventList.Count - 1; i >= 0; i--)
        {
            if (IsObsolete(eventList[i]))
            {
                eventList.RemoveAt(i);
            }
        }
    }
    static void GenRegisterFuncItems()
    {

        //bool isList = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>);
        //注册库函数
        for (int i = 0; i < methods.Count; i++)
        {
            _MethodBase m = methods[i];
            int count = 1;

            if (IsGenericMethod(m.Method))
            {
                continue;
            }

            string name = GetMethodName(m.Method);
            
            if (!nameCounter.TryGetValue(name, out count))
            {
                if (name == "get_Item" && IsThisArray(m.Method, 1))
                {
                    sb.AppendFormat("\t\tL.RegFunction(\"{0}\", get_Item);\r\n", ".geti");
                }
                else if (name == "set_Item" && IsThisArray(m.Method, 2))
                {
                    sb.AppendFormat("\t\tL.RegFunction(\"{0}\", set_Item);\r\n", ".seti");
                }

                if (!name.StartsWith("op_"))
                {
                    var list = GetOverrideMethods(name);
                    sb.AppendFormat("\t\tL.RegFunction(\"{0}\", {1});\r\n", name, name == "Register" ? "_Register" : name);
                    if (list.Count > 1)
                    {
                        for(int overrideIndex = 0; overrideIndex < list.Count; overrideIndex ++)
                        {
                            var genMethodName = GenMethodName(list[overrideIndex], overrideIndex);
                            sb.AppendFormat("\t\tL.RegFunction(\"{0}\", {1});\r\n", genMethodName, genMethodName);
                        }
                    }
                }

                nameCounter[name] = 1;
            }
            else
            {
                nameCounter[name] = count + 1;
            }
        }

        if (ctorList.Count > 0 || type.IsValueType || ctorExtList.Count > 0)
        {
            sb.AppendFormat("\t\tL.RegFunction(\"New\", _Create{0});\r\n", wrapClassName);
        }

        if (getItems.Count > 0 || setItems.Count > 0)
        {
            sb.AppendLineEx("\t\tL.RegVar(\"this\", _this, null);");
        }
    }

    static void GenRegisterOpItems()
    {
        if ((op & MetaOp.Add) != 0)
        {
            sb.AppendLineEx("\t\tL.RegFunction(\"__add\", op_Addition);");
        }

        if ((op & MetaOp.Sub) != 0)
        {
            sb.AppendLineEx("\t\tL.RegFunction(\"__sub\", op_Subtraction);");
        }

        if ((op & MetaOp.Mul) != 0)
        {
            sb.AppendLineEx("\t\tL.RegFunction(\"__mul\", op_Multiply);");
        }

        if ((op & MetaOp.Div) != 0)
        {
            sb.AppendLineEx("\t\tL.RegFunction(\"__div\", op_Division);");
        }

        if ((op & MetaOp.Eq) != 0)
        {
            sb.AppendLineEx("\t\tL.RegFunction(\"__eq\", op_Equality);");
        }

        if ((op & MetaOp.Neg) != 0)
        {
            sb.AppendLineEx("\t\tL.RegFunction(\"__unm\", op_UnaryNegation);");
        }

        if ((op & MetaOp.ToStr) != 0)
        {
            sb.AppendLineEx("\t\tL.RegFunction(\"__tostring\", ToLua.op_ToString);");
        }
    }


    static void GenRegisterVariables()
    {
        if (fields.Length == 0 && props.Length == 0 && events.Length == 0 && isStaticClass && baseType == null)
        {
            return;
        }

        for (int i = 0; i < fields.Length; i++)
        {
            if (fields[i].IsLiteral || fields[i].IsPrivate || fields[i].IsInitOnly)
            {
                if (fields[i].IsLiteral && fields[i].FieldType.IsPrimitive && !fields[i].FieldType.IsEnum)
                {
                    double d = Convert.ToDouble(fields[i].GetValue(null));
                    sb.AppendFormat("\t\tL.RegConstant(\"{0}\", {1});\r\n", fields[i].Name, d);
                }
                else
                {
                    sb.AppendFormat("\t\tL.RegVar(\"{0}\", get_{0}, null);\r\n", fields[i].Name);
                }
            }
            else
            {
                sb.AppendFormat("\t\tL.RegVar(\"{0}\", get_{0}, set_{0});\r\n", fields[i].Name);
            }
        }

        for (int i = 0; i < props.Length; i++)
        {
            if (props[i].CanRead && props[i].CanWrite && props[i].GetSetMethod(true).IsPublic)
            {
                _MethodBase md = methods.Find((p) => { return p.Name == "get_" + props[i].Name; });
                string get = md == null ? "get" : "_get";
                md = methods.Find((p) => { return p.Name == "set_" + props[i].Name; });
                string set = md == null ? "set" : "_set";
                sb.AppendFormat("\t\tL.RegVar(\"{0}\", {1}_{0}, {2}_{0});\r\n", props[i].Name, get, set);
                sb.AppendFormat("\t\tL.RegFunction(\"get{0}\", {1}_{0});\r\n", props[i].Name, get);
                if (props[i].GetSetMethod().IsStatic)
                    sb.AppendFormat("\t\tL.RegFunction(\"set{0}\", {1}_{0}ter);\r\n", props[i].Name, set);
                else
                    sb.AppendFormat("\t\tL.RegFunction(\"set{0}\", {1}_{0});\r\n", props[i].Name, set);
            }
            else if (props[i].CanRead)
            {
                _MethodBase md = methods.Find((p) => { return p.Name == "get_" + props[i].Name; });
                sb.AppendFormat("\t\tL.RegVar(\"{0}\", {1}_{0}, null);\r\n", props[i].Name, md == null ? "get" : "_get");
                sb.AppendFormat("\t\tL.RegFunction(\"get{0}\", {1}_{0});\r\n", props[i].Name, md == null ? "get" : "_get");
            }
            else if (props[i].CanWrite)
            {
                _MethodBase md = methods.Find((p) => { return p.Name == "set_" + props[i].Name; });
                sb.AppendFormat("\t\tL.RegVar(\"{0}\", null, {1}_{0});\r\n", props[i].Name, md == null ? "set" : "_set");
                if (props[i].GetSetMethod().IsStatic)
                    sb.AppendFormat("\t\tL.RegFunction(\"set{0}\", {1}_{0}ter);\r\n", props[i].Name, md == null ? "set" : "_set");
                else
                    sb.AppendFormat("\t\tL.RegFunction(\"set{0}\", {1}_{0});\r\n", props[i].Name, md == null ? "set" : "_set");
            }
        }

        for (int i = 0; i < events.Length; i++)
        {
            sb.AppendFormat("\t\tL.RegVar(\"{0}\", get_{0}, set_{0});\r\n", events[i].Name);
        }
    }

    static void GenRegisterEventTypes()
    {
        List<Type> list = new List<Type>();

        foreach (Type t in eventSet)
        {
            string funcName = null;
            string space = GetNameSpace(t, out funcName);

            if (space != className)
            {
                list.Add(t);
                continue;
            }

            funcName = ConvertToLibSign(funcName);
            int index = Array.FindIndex<DelegateType>(CustomSettings.customDelegateList, (p) => { return p.type == t; });
            string abr = null;
            if (index >= 0) abr = CustomSettings.customDelegateList[index].abr;
            abr = abr == null ? funcName : abr;
            funcName = ConvertToLibSign(space) + "_" + funcName;

            sb.AppendFormat("\t\tL.RegFunction(\"{0}\", {1});\r\n", abr, funcName);
        }

        for (int i = 0; i < list.Count; i++)
        {
            eventSet.Remove(list[i]);
        }
    }

    static void GenRegisterFunction()
    {
        sb.AppendLineEx("\tpublic static void Register(LuaState L)");
        sb.AppendLineEx("\t{");

        if (isStaticClass)
        {
            sb.AppendFormat("\t\tL.BeginStaticLibs(\"{0}\");\r\n", libClassName);
        }
        else if (!type.IsGenericType)
        {
            if (baseType == null)
            {
                sb.AppendFormat("\t\tL.BeginClass(typeof({0}), null);\r\n", className);
            }
            else
            {
                sb.AppendFormat("\t\tL.BeginClass(typeof({0}), typeof({1}));\r\n", className, GetBaseTypeStr(baseType));
            }
        }
        else
        {
            if (baseType == null)
            {
                sb.AppendFormat("\t\tL.BeginClass(typeof({0}), null, \"{1}\");\r\n", className, libClassName);
            }
            else
            {
                sb.AppendFormat("\t\tL.BeginClass(typeof({0}), typeof({1}), \"{2}\");\r\n", className, GetBaseTypeStr(baseType), libClassName);
            }
        }

        GenRegisterFuncItems();
        GenRegisterOpItems();
        GenRegisterVariables();
        GenRegisterEventTypes();            //注册事件类型

        if (!isStaticClass)
        {
            if (CustomSettings.outList.IndexOf(type) >= 0)
            {
                sb.AppendLineEx("\t\tL.RegVar(\"out\", get_out, null);");
            }

            sb.AppendFormat("\t\tL.EndClass();\r\n");
        }
        else
        {
            sb.AppendFormat("\t\tL.EndStaticLibs();\r\n");
        }

        sb.AppendLineEx("\t}");
    }
}
