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

            var results = Benchmarks.Benchmark.RunAll(3000,1);
            foreach (var result in results)
            {
                Console.WriteLine(result.ToString());
            }

            Console.ReadLine();
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
