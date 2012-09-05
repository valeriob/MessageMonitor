using CSharpTest.Net.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace File_System_ES
{
    class Program
    {
        static void Main(string[] args)
        {
            //Stream indexStream = new MemoryStream();
            //Stream dataStream = new MemoryStream();
            var indexStream = new FileStream("index.dat", FileMode.OpenOrCreate);
            var dataStream = new FileStream("data.dat", FileMode.OpenOrCreate);

            string result;
            var tree = new V3.BPlusTree<string>(indexStream, dataStream);
            var rnd = new Random(DateTime.Now.Millisecond);
            int number_Of_Inserts = 10000;
            var watch = new Stopwatch();
            watch.Start();

            for (int i = 0; i < number_Of_Inserts; i++)
            {
                tree.Put(i, "text about " + i);
            }

            //for (int i = 0; i < number_Of_Inserts; i++)
            //{
            //    result = tree.Get(i);
            //}

            watch.Stop();
            Console.WriteLine(watch.Elapsed);
            Console.WriteLine("Total reads : "+ tree._readMemory_Count.Sum(s => s.Value));
            Console.WriteLine("Total writes : " + tree._writeMemory_Count.Sum(s => s.Value));

            Console.ReadLine();
        }


        static FileStream file;
        static void Main_No(string[] args)
        {
            string fileName ="stream.dat";

            if (File.Exists(fileName))
                File.Delete(fileName);

            file = File.Open("index.dat", FileMode.OpenOrCreate);

            var opts = new CSharpTest.Net.Collections.BPlusTree<int, string>.OptionsV2(PrimitiveSerializer.Int32, PrimitiveSerializer.String) 
            { 
                BTreeOrder = 4, 
                FileName = "file", 
                CreateFile= CSharpTest.Net.Collections.CreatePolicy.IfNeeded,
                StorageType = CSharpTest.Net.Collections.StorageType.Disk,
            };
            var index = new CSharpTest.Net.Collections.BPlusTree<int, string>(opts);

            var watch = new Stopwatch();
            watch.Start();
            for (int i = 0; i < 10000; i++)
            {
                index.Add(i, "text about " + i);
            }
            index.Commit();
            watch.Stop();
            Console.WriteLine(watch.Elapsed);
            Console.ReadLine();
        }

        static void Main_2(string[] args)
        {
            var con = new System.Data.SqlClient.SqlConnection(@"Data Source=(localdb)\Projects;Initial Catalog=Test;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False");
            con.Open();
            var watch = new Stopwatch();
            watch.Start();


            for (int i = 0; i < 10000; i++)
            {
                var trans = con.BeginTransaction();
                using (var cmd = con.CreateCommand()) 
                {
                    cmd.Transaction = trans;
                    cmd.CommandText = @"INSERT INTO dbo.TABLE_Insert VALUES(@id, @value)";
                    var par = cmd.CreateParameter();
                    par.Value = i;
                    par.ParameterName = "id";
                    cmd.Parameters.Add(par);

                    par = cmd.CreateParameter();
                    par.Value = "value " + i;
                    par.ParameterName = "value";
                    cmd.Parameters.Add(par);

                    cmd.ExecuteNonQuery();
                }
                trans.Rollback();
            }


            watch.Stop();
            Console.WriteLine(watch.Elapsed);
            Console.ReadLine();
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

    public class MyFs : FileStream
    {
        public MyFs(string path, FileMode mode)
            : base(path, mode)
        { }

        public override void Flush()
        {
            base.Flush(true);
        }
    }
}
