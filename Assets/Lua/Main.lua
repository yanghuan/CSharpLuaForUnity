local socket = require("socket")

require("functions")
require("CoreSystemLua.All")("CoreSystemLua", { time = socket.gettime })
require("UnityAdapter")
require("CompiledScript.manifest")("CompiledScript")

--主入口函数。从这里开始lua逻辑
function Main()					
	print("logic start")	 		
end

--场景切换通知
function OnLevelWasLoaded(level)
	collectgarbage("collect")
	Time.timeSinceLevelLoad = 0
end

function OnApplicationQuit()
end
