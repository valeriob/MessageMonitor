﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace File_System_ES.Append
{
    public class Cache_LRU<TKey, TValue>
    {
        Func<TKey, TValue> _fetch;
        Dictionary<TKey, Cache_Line<TKey, TValue>> _store;
        int Max_Size;
        int Batch;
        Task cleanup;
        AutoResetEvent gate = new AutoResetEvent(false);

        public Cache_LRU(Func<TKey, TValue> fetchValue)
        {
            _fetch = fetchValue;
            _store = new Dictionary<TKey, Cache_Line<TKey, TValue>>();
            Max_Size = 1024;
            Batch = 50;

            cleanup = Task.Factory.StartNew(Cleanup);
        }

        public TValue Get(TKey key)
        {
            if (cleaning)
                return _fetch(key);

            if (_store.ContainsKey(key))
            {
                var value = _store[key];
                value.Last_Used = DateTime.Now;
                value.Used_Count++;
                return value.Value;
            }
            else
            {
                var value = _fetch(key);
                _store[key] = new Cache_Line<TKey, TValue> { Added = DateTime.Now, Key = key, Value = value, Last_Used = DateTime.Now, Used_Count = 1 };
                Evict_If_Necessary();
                return value;
            }
        }

        public void Invalidate(TKey key)
        {
            _store.Remove(key);
        }


        protected bool Max_Size_Exceeded()
        {
            return Max_Size + Batch <= _store.Count;
        }
        protected void Evict_If_Necessary()
        {
            if (!Max_Size_Exceeded())
                return;

            //gate.Set();
        }

        bool cleaning;
        protected void Cleanup()
        {
            while (true)
            {
                gate.WaitOne();

                lock (this)
                    cleaning = true;

                var items = _store.Select(s => s.Value).OrderBy(d => d.Used_Count).Take(Batch).ToList();
                for (int i = 0; i < Batch; i++)
                {
                    _store.Remove(items[i].Key);
                }


                lock (this)
                {
                    gate.Reset();
                    cleaning = false;
                }
            }
        }

    }

    public class Cache_Line<TKey, TValue>
    {
        public TKey Key { get; set; }
        public TValue Value { get; set; }
        public int Used_Count { get; set; }
        public DateTime Added { get; set; }
        public DateTime Last_Used { get; set; }
    }
}
