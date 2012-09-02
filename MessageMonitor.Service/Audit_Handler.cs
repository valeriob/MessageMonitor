using MessageMonitor.Service.Messages;
using MessageMonitor.Service.Saga;
using MessageMonitor.Services;
using NServiceBus;
using NServiceBus.MessageInterfaces.MessageMapper.Reflection;
using NServiceBus.Serializers.XML;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageMonitor.Service
{
    public class Audit_Handler : IHandleMessages<NServiceBus_Audit_Message>, IHandleMessages<NServiceBus_Failed_Message>
    {
        public NServiceBus_Audit_Handler Service { get; set; }

        public void Handle(NServiceBus_Audit_Message message)
        {
            Service.Append(message);
        }

        public void Handle(NServiceBus_Failed_Message message)
        {
            // Notifica
        }
    }


    public class Error_Queues_Monitoring_Handlers : IHandleMessages<Start_Monitoring_Queue>, 
        IHandleMessages<Queue_Alarm>
    {
        public IBus Bus { get; set; }

        public void Handle(Start_Monitoring_Queue message)
        {
            var manager = MSMQ_Multi_Queue_Notification_Listener.Instance();
            manager.Start_Monitoring_Queue(Address.Parse(message.Queue_Name));
        }

        public void Handle(Queue_Alarm message)
        {
            Bus.Send(new Quarantine_Error_Queue 
            { 
                Queue_Name = message.Queue_Name, 
                Frequency_Check = TimeSpan.FromMinutes(1)
            });
        }
    }
   

}
