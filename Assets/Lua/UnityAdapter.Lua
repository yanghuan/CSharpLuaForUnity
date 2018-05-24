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
  
  
  -- Object methods
  getname = function (this)
    return this.ref.name
  end,
  setname = function (this, value)
    this.ref.name = value
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