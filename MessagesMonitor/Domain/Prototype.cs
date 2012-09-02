using CommonDomain.Core;
using MessageMonitor.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageMonitor.Domain
{
    public class NServiceBus_Error_Queue_Monitoring : AggregateBase
    {
        string Queue_Name { get; set; }

        public NServiceBus_Error_Queue_Monitoring()
        {
            base.Register<NServiceBus_Error_Queue_Added>(@event => { Queue_Name = @event.Queue_Name; Id = @event.Monitoring_Id; });
            base.Register<Started_Monitoring_NServiceBus_Error_Queue>(@event => { Queue_Name = @event.Queue_Name;  });
            base.Register<Stopped_Monitoring_NServiceBus_Error_Queue>(@event => { });
        }

        public void Add(string queueName) //: this()
        {
            RaiseEvent(new NServiceBus_Error_Queue_Added { Id = Guid.NewGuid(), Monitoring_Id =Guid.NewGuid(), Timestamp = DateTime.Now, Queue_Name = queueName });
        }


        public void Start() 
        {
            RaiseEvent(new Started_Monitoring_NServiceBus_Error_Queue { Id = Guid.NewGuid(), Timestamp = DateTime.Now, Queue_Name = Queue_Name });
        }

        public void Stop()
        {
            RaiseEvent(new Stopped_Monitoring_NServiceBus_Error_Queue { Id = Guid.NewGuid(), Timestamp = DateTime.Now, Queue_Name = Queue_Name });
        }

    }

    public class Start_Monitor_NServiceBus_ErrorQueue : Command
    {
        public Guid Id { get; set; }
        public string Queue_Name { get; set; }
    }


    public class NServiceBus_Error_Queue_Added : Event
    {
        public Guid Id { get; set; }
        public DateTime Timestamp { get; set; }
        public Guid Monitoring_Id { get; set; }
        public string Queue_Name { get; set; }
    }
    public class Started_Monitoring_NServiceBus_Error_Queue : Event
    {
        public Guid Id { get; set; }
        public DateTime Timestamp { get; set; }
        public Guid Monitoring_Id { get; set; }
        public string Queue_Name { get; set; }
    }
    public class Stopped_Monitoring_NServiceBus_Error_Queue : Event
    {
        public Guid Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string Queue_Name { get; set; }
    }

}
