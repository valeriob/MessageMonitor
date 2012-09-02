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
using EventStore;

namespace MessageMonitor.Infrastructure
{
    public class Container
    {
        //private static string Database_Name = "MessageMonitor";

        private static IContainer _instance;
        public static IContainer Instance()
        {
            if(_instance != null)
                return _instance;

            var builder = new ContainerBuilder();

            var documentStore = new DocumentStore { ConnectionStringName="MessageMonitor"  };
            documentStore.Initialize();
            //documentStore.DatabaseCommands.EnsureDatabaseExists(Database_Name);
           // documentStore.DefaultDatabase = Database_Name;

            Raven.Client.Indexes.IndexCreation.CreateIndexes(typeof(Group_Result_Index).Assembly, documentStore);
            
            builder.RegisterInstance(documentStore).As<IDocumentStore>();
            builder.Register<IDocumentSession>(f => f.Resolve<IDocumentStore>().OpenSession());

            var es = WireupEventStore();
            builder.RegisterInstance(es).As<IStoreEvents>();


            builder.RegisterType<NServiceBus_Audit_Handler>();

            _instance = builder.Build();

            return _instance;
        }


        private static IStoreEvents WireupEventStore()
        {
            return Wireup.Init()
               .LogToOutputWindow()
               .UsingRavenPersistence("MessageMonitor_EventStore")
               
               //.UsingSqlPersistence("EventStore") // Connection string is in app.config
                   //.EnlistInAmbientTransaction() // two-phase commit
                  // .InitializeStorageEngine()
                  // .TrackPerformanceInstance("example")
                 //  .UsingJsonSerialization()
                  //     .Compress()
                  //     .EncryptWith(EncryptionKey)
               //.HookIntoPipelineUsing(new[] { new AuthorizationPipelineHook() })
               .UsingSynchronousDispatchScheduler()
                   .DispatchTo(new EventStore.Dispatcher.DelegateMessageDispatcher(DispatchCommit))
               .Build();
        }

        private static void DispatchCommit(Commit commit) { }
    }


}
