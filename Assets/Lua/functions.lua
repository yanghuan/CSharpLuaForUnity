local type = type
local tostring = tostring

local originalError = error
function error(str)
  if type(str) == "table" then
    str = tostring(str)
  else
    str = debug.traceback(str, 2)
  end
  originalError(str)
end
