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
        Stream indexStream;

        public BPlusTree()
        {
            var indexFile = "index.dat";
            var metadataFile = "metadata.dat";
            var dataFile = "data.dat";

            if (File.Exists(indexFile))
                File.Delete(indexFile);
            if (File.Exists(metadataFile))
                File.Delete(metadataFile);
            if (File.Exists(dataFile))
                File.Delete(dataFile);
            
            //var indexStream = new MemoryStream();
            //var dataStream = new MemoryStream();
            //var indexStream = new MyFileStream(indexFile, FileMode.OpenOrCreate);
            var metadataStream = new FileStream(metadataFile, FileMode.OpenOrCreate);
             indexStream = new FileStream(indexFile, FileMode.OpenOrCreate);
            //var indexStream = new FileStream(indexFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 4096,
            //    FileOptions.WriteThrough | FileOptions.SequentialScan );

            var dataStream = new FileStream(dataFile, FileMode.OpenOrCreate);

            var appendBpTree = new Append.BPlusTree(metadataStream, indexStream, dataStream, 20);
            tree = new String_BPlusTree(appendBpTree);

            //for (int i = 0; i <= 1000000; i += 1)
            //{
            //    for (var j = i; j < i + 1; j++)
            //    {
            //        tree.Put(j, "text about " + j);
            //    }
            //    tree.Commit();
            //}
        }


        public override void Run(int number_Of_Inserts, int batch)
        {
            string result;

            /// Random

            //var random = new Random();
            //for (int i = 0; i <= number_Of_Inserts; i += batch)
            //{
            //    for (var j = i; j < i + batch; j++)
            //    {
            //        int value = random.Next();
            //        tree.Put(value, "text about " + value);
            //        result = tree.Get(value);
            //    }
            //    tree.Commit();
            //}

            //int count = number_Of_Inserts;
            //var random = new Random();
            //while (count > 0)
            //{
            //    int value = random.Next();
            //    tree.Put(value, "text about " + value);

            //    if (count % 100 == 0)
            //        tree.Commit();
            //    count--;
            //}

            //tree.Commit();
            /// Reverse
            //for (int i = number_Of_Inserts; i >= 0; i -= batch)
            //{
            //    for (var j = i; j > i - batch; j--)
            //    {
            //        tree.Put(j, "text about " + j);
            //        tree.Get(j);
            //        //for (int k = i; k <= number_Of_Inserts; k++)
            //        //    result = tree.Get(k);
            //    }
            //    tree.Commit();
            //}

            for (int i = 0; i < number_Of_Inserts; i += batch)
            {
                for (var j = i; j < i + batch; j++)
                {
                    tree.Put(j, "text about " + j);
                    //for (int k = i; k >= 0; k--)
                    //    result = tree.Get(k);

                    result = tree.Get(j);
                }
                tree.Commit();
                // var usage = Count_Empty_Slots();
            }


            // Read Only
            //for (int i = 0; i < number_Of_Inserts; i++)
            //{
            //    result = tree.Get(i);
            //}

            //var inner = tree.BPlusTree as Append.BPlusTree;
            //var rgps = File_System_ES.Append.Pending_Changes._statistics_blocks_found.GroupBy(g => g).ToList();
            //int wasted = inner.Empty_Slots.Sum(s => s.Length * s.Blocks.Count);
            //var stats = inner.Empty_Slots.GroupBy(g => g.Length).ToList();

            //var usage = Count_Empty_Slots();
        }

        protected File_System_ES.Append.Usage Count_Empty_Slots()
        {
            int invalid = 0;
            int valid = 0;
            int blockSize = File_System_ES.Append.Node.Size_In_Bytes(3);
            long position = indexStream.Position;

            indexStream.Seek(8, SeekOrigin.Begin);
            var buffer = new byte[blockSize];
            while (indexStream.Read(buffer, 0, buffer.Length) > 0)
            {
                var node = File_System_ES.Append.Node.From_Bytes(buffer, 3);
                if (node.IsValid)
                    valid++;
                else
                    invalid++;
            }

            int used = valid * blockSize;
            int wasted = invalid * blockSize;

            indexStream.Seek(position, SeekOrigin.Begin);
            return new File_System_ES.Append.Usage { Invalid = invalid, Valid = valid };
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
