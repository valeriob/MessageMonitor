using Raven.Client.Document;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace File_System_ES.Benchmarks
{
    public class Ravendb : Benchmark
    {
        DocumentStore _documentStore;
        public Ravendb()
        {
            _documentStore = new DocumentStore { ConnectionStringName = "Benchmark" };
            _documentStore.Initialize();
        }

        public override void Run(int count, int? batch)
        {

            batch = batch.GetValueOrDefault(1);


            for (int i = 0; i < count; i += batch.Value) 
            {
                using (var session = _documentStore.OpenSession())
                {
                    for (int j = 0; j < batch; j++)
                    {
                        session.Store(new My_Test_Entity { Id= Guid.NewGuid(), Body = "test "+i+" "+j, Timestamp =DateTime.Now });
                    }
                    session.SaveChanges();
                }
            }
        }
    }

    public class My_Test_Entity 
    {
        public Guid Id { get; set; }
        public string Body { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
