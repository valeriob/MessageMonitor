using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace File_System_ES.Benchmarks
{
    public abstract class Benchmark
    {

        public abstract void Run(int count, int batch);

        public static IEnumerable<Result> RunAll(int count, int? batch = null)
        {
            var benchmarks = new Benchmark[] 
            { 
              // new Ravendb(),
               new BPlusTree(),
              // new File_Flush_Benchmark(),
             //  new CSharpTest_BPlusTree(),
            //  new Esent(),
            //   new SqlServer()
            };

            var results = new List<Result>();

            foreach (var b in benchmarks)
            {
                var result = new Result { Name= b.GetType().Name, Start = DateTime.Now, Count = count, Batch = batch };
                b.Run(count, batch.GetValueOrDefault(1));
                result.Stop = DateTime.Now;
                results.Add(result);
            }

            return results;
        }

        

    }

    public class Result
    {
        public string Name { get; set; }

        public DateTime Start { get; set; }
        public DateTime Stop { get; set; }
        public int Count { get; set; }
        public int? Batch { get; set; }

        public override string ToString()
        {
            var delta = Stop - Start;
            var speed = delta.TotalSeconds == 0 ? float.PositiveInfinity: Count / delta.TotalSeconds;
            var batch = Batch.HasValue ? "( " + Batch + " )" : "";

            return string.Format("{0} -   {1} {2} tx in {3:0.000}. {4:0.000} tx/s", Name, Count, batch, delta.TotalSeconds, speed);
        }
    }
}
