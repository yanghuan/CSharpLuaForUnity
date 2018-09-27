local typeof = typeof
local UnityEngineMonoBehaviour = UnityEngine.MonoBehaviour
local isInstanceOfType = typeof(UnityEngineMonoBehaviour).IsInstanceOfType
local Timer = Timer.New  -- tolua.Timer
toluaSystem = System

local config = {
  time = tolua.gettime, 
  isUserdataTypeOf = function (obj, cls)
    return isInstanceOfType(typeof(cls), obj)
  end,
  setTimeout = function (f, milliseconds)
    local t = Timer(f, milliseconds / 1000, 1, true)
    t:Start()
    return t
  end,
  clearTimeout = function (t)
    t:Stop()
  end
}

require("CoreSystemLua.All")("CoreSystemLua", config)
require("UnityAdapter")
UnityEngine.MonoBehaviour = MonoBehaviour
require("CompiledScripts.manifest")("CompiledScripts")
UnityEngine.MonoBehaviour = UnityEngineMonoBehaviour