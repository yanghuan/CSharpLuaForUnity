using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CSharpLua {
  public interface IProvider {
    void ConvertCustomMonoBehaviour(ref GameObject prefab);
  }

  public static class BaseUtility {
    public static IProvider Provider { get; set; }
  }
}
