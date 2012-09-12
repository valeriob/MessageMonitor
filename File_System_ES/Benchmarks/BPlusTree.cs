using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace File_System_ES.Benchmarks
{
    public class BPlusTree : Benchmark
    {
        String_BPlusTree<Guid> tree;
        Stream indexStream;
        File_System_ES.Append.ISerializer<Guid> serializer = new Guid_Serializer();

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

            var appendBpTree = new Append.BPlusTree<Guid>(metadataStream, indexStream, 
                dataStream, 11, serializer);
            tree = new String_BPlusTree<Guid>(appendBpTree);

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
                    var g = Guid.NewGuid();
                    tree.Put(g, "text about " + j);

                    //result = tree.Get(g);
                }
                tree.Commit();

                //for (int k = i; k >= 0; k--)
                //    result = tree.Get(k);
            }


            ///  Read Only
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

        //protected File_System_ES.Append.Usage Count_Empty_Slots()
        //{
        //    int invalid = 0;
        //    int valid = 0;
        //    int blockSize = File_System_ES.Append.Node<int>.Size_In_Bytes(3, serializer);
        //    long position = indexStream.Position;

        //    indexStream.Seek(8, SeekOrigin.Begin);
        //    var buffer = new byte[blockSize];
        //    while (indexStream.Read(buffer, 0, buffer.Length) > 0)
        //    {
        //        var node = File_System_ES.Append.Node<int>.From_Bytes(buffer, 3, null);
        //        if (node.IsValid)
        //            valid++;
        //        else
        //            invalid++;
        //    }

        //    int used = valid * blockSize;
        //    int wasted = invalid * blockSize;

        //    indexStream.Seek(position, SeekOrigin.Begin);
        //    return new File_System_ES.Append.Usage { Invalid = invalid, Valid = valid };
        //}
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


    public class Int_Serializer : File_System_ES.Append.ISerializer<int>
    {
        public byte[] GetBytes(int value)
        {
            return BitConverter.GetBytes(value);
        }

        public int Get_Instance(byte[] value, int startIndex)
        {
            return BitConverter.ToInt32(value, startIndex);
        }

        public int Fixed_Size()
        {
            return 4;
        }
    }

    public class Long_Serializer : File_System_ES.Append.ISerializer<long>
    {
        public byte[] GetBytes(long value)
        {
            return BitConverter.GetBytes(value);
        }

        public long Get_Instance(byte[] value, int startIndex)
        {
            return BitConverter.ToInt64(value, startIndex);
        }

        public int Fixed_Size()
        {
            return 8;
        }
    }

    public class Guid_Serializer : File_System_ES.Append.ISerializer<Guid>
    {
        public byte[] GetBytes(Guid value)
        {
            return value.ToByteArray();
        }

        public Guid Get_Instance(byte[] value, int startIndex)
        {
            //var slice = new byte[16];
            //Array.Copy(value, startIndex, slice, 0, 16);
            //return new Guid(slice);
            return new Guid(new byte[] {   value[startIndex], value[startIndex + 1], value[startIndex + 2], value[startIndex + 3],
                                    value[startIndex + 4], value[startIndex + 5], value[startIndex + 6], value[startIndex + 7],
                                    value[startIndex + 8], value[startIndex + 9], value[startIndex + 10], value[startIndex + 11],
                                    value[startIndex + 12], value[startIndex + 13], value[startIndex + 14], value[startIndex + 15]});
        }

        public int Fixed_Size()
        {
            return 16;
        }
    }
}
