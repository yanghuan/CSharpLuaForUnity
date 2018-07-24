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


local assert = assert
local setmetatable = setmetatable
local getmetatable = getmetatable
local rawget = rawget
local rawset = rawset
local type = type
local tinsert = table.insert
local sformat = string.format
local loadstring = loadstring
local pairs = pairs
local typeof = typeof

local UnityEngine = UnityEngine
local toluaSystem = toluaSystem
UnityEngineUI = UnityEngine.UI

local function isFromCSharp(T)
  return T.__gc ~= nil
end

local function isSystemType(t)
  return getmetatable(t) == SystemType
end

local metatableOfObject = getmetatable(UnityEngine.Object)
local source__eq = rawget(metatableOfObject, "__eq")


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

rawset(metatableOfSystemObject, "op_Equality", op_Equality)
rawset(metatableOfSystemObject, "op_Inequality", op_Inequality)
rawset(metatableOfSystemObject, "op_Implicit", op_Implicit)


function UnityEngine.bind(monoBehaviour, luaClass, serializeData)
  T = getClass(luaClass)
  assert(T, luaClass .. " is not found")
  return T(monoBehaviour, true, serializeData)
end

local function defineUnityStruct(name, T)
  local __call = T.__call
  local __index = T.__index
  System.defStc(name, T)
   
  local super = getmetatable(cls)
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
  if getmetatable(other) ~= Vector3 then
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

local function setOfVector4(this, inde, value, error)
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

function Vector4.__inherits__()
  return { System.IEquatable_1(Vector4) }
end

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

function Color.__inherits__()
  return { System.IEquatable_1(Color) }
end

defineUnityStruct("UnityEngine.Color", Color)

function UnityEngine.ToColor(v)
  return newColor(v.x ,v.y, v.z, v.w)
end

function UnityEngine.ToVector4FromColor(v)
  return newVector4(v.r, v.g, v.b, b.a)
end



local Color32 = UnityEngine.Color32
local newColor32 = Color32.New
UnityEngine.Color32 = nil

function Color32.get(this, index)
  if index < 0 or index > 2 then
    throw(ArgumentOutOfRangeException("Invalid Color32 index!"))
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

function Color32.set(this, index, value) 
  if index < 0 or index > 2 then
    throw(ArgumentOutOfRangeException("Invalid Color32 index!"))
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

local function equalsOfColor32(this, other)
  return this.r:Equals(other.r) and this.g:Equals(other.g) and this.b:Equals(other.b) and this.a:Equals(other.a)
end

function Color32.EqualsObj(this, other)
  if getmetatable(other) ~= Color32 then
    return false
  end
  return equalsOfColor32(this, other)
end

Color32.Equals = equalsOfColor32
Color32.ToString = Color32.__tostring
Color32.__clone__ = function(this) return newColor32(this.r, this.g, this.b, this.a) end

function Color32.__inherits__()
  return { System.IEquatable_1(Color32) }
end

defineUnityStruct("UnityEngine.Color32", Color32)

function UnityEngine.ToColor32(v)
  return newColor32(v.x ,v.y, v.z, v.w)
end

function UnityEngine.ToVector4FromColor32(v)
  return newVector4(v.r, v.g, v.b, b.a)
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

function Quaternion.__inherits__()
  return { System.IEquatable_1(Quaternion) }
end

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

function Bounds.__inherits__()
  return { System.IEquatable_1(Bounds) }
end

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


System.define("CSLua.LuaObject", {})

local LuaTable_2 = {__call = function()end}
System.define("CSLua.LuaTable_2", LuaTable_2)

function GenericMethodNotImplement(funcName)
  ALog.Error("没有找到泛型方法的Lua导出，请确保函数签名一致，并且已经添加到CustomSetting.cs中重新生成Lua Wrapper：" .. funcName);
end

local function TypeOfTolua(T)
  if isSystemType(T) then
    T = T.c
  end
  if isFromCSharp(T) then
    if type(T) ~= "userdata" then
      T = typeof(T)
    end
    return T;
  else
    return T;
  end
end
System.TypeOfTolua = TypeOfTolua