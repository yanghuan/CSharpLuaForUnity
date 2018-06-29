toluaSystem = System
require("CoreSystemLua.All")("CoreSystemLua", { time = tolua.gettime })
require("UnityAdapter")

local UnityEngineMonoBehaviour = UnityEngine.MonoBehaviour
UnityEngine.MonoBehaviour = MonoBehaviour
require("CompiledScripts.manifest")("CompiledScripts")
UnityEngine.MonoBehaviour = UnityEngineMonoBehaviour