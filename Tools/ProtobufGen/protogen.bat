set Protocol=..\..\Assets\CSharpLua\Compiled\Protocol\
set AutoGen=%Protocol%AutoGen\
set LuaProtocol=..\..\Assets\Lua\3rd\pbc\Protocol\

set cur=%cd%
cd %Protocol%

for %%i in (*.proto) do (
    %cur%\protobuf-net\protogen -i:%%i -o:AutoGen\%%~ni.cs
 )

for %%i in (*.proto) do (
    %cur%\protobuf-net\protoc %%i -o %cur%\%LuaProtocol%%%~ni.pb
 )

 cd %cur%
 echo protogen success
