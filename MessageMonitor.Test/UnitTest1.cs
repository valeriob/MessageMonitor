using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MessageMonitor.Infrastructure;
using Autofac;
using EventStore;
using CommonDomain;
using EventStore.Persistence.RavenPersistence;
using MessageMonitor.Domain;

namespace MessageMonitor.Test
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var container = Container.Instance();

            var es = container.Resolve<IStoreEvents>();

            var repository = new CommonDomain.Persistence.EventStore.EventStoreRepository(es, new Aggregate_Factory() , new CommonDomain.Core.ConflictDetector());

            var arId = Guid.NewGuid();
            var queueName= "myTestQueue";

            var ar = repository.GetById<NServiceBus_Error_Queue_Monitoring>(arId);
            ar.Add(queueName);
            ar.Start();
            ar.Stop();
            repository.Save(ar, Guid.NewGuid(), null);

            repository = new CommonDomain.Persistence.EventStore.EventStoreRepository(es, new Aggregate_Factory(), new CommonDomain.Core.ConflictDetector());
            ar = repository.GetById<NServiceBus_Error_Queue_Monitoring>(arId);
        }
    }

    public class Aggregate_Factory : CommonDomain.Persistence.IConstructAggregates
    {
        public IAggregate Build(Type type, Guid id, CommonDomain.IMemento snapshot)
        {
            var instance = Activator.CreateInstance(type) as IAggregate;

            return instance;
        }
    }
}
