using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using MessageMonitor.Services;
using Raven.Client.Extensions;
using Raven.Client.Document;
using Raven.Client;

namespace MessageMonitor.Infrastructure
{
    public class Container
    {
        private static string Database_Name = "MessageMonitor";

        private static IContainer _instance;
        public static IContainer Instance()
        {
            if(_instance != null)
                return _instance;

            var builder = new ContainerBuilder();

            var documentStore = new DocumentStore { Url = "http://localhost:8081"};
            documentStore.Initialize();
            documentStore.DatabaseCommands.EnsureDatabaseExists(Database_Name);
         

            builder.RegisterInstance(documentStore).As<IDocumentStore>();
            builder.Register<IDocumentSession>(f => f.Resolve<IDocumentStore>().OpenSession(Database_Name));

          
            builder.RegisterType<NServiceBus_Audit_Appender>();

            _instance = builder.Build();

            return _instance;
        }
    }
}
