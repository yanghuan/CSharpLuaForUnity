--------------------------------------------------------------------------------
--      Copyright (c) 2015 - 2016 , 蒙占志(topameng) topameng@gmail.com
--      All rights reserved.
--      Use, modification and distribution are subject to the "MIT License"
--------------------------------------------------------------------------------

local rawget = rawget
local setmetatable = setmetatable
local type = type
local Mathf = Mathf

local Color32 = {}
local get = tolua.initget(Color32)

Color32.__index = function(t,k)
	local var = rawget(Color32, k)
		
	if var == nil then							
		var = rawget(get, k)
		
		if var ~= nil then
			return var(t)	
		end
	end
	
	return var
end

Color32.__call = function(t, r, g, b, a)
	return setmetatable({r = r or 0, g = g or 0, b = b or 0, a = a or 255}, Color32)   
end

function Color32.New(r, g, b, a)
	return setmetatable({r = r or 0, g = g or 0, b = b or 0, a = a or 255}, Color32)		
end

function Color32:Set(r, g, b, a)
	self.r = r
	self.g = g
	self.b = b
	self.a = a or 255 
end

function Color32:Get()
	return self.r, self.g, self.b, self.a
end

function Color32:Equals(other)
	return self.r == other.r and self.g == other.g and self.b == other.b and self.a == other.a
end

function Color32.Lerp(a, b, t)
	t = Mathf.Clamp01(t)
	return Color32.New(a.r + t * (b.r - a.r), a.g + t * (b.g - a.g), a.b + t * (b.b - a.b), a.a + t * (b.a - a.a))
end

function Color32.LerpUnclamped(a, b, t)
  return Color32.New(a.r + t * (b.r - a.r), a.g + t * (b.g - a.g), a.b + t * (b.b - a.b), a.a + t * (b.a - a.a))
end


Color32.__tostring = function(self)
	return string.format("RGBA(%f,%f,%f,%f)", self.r, self.g, self.b, self.a)
end

Color32.__add = function(a, b)
	return Color32.New(a.r + b.r, a.g + b.g, a.b + b.b, a.a + b.a)
end

Color32.__sub = function(a, b)	
	return Color32.New(a.r - b.r, a.g - b.g, a.b - b.b, a.a - b.a)
end

Color32.__mul = function(a, b)
	if type(b) == "number" then
		return Color32.New(a.r * b, a.g * b, a.b * b, a.a * b)
	elseif getmetatable(b) == Color32 then
		return Color32.New(a.r * b.r, a.g * b.g, a.b * b.b, a.a * b.a)
	end
end

Color32.__div = function(a, d)
	return Color32.New(a.r / d, a.g / d, a.b / d, a.a / d)
end

Color32.__eq = function(a,b)
	return a.r == b.r and a.g == b.g and a.b == b.b and a.a == b.a
end

UnityEngine.Color32 = Color32
setmetatable(Color32, Color32)
return Color32



