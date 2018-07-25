/*
 * @create:  李锦俊 
 * @email: mybios@qq.com
 * lua的枚举包装
 * */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using LuaInterface;

namespace CSharpLua
{
    internal sealed class LuaIEnumerator : IEnumerator, IDisposable
    {
        private LuaTable table_;
        private LuaFunction current_;
        private LuaFunction moveNext_;

        public void Push(IntPtr L)
        {
            table_.Push(L);
        }

        public static LuaIEnumerator Create(LuaTable table)
        {
            var ret = table.GetTable<LuaIEnumerator>("ref");
            if (ret == null)
            {
                ret = new LuaIEnumerator(table);
                table.SetTable("ref", ret);
            }
            return ret;
        }

        LuaIEnumerator(LuaTable table)
        {
            table_ = table;
            current_ = table.GetLuaFunction("getCurrent");
            if (current_ == null)
            {
                throw new ArgumentNullException();
            }
            moveNext_ = table.GetLuaFunction("MoveNext");
            if (moveNext_ == null)
            {
                throw new ArgumentNullException();
            }
        }

        public object Current
        {
            get
            {
                return current_.Invoke<LuaTable, object>(table_);
            }
        }

        public void Dispose()
        {
            if (current_ != null)
            {
                current_.Dispose();
                current_ = null;
            }

            if (moveNext_ != null)
            {
                moveNext_.Dispose();
                moveNext_ = null;
            }

            if (table_ != null)
            {
                table_.Dispose();
                table_ = null;
            }
        }

        public bool MoveNext()
        {
            bool hasNext = moveNext_.Invoke<LuaTable, bool>(table_);
            if (!hasNext)
            {
                Dispose();
            }
            return hasNext;
        }

        public void Reset()
        {
            throw new NotSupportedException();
        }
    }

}

