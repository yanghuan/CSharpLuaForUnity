local setmetatable = setmetatable
local adapterMonoBehaviour = {
  __kind__ = "C"
}
setmetatable(adapterMonoBehaviour, UnityEngine.MonoBehaviour)
AdapterMonoBehaviour = adapterMonoBehaviour

