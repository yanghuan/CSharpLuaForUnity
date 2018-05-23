local System = System
local getClass = System.getClass
local Debugger = Debugger
local BridgeMonoBehaviour = BridgeMonoBehaviour

local assert = assert
local setmetatable = setmetatable
local type = type
local typeof = typeof

local Debug = {
  Log = Debugger.Log
}

System.define("UnityEngine.Debug", Debug)

local MonoBehaviour = {
  __kind__ = "C",
  __ctor__ = function (this, monoBehaviour)
    this.ref = monoBehaviour
    monoBehaviour:Bind(this)
  end,
  getname = function (this)
    return this.ref.name
  end,
  setname = function (this, name)
    this.ref.name = name
  end,
  getgameObject = function (this)
    return this.ref.gameObject
  end,
  Awake = function ()
  end,
  Start = function ()
  end,
}

System.define("MonoBehaviour", MonoBehaviour)

local function addBridgeMonoBehaviour(gameObject, T)
  local monoBehaviour = gameObject:AddComponent(typeof(BridgeMonoBehaviour))
  local t = T(monoBehaviour)
  t:Awake()
  return t
end

function UnityEngine.AddComponent(gameObject, T)
  if type(T) == "userdata" then
    return gameObject:AddComponent(typeof(T))
  else
    return addBridgeMonoBehaviour(gameObject, T)
  end
end

function UnityEngine.AddComponentOfType(gameObject, componentType)
  local t = type(componentType)
  if t == "userdata" then
    return gameObject:AddComponent(componentType)
  elseif t == "table" then
    return addBridgeMonoBehaviour(gameObject, componentType.c)
  else
    assert(t == "string")
    local cls = getClass(componentType)  
    if cls ~= nil then
      return addBridgeMonoBehaviour(gameObject, cls)
    else
      return gameObject:AddComponent(typeof(componentType))
    end
  end
end