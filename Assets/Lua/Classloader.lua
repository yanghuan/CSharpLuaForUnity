local type = type
local tostring = tostring

local originalError = error
function error(str)
  if type(str) == "table" then
    str = tostring(str)
  else
    str = debug.traceback(str, 2)
  end
  originalError(str)
end

toluaSystem = System
require("CoreSystemLua.All")("CoreSystemLua", { time = tolua.gettime })
require("UnityAdapter")

local UnityEngineMonoBehaviour = UnityEngine.MonoBehaviour
UnityEngine.MonoBehaviour = MonoBehaviour
require("CompiledScripts.manifest")("CompiledScripts")
UnityEngine.MonoBehaviour = UnityEngineMonoBehaviour