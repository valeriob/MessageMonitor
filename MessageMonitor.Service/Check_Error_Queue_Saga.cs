using NServiceBus;
using NServiceBus.Saga;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace MessageMonitor.Service.Saga
{
    public class Check_Error_Queue_State : ISagaEntity
    {
        public Guid Id { get; set; }
        public string OriginalMessageId { get; set; }
        public string Originator { get; set; }

        public string Queue_Name { get; set; }
        public TimeSpan Freqency_Check { get; set; }
    }

    public class Check_Error_Queue_Saga : Saga<Check_Error_Queue_State>,
        IHandleMessages<Quarantine_Error_Queue>,
        IHandleTimeouts<Check_Error_Queue_Timeout>
    {
        public Check_Error_Queue_Saga()
        { 
            SagaMessageFindingConfiguration.ConfigureMapping<Check_Error_Queue_State,Quarantine_Error_Queue>(s=> s.Queue_Name, m=> m.Queue_Name );
            //SagaMessageFindingConfiguration.ConfigureMapping<Check_Error_Queue_State, Quarantine_Error_Queue>(s => s.Queue_Name, m => m.Queue_Name);
        }
        public void Handle(Quarantine_Error_Queue message)
        {
            Data = new Check_Error_Queue_State 
            { 
                Id= Guid.NewGuid(), 
                Queue_Name = message.Queue_Name,
                Freqency_Check = message.Frequency_Check.GetValueOrDefault(TimeSpan.FromMinutes(1))
            };

            if (Queue_Contains_Messages())
                Check_Queue_Later();
        }

        public void Timeout(Check_Error_Queue_Timeout state)
        {
            if (Queue_Contains_Messages())
                Check_Queue_Later();
        }

        protected void Check_Queue_Later()
        {
            RequestUtcTimeout(Data.Freqency_Check, new Check_Error_Queue_Timeout
            {
                Id = Guid.NewGuid(),
                Check_Error_Queue_Saga_Id = Data.Id
            });
        }
        protected void Notify_Good()
        { 
        
        }
        protected bool Queue_Contains_Messages()
        {
            var path = NServiceBus.Utils.MsmqUtilities.GetFullPath(Data.Queue_Name);
            using (var queue = new MessageQueue(path, QueueAccessMode.PeekAndAdmin))
            {
                var mpf = new MessagePropertyFilter();
                mpf.SetAll();
                queue.MessageReadPropertyFilter = mpf;

                return queue.Queue_Contains_Messages();
            }
        }

    }

    public class Check_Error_Queue_Timeout : IMessage
    {
        public Guid Id { get; set; }
        public Guid Check_Error_Queue_Saga_Id { get; set; }
    }
    public class Quarantine_Error_Queue : IMessage
    {
        public Guid Id { get; set; }
        public string Queue_Name { get; set; }
        public TimeSpan? Frequency_Check { get; set; }
    }

}
