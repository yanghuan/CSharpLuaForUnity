local System = System
local throw = System.throw
local getClass = System.getClass
local is = System.is
local NotSupportedException = System.NotSupportedException
local SystemType = System.Type
local SystemList = System.List
local SystemTypeof = System.typeof
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
local table = table
local tinsert = table.insert
local loadstring = loadstring
local pairs = pairs
local typeof = typeof

local Debugger = Debugger
local BridgeMonoBehaviour = CSharpLua.BridgeMonoBehaviour
local UnityEngine = UnityEngine
local toluaSystem = toluaSystem

local Debug = {
  Log = Debugger.Log
}

System.define("UnityEngine.Debug", Debug)

local MonoBehaviour = {
  __ctor__ = false,
  Awake = function (this)
  end,
  Start = function (this)
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
    if type(routine) == "string" then
      throw(NotSupportedException())
    end  
    return this.ref:StartCoroutine(routine)
  end,
  StopAllCoroutines = function (this)
    this.ref:StopAllCoroutines()
  end,
  StopCoroutine = function (this, routine)
    if type(routine) ~= "userdata" then
      throw(NotSupportedException())
    end  
    this.ref:StopCoroutine(routine)
  end,
  
  -- Behaviour methods
  getenabled = function (this)
    return this.ref.enabled
  end,
  setenabled = function (this, value)
    this.ref.enabled = value
  end,
  getisActiveAndEnabled = function (this)
    return this.ref.isActiveAndEnabled
  end,
  setisActiveAndEnabled = function (this, value)
    this.ref.isActiveAndEnabled = value
  end,
  
  -- Component methods
  getgameObject = function (this)
    return this.ref.gameObject
  end,
  gettag = function (this)
    return this.ref.tag
  end,
  settag = function (this, value)
    this.ref.tag = value
  end,
  getrigidbody = function (this)
    return this.ref.rigidbody
  end,
  getrigidbody2D = function (this)
    return this.ref.rigidbody2D
  end,
  getcamera = function (this)
    return this.ref.camera
  end,
  getlight = function (this)
    return this.ref.light
  end,
  getanimation = function (this)
    return this.ref.animation
  end,
  getconstantForce = function (this)
    return this.ref.constantForce
  end,
  getrenderer = function (this)
    return this.ref.renderer
  end,
  getaudio = function (this)
    return this.ref.audio
  end,
  getguiText = function (this)
    return this.ref.guiText
  end,
  getnetworkView = function (this)
    return this.ref.networkView
  end,
  getguiElement = function (this)
    return this.ref.guiElement
  end,
  getguiTexture = function (this)
    return this.ref.guiTexture
  end,
  getcollider = function (this)
    return this.ref.collider
  end,
  getcollider2D = function (this)
    return this.ref.collider2D
  end,
  gethingeJoint = function (this)
    return this.ref.hingeJoint
  end,
  gettransform = function (this)
    return this.ref.transform
  end,
  getparticleEmitter = function (this)
    return this.ref.particleEmitter
  end,
  getparticleSystem = function (this)
    return this.ref.particleSystem
  end,
  BroadcastMessage = function (this)
    throw(NotSupportedException())
  end,
  CompareTag = function (this, tag)
    return this.ref:CompareTag(tag)
  end,
  GetComponent = function (this, componentType)
    return this.ref:GetComponent(componentType)
  end,
  GetComponentInChildren = function (this, t, includeInactive)
    return this.ref:GetComponentInChildren(t, includeInactive)
  end,
  GetComponents = function (this, t, results)
    return this.ref:GetComponents(t, results)
  end,
  
  -- Object methods
  getname = function (this)
    return this.ref.name
  end,
  setname = function (this, value)
    this.ref.name = value
  end,
  gethideFlags = function (this)
    return this.ref.hideFlags
  end,
  sethideFlags = function (this, value)
    this.ref.hideFlags = value
  end,
  EqualsObj = function (this, other)
    return this.ref:EqualsObj(other)
  end,
  GetInstanceID = function (this)
    return this.ref:GetInstanceID()
  end,
  ToString = function (this)
    return this.ref:ToString()
  end,
}

local updateIndexs = { Update = 0, FixedUpdate = 1, LateUpdate = 2 }

local function registerUpdate(this, bridgeMonoBehaviour, nameOfFn)
  local update = this[nameOfFn]
  if update and type(update) == "function" then
    bridgeMonoBehaviour:RegisterUpdate(updateIndexs[nameOfFn], update)
  end
end

function MonoBehaviour.__ctor__(this, bridgeMonoBehaviour, isFromConverter, serializeData)
  this.ref = bridgeMonoBehaviour
  if not isFromConverter then
    bridgeMonoBehaviour:Bind(this)
  else
    if serializeData and #serializeData > 0 then
      local datas = loadstring(serializeData)()
      local normals = datas[1]
      if normals then
        for k, v in pairs(normals) do
          this[k] = v
        end
      end
      local objects = datas[2]
      if objects then
        local serializeObjects = bridgeMonoBehaviour.SerializeObjects 
        for k, v in pairs(objects) do
          this[k] = serializeObjects[v]
        end 
      end
    end
  end
  registerUpdate(this, bridgeMonoBehaviour, "Update")
  registerUpdate(this, bridgeMonoBehaviour, "FixedUpdate")
  registerUpdate(this, bridgeMonoBehaviour, "LateUpdate")
  this:Awake()
end

System.define("MonoBehaviour", MonoBehaviour)

local function isFromCSharp(T)
  return T.__gc ~= nil
end

local function isSystemType(t)
  return getmetatable(t) == SystemType
end

local typeofBridgeMonoBehaviour = typeof(BridgeMonoBehaviour)
local isInstanceOfType = typeofBridgeMonoBehaviour.IsInstanceOfType 

local function checkBridgeMonoBehaviour(t)
  if isInstanceOfType(typeofBridgeMonoBehaviour, t) then
    t = t.Table
  end
  return t
end

local metatableOfComponent = getmetatable(UnityEngine.Component)
local sourceGetComponent = rawget(metatableOfComponent, "GetComponent")
local sourceGetComponentInChildren = rawget(metatableOfComponent, "GetComponentInChildren")
local sourceGetComponentInParent = rawget(metatableOfComponent, "GetComponentInParent")
local sourceGetComponents = rawget(metatableOfComponent, "GetComponents")
local sourceGetComponentsInChildren = rawget(metatableOfComponent, "GetComponentsInChildren")
local sourceGetComponentsInParent = rawget(metatableOfComponent, "GetComponentsInParent")

local function getBridgeComponent(sourceGetComponents, component, T, ...)
  local coms = sourceGetComponents(component, typeofBridgeMonoBehaviour, ...)
  for i = 0, coms.Length - 1 do
    local t = coms[i].Table
    if is(t, T) then
      return t
    end
  end
end

local function getComponent(component, T)
  if T == nil then throw(ArgumentNullException("type")) end
  if isSystemType(T) then
    T = T.c
  end  
  if isFromCSharp(T) then
    return sourceGetComponent(component, typeof(T))   
  elseif type(T) == "string" then
    local cls = getClass(T)
    if cls ~= nil then
      return checkBridgeMonoBehaviour(getBridgeComponent(sourceGetComponents, component, cls))
    else
      local type_ = typeof(T)
      assert(type_, T .. " is not found")
      return checkBridgeMonoBehaviour(sourceGetComponent(component, type_))
    end
  else
    return getBridgeComponent(sourceGetComponents, component, T)
  end
end

rawset(metatableOfComponent, "GetComponent", getComponent)

local function getComponentInChildren(component, T, includeInactive)
  if T == nil then throw(ArgumentNullException("type")) end
  if isSystemType(T) then
    if includeInactive == nil then
      includeInactive = false
    end
    T = T.c
  else
    if type(T) == "boolean" then
      T, includeInactive = includeInactive, T
    else
      includeInactive = false
    end
  end
  if isFromCSharp(T) then
    return checkBridgeMonoBehaviour(sourceGetComponentInChildren(component, typeof(T), includeInactive))
  end
    return getBridgeComponent(sourceGetComponentsInChildren, component, T, includeInactive)
end

rawset(metatableOfComponent, "GetComponentInChildren", getComponentInChildren)

local function getComponentInParent(component, T)
  if T == nil then throw(ArgumentNullException("type")) end
  if isSystemType(T) then
    T = T.c 
  end
  if isFromCSharp(T) then
    return checkBridgeMonoBehaviour(sourceGetComponentInParent(component, typeof(T)))
  else
    return getBridgeComponent(sourceGetComponentsInParent, component, T, false)
  end  
end

rawset(metatableOfComponent, "GetComponentInParent", getComponentInParent)

local function getBridgeComponents(sourceGetComponents, component, T, results, ...)
  hasReturn = results == nil
  if hasReturn then
    results = SystemList(T)
  end
  if isFromCSharp(T) then
    local coms = sourceGetComponents(component, typeof(T), ...)
    for i = 0, coms.Length - 1 do
      local t = coms[i]
      results:Add(checkBridgeMonoBehaviour(t))
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
    T = T.c
  elseif results then
    T, results = results, T
  end
  return getBridgeComponents(sourceGetComponents, component, T, results)
end

rawset(metatableOfComponent, "GetComponents", getComponents)

local function getComponentsInChildren(component, T, includeInactive, results)
  if T == nil then throw(ArgumentNullException("type")) end
  if isSystemType(T) then
    T = T.c
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

rawset(metatableOfComponent, "GetComponentsInChildren", getComponentsInChildren)

local function getComponentsInParent(component, T, includeInactive, results)
  if T == nil then throw(ArgumentNullException("type")) end
  if isSystemType(T) then
     T = T.c
     includeInactive = false
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

rawset(metatableOfComponent, "GetComponentsInParent", getComponentsInParent)

local metatableOfGameObject = getmetatable(UnityEngine.GameObject)
local sourceAddComponent = rawget(metatableOfGameObject, "AddComponent")

local function addBridgeMonoBehaviour(gameObject, T)
  local monoBehaviour = sourceAddComponent(gameObject, typeofBridgeMonoBehaviour)
  return T(monoBehaviour)
end

local function addComponent(gameObject, T)
  if T == nil then throw(ArgumentNullException("type")) end
  if isSystemType(T) then
    T = T.c
  end  
  if isFromCSharp(T) then
    return sourceAddComponent(gameObject, typeof(T))
  elseif type(T) == "string" then
    local cls = getClass(T)  
    if cls ~= nil then
      return addBridgeMonoBehaviour(gameObject, cls)
    else
      local type_ = typeof(T)
      assert(type_, T .. " is not found")
      return sourceAddComponent(gameObject, type_)
    end
  else
    return addBridgeMonoBehaviour(gameObject, T)
  end
end

rawset(metatableOfGameObject, "AddComponent", addComponent)

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

local function dontDestroyOnLoad(target)
  if obj and not isFromCSharp(obj) then
    obj = assert(obj.ref)
  end
  sourceDontDestroyOnLoad(target)
end

local function findObjectOfType(T)
  if T == nil then throw(ArgumentNullException("type")) end
  if isSystemType(T) then
    T = T.c
  end
  if isFromCSharp(T) then
    return checkBridgeMonoBehaviour(sourceFindObjectOfType(typeof(T)))
  else
    local objs = sourceFindObjectsOfType(typeofBridgeMonoBehaviour)
    for i = 0, objs.Length - 1 do
      local t = coms[i].Table
      if is(t, T) then
        return t
      end
    end
  end
end

local function findBridgeObjectsOfType(sourceFindObjectsOfType, T)
  local results = SystemList(T)
  if isFromCSharp(T) then
    local objs = sourceFindObjectsOfType(typeof(T))
    for i = 0, objs.Length - 1 do
      local t = objs[i]
      results:Add(checkBridgeMonoBehaviour(t))
    end
  else
    local objs = sourceFindObjectsOfType(typeofBridgeMonoBehaviour)
    for i = 0, coms.Length - 1 do
      local t = coms[i].Table
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
    T = T.c
  end
  return findBridgeObjectsOfType(sourceFindObjectsOfType, T)
end

local function findObjectsOfTypeAll(t)
  if t == nil then throw(ArgumentNullException("type")) end
  return findBridgeObjectsOfType(sourceFindObjectsOfTypeAll, t.c)
end

local function findObjectsOfTypeIncludingAssets(t)
  if t == nil then throw(ArgumentNullException("type")) end
  return findBridgeObjectsOfType(sourceFindObjectsOfTypeIncludingAssets, t.c)
end

local function findSceneObjectsOfType(t)
  if t == nil then throw(ArgumentNullException("type")) end
  return findBridgeObjectsOfType(sourceFindSceneObjectsOfType, t.c)
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

rawset(metatableOfSystemObject, "__kind__", "C")
rawset(metatableOfSystemObject, "__default__", System.emptyFn)
rawset(metatableOfSystemObject, "EqualsObj", equals)
rawset(metatableOfSystemObject, "GetType", getType)

UnityEngine.addComponent = addComponent 
UnityEngine.op_Equality = op_Equality
UnityEngine.op_Inequality = op_Inequality
UnityEngine.op_Implicit = op_Implicit

function UnityEngine.bind(monoBehaviour, luaClass, serializeData)
  T = getClass(luaClass)
  assert(T, luaClass .. " is not found")
  return T(monoBehaviour, true, serializeData)
end

local function refactorCall(cls, call)
  local super = getmetatable(cls)
  setmetatable(cls, { __index = super, __call = call })
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

function Vector2.__inherits__()
  return { System.IEquatable_1(Vector2) }
end

local __call = Vector2.__call
System.defStc("UnityEngine.Vector2", Vector2)
refactorCall(Vector2, __call)

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

Vector3.getnormalized = Vector3.Magnitude
Vector3.getsqrMagnitude = Vector3.SqrMagnitude
Vector3.getnormalized = Vector3.Normalize

function Vector3.LerpUnclamped(a, b, t)
  return newVector3(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t, a.z + (b.z - a.z) * t)
end

local function equalsOfVector3(this, other)
  return this.x:Equals(other.x) and this.y:Equals(other.y) and this.z:Equals(other.z)
end

function Vector3.EqualsObj(this, other)
  if getmetatable(other) ~= Vector2 then
    return false
  end
  return equalsOfVector3(this, other)
end

Vector3.Equals = equalsOfVector3
Vector3.ToString = Vector3.__tostring
Vector3.__clone__ = Vector3.Clone

function Vector3.__inherits__()
  return { System.IEquatable_1(Vector3) }
end

__call = Vector3.__call
System.defStc("UnityEngine.Vector3", Vector3)
refactorCall(Vector3, __call)

local Vector4 = UnityEngine.Vector4
local newVector4 = Vector4.New
UnityEngine.Vector4 = nil

function Vector4.get(this, index)
  if index < 0 or index > 2 then
    throw(ArgumentOutOfRangeException("Invalid Vector4 index!"))
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

function Vector4.set(this, index, value) 
  if index < 0 or index > 2 then
    throw(ArgumentOutOfRangeException("Invalid Vector4 index!"))
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

Vector4.getzero = function() return newVector4(0, 0, 0, 0) end
Vector4.getone	 = function() return newVector4(1, 1, 1, 1) end
Vector4.getsqrMagnitude = Vector4.SqrMagnitude
Vector4.getmagnitude = Vector4.Magnitude
Vector4.getnormalized = Vector4.Normalize

local function equalsOfVector4(this, other)
  return this.x:Equals(other.x) and this.y:Equals(other.y) and this.z:Equals(other.z) and this.w:Equals(other.w)
end

function Vector4.EqualsObj(this, other)
  if getmetatable(other) ~= Vector2 then
    return false
  end
  return equalsOfVector4(this, other)
end

Vector4.Equals = equalsOfVector4
Vector4.ToString = Vector4.__tostring
Vector4.__clone__ = Vector4.Clone

function Vector4.__inherits__()
  return { System.IEquatable_1(Vector4) }
end

__call = Vector4.__call
System.defStc("UnityEngine.Vector4", Vector4)
refactorCall(Vector4, __call)

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
  if getmetatable(other) ~= Vector2 then
    return false
  end
  return equalsOfColor(this, other)
end

Color.Equals = equalsOfColor
Color.ToString = Color.__tostring
Color.__clone__ = function(this) return newColor(this.r, this.g, this.b, this.a) end

function Color.__inherits__()
  return { System.IEquatable_1(Color) }
end

__call = Color.__call
System.defStc("UnityEngine.Color", Color)
refactorCall(Color, __call)

function UnityEngine.ToColor(v)
  return newColor(v.x ,v.y, v.z, v.w)
end

function UnityEngine.ToVector4FromColor(v)
  return newVector4(v.r, v.g, v.b, b.a)
end