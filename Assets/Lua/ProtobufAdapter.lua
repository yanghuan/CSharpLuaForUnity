require("3rd.pbc.protobuf")

local protobuf = protobuf
local pairs = pairs
local ipairs = ipairs
local protobuf = protobuf
local type = type
local getmetatable = getmetatable
local assert = assert
local enums = {}

local function toEnumInt(name, T, str)
  local t = enums[T]
  if t then
    local v = t[str]
    if v then
      return v
    end
  else
    t = {}
    enums[T] = t
  end
  for k, cls in pairs(T) do
    if cls.class == "E" then
      local v = cls[str]
      if v then
        t[str] = v
        return v
      end
    end
  end
  assert(false, str .. " is not in " .. name)
end

local function decode(name, data)
  local proto, error = protobuf.decode(name, data)
  assert(proto, error)
  local T = System.getClass(name)
  local t = T()
  for k, v in pairs(proto) do
    if type(v) == "table" then
      if getmetatable(v) ~= nil then   
        v = decode(v[1], v[2])
        t[k] = v
      else 
        local list = t[k]
        assert(list)
        for _, v in ipairs(v) do
          if type(v) == "table" then
            v = decode(v[1], v[2])
          end
          list:Add(v)
        end
      end
    else 
       --is enum string
      if type(v) == "string" and type(T[k]) == "number" then 
        v = toEnumInt(name, T, v)
      end
      t[k] = v
    end
  end
  return t
end

function encodeProtobuf(t)
  local name = t.__name__
  return protobuf.encode(name, t)
end

function decodeProtobuf(data, T)
  return decode(T.__name__, data)
end