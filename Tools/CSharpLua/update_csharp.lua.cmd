set csharplua=D:\Project\Person\CSharp.lua
set launcher=%csharplua%\CSharp.lua.Launcher\bin\Debug\netcoreapp2.0
set coresystem=%csharplua%\CSharp.lua\CoreSystem.Lua
set localcoresystem=..\..\Assets\Lua\CoreSystemLua

xcopy %launcher% CSharp.lua /y 
del /s /q %localcoresystem%\*.lua
xcopy %coresystem% %localcoresystem% /s /y


