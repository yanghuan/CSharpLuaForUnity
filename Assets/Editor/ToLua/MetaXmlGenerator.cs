using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using LuaInterface;
using UnityEngine;

public class MetaMethodInfo
{
    public MethodBase method;
    public string name;
}

public class MetaXmlGenerator
{
    List<MetaMethodInfo> _methods = new List<MetaMethodInfo>();
    HashSet<Type> _skipClass = new HashSet<Type>();
    HashSet<Type> _includeClass = new HashSet<Type>();
    public void Clear()
    {
        _methods.Clear();
        _skipClass.Clear();
        _includeClass.Clear();
    }

    public void AddSkipClass(Type t)
    {
        _skipClass.Add(t);
    }
    public void AddIncludeClass(Type t)
    {
        _includeClass.Add(t.IsGenericType && !t.IsGenericTypeDefinition? t.GetGenericTypeDefinition() : t);
    }

    public void AddMethod(MethodBase method, string name)
    {
        var cls = method.ReflectedType;
        _methods.Add(new MetaMethodInfo() { method = method, name = name});
    }
    StringBuilder sb;

    static string GetArgumentTypeString(Type type, int tabCount)
    {
        var head = new String('\t', tabCount);
        if (type.IsGenericType && !type.IsGenericTypeDefinition)
        {
            var str = string.Format("{0}<arg type=\"{1}\">\n", head, ReplayXmlString(type.GetGenericTypeDefinition().FullName.Replace('`', '^')));

            foreach(var arg in type.GetGenericArguments())
            {
                if (!arg.IsGenericParameter)
                {
                    str += GetArgumentTypeString(arg, tabCount + 1);
                }
            }

            str += string.Format("{0}</arg>\n", head);
            return str;
        }
        else
        {
            return string.Format("{0}<arg type=\"{1}\"/>\n", head, ReplayXmlString(type.FullName));
        }
    }

    static string ReplayXmlString(string xml)
    {
        return xml.Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("'", "&apos;")
            .Replace("&", "&amp;")
            .Replace("\"", "&quot;")
            ;
    }

    string GenerateNormalMethodTemplate(MetaMethodInfo method, string parameters)
    {
        Type classType = method.method.ReflectedType;
        return string.Format("{0}{1}({2})", method.method.IsStatic ? classType.FullName + "." : "{this}:", method.name, parameters, " GenericArgCount=\"0\"");
    }

    private KeyValuePair<bool, string> GenerateGenericMethodTemplate(MetaMethodInfo method, string parameters)
    {
        Type classType = method.method.ReflectedType;

        string externionsClassName = "Generic." + classType.Name + "Externions";
        string externionsMethodName = method.name;
        if (!CheckExternionsMethod(externionsClassName, externionsMethodName, method.method))
        {
            var pars = method.method.GetParameters();
            var args = method.method.GetGenericArguments();
            var parametersString = "";

            if (!method.method.IsStatic)
            {
                parametersString += "this " + classType.FullName + " self";
                if (parameters.Length > 0 || args.Length > 0)
                {
                    parametersString += ", ";
                }
            }

            for (int i = 0; i < pars.Length; i++)
            {
                if (i != 0)
                {
                    parametersString += ", ";
                }
                var par = pars[i];
                parametersString += par.ParameterType.FullName + " " + par.Name;
            }

            for (int i = 0; i < args.Length; i++)
            {
                if (i != 0 || pars.Length > 0)
                {
                    parametersString += ", ";
                }
                parametersString += "System.Type " + args[i].Name;
            }

            var csharpMethod = string.Format("{0}.{1}({2})", externionsClassName, externionsMethodName, parametersString);
            Debug.LogWarningFormat("没有找到泛型方法的Lua导出：{0}", csharpMethod);

            return new KeyValuePair<bool, string>(false, csharpMethod);
        }
        else
        {
            return new KeyValuePair<bool, string>(true, string.Format("{0}.{1}({2}{3})", externionsClassName, externionsMethodName, method.method.IsStatic ? string.Empty : (parameters.Length > 0 ? "{this}, " : "{this}"), parameters));
        }
    }
    static Dictionary<string, Type> _allTypes;
    static MetaXmlGenerator()
    {
        _allTypes = typeof(Generic.GameObjectExternions).Assembly.GetTypes().ToDictionary(v=>v.FullName);
    }

    static bool CheckParameterType(Type type1, Type type2)
    {
        while(type1 != null && type1 != type2)
        {
            type1 = type1.BaseType;
        }
        return type1 == type2;
    }
    //static Type GetParameterTypeBaseNotGeneric(Type type)
    //{
    //    if (typeof(Delegate).IsAssignableFrom(type))
    //    {
    //        return type;
    //    }
    //    else
    //    {
    //        while (type.IsGenericType)
    //        {
    //            type = type.BaseType;
    //        }
    //    }
    //    return type;
    //}
    //static string GetGenericParameters(Type[] types)
    //{
    //    var str = "";
    //    for(int i = 0; i < types.Length; i ++)
    //    {
    //        if(i > 0)
    //        {
    //            str += ", ";
    //        }
    //        str += GetParameterTypeBaseNotGenericString(types[i]);
    //    }
    //    return str;
    //}
    //static string GetParameterTypeBaseNotGenericString(Type type)
    //{
    //    var t = GetParameterTypeBaseNotGeneric(type);
    //    if (!t.IsGenericType)
    //    {
    //        return t.FullName;
    //    }
    //    else
    //    {
    //        var args = t.GetGenericArguments();
    //        var dot = t.FullName.IndexOf('`');
    //        var fullName = t.FullName.Substring(0, dot);
    //        return string.Format("{0}<{1}>", fullName, GetGenericParameters(args));
    //    }
    //}

    static bool CheckExternionsMethod(string classFullName, string methodName, MethodBase method)
    {
        Type t;
        if(_allTypes.TryGetValue(classFullName , out t))
        {
            if (t.FullName == classFullName)
            {
                var targetMethod = t.GetMethod(methodName);
                if (targetMethod == null)
                    return false;

                var selfParam = method.IsStatic ? 0 : 1;
                var targetParameters = targetMethod.GetParameters();
                var sourceParameters = method.GetParameters();
                if (targetParameters.Length != selfParam + sourceParameters.Length + method.GetGenericArguments().Length)
                {
                    Debug.LogWarningFormat("泛型导出实现参数个数不匹配：{0}.{1} 的参数数量应为{2} + {3} + {4} = {5}", classFullName, methodName, selfParam, sourceParameters.Length , method.GetGenericArguments().Length, selfParam + sourceParameters.Length + method.GetGenericArguments().Length);
                    return false;
                }
                if(!method.IsStatic)
                {
                    var targetParam = targetParameters[0];
                    if(!CheckParameterType(targetParam.ParameterType , method.ReflectedType))
                    {
                        Debug.LogWarningFormat("泛型导出实现参数不匹配：{0}.{1} 的参数{2}类型{3}应为{4}", classFullName, methodName, targetParam.Name, targetParam.ParameterType.FullName, method.ReflectedType.FullName);
                        return false;
                    }
                }
                for(int i = 0; i < sourceParameters.Length; i ++)
                {
                    var sourceParam = sourceParameters[i];
                    var targetParam = targetParameters[selfParam + i];
                    if(!CheckParameterType(sourceParam.ParameterType , targetParam.ParameterType))
                    {
                        Debug.LogWarningFormat("泛型导出实现参数不匹配：{0}.{1} 的参数{2}类型{3}应为{4}", classFullName, methodName, targetParam.Name, targetParam.ParameterType.FullName, sourceParam.ParameterType.FullName);
                        return false;
                    }
                }
                for(int i = sourceParameters.Length + selfParam; i < targetParameters.Length; i ++)
                {
                    var targetParam = targetParameters[i];
                    if(targetParam.ParameterType != typeof(System.Type))
                    {
                        Debug.LogWarningFormat("泛型导出实现参数不匹配：{0}.{1} 的泛型参数{2}类型{3}应为{4}", classFullName, methodName, targetParam.Name, targetParam.ParameterType.FullName, typeof(System.Type).FullName);
                        return false;
                    }
                }

                return true;
            }
        }
        return false;
    }
    static bool IsParams(ParameterInfo param)
    {
        return param.GetCustomAttributes(typeof(ParamArrayAttribute), false).Length > 0;
    }
    void GenerateMethod(MetaMethodInfo method, bool forceBaned = false)
    {

        Type classType = method.method.ReflectedType;
        var pars = method.method.GetParameters();
        var parameters = "";
        for (int i = 0; i < pars.Length; i++)
        {
            if (i != 0)
            {
                parameters += ", ";
            }
            var par = pars[i];
            parameters += "{" + (IsParams(par) ? "*" : string.Empty) + i.ToString() + "}";
        }

        if (method.method.IsGenericMethod)
        {
            var args = method.method.GetGenericArguments();
            for (int i = 0; i < args.Length; i++)
            {
                if (i != 0 || pars.Length > 0)
                {
                    parameters += ", ";
                }
                parameters += "System.TypeOfTolua({^" + i.ToString() + "})";
            }
        }
        KeyValuePair<bool, string> template;
        if (forceBaned)
        {
            template = new KeyValuePair<bool, string>(false, "custom baned!");
        }
        else
        {
            template = (method.method.IsGenericMethod ? GenerateGenericMethodTemplate(method, parameters) : new KeyValuePair<bool, string>(true, GenerateNormalMethodTemplate(method, parameters)));
        }

        var genericArgumentsLength = (method.method.IsGenericMethod ? method.method.GetGenericArguments().Length : 0);
        sb.AppendFormat("\t\t\t\t<method name=\"{0}\" GenericArgCount=\"{1}\"{2}>\n", method.method.Name, genericArgumentsLength, !template.Key ? " Baned=\"true\" NotImplementMethod=\"" + template.Value + "\"" : " Template=\"" + template.Value + "\"");

        for (int i = 0; i < pars.Length; i++)
        {
            var par = pars[i];
            sb.AppendFormat(GetArgumentTypeString(par.ParameterType, 5));
        }
        sb.AppendFormat("\t\t\t\t</method>\n");
    }

    private void GenerateClass(IGrouping<Type, MetaMethodInfo> classPair)
    {
        var classType = classPair.Key;
        sb.AppendFormat("\t\t\t<class name=\"{0}\">\n", classType.Name);
        foreach (var method in classPair)
        {
            GenerateMethod(method, CustomSettings.banedMetaList.Contains(method.method));
        }
        sb.AppendFormat("\t\t\t</class>\n");
    }

    private void GenerateNamespace(IGrouping<string, MetaMethodInfo> namespacePair)
    {
        var ns = namespacePair.Key;
        sb.AppendFormat("\t\t<namespace name=\"{0}\">\n", ns);
        // 根据class分组
        foreach (var classPair in namespacePair.GroupBy(method =>
        {
            var cls = method.method.ReflectedType;
            if (!cls.IsGenericType)
            {
                return cls;
            }
            else
            {
                if (cls.IsGenericTypeDefinition)
                {
                    return cls;
                }
                else
                {
                    return cls.GetGenericTypeDefinition();
                }
            }
        }))
        {
            GenerateClass(classPair);

        }
        sb.AppendFormat("\t\t</namespace>\n");
    }

    private void GenerateAssembly(IGrouping<Assembly, MetaMethodInfo> assemblyPair, string metaXmlDir)
    {
        var assembly = assemblyPair.Key;
        sb = new StringBuilder();

        sb.AppendFormat("<?xml version=\"1.0\" encoding=\"utf-8\" ?>\n");
        sb.AppendFormat("<meta>\n");
        sb.AppendFormat("\t<assembly>\n");
        // 根据namespace分组
        foreach (var namespacePair in assemblyPair.GroupBy(method => method.method.ReflectedType.Namespace))
        {
            GenerateNamespace(namespacePair);
        }
        GenerateClassSkip(assembly);

        sb.AppendFormat("\t</assembly>\n");
        sb.AppendFormat("</meta>\n");
        File.WriteAllText(metaXmlDir + assembly.GetName().Name + ".xml", sb.ToString(), Encoding.UTF8);
    }

    void GenerateClassSkip(Assembly assembly)
    {
        // 所有public的类
        var types = assembly.GetExportedTypes().ToList();
        types.RemoveAll(v => _skipClass.Contains(v));

        // 所有需要生成的类
        var gens = _methods.GroupBy(v => v.method.ReflectedType.IsGenericType && !v.method.ReflectedType.IsGenericTypeDefinition ? v.method.ReflectedType.GetGenericTypeDefinition() : v.method.ReflectedType).Select(v=>v.Key).Concat(_includeClass).GroupBy(v=>v).ToDictionary(v=>v.Key);

        // 找到不需要生成的类
        types.RemoveAll(v => gens.ContainsKey(v) || !v.IsClass);

        foreach (var namespacePair in types.GroupBy(t => t.Namespace))
        {
            var ns = namespacePair.Key;
            sb.AppendFormat("\t\t<namespace name=\"{0}\">\n", ns);
            foreach(var classTypePair in namespacePair)
            {
                var classType = classTypePair;
                GenerateBanedClass(classType);
            }
            sb.AppendFormat("\t\t</namespace>\n");
        }
    }

    string GenClassName(Type classType)
    {
        var className = classType.Name;
        if(classType.ReflectedType != null)
        {
            return GenClassName(classType.ReflectedType) + "." + className;
        }
        return className;
    }
    void GenerateBanedClass(Type classType)
    {
        sb.AppendFormat("\t\t\t<class name=\"{0}\" Baned=\"true\">\n", GenClassName(classType));

        sb.AppendFormat("\t\t\t</class>\n");
    }

    public void Generate(string metaXmlDir)
    {
        // 根据Assembly分组
        foreach (var assemblyPair in _methods.GroupBy(v => v.method.ReflectedType.Assembly))
        {
            GenerateAssembly(assemblyPair, metaXmlDir);
        }

    }
}
