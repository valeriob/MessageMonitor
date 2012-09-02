
using EventStore.Persistence.RavenPersistence;
using Raven.Client.Indexes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageMonitor.Projections
{

    public class NServiceBus_Error_Queue
    {
        public Guid Id { get; set; }
        public string Queue_Name { get; set; }
        public DateTime? Started_Last { get; set; }
        public DateTime? Stopped_Last { get; set; }
    }
    // AggregateType

    //public class index : AbstractIndexCreationTask<RavenCommit, NServiceBus_Error_Queue>
    //{
    //    //public index() 
    //    //{ 
    //    //    Map = docs => from doc in docs
    //    //                  where doc.
    //    //}
    //}

}
