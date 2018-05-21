using System;

/// <summary>
/// 加入此标记,可以自动添加到导出列表
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum | AttributeTargets.Struct)]
public class LuaAutoWrapAttribute : Attribute {
  public LuaAutoWrapAttribute() { }
}
