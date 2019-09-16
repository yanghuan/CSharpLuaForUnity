local typeof = typeof
local UnityEngineMonoBehaviour = UnityEngine.MonoBehaviour
local isInstanceOfType = typeof(UnityEngineMonoBehaviour).IsInstanceOfType
local Timer = Timer.New  -- tolua.Timer
toluaSystem = System

local function isUserdataType(obj, cls)
  if cls.__gc ~= nil then
    return isInstanceOfType(typeof(cls), obj)
  end
  return true
end

local config = {
  time = tolua.gettime, 
  setTimeout = function (f, milliseconds)
    local t = Timer(f, milliseconds / 1000, 1, true)
    t:Start()
    return t
  end,
  clearTimeout = function (t)
    t:Stop()
  end,
  customTypeCheck = function (T)
    if T.__gc ~= nil then
      return isUserdataType
    end
  end
}

-- luajit table.move may causes a crash
if jit then
  table.move = function(a1, f, e, t, a2)
    if a2 == nil then a2 = a1 end
    t = e - f + t
    while e >= f do
      a2[t] = a1[e]
      t = t - 1
      e = e - 1
    end
  end
end

require("CoreSystemLua.All")("CoreSystemLua", config)
require("UnityAdapter")
UnityEngine.MonoBehaviour = MonoBehaviour
require("Compiled.manifest")("Compiled")
UnityEngine.MonoBehaviour = UnityEngineMonoBehaviour