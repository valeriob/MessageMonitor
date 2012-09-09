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
            //var indexStream = new MyFileStream(indexFile, FileMode.OpenOrCreate);
            var indexStream = new FileStream(indexFile, FileMode.OpenOrCreate);
            //var indexStream = new FileStream(indexFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 4096,
            //    FileOptions.WriteThrough | FileOptions.SequentialScan );

            var dataStream = new FileStream(dataFile, FileMode.OpenOrCreate);

            var appendBpTree = new Append.BPlusTree(indexStream, dataStream, 3);
            tree = new String_BPlusTree(appendBpTree);
        }


        public override void Run(int number_Of_Inserts, int batch)
        {
            string result;

            /// Random
            //int count = number_Of_Inserts;
            //var random = new Random();
            //while (count > 0)
            //{
            //    int value = random.Next();
            //    tree.Put(value, "text about " + value);
            //    tree.Commit();
            //    count--;
            //}
            /// Reverse
            //for (int i = number_Of_Inserts; i >= 0; i -= batch)
            //{
            //    for (var j = i; j > i - batch; j--)
            //    {
            //        tree.Put(j, "text about " + j);
            //        for (int k = i; k <= number_Of_Inserts; k++)
            //            result = tree.Get(k);
            //    }
            //    tree.Commit();
            //}

            for (int i = 0; i <= number_Of_Inserts; i += batch)
            {
                for (var j = i; j < i + batch; j++)
                {
                    tree.Put(j, "text about " + j);
                    for (int k = i; k >= 0; k--)
                        result = tree.Get(k);

                     result = tree.Get(j);
                }
                tree.Commit();
            }

            var inner = tree.BPlusTree as Append.BPlusTree;
            var rgps = File_System_ES.Append.Pending_Changes._statistics_blocks_found.GroupBy(g => g).ToList();
            int wasted = inner.Empty_Slots.Where(s => s != null).Sum(s => s.Length);
            var stats = inner.Empty_Slots.Where(s => s != null).GroupBy(g => g.Length).ToList();
        }
    }



    public class MyFileStream : FileStream
    {
        public MyFileStream(string path, FileMode mode)
            : base(path, mode)// FileAccess.ReadWrite, FileShare.None, 4096, FileOptions.WriteThrough | FileOptions.SequentialScan) 
        { }

        public override void Flush()
        {
            base.Flush(true);
        }
    }
}
