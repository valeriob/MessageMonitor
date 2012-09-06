using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Messaging;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using NServiceBus;
using MessageMonitor.Service.Messages;

namespace MessageMonitor.Service
{
    public class MSMQ_Queue_Listener
    {
        protected IAsyncResult Peek_Result { get; set; }
        protected MessageQueue Queue { get; set; }

        public virtual void Start()
        { 
            
        }

        public virtual void Stop()
        {
            Queue.Dispose();
        }
    }

    public class MSMQ_Multi_Queue_Notification_Listener 
    {
        protected IBus Bus { get; set; }
        public List<string> Queues_To_Monitor { get; set; }


        static MSMQ_Multi_Queue_Notification_Listener _instance;
        public static MSMQ_Multi_Queue_Notification_Listener Instance()
        {
            return _instance;
        }
        public static void Init(IBus bus) 
        {
            _instance = new MSMQ_Multi_Queue_Notification_Listener(bus);
        }

        public MSMQ_Multi_Queue_Notification_Listener(IBus bus)
        {
            Bus = bus;
            Queues_To_Monitor = new List<string>();
        }


        //public void Start_Monitoring_Queue(string queueName)
        //{
        //    Start_Monitoring_Queue(Address.Parse(queueName));
        //}
        public void Start_Monitoring_Queue(string queueName)
        {
            //var path = NServiceBus.Utils.MsmqUtilities.GetFullPath(queueName);

            var queue = new MessageQueue(queueName, QueueAccessMode.PeekAndAdmin);
            var mpf = new System.Messaging.MessagePropertyFilter();
            mpf.SetAll();
            queue.MessageReadPropertyFilter = mpf;

            queue.PeekCompleted += queue_PeekCompleted;

            if(!Queues_To_Monitor.Contains(queueName))
                Queues_To_Monitor.Add(queueName);
        }


        void queue_PeekCompleted(object sender, PeekCompletedEventArgs e)
        {
            var queue = sender as MessageQueue;
            if (e.Message == null || queue == null)
                return;

            Bus.Publish(new Queue_Alarm 
            { 
                Id= Guid.NewGuid(), 
                Timestamp = DateTime.Now,
                Queue_Name = queue.FormatName
            });
        }



        public static void Pick_From_NServiceBus_Msmq()
        {
            var address = Address.Parse("pianificazionecarichi_web_messagequeue_error@orowebapp.orogel.local");
            address = Address.Parse("MessageMonitorAudit");
            var path = NServiceBus.Utils.MsmqUtilities.GetFullPath(address);
            using (var queue = new MessageQueue(path, QueueAccessMode.PeekAndAdmin))
            {
                var mpf = new MessagePropertyFilter();
                mpf.SetAll();
                queue.MessageReadPropertyFilter = mpf;

                var msg = queue.Peek(TimeSpan.FromMinutes(1));
              
                var tm = NServiceBus.Utils.MsmqUtilities.Convert(msg);

                if(tm.Is_Failed_Message())
                    tm.To_NServiceBus_Failed_Message();
            }
        }
    }





    public class NServiceBus_MSMQ_Audit_Queue_Listener : MSMQ_Queue_Listener
    {
        protected IAsyncResult Receive_Result { get; set; }
        protected IBus Bus { get; set; }

        public NServiceBus_MSMQ_Audit_Queue_Listener(IBus bus, string queueName) : this(bus, Address.Parse(queueName)) { }

        public NServiceBus_MSMQ_Audit_Queue_Listener(IBus bus, Address queueName)
        {
            Bus = bus;

            var path = NServiceBus.Utils.MsmqUtilities.GetFullPath(queueName);
            Queue = new MessageQueue(path, QueueAccessMode.ReceiveAndAdmin);
            var mpf = new MessagePropertyFilter();
            mpf.SetAll();
            
            Queue.MessageReadPropertyFilter = mpf;

            Queue.PeekCompleted += queue_PeekCompleted;
        }


        private void queue_PeekCompleted(object sender, PeekCompletedEventArgs e)
        {
            var queue = (MessageQueue)sender;

            var transaction = new MessageQueueTransaction();
            transaction.Begin();
            try
            {
                var message = queue.Receive(transaction);
                var tm = NServiceBus.Utils.MsmqUtilities.Convert(message);
                var audit = tm.To_NServiceBus_Audit_Message();
                Bus.SendLocal(audit);
              
                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Abort();
            }
            finally
            {
                queue.BeginPeek();
            }
        }

        public override void Start()
        {
            Peek_Result = Queue.BeginPeek();
        }
    }
}
