/*
 * @create:  李锦俊 
 * @email: mybios@qq.com
 * 在lua版本里，用的是表实现，在C#里用的是Dictionary的实现
 * */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CSLua
{
    public class LuaTable<TKey,TValue>
    {
        Dictionary<TKey, TValue> _value = new Dictionary<TKey, TValue>();
        public LuaTable()
        {

        }

        public int Count { get { return _value.Count; } }

        public void SetValue(TKey key, TValue value)
        {
            TValue oldValue;
            if(_value.TryGetValue(key , out oldValue))
            {
                _value[key] = value;
            }
            else
            {
                _value.Add(key, value);
            }
        }
        public TValue GetValue(TKey key)
        {
            TValue value;
            if (_value.TryGetValue(key, out value))
            {
                return value;
            }
            else
            {
                return default(TValue);
            }
        }
        public AsValue GetValue<AsValue>(TKey key)
        {
            TValue value;
            if (_value.TryGetValue(key, out value))
            {
                return (AsValue)(object)value;
            }
            else
            {
                return default(AsValue);
            }
        }
        public bool ContainsKey(TKey key)
        {
            return _value.ContainsKey(key);
        }

        public void ForEach(Action<TKey, TValue> each)
        {
            foreach(var item in _value)
            {
                each(item.Key, item.Value);
            }
        }
        public void ForArrayEach(Action<int, TValue> each)
        {
            foreach (var item in _value)
            {
                each((int)(object)item.Key, item.Value);
            }
        }
    }


    public class LuaObject : LuaTable<string, object>
    {

    }

}
