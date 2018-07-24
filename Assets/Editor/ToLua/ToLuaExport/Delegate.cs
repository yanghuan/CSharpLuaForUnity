using LuaInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

public static partial class ToLuaExport
{

    static void GenLuaFunctionRetValue(StringBuilder sb, Type t, string head, string name, bool beDefined = false)
    {
        if (t == typeof(bool))
        {
            name = beDefined ? name : "bool " + name;
            sb.AppendFormat("{0}{1} = func.CheckBoolean();\r\n", head, name);
        }
        else if (t == typeof(long))
        {
            name = beDefined ? name : "long " + name;
            sb.AppendFormat("{0}{1} = func.CheckLong();\r\n", head, name);
        }
        else if (t == typeof(ulong))
        {
            name = beDefined ? name : "ulong " + name;
            sb.AppendFormat("{0}{1} = func.CheckULong();\r\n", head, name);
        }
        else if (t.IsPrimitive || IsNumberEnum(t))
        {
            string type = GetTypeStr(t);
            name = beDefined ? name : type + " " + name;
            sb.AppendFormat("{0}{1} = ({2})func.CheckNumber();\r\n", head, name, type);
        }
        else if (t == typeof(string))
        {
            name = beDefined ? name : "string " + name;
            sb.AppendFormat("{0}{1} = func.CheckString();\r\n", head, name);
        }
        else if (typeof(System.MulticastDelegate).IsAssignableFrom(t))
        {
            name = beDefined ? name : GetTypeStr(t) + " " + name;
            sb.AppendFormat("{0}{1} = func.CheckDelegate();\r\n", head, name);
        }
        else if (t == typeof(Vector3))
        {
            name = beDefined ? name : "UnityEngine.Vector3 " + name;
            sb.AppendFormat("{0}{1} = func.CheckVector3();\r\n", head, name);
        }
        else if (t == typeof(Quaternion))
        {
            name = beDefined ? name : "UnityEngine.Quaternion " + name;
            sb.AppendFormat("{0}{1} = func.CheckQuaternion();\r\n", head, name);
        }
        else if (t == typeof(Vector2))
        {
            name = beDefined ? name : "UnityEngine.Vector2 " + name;
            sb.AppendFormat("{0}{1} = func.CheckVector2();\r\n", head, name);
        }
        else if (t == typeof(Vector4))
        {
            name = beDefined ? name : "UnityEngine.Vector4 " + name;
            sb.AppendFormat("{0}{1} = func.CheckVector4();\r\n", head, name);
        }
        else if (t == typeof(Color))
        {
            name = beDefined ? name : "UnityEngine.Color " + name;
            sb.AppendFormat("{0}{1} = func.CheckColor();\r\n", head, name);
        }
        else if (t == typeof(Color32))
        {
            name = beDefined ? name : "UnityEngine.Color32 " + name;
            sb.AppendFormat("{0}{1} = func.CheckColor32();\r\n", head, name);
        }
        else if (t == typeof(Ray))
        {
            name = beDefined ? name : "UnityEngine.Ray " + name;
            sb.AppendFormat("{0}{1} = func.CheckRay();\r\n", head, name);
        }
        else if (t == typeof(Bounds))
        {
            name = beDefined ? name : "UnityEngine.Bounds " + name;
            sb.AppendFormat("{0}{1} = func.CheckBounds();\r\n", head, name);
        }
        else if (t == typeof(LayerMask))
        {
            name = beDefined ? name : "UnityEngine.LayerMask " + name;
            sb.AppendFormat("{0}{1} = func.CheckLayerMask();\r\n", head, name);
        }
        else if (t == typeof(object))
        {
            name = beDefined ? name : "object " + name;
            sb.AppendFormat("{0}{1} = func.CheckVariant();\r\n", head, name);
        }
        else if (t == typeof(byte[]))
        {
            name = beDefined ? name : "byte[] " + name;
            sb.AppendFormat("{0}{1} = func.CheckByteBuffer();\r\n", head, name);
        }
        else if (t == typeof(char[]))
        {
            name = beDefined ? name : "char[] " + name;
            sb.AppendFormat("{0}{1} = func.CheckCharBuffer();\r\n", head, name);
        }
        else
        {
            string type = GetTypeStr(t);
            name = beDefined ? name : type + " " + name;
            sb.AppendFormat("{0}{1} = ({2})func.CheckObject(typeof({2}));\r\n", head, name, type);

            //Debugger.LogError("GenLuaFunctionCheckValue undefined type:" + t.FullName);
        }
    }
    
    //是否为委托类型，没处理废弃
    public static bool IsDelegateType(Type t)
    {
        if (!typeof(System.MulticastDelegate).IsAssignableFrom(t) || t == typeof(System.MulticastDelegate))
        {
            return false;
        }

        if (IsMemberFilter(t))
        {
            return false;
        }

        return true;
    }

    static string CreateDelegate = @"    
    public static Delegate CreateDelegate(Type t, LuaFunction func = null)
    {
        DelegateCreate Create = null;

        if (!dict.TryGetValue(t, out Create))
        {
            throw new LuaException(string.Format(""Delegate {0} not register"", LuaMisc.GetTypeName(t)));            
        }

        if (func != null)
        {
            LuaState state = func.GetLuaState();
            LuaDelegate target = state.GetLuaDelegate(func);
            
            if (target != null)
            {
                return Delegate.CreateDelegate(t, target, target.method);
            }  
            else
            {
                Delegate d = Create(func, null, false);
                target = d.Target as LuaDelegate;
                state.AddLuaDelegate(target, func);
                return d;
            }       
        }

        return Create(null, null, false);        
    }
    
    public static Delegate CreateDelegate(Type t, LuaFunction func, LuaTable self)
    {
        DelegateCreate Create = null;

        if (!dict.TryGetValue(t, out Create))
        {
            throw new LuaException(string.Format(""Delegate {0} not register"", LuaMisc.GetTypeName(t)));
        }

        if (func != null)
        {
            LuaState state = func.GetLuaState();
            LuaDelegate target = state.GetLuaDelegate(func, self);

            if (target != null)
            {
                return Delegate.CreateDelegate(t, target, target.method);
            }
            else
            {
                Delegate d = Create(func, self, true);
                target = d.Target as LuaDelegate;
                state.AddLuaDelegate(target, func, self);
                return d;
            }
        }

        return Create(null, null, true);
    }
";

    static string RemoveDelegate = @"    
    public static Delegate RemoveDelegate(Delegate obj, LuaFunction func)
    {
        LuaState state = func.GetLuaState();
        Delegate[] ds = obj.GetInvocationList();

        for (int i = 0; i < ds.Length; i++)
        {
            LuaDelegate ld = ds[i].Target as LuaDelegate;

            if (ld != null && ld.func == func)
            {
                obj = Delegate.Remove(obj, ds[i]);
                state.DelayDispose(ld.func);
                break;
            }
        }

        return obj;
    }
    
    public static Delegate RemoveDelegate(Delegate obj, Delegate dg)
    {
        LuaDelegate remove = dg.Target as LuaDelegate;

        if (remove == null)
        {
            obj = Delegate.Remove(obj, dg);
            return obj;
        }

        LuaState state = remove.func.GetLuaState();
        Delegate[] ds = obj.GetInvocationList();        

        for (int i = 0; i < ds.Length; i++)
        {
            LuaDelegate ld = ds[i].Target as LuaDelegate;

            if (ld != null && ld == remove)
            {
                obj = Delegate.Remove(obj, ds[i]);
                state.DelayDispose(ld.func);
                state.DelayDispose(ld.self);
                break;
            }
        }

        return obj;
    }
";
    static void GenDelegateBody(StringBuilder sb, Type t, string head, bool hasSelf = false)
    {
        MethodInfo mi = t.GetMethod("Invoke");
        ParameterInfo[] pi = mi.GetParameters();
        int n = pi.Length;

        if (n == 0)
        {
            if (mi.ReturnType == typeof(void))
            {
                if (!hasSelf)
                {
                    sb.AppendFormat("{0}{{\r\n{0}\tfunc.Call();\r\n{0}}}\r\n", head);
                }
                else
                {
                    sb.AppendFormat("{0}{{\r\n{0}\tfunc.BeginPCall();\r\n", head);
                    sb.AppendFormat("{0}\tfunc.Push(self);\r\n", head);
                    sb.AppendFormat("{0}\tfunc.PCall();\r\n", head);
                    sb.AppendFormat("{0}\tfunc.EndPCall();\r\n", head);
                    sb.AppendFormat("{0}}}\r\n", head);
                }
            }
            else
            {
                sb.AppendFormat("{0}{{\r\n{0}\tfunc.BeginPCall();\r\n", head);
                if (hasSelf) sb.AppendFormat("{0}\tfunc.Push(self);\r\n", head);
                sb.AppendFormat("{0}\tfunc.PCall();\r\n", head);
                GenLuaFunctionRetValue(sb, mi.ReturnType, head + "\t", "ret");
                sb.AppendFormat("{0}\tfunc.EndPCall();\r\n", head);
                sb.AppendLineEx(head + "\treturn ret;");
                sb.AppendFormat("{0}}}\r\n", head);
            }

            return;
        }

        sb.AppendFormat("{0}{{\r\n{0}", head);
        sb.AppendLineEx("\tfunc.BeginPCall();");
        if (hasSelf) sb.AppendFormat("{0}\tfunc.Push(self);\r\n", head);

        for (int i = 0; i < n; i++)
        {
            string push = GetPushFunction(pi[i].ParameterType);

            if (!IsParams(pi[i]))
            {
                if (pi[i].ParameterType == typeof(byte[]) && IsByteBuffer(t))
                {
                    sb.AppendFormat("{2}\tfunc.PushByteBuffer(param{1});\r\n", push, i, head);
                }
                else if (pi[i].Attributes != ParameterAttributes.Out)
                {
                    sb.AppendFormat("{2}\tfunc.{0}(param{1});\r\n", push, i, head);
                }
            }
            else
            {
                sb.AppendLineEx();
                sb.AppendFormat("{0}\tfor (int i = 0; i < param{1}.Length; i++)\r\n", head, i);
                sb.AppendLineEx(head + "\t{");
                sb.AppendFormat("{2}\t\tfunc.{0}(param{1}[i]);\r\n", push, i, head);
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
                    GenLuaFunctionRetValue(sb, pi[i].ParameterType.GetElementType(), head + "\t", "param" + i, true);
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
                    GenLuaFunctionRetValue(sb, pi[i].ParameterType.GetElementType(), head + "\t", "param" + i, true);
                }
            }

            sb.AppendFormat("{0}\tfunc.EndPCall();\r\n", head);
            sb.AppendLineEx(head + "\treturn ret;");
        }

        sb.AppendFormat("{0}}}\r\n", head);
    }

    public static void GenDelegates(DelegateType[] list)
    {
        usingList.Add("System");
        usingList.Add("System.Collections.Generic");

        for (int i = 0; i < list.Length; i++)
        {
            Type t = list[i].type;

            if (!typeof(System.Delegate).IsAssignableFrom(t))
            {
                Debug.LogError(t.FullName + " not a delegate type");
                return;
            }
        }

        sb.Append("internal class DelegateFactory\r\n");
        sb.Append("{\r\n");
        sb.Append("\tpublic delegate Delegate DelegateCreate(LuaFunction func, LuaTable self, bool flag);\r\n");
        sb.Append("\tpublic static Dictionary<Type, DelegateCreate> dict = new Dictionary<Type, DelegateCreate>();\r\n");
        sb.Append("\tstatic DelegateFactory factory = new DelegateFactory();\r\n");
        sb.AppendLineEx();
        sb.Append("\tpublic static void Init()\r\n");
        sb.Append("\t{\r\n");
        sb.Append("\t\tRegister();\r\n");
        sb.AppendLineEx("\t}\r\n");

        sb.Append("\tpublic static void Register()\r\n");
        sb.Append("\t{\r\n");
        sb.Append("\t\tdict.Clear();\r\n");

        for (int i = 0; i < list.Length; i++)
        {
            string type = list[i].strType;
            string name = list[i].name;
            sb.AppendFormat("\t\tdict.Add(typeof({0}), factory.{1});\r\n", type, name);
        }

        sb.AppendLineEx();

        for (int i = 0; i < list.Length; i++)
        {
            string type = list[i].strType;
            string name = list[i].name;
            sb.AppendFormat("\t\tDelegateTraits<{0}>.Init(factory.{1});\r\n", type, name);
        }

        sb.AppendLineEx();

        for (int i = 0; i < list.Length; i++)
        {
            string type = list[i].strType;
            string name = list[i].name;
            sb.AppendFormat("\t\tTypeTraits<{0}>.Init(factory.Check_{1});\r\n", type, name);
        }

        sb.AppendLineEx();

        for (int i = 0; i < list.Length; i++)
        {
            string type = list[i].strType;
            string name = list[i].name;
            sb.AppendFormat("\t\tStackTraits<{0}>.Push = factory.Push_{1};\r\n", type, name);
        }

        sb.Append("\t}\r\n");
        sb.Append(CreateDelegate);
        sb.AppendLineEx(RemoveDelegate);

        for (int i = 0; i < list.Length; i++)
        {
            Type t = list[i].type;
            string strType = list[i].strType;
            string name = list[i].name;
            MethodInfo mi = t.GetMethod("Invoke");
            string args = GetDelegateParams(mi);

            //生成委托类
            sb.AppendFormat("\tclass {0}_Event : LuaDelegate\r\n", name);
            sb.AppendLineEx("\t{");
            sb.AppendFormat("\t\tpublic {0}_Event(LuaFunction func) : base(func) {{ }}\r\n", name);
            sb.AppendFormat("\t\tpublic {0}_Event(LuaFunction func, LuaTable self) : base(func, self) {{ }}\r\n", name);
            sb.AppendLineEx();
            sb.AppendFormat("\t\tpublic {0} Call({1})\r\n", GetTypeStr(mi.ReturnType), args);
            GenDelegateBody(sb, t, "\t\t");
            sb.AppendLineEx();
            sb.AppendFormat("\t\tpublic {0} CallWithSelf({1})\r\n", GetTypeStr(mi.ReturnType), args);
            GenDelegateBody(sb, t, "\t\t", true);
            sb.AppendLineEx("\t}\r\n");

            //生成转换函数1
            sb.AppendFormat("\tpublic {0} {1}(LuaFunction func, LuaTable self, bool flag)\r\n", strType, name);
            sb.AppendLineEx("\t{");
            sb.AppendLineEx("\t\tif (func == null)");
            sb.AppendLineEx("\t\t{");
            sb.AppendFormat("\t\t\t{0} fn = delegate({1}) {2}", strType, args, GetDefaultDelegateBody(mi));
            sb.AppendLineEx("\t\t\treturn fn;");
            sb.AppendLineEx("\t\t}\r\n");
            sb.AppendLineEx("\t\tif(!flag)");
            sb.AppendLineEx("\t\t{");
            sb.AppendFormat("\t\t\t{0}_Event target = new {0}_Event(func);\r\n", name);
            sb.AppendFormat("\t\t\t{0} d = target.Call;\r\n", strType);
            sb.AppendLineEx("\t\t\ttarget.method = d.Method;");
            sb.AppendLineEx("\t\t\treturn d;");
            sb.AppendLineEx("\t\t}");
            sb.AppendLineEx("\t\telse");
            sb.AppendLineEx("\t\t{");
            sb.AppendFormat("\t\t\t{0}_Event target = new {0}_Event(func, self);\r\n", name);
            sb.AppendFormat("\t\t\t{0} d = target.CallWithSelf;\r\n", strType);
            sb.AppendLineEx("\t\t\ttarget.method = d.Method;");
            sb.AppendLineEx("\t\t\treturn d;");
            sb.AppendLineEx("\t\t}");
            sb.AppendLineEx("\t}\r\n");

            sb.AppendFormat("\tbool Check_{0}(IntPtr L, int pos)\r\n", name);
            sb.AppendLineEx("\t{");
            sb.AppendFormat("\t\treturn TypeChecker.CheckDelegateType(typeof({0}), L, pos);\r\n", strType);
            sb.AppendLineEx("\t}\r\n");

            sb.AppendFormat("\tvoid Push_{0}(IntPtr L, {1} o)\r\n", name, strType);
            sb.AppendLineEx("\t{");
            sb.AppendLineEx("\t\tToLua.Push(L, o);");
            sb.AppendLineEx("\t}\r\n");
        }

        sb.AppendLineEx("}\r\n");
        SaveFile(CustomSettings.saveDir + "DelegateFactory.cs");

        Clear();
    }

    static string GetDefaultDelegateBody(MethodInfo md)
    {
        string str = "\r\n\t\t\t{\r\n";
        bool flag = false;
        ParameterInfo[] pis = md.GetParameters();

        for (int i = 0; i < pis.Length; i++)
        {
            if (pis[i].Attributes == ParameterAttributes.Out)
            {
                str += string.Format("\t\t\t\tparam{0} = {1};\r\n", i, GetReturnValue(pis[i].ParameterType.GetElementType()));
                flag = true;
            }
        }

        if (flag)
        {
            if (md.ReturnType != typeof(void))
            {
                str += "\t\t\treturn ";
                str += GetReturnValue(md.ReturnType);
                str += ";";
            }

            str += "\t\t\t};\r\n\r\n";
            return str;
        }

        if (md.ReturnType == typeof(void))
        {
            return "{ };\r\n";
        }
        else
        {
            return string.Format("{{ return {0}; }};\r\n", GetReturnValue(md.ReturnType));
        }
    }


    static string GetDelegateParams(MethodInfo mi)
    {
        ParameterInfo[] infos = mi.GetParameters();
        List<string> list = new List<string>();

        for (int i = 0; i < infos.Length; i++)
        {
            string s2 = GetTypeStr(infos[i].ParameterType) + " param" + i;

            if (infos[i].ParameterType.IsByRef)
            {
                if (infos[i].Attributes == ParameterAttributes.Out)
                {
                    s2 = "out " + s2;
                }
                else
                {
                    s2 = "ref " + s2;
                }
            }

            list.Add(s2);
        }

        return string.Join(", ", list.ToArray());
    }
}
