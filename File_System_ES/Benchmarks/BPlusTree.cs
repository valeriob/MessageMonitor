using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace File_System_ES.Benchmarks
{
    public class BPlusTree : Benchmark
    {
        String_BPlusTree tree;
        public BPlusTree()
        {
            var indexFile = "index.dat";
            var dataFile = "data.dat";

            if (File.Exists(indexFile))
                File.Delete(indexFile);
            if (File.Exists(dataFile))
                File.Delete(dataFile);

            //var indexStream = new MemoryStream();
            //var dataStream = new MemoryStream();
            var indexStream = new MyFileStream(indexFile, FileMode.OpenOrCreate);
            var dataStream = new FileStream(dataFile, FileMode.OpenOrCreate);

            var appendBpTree = new Append.BPlusTree(indexStream, dataStream, 3);
            tree = new String_BPlusTree(appendBpTree);


            //for (int i = 0; i <= 1000000; i++)
            //{
            //    tree.Put(i, "text about " + i);
            //}
        }


        public override void Run(int number_Of_Inserts, int? batch)
        {
            batch = batch.GetValueOrDefault(1);

            string result;

    
            for (int i = 0; i <= number_Of_Inserts; i += batch.Value)
            {
                for(var j=0; j< batch; j++)
                {
                    tree.Put(i, "text about " + i);

                    for (int k = i; k >= 0; k--)
                        result = tree.Get(k);
                    
                    //result = tree.Get(i);
                }

            }
            var inner = tree.BPlusTree as Append.BPlusTree;
            int wasted = inner.Empty_Slots.Where(s => s != null).Sum(s => s.Length);
            var stats = inner.Empty_Slots.Where(s => s != null).GroupBy(g => g.Length).ToList();
        }
    }



    public class MyFileStream : FileStream
    {
        public MyFileStream(string path, FileMode mode)
            : base(path, mode)//, FileAccess.ReadWrite, FileShare.None, 4096, FileOptions.WriteThrough | FileOptions.SequentialScan) 
        { }

        public override void Flush()
        {
            base.Flush(true);
        }
    }
}
