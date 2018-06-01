local System = System
local throw = System.throw
local getClass = System.getClass
local is = System.is
local NotSupportedException = System.NotSupportedException
local SystemType = System.Type
local SystemList = System.List
local arrayFromList = System.arrayFromList
local ArgumentNullException = System.ArgumentNullException

local assert = assert
local setmetatable = setmetatable
local getmetatable = getmetatable
local rawget = rawget
local rawset = rawset
local type = type
local table = table
local tinsert = table.insert
local typeof = typeof

local Debugger = Debugger
local BridgeMonoBehaviour = BridgeMonoBehaviour
local UnityEngine = UnityEngine

local Debug = {
  Log = Debugger.Log
}

System.define("UnityEngine.Debug", Debug)

local function isFromCSharp(T)
  return T.__gc ~= nil
end

local MonoBehaviour = {
  __kind__ = "C",
  __ctor__ = function (this, monoBehaviour)
    this.ref = monoBehaviour
    monoBehaviour:Bind(this)
  end,
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
    if other ~= nil then
      if not isFromCSharp(other) then
        other = other.ref
        if other == nil or not isFromCSharp(other) then
          return false
        end
      end
    end
    return this.ref:EqualsObj(other)
  end,
  GetInstanceID = function (this)
    return this.ref:GetInstanceID()
  end,
  ToString = function (this)
    return this.ref:ToString()
  end,
}

System.define("MonoBehaviour", MonoBehaviour)

local function isSystemType(t)
  return getmetatable(t) == SystemType
end

local metatableOfComponent = getmetatable(UnityEngine.Component)
local sourceGetComponent = rawget(metatableOfComponent, "GetComponent")
local sourceGetComponentInChildren = rawget(metatableOfComponent, "GetComponentInChildren")
local sourceGetComponentInParent = rawget(metatableOfComponent, "GetComponentInParent")
local sourceGetComponents = rawget(metatableOfComponent, "GetComponents")
local sourceGetComponentsInChildren = rawget(metatableOfComponent, "GetComponentsInChildren")
local sourceGetComponentsInParent = rawget(metatableOfComponent, "GetComponentsInParent")

local function getBridgeComponent(sourceGetComponents, component, T, ...)
  local coms = sourceGetComponents(component, typeof(BridgeMonoBehaviour), ...)
  for i = 0, coms.Length - 1 do
    local t = coms[i].Table
    if is(t, T) then
      return t
    end
  end
end

local function getComponent(component, T)
  if T == nil then throw(ArgumentNullException("type")) end
  if isFromCSharp(T) then
    return sourceGetComponent(component, typeof(T))
  elseif isSystemType(T) then
    T = T.c
    if isFromCSharp(T) then
      return sourceGetComponent(component, typeof(T))   
    else
      return getBridgeComponent(sourceGetComponents, component, T)
    end    
  elseif type(T) == "string" then
    T = getClass(T)
    if T ~= nil then
      return getBridgeComponent(gameObject, T)
    else
      return sourceGetComponent(sourceGetComponents, gameObject, typeof(T))
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
    return sourceGetComponentInChildren(component, typeof(T), includeInactive)
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
    return sourceGetComponentInParent(component, typeof(T))
  else
    return getBridgeComponent(sourceGetComponentsInParent, component, T, false)
  end  
end

rawset(metatableOfComponent, "GetComponentInParent", getComponentInParent)

local typeofBridgeMonoBehaviour = typeof(BridgeMonoBehaviour)
local isInstanceOfType = typeofBridgeMonoBehaviour.IsInstanceOfType 

local function getBridgeComponents(sourceGetComponents, component, T, results, ...)
  hasReturn = results == nil
  if hasReturn then
    results = SystemList(T)
  end
  if isFromCSharp(T) then
    local coms = sourceGetComponents(component, typeof(T), ...)
    for i = 0, coms.Length - 1 do
      local t = coms[i]
      if isInstanceOfType(typeofBridgeMonoBehaviour, t) then
        t = t.Table
      end
      results:Add(t)
    end
  else
    local coms = sourceGetComponents(component, typeof(BridgeMonoBehaviour), ...)
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
  local monoBehaviour = sourceAddComponent(gameObject, typeof(BridgeMonoBehaviour))
  local t = T(monoBehaviour)
  t:Awake()
  return t
end

local function addComponent(gameObject, T)
  if T == nil then throw(ArgumentNullException("type")) end
  if isFromCSharp(T) then
    return sourceAddComponent(gameObject, typeof(T))
  elseif isSystemType(T) then
    T = T.c
    if isFromCSharp(T) then
      return sourceAddComponent(gameObject, typeof(T))
    else
      return addBridgeMonoBehaviour(gameObject, T)
    end
  elseif type(T) == "string" then
    T = getClass(T)  
    if T ~= nil then
      return addBridgeMonoBehaviour(gameObject, T)
    else
      return sourceAddComponent(gameObject, typeof(T))
    end
  else
    return addBridgeMonoBehaviour(gameObject, T)
  end
end

rawset(metatableOfGameObject, "AddComponent", addComponent)

local metatableOfObject = getmetatable(UnityEngine.Object)
local source__eq = rawget(metatableOfObject, "__eq")

local function op_Equality(x, y)
  if x == nil and y == nil then
    return true
  end
  if x and not isFromCSharp(x) then
    x = x.ref
  end
  if y and not isFromCSharp(y) then
    y = y.ref
  end
  return source__eq(x, y)
end

local function op_Inequality(x, y)
  return not op_Equality(x, y)
end

local function op_Implicit(x)
  return not op_Equality(x, nil)
end

UnityEngine.addComponent = addComponent 
UnityEngine.op_Equality = op_Equality
UnityEngine.op_Inequality = op_Inequality
UnityEngine.op_Implicit = op_Implicit