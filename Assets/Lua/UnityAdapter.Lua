local System = System
local throw = System.throw
local getClass = System.getClass
local NotSupportedException = System.NotSupportedException

local assert = assert
local setmetatable = setmetatable
local type = type
local typeof = typeof

local Debugger = Debugger
local BridgeMonoBehaviour = BridgeMonoBehaviour
local UnityEngine = UnityEngine

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
  Awake = function ()
  end,
  Start = function ()
  end,
  
  -- MonoBehaviour methods
  getuseGUILayout = function (this)
    return this.ref.useGUILayout
  end,
  setuseGUILayout = function (this, value)
    this.ref.useGUILayout = value
  end,
  print = UnityEngine.MonoBehaviour.print,
  CancelInvoke = function (this, methodName)  
    if methodName == nil then
      this.ref:CancelInvoke()
    else
      throw(NotSupportedException())
    end
  end,
  Invoke = function (this, methodName, time)
    throw(NotSupportedException())
  end,
  InvokeRepeating = function (this, methodName, time, repeatRate)
    throw(NotSupportedException())
  end,
  IsInvoking = function (this, methodName)
    if methodName == nil then
      this.ref:IsInvoking()
    else
      throw(NotSupportedException())
    end
  end,
  StartCoroutine = function (this, routine)
    return this.ref:StartCoroutine(routine)
  end,
  StopAllCoroutines = function (this)
    this.ref:StopAllCoroutines()
  end,
  StopCoroutine = function (this, routine)
    this.ref:StopCoroutine(routine)
  end,
  
  getname = function (this)
    return this.ref.name
  end,
  setname = function (this, value)
    this.ref.name = value
  end,
  getgameObject = function (this)
    return this.ref.gameObject
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