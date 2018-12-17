# CSharpLuaForUnity
CSharpLuaForUnity尝试使用[CSharp.lua](https://github.com/yanghuan/CSharp.lua)将Unity工程中的C#脚本编译至Lua,以使其可用C#进行高效的开发,但是也能用Lua完成热更新,也已经开始在部分新项目中被采用。

## 依赖说明
* 使用[tolua1.0.7.392](https://github.com/topameng/tolua/tree/1.0.7.392)版本作为Lua支持环境
* 使用[CSharp.lua](https://github.com/yanghuan/CSharp.lua)来将C#代码转换至Lua

## 如何使用
  确保本机已经安装net core 2.0+ 的运行环境或者sdk，https://dotnet.microsoft.com/download ，CSharp.lua需要net core运行环境支持。
1)  使用Visual Studio打开[GameScript/GameScript.sln](https://github.com/yanghuan/CSharpLuaForUnity/blob/master/GameScript/GameScript.sln)解决方案，里面的GameScript工程中的代码可以编译至Lua
2)  菜单栏配置管理可在"Lua"以及"CSharp"之间进行切换，在Lua设置下会将GameScript工程中的代码编译至Lua并放至到[Assets/Lua/CompiledScripts](https://github.com/yanghuan/CSharpLuaForUnity/tree/master/Assets/Lua/CompiledScripts)目录，在CSharp设置下会将编译成功后的dll放置到Unity工程中加载。希望的是平时尽量在CSharp环境下开发、调试，仅需要的时候才编译至Lua。

  Assets/LuaRuntime/CSharpLua/Examples/01_Hello/Hello.unity 是一个测试场景，可以直接运行。

## 致谢
* https://github.com/topameng/tolua
* https://github.com/jarjin/LuaFramework_UGUI

## 相关工程
也可以考虑使用ILRuntime完成类似的需求
https://github.com/Ourpalm/ILRuntime
