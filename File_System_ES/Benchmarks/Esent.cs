using Microsoft.Isam.Esent.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace File_System_ES.Benchmarks
{
    public class Esent : Benchmark
    {
        PersistentDictionary<int, string> dictionary;
        public Esent()
        {
            dictionary = new PersistentDictionary<int, string>("Names");
            
            dictionary.Clear();
        }
        public override void Run(int count, int? batch)
        {
            batch = batch.GetValueOrDefault(1);

            for (int i = 0; i < count; i += batch.Value)
            {
                for (var j = 0; j < batch; j++)
                {
                    dictionary[i] = "test" + i;
                }
                dictionary.Flush();
            }
        }
    }
}
