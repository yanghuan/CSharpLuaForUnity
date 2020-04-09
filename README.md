# CSharpLuaForUnity
CSharpLuaForUnity尝试使用[CSharp.lua](https://github.com/yanghuan/CSharp.lua)将Unity工程中的C#脚本编译至Lua,以使其可用C#进行高效的开发,但是也能用Lua完成热更新,也已经开始在部分新项目中被采用。

## 依赖说明
* 使用[tolua](https://github.com/topameng/tolua)版本作为Lua支持环境
* 使用[CSharp.lua](https://github.com/yanghuan/CSharp.lua)来将C#代码转换至Lua

## 如何使用
* 在Unity编辑器环境下，会新增菜单项'CSharpLua',子菜单'Compile'可将工程目录[Compiled](https://github.com/yanghuan/CSharpLuaForUnity/tree/master/Assets/CSharpLua/Compiled)下的C#代码编译成Lua代码放置到Assets/Lua/Compiled目录，子菜单'Switch to XXX'可在运行C#代码还是编译后的Lua代码中切换。设想的是实际开发中一直使用C#代码开发和调试，需要真机发布时，才编译到Lua代码。
* [Examples](https://github.com/yanghuan/CSharpLuaForUnity/tree/master/Assets/CSharpLua/Examples)目录下有一个简易的列子,可直接运行。可以看出能够支持在预设中挂载自定义的C#脚本，在运行Lua代码时，预设会被动态适配处理，具体实现可见代码。因而在打包时也需要对存在挂载了自定义C#脚本的的预设做相同的处理。

## 项目结构
使用了[assembly definition files](https://docs.unity3d.com/Manual/ScriptCompilationAssemblyDefinitionFiles.html)额外定义了一些程序集工程，依赖顺序如下。

Assembly-CSharp.dll --------> Compiled.dll --------> Bridge.dll --------> Base.dll

* Compiled 此工程中的代码可编译至Lua，需要热更新的代码放到这个工程中
* Bridge 可被Compiled引用的代码，需要Wrap到Lua的环境中
* Base 可被Bridge引用的代码，但是不需要被Compiled所引用到

## 交流讨论
- [Issues](https://github.com/yanghuan/CSharpLuaForUnity/issues)
- 邮箱：sy.yanghuan@gmail.com
- QQ群: 715350749

## 致谢
* https://github.com/topameng/tolua
* https://github.com/jarjin/LuaFramework_UGUI_V2

## 相关工程
* ILRuntime C#实现的IL运行环境   
  https://github.com/Ourpalm/ILRuntime
* DCET 集成了CSharp.lua和**xlua**  
  https://github.com/DukeChiang/DCET
