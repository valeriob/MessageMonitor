using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Test_Key
{
    class Program
    {
        static void Main(string[] args)
        {
            int n = 10000000;
            var watch = new System.Diagnostics.Stopwatch();
            watch.Start();

            IKey key1 = new Key { Value = BitConverter.GetBytes(int.MaxValue) };
            IKey key2 = new Key { Value = BitConverter.GetBytes(0) };

            //var v1 = Guid.NewGuid();
            //var v2 = Guid.Empty;

            var v1 = int.MaxValue;
            var v2 = 0;

            for (int i = 0; i < n; i++)
            {
               //bool value = key1.Equals(key2);
                bool value = v1.Equals(v2);
            }

            watch.Stop();

            Console.WriteLine("elapsed" + watch.Elapsed);
            Console.ReadLine();
        }
    }

    public interface IKey 
    {
        byte[] Value { get; }
    }
    public struct Key : IEquatable<Key>, IKey
    {
        public byte[] Value { get; set; }


        public bool Equals(Key other)
        {
            for (int i = 0; i < Value.Length; i++)
                if (Value[i] != other.Value[i])
                    return false;
            return true;
        }

    }
}
