﻿using System;
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

            var indexStream = new MyFileStream(indexFile, FileMode.OpenOrCreate);
            var dataStream = new FileStream(dataFile, FileMode.OpenOrCreate  );

            var appendBpTree = new Append.BPlusTree(indexStream, dataStream, 3);
            tree = new String_BPlusTree(appendBpTree);
            tree.Commit();
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

                    //for (int k = i; k >= 0; k--) 
                    //    result = tree.Get(k);
                    
                    //tree.Flush();
                    //result = tree.Get(i);
                    //if (i - 1 >= 0)
                    //    result = tree.Get(i - 1);
                }
                //tree.Flush();
                //tree.Commit();
            }

            //for (int i = number_Of_Inserts; i >= 0; i--)
            //{
            //   result = tree.Get(i);
            //}
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