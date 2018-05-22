local System = System
local getClass = System.getClass
local Debugger = Debugger

local assert = assert
local setmetatable = setmetatable
local global = _G

local Debug = {
  Log = Debugger.Log
}

System.define("UnityEngine.Debug", Debug)

local MonoBehaviour = {
  __kind__ = "C",
  getname = function (this)
    return this.ref:getname()
  end,
  setname = function (this, name)
    this.ref:setname(name)
  end,
  getgameObject = function (this)
    return this.ref:getgameObject()
  end,
  Awake = function ()
  end,
  Start = function ()
  end,
}

System.define("MonoBehaviour", MonoBehaviour)

function bindMonoBehaviour(monoBehaviour, typeName)
  local cls = getClass(typeName)  
  assert(cls, typeName .. " not found") 
  local t = setmetatable({ ref = monoBehaviour }, cls)
  t:Awake()
end