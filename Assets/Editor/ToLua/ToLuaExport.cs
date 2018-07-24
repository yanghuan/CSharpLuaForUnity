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
using System.Collections;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using LuaInterface;

using Object = UnityEngine.Object;
using System.IO;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;

public enum MetaOp
{
    None = 0,
    Add = 1,
    Sub = 2,
    Mul = 4,
    Div = 8,
    Eq = 16,
    Neg = 32,
    ToStr = 64,
    ALL = Add | Sub | Mul | Div | Eq | Neg | ToStr,
}

public enum ObjAmbig
{
    None = 0, 
    U3dObj = 1,
    NetObj = 2,
    All = 3
}

public class DelegateType
{
    public string name;
    public Type type;
    public string abr = null;

    public string strType = "";

    public DelegateType(Type t)
    {
        type = t;
        strType = ToLuaExport.GetTypeStr(t);                
        name = ToLuaExport.ConvertToLibSign(strType);        
    }

    public DelegateType SetAbrName(string str)
    {
        abr = str;
        return this;
    }
}

public static partial class ToLuaExport 
{
    public static string className = string.Empty;
    public static Type type = null;
    public static Type baseType = null;
        
    public static bool isStaticClass = true;

    public static MetaXmlGenerator metaXml;

    static HashSet<string> usingList = new HashSet<string>();
    static MetaOp op = MetaOp.None;    
    static StringBuilder sb = null;
    static List<_MethodBase> methods = new List<_MethodBase>();
    static Dictionary<string, int> nameCounter = new Dictionary<string, int>();
    static FieldInfo[] fields = null;
    static PropertyInfo[] props = null;    
    static List<PropertyInfo> propList = new List<PropertyInfo>();  //非静态属性
    static List<PropertyInfo> allProps = new List<PropertyInfo>();
    static EventInfo[] events = null;
    static List<EventInfo> eventList = new List<EventInfo>();
    static List<_MethodBase> ctorList = new List<_MethodBase>();
    static List<ConstructorInfo> ctorExtList = new List<ConstructorInfo>();
    static List<_MethodBase> getItems = new List<_MethodBase>();   //特殊属性
    static List<_MethodBase> setItems = new List<_MethodBase>();

    static BindingFlags binding = BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase;
        
    static ObjAmbig ambig = ObjAmbig.NetObj;    
    //wrapClaaName + "Wrap" = 导出文件名，导出类名
    public static string wrapClassName = "";

    public static string libClassName = "";
    public static string extendName = "";
    public static Type extendType = null;

    public static HashSet<Type> eventSet = new HashSet<Type>();
    public static List<Type> extendList = new List<Type>();    

    public static List<string> memberFilter = new List<string>
    {
        "String.Chars",
        "Directory.SetAccessControl",
        "File.GetAccessControl",
        "File.SetAccessControl",
        //UnityEngine
        "AnimationClip.averageDuration",
        "AnimationClip.averageAngularSpeed",
        "AnimationClip.averageSpeed",
        "AnimationClip.apparentSpeed",
        "AnimationClip.isLooping",
        "AnimationClip.isAnimatorMotion",
        "AnimationClip.isHumanMotion",
        "AnimatorOverrideController.PerformOverrideClipListCleanup",
        "AnimatorControllerParameter.name",
        "Caching.SetNoBackupFlag",
        "Caching.ResetNoBackupFlag",
        "Light.areaSize",
        "Light.lightmappingMode",
        "Light.lightmapBakeType",
        "Security.GetChainOfTrustValue",
        "Texture2D.alphaIsTransparency",
        "WWW.movie",
        "WWW.GetMovieTexture",
        "WebCamTexture.MarkNonReadable",
        "WebCamTexture.isReadable",
        "Graphic.OnRebuildRequested",
        "Text.OnRebuildRequested",
        "Resources.LoadAssetAtPath",
        "Application.ExternalEval",
        "Handheld.SetActivityIndicatorStyle",
        "CanvasRenderer.OnRequestRebuild",
        "CanvasRenderer.onRequestRebuild",
        "Terrain.bakeLightProbesForTrees",
        "MonoBehaviour.runInEditMode",
        "TextureFormat.DXT1Crunched",
        "TextureFormat.DXT5Crunched",
        "Texture.imageContentsHash",
        //NGUI
        "UIInput.ProcessEvent",
        "UIWidget.showHandlesWithMoveTool",
        "UIWidget.showHandles",
        "Input.IsJoystickPreconfigured",
        "UIDrawCall.isActive"
    };

    public static List<MemberInfo> memberInfoFilter = new List<MemberInfo>
    {
        //可精确查找一个函数
		//Type.GetMethod(string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers);
    };


    static ToLuaExport()
    {
        Debugger.useLog = true;
    }

    public static void Clear()
    {
        className = null;
        type = null;
        baseType = null;
        isStaticClass = false;        
        usingList.Clear();
        op = MetaOp.None;    
        sb = new StringBuilder();        
        fields = null;
        props = null;
        methods.Clear();
        allProps.Clear();
        propList.Clear();
        eventList.Clear();
        ctorList.Clear();
        ctorExtList.Clear();        
        ambig = ObjAmbig.NetObj;
        wrapClassName = "";
        libClassName = "";
        extendName = "";
        eventSet.Clear();
        extendType = null;
        nameCounter.Clear();
        events = null;
        getItems.Clear();
        setItems.Clear();
    }

    public static void Generate(string dir)
    {
#if !EXPORT_INTERFACE
        Type iterType = typeof(System.Collections.IEnumerator);

        if (type.IsInterface && type != iterType)
        {
            return;
        }
#endif

        //Debugger.Log("Begin Generate lua Wrap for class {0}", className);        
        sb = new StringBuilder();
        usingList.Add("System");                

        if (wrapClassName == "")
        {
            wrapClassName = className;
        }

        if (type.IsEnum)
        {
            BeginCodeGen();
            GenEnum();                                    
            EndCodeGen(dir);
            return;
        }

        InitMethods();
        InitPropertyList();
        InitCtorList();

        BeginCodeGen();

        GenRegisterFunction();
        GenConstructFunction();
        GenItemPropertyFunction();             
        GenFunctions();
        //GenToStringFunction();
        GenIndexFunc();
        GenNewIndexFunc();
        GenOutFunction();
        GenEventFunctions();

        EndCodeGen(dir);

    }

    //记录所有的导出类型
    public static List<Type> allTypes = new List<Type>();



    static void SaveFile(string file)
    {        
        using (StreamWriter textWriter = new StreamWriter(file, false, Encoding.UTF8))
        {            
            StringBuilder usb = new StringBuilder();
            usb.AppendLineEx("//this source code was auto-generated by tolua#, do not modify it");

            foreach (string str in usingList)
            {
                usb.AppendFormat("using {0};\r\n", str);
            }

            usb.AppendLineEx("using LuaInterface;");

            if (ambig == ObjAmbig.All)
            {
                usb.AppendLineEx("using Object = UnityEngine.Object;");
            }

            usb.AppendLineEx();

            textWriter.Write(usb.ToString());
            textWriter.Write(sb.ToString());
            textWriter.Flush();
            textWriter.Close();
        }  
    }



    //decimal 类型扔掉了
    static Dictionary<Type, int> typeSize = new Dictionary<Type, int>()
    {        
        { typeof(char), 2 },
        { typeof(byte), 3 },
        { typeof(sbyte), 4 },
        { typeof(ushort),5 },      
        { typeof(short), 6 },        
        { typeof(uint), 7 },
        { typeof(int), 8 },                
        //{ typeof(ulong), 9 },
        //{ typeof(long), 10 },
        { typeof(decimal), 11 },
        { typeof(float), 12 },
        { typeof(double), 13 },

    };

}
