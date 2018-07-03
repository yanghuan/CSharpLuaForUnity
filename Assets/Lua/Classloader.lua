local typeof = typeof

local function isUserdataTypeOf(obj, cls)
  return typeof(cls):IsInstanceOfType(obj)
end

toluaSystem = System
require("CoreSystemLua.All")("CoreSystemLua", { time = tolua.gettime, isUserdataTypeOf = isUserdataTypeOf })
require("UnityAdapter")

local UnityEngineMonoBehaviour = UnityEngine.MonoBehaviour
UnityEngine.MonoBehaviour = MonoBehaviour
require("CompiledScripts.manifest")("CompiledScripts")
UnityEngine.MonoBehaviour = UnityEngineMonoBehaviour