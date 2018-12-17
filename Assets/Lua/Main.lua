require("Classloader")

--主入口函数。从这里开始lua逻辑
function Main()					
	print("logic start")	 		
	local methodInfo = System.Reflection.Assembly.GetEntryAssembly().getEntryPoint()
	assert(methodInfo, "not found Main")
	methodInfo:Invoke(nil, System.Array(System.Object)(System.Array.Empty(System.String)()))
end

--场景切换通知
function OnLevelWasLoaded(level)
	collectgarbage("collect")
	Time.timeSinceLevelLoad = 0
end

function OnApplicationQuit()
end
