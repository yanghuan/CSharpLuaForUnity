local System = System
local throw = System.throw
local emptyFn = System.emptyFn
local getClass = System.getClass
local is = System.is
local NotSupportedException = System.NotSupportedException
local SystemType = System.Type
local SystemListObject = System.List(System.Object)
local SystemTypeof = System.typeof
local SystemIEnumerator = System.IEnumerator
local arrayFromList = System.arrayFromList
local ArgumentNullException = System.ArgumentNullException
local ArgumentOutOfRangeException = System.ArgumentOutOfRangeException
local CSharpLua = CSharpLua

local assert = assert
local setmetatable = setmetatable
local getmetatable = getmetatable
local rawget = rawget
local rawset = rawset
local type = type
local tinsert = table.insert
local sformat = string.format
local sbyte = string.byte
local loadstring = loadstring
local ipairs = ipairs
local pairs = pairs
local typeof = typeof
local pcall = pcall

local Clamp01 = Mathf.Clamp01

local Debugger = Debugger
local BridgeMonoBehaviour = CSharpLua.BridgeMonoBehaviour
local UnityEngine = UnityEngine
local toluaSystem = toluaSystem
local isFromCSharp = UnityEngine.isFromCSharp

System.define("UnityEngine.Debug", {
  Log = Debugger.Log,
  LogErrorFormat = function (format, ...)
    Debugger.LogError(format:Format(...))
  end,
})

local UnityEngineMonoBehaviour = UnityEngine.MonoBehaviour
UnityEngine.MonoBehaviour = nil

local MonoBehaviour = System.define("UnityEngine.MonoBehaviour", {
  __ctor__ = emptyFn,
  class = "C",
  GetType = System.Object.GetType,
  print = UnityEngineMonoBehaviour.print,
  Awake = emptyFn,
  Start = emptyFn,
  Update = emptyFn,
  FixedUpdate = emptyFn,
  LateUpdate = emptyFn,
  ToString = function (this)
    return sformat("%s (%s)", this.ref.gameObject.name, this.__name__)
  end,
})
SystemTypeof(MonoBehaviour)[1] = UnityEngineMonoBehaviour

local function getMonoBehaviourFunction(metatableOfMonoBehaviour, name)
  return metatableOfMonoBehaviour[name]   
end

local metatableOfMonoBehaviour = getmetatable(BridgeMonoBehaviour) 
setmetatable(MonoBehaviour, {
  __index = function (t, k)
    if type(k) == "string" then
      local c = sbyte(k, 1)
      if c ~= 95 and c ~= 46 then -- not '.' or '_'
        local ok, f = pcall(getMonoBehaviourFunction, metatableOfMonoBehaviour, k)
        if ok then
          local v = function (this, ...)
            return f(this.ref, ...)
          end
          t[k] = v
          return v
        end
      end
    end
    return nil
  end
})

local updateIndexs = { Update = 0, FixedUpdate = 1, LateUpdate = 2 }

local function registerUpdate(this, bridgeMonoBehaviour, nameOfFn)
  local f = this[nameOfFn]
  if f ~= emptyFn and type(f) == "function" then
    bridgeMonoBehaviour:RegisterUpdate(updateIndexs[nameOfFn], f)
  end
end

local typeofBridgeMonoBehaviour = typeof(BridgeMonoBehaviour)
local isInstanceOfType = typeofBridgeMonoBehaviour.IsInstanceOfType 

local function isBridgeInstance(t)
  return isInstanceOfType(typeofBridgeMonoBehaviour, t)
end

local makeBridge
local function checkBridgeMonoBehaviour(t)
  if isBridgeInstance(t) then
    local bridge = t
    t = bridge.Table
    if t == nil then
      local luaClass = bridge.LuaClass
      local T = getClass(luaClass)
      assert(T, luaClass .. " is not found")
      t = setmetatable({}, T)
      T.__ctor__(t)
      makeBridge(t, bridge, true, bridge.SerializeData, bridge.SerializeObjects)
      bridge:Bind(t)
    end
  end
  return t
end

function makeBridge(this, bridgeMonoBehaviour, state, serializeData, serializeObjects)
  this.ref = assert(bridgeMonoBehaviour)
  if not state then
    bridgeMonoBehaviour:Bind(this, this.__name__)
  else
    if serializeData and #serializeData > 0 then
      local datas = loadstring(serializeData)()
      local normals = datas[1]
      if normals then
        for k, v in pairs(normals) do
          if type(v) == "table" then
            local n = #v
            local T = v[n]
            v[n] = nil
            setmetatable(v, T)
          end
          this[k] = v
        end
      end
      local objects = datas[2]
      if objects then
        for k, v in pairs(objects) do
          if type(v) == "table" then
            local n = #v
            local T = v[n]
            v[n] = nil
            for i = 1, n - 1 do
              v[i] = checkBridgeMonoBehaviour(serializeObjects[v[i]])
            end
            setmetatable(v, T)
          else
            v = checkBridgeMonoBehaviour(serializeObjects[v])
          end
          this[k] = v
        end
      end
    end
  end
  registerUpdate(this, bridgeMonoBehaviour, "Update")
  registerUpdate(this, bridgeMonoBehaviour, "FixedUpdate")
  registerUpdate(this, bridgeMonoBehaviour, "LateUpdate")
end

local function newMonoBehaviour(T, bridgeMonoBehaviour, state, serializeData, serializeObjects)
  local this = setmetatable({}, T)
  T.__ctor__(this)
  makeBridge(this, bridgeMonoBehaviour, state, serializeData, serializeObjects)
  this:Awake()
  return this
end

function System.IsIEnumerator(t)
  return is(t, SystemIEnumerator)
end

local function isSystemType(t)
  return getmetatable(t) == SystemType
end

local metatableOfGameObject = getmetatable(UnityEngine.GameObject)
local sourceAddComponent = rawget(metatableOfGameObject, "AddComponent")
local sourceGetComponent = rawget(metatableOfGameObject, "GetComponent")
local sourceGetComponentInChildren = rawget(metatableOfGameObject, "GetComponentInChildren")
local sourceGetComponentInParent = rawget(metatableOfGameObject, "GetComponentInParent")
local sourceGetComponents = rawget(metatableOfGameObject, "GetComponents")
local sourceGetComponentsInChildren = rawget(metatableOfGameObject, "GetComponentsInChildren")
local sourceGetComponentsInParent = rawget(metatableOfGameObject, "GetComponentsInParent")

local function addBridgeMonoBehaviour(gameObject, T)
  local monoBehaviour = sourceAddComponent(gameObject, typeofBridgeMonoBehaviour)
  return newMonoBehaviour(T, monoBehaviour)
end

local function addComponent(gameObject, T)
  if T == nil then throw(ArgumentNullException("type")) end
  if isSystemType(T) then
    T = T[1]
  end
  if isFromCSharp(T) then
    if type(T) ~= "userdata" then
      T = typeof(T)
    end
    return sourceAddComponent(gameObject, T)
  elseif type(T) == "string" then
    local cls = getClass(T)  
    if cls ~= nil and not isFromCSharp(cls) then
      return addBridgeMonoBehaviour(gameObject, cls)
    end
    local type_ = typeof(T)
    assert(type_, T .. " is not found")
    return sourceAddComponent(gameObject, type_)
  else
    return addBridgeMonoBehaviour(gameObject, T)
  end
end

rawset(metatableOfGameObject, "AddComponent", addComponent)

local function getBridgeComponent(sourceGetComponents, component, T, ...)
  local coms = sourceGetComponents(component, typeofBridgeMonoBehaviour, ...)
  for i = 0, coms.Length - 1 do
    local t = assert(coms[i].Table)
    if is(t, T) then
      return t
    end
  end
end

local function getComponent(component, T)
  if T == nil then throw(ArgumentNullException("type")) end
  if isSystemType(T) then
    T = T[1]
  end  
  if isFromCSharp(T) then
    if type(T) ~= "userdata" then
      T = typeof(T)
    end
    return sourceGetComponent(component, T) 
  elseif type(T) == "string" then
    local cls = getClass(T)
    if cls ~= nil and not isFromCSharp(cls) then
      return getBridgeComponent(sourceGetComponents, component, cls)
    end
    return sourceGetComponent(component, T)
  else
    return getBridgeComponent(sourceGetComponents, component, T)
  end
end

rawset(metatableOfGameObject, "GetComponent", getComponent)

local function getComponentInChildren(component, T, includeInactive)
  if T == nil then throw(ArgumentNullException("type")) end
  if isSystemType(T) then
    if includeInactive == nil then
      includeInactive = false
    end
    T = T[1]
  else
    if type(T) == "boolean" then
      T, includeInactive = includeInactive, T
    else
      includeInactive = false
    end
  end
  if isFromCSharp(T) then
    if type(T) ~= "userdata" then
      T = typeof(T)
    end
    return checkBridgeMonoBehaviour(sourceGetComponentInChildren(component, T, includeInactive))
  end
    return getBridgeComponent(sourceGetComponentsInChildren, component, T, includeInactive)
end

rawset(metatableOfGameObject, "GetComponentInChildren", getComponentInChildren)

local function getComponentInParent(component, T)
  if T == nil then throw(ArgumentNullException("type")) end
  if isSystemType(T) then
    T = T[1] 
  end
  if isFromCSharp(T) then
    if type(T) ~= "userdata" then
      T = typeof(T)
    end
    return checkBridgeMonoBehaviour(sourceGetComponentInParent(component, T))
  else
    return getBridgeComponent(sourceGetComponentsInParent, component, T, false)
  end  
end

rawset(metatableOfGameObject, "GetComponentInParent", getComponentInParent)

local function getBridgeComponents(sourceGetComponents, component, T, results, ...)
  local hasReturn = results == nil
  if hasReturn then
    results = SystemListObject()
  end
  if isFromCSharp(T) then
    if type(T) ~= "userdata" then
      T = typeof(T)
    end
    local coms = sourceGetComponents(component, T, ...)
    for i = 0, coms.Length - 1 do
      local t = coms[i]
      results:Add(t)
    end
  else
    local coms = sourceGetComponents(component, typeofBridgeMonoBehaviour, ...)
    for i = 0, coms.Length - 1 do
      local t = coms[i].Table
      if is(t, T) then
        results:Add(t)
      end
    end
  end
  if hasReturn then
    return arrayFromList(results)
  end
end

local function getComponents(component, T, results)
  if T == nil then throw(ArgumentNullException("type")) end
  if isSystemType(T) then
    T = T[1]
  elseif results then
    T, results = results, T
  end
  return getBridgeComponents(sourceGetComponents, component, T, results)
end

rawset(metatableOfGameObject, "GetComponents", getComponents)

local function getComponentsInChildren(component, T, includeInactive, results)
  if T == nil then throw(ArgumentNullException("type")) end
  if isSystemType(T) then
    T = T[1]
    if includeInactive == nil then
      includeInactive = false
    end
  elseif isFromCSharp(T) then
    if includeInactive == nil then
      includeInactive = false
    end
  else
    if type(T) == "boolean" then
      if not results then
        T, includeInactive = includeInactive, T
      else
        T, includeInactive, results = results, T, includeInactive    
      end
    else
      if not includeInactive then
        includeInactive = false
      else
        T, includeInactive, results = includeInactive, false, T
      end
    end
  end
  return getBridgeComponents(sourceGetComponentsInChildren, component, T, results, includeInactive)
end

rawset(metatableOfGameObject, "GetComponentsInChildren", getComponentsInChildren)

local function getComponentsInParent(component, T, includeInactive, results)
  if T == nil then throw(ArgumentNullException("type")) end
  if isSystemType(T) then
    T = T[1]
    if includeInactive == nil then
      includeInactive = false
    end
  elseif isFromCSharp(T) then
    if includeInactive == nil then
      includeInactive = false
    end
  else
    if type(T) == "boolean" then
      if not results then
        T, includeInactive = includeInactive, T
      else
        T, includeInactive, results = results, T, includeInactive    
      end
    else
      includeInactive = false
    end
  end
  return getBridgeComponents(sourceGetComponentsInParent, component, T, results, includeInactive)
end

rawset(metatableOfGameObject, "GetComponentsInParent", getComponentsInParent)


local metatableOfComponent = getmetatable(UnityEngine.Component)

rawset(metatableOfComponent, "GetComponent", function (this, ...)
  return this.gameObject:GetComponent(...)
end)

rawset(metatableOfComponent, "GetComponentInChildren", function (this, ...)
  return this.gameObject:GetComponentInChildren(...)
end)

rawset(metatableOfComponent, "GetComponentInParent", function (this, ...)
  return this.gameObject:GetComponentInParent(...)
end)

rawset(metatableOfComponent, "GetComponents", function (this, ...)
  return this.gameObject:GetComponents(...)
end)

rawset(metatableOfComponent, "GetComponentsInChildren", function (this, ...)
  return this.gameObject:GetComponentsInChildren(...)
end)

rawset(metatableOfComponent, "GetComponentsInParent", function (this, ...)
  return this.gameObject:GetComponentsInParent(...)
end)

local metatableOfObject = getmetatable(UnityEngine.Object)
local sourceDestroy = rawget(metatableOfObject, "Destroy")
local sourceDestroyImmediate = rawget(metatableOfObject, "DestroyImmediate")
local sourceDontDestroyOnLoad = rawget(metatableOfObject, "DontDestroyOnLoad")
local sourceFindObjectOfType = rawget(metatableOfObject, "FindObjectOfType")
local sourceFindObjectsOfType = rawget(metatableOfObject, "FindObjectsOfType")
local sourceFindObjectsOfTypeAll = rawget(metatableOfObject, "FindObjectsOfTypeAll")
local sourceFindObjectsOfTypeIncludingAssets = rawget(metatableOfObject, "FindObjectsOfTypeIncludingAssets")
local sourceFindSceneObjectsOfType = rawget(metatableOfObject, "FindSceneObjectsOfType")
local source__eq = rawget(metatableOfObject, "__eq")

local function destroy(obj, t)
  if obj and not isFromCSharp(obj) then
    obj = assert(obj.ref)
  end
  if t then
    sourceDestroy(obj, t)
  else
    sourceDestroy(obj)  
  end  
end

local function destroyImmediate(obj, t)
  if obj and not isFromCSharp(obj) then
    obj = assert(obj.ref)
  end
  if t then
    sourceDestroyImmediate(obj, t)
  else
    sourceDestroyImmediate(obj)  
  end  
end

local function dontDestroyOnLoad(obj)
  if obj and not isFromCSharp(obj) then
    obj = assert(obj.ref)
  end
  sourceDontDestroyOnLoad(obj)
end

local function findObjectOfType(T)
  if T == nil then throw(ArgumentNullException("type")) end
  if isSystemType(T) then
    T = T[1]
  end
  if isFromCSharp(T) then
    if type(T) ~= "userdata" then
      T = typeof(T)
    end
    return checkBridgeMonoBehaviour(sourceFindObjectOfType(T))
  else
    local objs = sourceFindObjectsOfType(typeofBridgeMonoBehaviour)
    for i = 0, objs.Length - 1 do
      local t = objs[i].Table
      if is(t, T) then
        return t
      end
    end
  end
end

local function findBridgeObjectsOfType(sourceFindObjectsOfType, T)
  local results = SystemListObject()
  if isFromCSharp(T) then
    if type(T) ~= "userdata" then
      T = typeof(T)
    end
    local objs = sourceFindObjectsOfType(T)
    for i = 0, objs.Length - 1 do
      local t = objs[i]
      results:Add(checkBridgeMonoBehaviour(t))
    end
  else
    local objs = sourceFindObjectsOfType(typeofBridgeMonoBehaviour)
    for i = 0, objs.Length - 1 do
      local t = objs[i].Table
      if is(t, T) then
        results:Add(t)
      end
    end
  end
  return arrayFromList(results)
end

local function findObjectsOfType(T)
  if T == nil then throw(ArgumentNullException("type")) end
  if isSystemType(T) then
    T = T[1]
  end
  return findBridgeObjectsOfType(sourceFindObjectsOfType, T)
end

local function findObjectsOfTypeAll(t)
  if t == nil then throw(ArgumentNullException("type")) end
  return findBridgeObjectsOfType(sourceFindObjectsOfTypeAll, t[1])
end

local function findObjectsOfTypeIncludingAssets(t)
  if t == nil then throw(ArgumentNullException("type")) end
  return findBridgeObjectsOfType(sourceFindObjectsOfTypeIncludingAssets, t[1])
end

local function findSceneObjectsOfType(t)
  if t == nil then throw(ArgumentNullException("type")) end
  return findBridgeObjectsOfType(sourceFindSceneObjectsOfType, t[1])
end

local function op_Equality(x, y)
  if x == nil and y == nil then
    return true
  end
  if x and not isFromCSharp(x) then
    x = assert(x.ref)
  end
  if y and not isFromCSharp(y) then
    y = assert(y.ref)
  end
  return source__eq(x, y)
end

local function op_Inequality(x, y)
  return not op_Equality(x, y)
end

local function op_Implicit(x)
  return not op_Equality(x, nil)
end

local function equalsObj(this, other)
  if other ~= nil then
    if not isFromCSharp(other) then
      other = other.ref
      if other == nil or not isFromCSharp(other) then
        return false
      end
    end
  end
  this:Equals(other)
end

rawset(metatableOfObject, "Destroy", destroy)
rawset(metatableOfObject, "DestroyImmediate", destroyImmediate)
rawset(metatableOfObject, "DontDestroyOnLoad", dontDestroyOnLoad)
rawset(metatableOfObject, "FindObjectOfType", findObjectOfType)
rawset(metatableOfObject, "FindObjectsOfType", findObjectsOfType)
rawset(metatableOfObject, "FindObjectsOfTypeAll", findObjectsOfTypeAll)
rawset(metatableOfObject, "FindObjectsOfTypeIncludingAssets", findObjectsOfTypeIncludingAssets)
rawset(metatableOfObject, "FindSceneObjectsOfType", findSceneObjectsOfType)
rawset(metatableOfObject, "EqualsObj", equalsObj)

local metatableOfSystemObject = getmetatable(toluaSystem.Object)
local equals = rawget(metatableOfSystemObject, "Equals")

local function getType(this)
  local name = assert(this.__name__)
  local cls = getClass(name)
  return SystemTypeof(cls)
end

rawset(metatableOfSystemObject, "class", "C")
rawset(metatableOfSystemObject, "default", System.emptyFn)
rawset(metatableOfSystemObject, "EqualsObj", equals)
rawset(metatableOfSystemObject, "GetType", getType)

function UnityEngine.addComponent(gameObject, componentString)
  local pos = componentString:find(",")
  if pos then
    local name = componentString:sub(1, pos - 1)
    local cls = getClass(name)  
    if cls ~= nil and not isFromCSharp(cls) then
      return addBridgeMonoBehaviour(gameObject, cls)
    end
  end
  local type_ = typeof(componentString)
  assert(type_, componentString .. " is not found")
  return sourceAddComponent(gameObject, type_)
end

UnityEngine.op_Equality = op_Equality
UnityEngine.op_Inequality = op_Inequality
UnityEngine.op_Implicit = op_Implicit

function UnityEngine.bind(monoBehaviour, luaClass, serializeData, serializeObjects)
  local T = getClass(luaClass)
  assert(T, luaClass .. " is not found")
  return newMonoBehaviour(T, monoBehaviour, true, serializeData, serializeObjects)
end

local cjson = require("cjson")
local cjsonDecode = cjson.decode

local function fromJsonTable(t, T)
  for k, v in pairs(t) do
    if v == cjson.null then
      t[k] = nil
    elseif type(v) == "table" then
      if #v > 0 then  -- is list
        fromJsonTable(v, System.List(System.Object))
      else
        fromJsonTable(v, System.Object)
      end
    end
  end
  setmetatable(t, T)
end

System.define("UnityEngine.JsonUtility", {
  ToJson = cjson.encode,
  FromJson = function (json, T)
    if T == nil then throw(ArgumentNullException("type")) end
    if isSystemType(T) then
      T = T[1]
    end
    local t = cjsonDecode(json)
    fromJsonTable(t, T)
    return t
  end
})

local function defineUnityStruct(name, T)
  local __call = T.__call
  local __index = T.__index
  setmetatable(T, nil)
  System.defStc(name, T)
   
  local super = getmetatable(T)
  setmetatable(T, { 
    __call = __call,
    __index = function (t, k)
      local v = __index(t, k)
      if v == nil then
        v = super[k]
      end
      return v
    end
  })
end

local function inherits(_, T)
  return { System.IEquatable_1(T) } 
end

local Vector2 = UnityEngine.Vector2
local newVector2 = Vector2.New
UnityEngine.Vector2 = nil

function Vector2.get(this, index)
  if index < 0 or index > 1 then
    throw(ArgumentOutOfRangeException("Invalid Vector2 index!"))
  end
  return index == 0 and this.x or this.y
end

function Vector2.set(this, index, value)
  if index < 0 or index > 1 then
    throw(ArgumentOutOfRangeException("Invalid Vector2 index!"))
  end
  if index == 0 then
    this.x = value
  else
    this.y = value
  end
end

local PositiveInfinity = System.Double.PositiveInfinity
local NegativeInfinity = System.Double.NegativeInfinity

Vector2.getdown = function() return newVector2(0, -1) end
Vector2.getleft = function() return newVector2(-1, 0) end
Vector2.getup = function() return newVector2(0, 1) end
Vector2.getright = function() return newVector2(1, 0) end
Vector2.getzero = function() return newVector2(0, 0) end
Vector2.getone = function() return newVector2(1, 1) end
Vector2.getpositiveInfinityVector = function() return newVector2(PositiveInfinity, PositiveInfinity) end
Vector2.getnegativeInfinityVector = function() return newVector2(NegativeInfinity, NegativeInfinity) end

Vector2.getmagnitude = Vector2.Magnitude
Vector2.getsqrMagnitude = Vector2.SqrMagnitude
Vector2.getnormalized = Vector2.Normalize

local function equalsOfVector2(this, other)
  return this.x:Equals(other.x) and this.y:Equals(other.y)
end

function Vector2.EqualsObj(this, other)
  if getmetatable(other) ~= Vector2 then
    return false
  end
  return equalsOfVector2(this, other)
end

Vector2.Equals = equalsOfVector2
Vector2.ToString = Vector2.__tostring
Vector2.__clone__ = Vector2.Clone
Vector2.__inherits__ = inherits

defineUnityStruct("UnityEngine.Vector2", Vector2)

local Vector3 = UnityEngine.Vector3
local newVector3 = Vector3.New
UnityEngine.Vector3 = nil

function Vector3.get(this, index)
  if index < 0 or index > 2 then
    throw(ArgumentOutOfRangeException("Invalid Vector3 index!"))
  end
  if index == 0 then
    return this.x
  elseif index == 1 then
    return this.y
  else
    return this.z
  end
end

function Vector3.set(this, index, value) 
  if index < 0 or index > 2 then
    throw(ArgumentOutOfRangeException("Invalid Vector3 index!"))
  end
  if index == 0 then
    this.x = value
  elseif index == 1 then
    this.y = value
  else
    this.z = value
  end
end

Vector3.getup = function() return newVector3(0, 1, 0) end
Vector3.getdown = function() return newVector3(0, -1, 0) end
Vector3.getright = function() return newVector3(1, 0, 0) end
Vector3.getleft = function() return newVector3(-1, 0, 0) end
Vector3.getforward = function() return newVector3(0, 0, 1) end
Vector3.getback = function() return newVector3(0, 0, -1) end
Vector3.getzero = function() return newVector3(0, 0, 0) end
Vector3.getone = function() return newVector3(1, 1, 1) end

Vector3.getmagnitude = Vector3.Magnitude
Vector3.getsqrMagnitude = Vector3.SqrMagnitude
Vector3.getnormalized = Vector3.Normalize

function Vector3.LerpUnclamped(a, b, t)
  return newVector3(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t, a.z + (b.z - a.z) * t)
end

local function equalsOfVector3(this, other)
  return this.x:Equals(other.x) and this.y:Equals(other.y) and this.z:Equals(other.z)
end

function Vector3.EqualsObj(this, other)
  if getmetatable(other) ~= Vector3 then
    return false
  end
  return equalsOfVector3(this, other)
end

Vector3.Equals = equalsOfVector3
Vector3.ToString = Vector3.__tostring
Vector3.__clone__ = Vector3.Clone
Vector3.__inherits__ = inherits

defineUnityStruct("UnityEngine.Vector3", Vector3)

local Vector4 = UnityEngine.Vector4
local newVector4 = Vector4.New
UnityEngine.Vector4 = nil

local function getOfVector4(this, index, error)
  if index < 0 or index > 2 then
    throw(ArgumentOutOfRangeException(error), 1)
  end
  if index == 0 then
    return this.x
  elseif index == 1 then
    return this.y
  elseif index == 2 then
    return this.z
  else 
    return this.w
  end
end

function Vector4.get(this, index)
  return getOfVector4(this, index, "Invalid Vector4 index!")
end

local function setOfVector4(this, index, value, error)
  if index < 0 or index > 2 then
    throw(ArgumentOutOfRangeException(error), 1)
  end
  if index == 0 then
    this.x = value
  elseif index == 1 then
    this.y = value
  elseif index == 2 then
    this.z = value
  else 
    this.w = value
  end
end

function Vector4.set(this, index, value) 
  setOfVector4(this, inde, value, "Invalid Vector4 index!")
end

Vector4.getzero = function() return newVector4(0, 0, 0, 0) end
Vector4.getone	 = function() return newVector4(1, 1, 1, 1) end
Vector4.getsqrMagnitude = Vector4.SqrMagnitude
Vector4.getmagnitude = Vector4.Magnitude
Vector4.getnormalized = Vector4.Normalize

local function equalsOfVector4(this, other)
  return this.x:Equals(other.x) and this.y:Equals(other.y) and this.z:Equals(other.z) and this.w:Equals(other.w)
end

function Vector4.EqualsObj(this, other)
  if getmetatable(other) ~= Vector4 then
    return false
  end
  return equalsOfVector4(this, other)
end

Vector4.Equals = equalsOfVector4
Vector4.ToString = Vector4.__tostring
Vector4.__clone__ = Vector4.Clone
Vector4.__inherits__ = inherits

defineUnityStruct("UnityEngine.Vector4", Vector4)

function UnityEngine.ToVector2(v)
  return newVector2(v.x, v.y)
end

function UnityEngine.ToVector3(v)
  return newVector3(v.x, v.y, v.z)
end

function UnityEngine.ToVector4(v)
  return newVector4(v.x, v.y, v.z, v.w)
end

local Color = UnityEngine.Color
local newColor = Color.New
UnityEngine.Color = nil

function Color.get(this, index)
  if index < 0 or index > 2 then
    throw(ArgumentOutOfRangeException("Invalid Color index!"))
  end
  if index == 0 then
    return this.r
  elseif index == 1 then
    return this.g
  elseif index == 2 then
    return this.b
  else 
    return this.a
  end
end

function Color.set(this, index, value) 
  if index < 0 or index > 2 then
    throw(ArgumentOutOfRangeException("Invalid Color index!"))
  end
  if index == 0 then
    this.r = value
  elseif index == 1 then
    this.g = value
  elseif index == 2 then
    this.b = value
  else 
    this.a = value
  end
end

local Mathf = Mathf
local LinearToGammaSpace = Mathf.LinearToGammaSpace
local GammaToLinearSpace = Mathf.GammaToLinearSpace
local Max = Mathf.Max

Color.getred 	= function() return newColor(1, 0, 0, 1) end
Color.getgreen	= function() return newColor(0, 1, 0, 1) end
Color.getblue	= function() return newColor(0, 0, 1, 1) end
Color.getwhite	= function() return newColor(1, 1, 1, 1) end
Color.getblack	= function() return newColor(0, 0, 0, 1) end
Color.getyellow	= function() return newColor(1, 0.9215686, 0.01568628, 1) end
Color.getcyan	= function() return newColor(0, 1, 1, 1) end
Color.getmagenta	= function() return newColor(1, 0, 1, 1) end
Color.getgray	= function() return newColor(0.5, 0.5, 0.5, 1) end
Color.getclear	= function() return newColor(0, 0, 0, 0) end
Color.getgrey = Color.gray
Color.getgrayscale = Color.GrayScale
Color.getgamma = function(c) return newColor(LinearToGammaSpace(c.r), LinearToGammaSpace(c.g), LinearToGammaSpace(c.b), c.a) end
Color.getlinear = function(c) return newColor(GammaToLinearSpace(c.r), GammaToLinearSpace(c.g), GammaToLinearSpace(c.b), c.a) end
Color.getmaxColorComponent = function(c) return Max(Max(c.r, c.g), c.b) end

local function equalsOfColor(this, other)
  return this.r:Equals(other.r) and this.g:Equals(other.g) and this.b:Equals(other.b) and this.a:Equals(other.a)
end

function Color.EqualsObj(this, other)
  if getmetatable(other) ~= Color then
    return false
  end
  return equalsOfColor(this, other)
end

Color.Equals = equalsOfColor
Color.ToString = Color.__tostring
Color.__clone__ = function(this) return newColor(this.r, this.g, this.b, this.a) end
Color.__inherits__ = inherits

defineUnityStruct("UnityEngine.Color", Color)

function UnityEngine.ToColorFromVector4(v)
  return newColor(v.x ,v.y, v.z, v.w)
end

function UnityEngine.ToVector4FromColor(v)
  return newVector4(v.r, v.g, v.b, b.a)
end

local Color32 = UnityEngine.Color32
local newColor32 = Color32.New
local equalsOfColor32 = Color32.Equals
UnityEngine.Color32 = nil

Color32.ToString = Color32.__tostring
Color32.__clone__ = function (this) return newColor32(this.r, this.g, this.b, this.a) end
Color32.__inherits__ = System.emptyFn

function Color32.EqualsObj(this, other)
  if getmetatable(other) ~= Color32 then
    return false
  end
  return equalsOfColor32(this, other)
end

defineUnityStruct("UnityEngine.Color32", Color32)

function UnityEngine.ToColor32FromColor(c)
  return newColor32(Clamp01(c.r) * 255, Clamp01(c.g) * 255, Clamp01(c.b) * 255, Clamp01(c.a) * 255)
end

function UnityEngine.ToColorFromColor32(c)
  return newColor(c.r / 255, c.g / 255, c.b / 255, c.a / 255) 
end

local Quaternion = UnityEngine.Quaternion
local newQuaternion = Quaternion.New
UnityEngine.Quaternion = nil

function Quaternion.get(this, index)
  return getOfVector4(this, index, "Invalid Quaternion index!") 
end

function Quaternion.set(this, index, value)
  setOfVector4(this, inde, value, "Invalid Quaternion index!")
end

Quaternion.getidentity = function() return newQuaternion(0, 0, 0, 1) end
Quaternion.geteulerAngles = Quaternion.ToEulerAngles
Quaternion.seteulerAngles = Quaternion.SetEuler
Quaternion.getnormalized = Quaternion.Normalize

function Quaternion.EqualsObj(this, other)
  if getmetatable(other) ~= Quaternion then
    return false
  end
  return equalsOfVector4(this, other)
end

Quaternion.Equals = equalsOfVector4
Quaternion.ToString = Quaternion.__tostring
Quaternion.__clone__ = Quaternion.Clone
Quaternion.__inherits__ = inherits

defineUnityStruct("UnityEngine.Quaternion", Quaternion)

local Bounds = UnityEngine.Bounds
local newBounds = Bounds.New
UnityEngine.Bounds = nil

Bounds.getsize = Bounds.GetSize
Bounds.getmin = Bounds.GetMin
Bounds.getmax = Bounds.GetMax

local function equalsOfBounds(this, other)
  return this.center:Equals(other.center) and this.extents:Equals(other.extents)
end

function Bounds.EqualsObj(this, other)
  if getmetatable(other) ~= Bounds then
    return false
  end
  return equalsOfBounds(this, other)
end

Bounds.Equals = equalsOfBounds
Bounds.ToString = Quaternion.__tostring
Bounds.__clone__ = function (this) return newBounds(this.center, this.extents) end
Bounds.__inherits__ = inherits

defineUnityStruct("UnityEngine.Bounds", Bounds)

local Plane = UnityEngine.Plane
local newPlane = Plane.New
UnityEngine.Plane = nil

Plane.ToString = function (this) 
  local normal = this.normal
  return sformat("(normal:(%.1f, %.1f, %.1f), distance:%1.f)", normal.x, normal.y, normal.z, this.distance)  
end

Plane.__clone__ = function (this) return newPlane(this.normal, this.distance) end

local Set3Points = Plane.Set3Points
local SetNormalAndPosition = Plane.SetNormalAndPosition

Plane.__call = function (cls, a, b, c)
  if c ~= nil then
    local this = setmetatable({ normal = false, distance = false }, Plane)
    Set3Points(this, a, b, c)
    return this
  elseif type(b) == "number" then
    return newPlane(a, b)  
  else
    local this = setmetatable({ normal = false, distance = false }, Plane)
    SetNormalAndPosition(this, a, b)
    return this
  end
end

Plane.__inherits__ = System.emptyFn
defineUnityStruct("UnityEngine.Plane", Plane)

local LayerMask = UnityEngine.LayerMask
local newLayerMask = LayerMask.New
UnityEngine.LayerMask = nil

LayerMask.__clone__ = function (this) return newLayerMask(this.value) end
UnityEngine.ToLayerMask = newLayerMask

LayerMask.__inherits__ = System.emptyFn
defineUnityStruct("UnityEngine.LayerMask", LayerMask)
