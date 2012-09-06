using CSharpTest.Net.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace File_System_ES.Benchmarks
{
    public class CSharpTest_BPlusTree : Benchmark
    {
        public override void Run(int count, int? batch)
        {
            string fileName = "stream.dat";

            if (File.Exists(fileName))
                File.Delete(fileName);

            var file = File.Open("index.dat", FileMode.OpenOrCreate);

            var opts = new CSharpTest.Net.Collections.BPlusTree<int, string>.OptionsV2(PrimitiveSerializer.Int32, PrimitiveSerializer.String)
            {
                BTreeOrder = 4,
                FileName = "file",
                CreateFile = CSharpTest.Net.Collections.CreatePolicy.IfNeeded,
                StorageType = CSharpTest.Net.Collections.StorageType.Disk,
            };
            var index = new CSharpTest.Net.Collections.BPlusTree<int, string>(opts);

            for (int i = 0; i < count; i++)
            {
                index.Add(i, "text about " + i);
                index.Commit();
            }

        }
    }
}
