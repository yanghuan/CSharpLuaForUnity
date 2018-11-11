# CSharpLuaForUnity
CSharpLuaForUnity尝试使用[CSharp.lua](https://github.com/yanghuan/CSharp.lua)将Unity工程中的C#脚本编译至lua,以使其可用C#进行高效的开发,但是也能用Lua完成热更新,也已经开始在部分新项目中被采用。

## 依赖说明
* 使用[tolua1.0.7.392](https://github.com/topameng/tolua/tree/1.0.7.392)版本作为lua支持环境
* 使用[CSharp.lua](https://github.com/yanghuan/CSharp.lua)来将C#代码转换至lua

## 如何使用
* 在Unity编辑器环境下，会新增菜单项'CSharpLua',子菜单'Compile'可将工程目录[CompiledScripts](https://github.com/yanghuan/CSharpLuaForUnity/tree/master/Assets/CompiledScripts)下的C#代码编译成Lua代码，子菜单'Switch to XXX'可在运行C#代码还是编译后的Lua代码中切换。设想的是实际开发中一直使用C#代码开发和调试，需要真机发布时，才编译到Lua代码。
* [Examples](https://github.com/yanghuan/CSharpLuaForUnity/tree/master/Assets/BaseScripts/CSharpLua/Examples)目录下有一个简易的列子,可直接运行。可以看出能够支持在预设中挂载自定义的C#脚本，在运行Lua代码时，预设会被动态适配处理，具体实现见代码。因而在打包时也需要对存在挂载了自定义c#脚本的的预设做相同的处理。

## 致谢
* https://github.com/topameng/tolua
* https://github.com/jarjin/LuaFramework_UGUI

## 相关工程
也可以考虑使用ILRuntime完成类似的需求
https://github.com/Ourpalm/ILRuntime
