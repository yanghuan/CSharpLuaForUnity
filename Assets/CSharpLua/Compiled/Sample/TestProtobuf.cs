using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ProtoBuf;
using CSharpLua.Project.Protocol;
using System.IO;
using ProtoBuf.Meta;

namespace Sample {
  public static class TestProtobuf {
    public static void Run() {
#if __CSharpLua__
      /*
      [[
      protobuf.register_file("Assets/Lua/3rd/pbc/Protocol/CommonPrototype.pb")
      ]]
      */
#endif
      SettingProto proto = new SettingProto() { SettingsMark = 101 };
      proto.Values.Add(new SettingProto.ValuePairProto() { Key = "a", Value = "b" });
      var bytes = Encode(proto);
      var t = Decode<SettingProto>(bytes);
      UnityEngine.Debug.LogFormat("ProtobufDecode {0}", t.SettingsMark);
    }

    private static byte[] Encode(IProtocol proto) {
#if !__CSharpLua__
      using (MemoryStream s = new MemoryStream()) {
        RuntimeTypeModel.Default.Serialize(s, proto);
        return s.ToArray();
      }
#else
      byte[] bytes = null;
      /*
      [[
       bytes = encodeProtobuf(proto)
      ]]
      */
      return bytes;
#endif
    }

    private static T Decode<T>(byte[] bytes) where T : class {
#if !__CSharpLua__
      using (MemoryStream s = new MemoryStream(bytes)) {
        var t = (T)RuntimeTypeModel.Default.Deserialize(s, null, typeof(T));
        return t;
      }
#else
      T t = null;
      /*
      [[
       t = decodeProtobuf(bytes, T)
      ]]
      */
      return t;
#endif
    }

  }
}
