/*
 * @create:  李锦俊 
 * @email: mybios@qq.com
 * 在lua版本里，用的是表实现，在C#里用的是List的实现
 * */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CSLua
{
    public class LuaArray<TValue>
    {
        List<TValue> _list = new List<TValue>();
        public LuaArray()
        {

        }

        public int Count { get { return _list.Count; } }

        public void Insert(TValue value) { _list.Add(value); }
        public void SetValue(int key, TValue value) { _list[key] = value; }
        public TValue GetValue(int key) { return _list[key]; }
        public AsValue GetValue<AsValue>(int key) { return (AsValue)(object)_list[key]; }
        public void ForEach(Action<int, TValue> each)
        {
            for (int i = 0; i < _list.Count; i++)
            {
                each(i, _list[i]);
            }
        }
        public void Remove(int i) { _list.RemoveAt(i); }
        public void Clear() { _list.Clear(); }
    }
}
