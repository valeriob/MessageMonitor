using CSharpTest.Net.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace File_System_ES
{
    class Program
    {
        static void Main(string[] args)
        {
            var stream = new MemoryStream();
            string result;
            var tree = new V3.BPlusTree<string>(stream);

            tree.Put(0, "ciao 0");
            result = tree.Get(0);

            tree.Put(2, "ciao 2");
            result = tree.Get(2);

            tree.Put(3, "ciao 3");
            result = tree.Get(3);

            //tree.Put(4, "ciao 4");
            //result = tree.Get(4);

            tree.Put(1, "ciao 1");
            result = tree.Get(1);
        }

        //static void Main(string[] args)
        //{
        //    string result;

        //    var stream = new MemoryStream();
        //    var tree = new BPlusTree<string>(stream);


        //    tree.Put(0, "ciao 0");
        //    result = tree.Get(0);

        //    //tree.Put(1, "ciao 1");
        //    //result = tree.Get(1);

        //    tree.Put(2, "ciao 2");
        //    result = tree.Get(2);

        //    tree.Put(3, "ciao 3");
        //    result = tree.Get(3);

        //    tree.Put(1, "ciao 1");
        //    result = tree.Get(1);

        //    tree.Put(4, "ciao 4");
        //    result = tree.Get(4);

        //    tree.Put(5, "ciao 5");
        //    result = tree.Get(5);


        //    tree.Put(6, "ciao 6");
        //    result = tree.Get(6);
        //}


        static FileStream file;
        static void Main_old(string[] args)
        {
            string fileName ="stream.dat";

            if (File.Exists(fileName))
                File.Delete(fileName);

            file = File.Open("index.dat", FileMode.OpenOrCreate);

            var opts = new CSharpTest.Net.Collections.BPlusTree<int, int>.OptionsV2(PrimitiveSerializer.Int32, PrimitiveSerializer.Int32) 
            { 
                BTreeOrder = 4, 
                FileName = "file", 
                CreateFile= CSharpTest.Net.Collections.CreatePolicy.IfNeeded,
                StorageType = CSharpTest.Net.Collections.StorageType.Disk,
            };
            var index = new CSharpTest.Net.Collections.BPlusTree<int, int>(opts);

            var id = Guid.Empty;
            index[0] = 0;
            index[2] = 2;
            index[3] = 3;
            index[1] = 1;
            index.Commit();
            
        }

    }

    public class Head_Serializer : ISerializer<Stream_Head>
    {
        public Stream_Head ReadFrom(Stream stream)
        {
            int lenght = 0;
            var head =new Stream_Head
            {
                Id = PrimitiveSerializer.Guid.ReadFrom(stream),
                Version = PrimitiveSerializer.Int32.ReadFrom(stream),
            };

            lenght = PrimitiveSerializer.Int32.ReadFrom(stream);
            head.Commits = new long[lenght];
            for (int i = 0; i < lenght; i++)
                head.Commits[i] = PrimitiveSerializer.Int64.ReadFrom(stream);
            return  head;
        }

        public void WriteTo(Stream_Head value, Stream stream)
        {
            PrimitiveSerializer.Guid.WriteTo(value.Id, stream);
            PrimitiveSerializer.Int32.WriteTo(value.Version, stream);
            PrimitiveSerializer.Int32.WriteTo(value.Commits.Length, stream);
            for(int i=0; i< value.Commits.Length; i++)
                PrimitiveSerializer.Int64.WriteTo(value.Commits[i], stream);
        }
    }

    // 16TB = 2^44
    // Pagina 14bit (16k), directory 10bit 
    public class Stream_Head
    {
        public Guid Id { get; set; }
        public int Version { get; set; }
        public long[] Commits { get; set; }
    }


    public class First_Page_Level_Directory
    {
        public int[] Directories { get; set; }
    }
    public class Second_Page_Level_Directory
    {
        public int[] Directories { get; set; }
    }
    public class Third_Page_Level_Directory
    {
        public int[] Pages { get; set; }
    }

    public class Page_Table_Entry
    {
        public int Page_Address { get; set; }
        public int Offset { get; set; }

    }

    public class Page
    {
        public long Address { get; set; }
        public int Offset { get; set; }
    }
}
