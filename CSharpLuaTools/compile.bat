set compiledScriptDir=../Assets/CSharpLua/CompiledScript/
set outdir=../Assets/Lua/CompiledScript/

dotnet CSharp.lua/CSharp.lua.Launcher.dll -s %compiledScriptDir% -d %outdir%