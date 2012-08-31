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

    public class NServiceBus_MSMQ_Fault_Queue_Listener : MSMQ_Queue_Listener
    {
        protected IBus Bus { get; set; }


        public NServiceBus_MSMQ_Fault_Queue_Listener(IBus bus, string queueName) : this(bus, Address.Parse(queueName)) { }

        public NServiceBus_MSMQ_Fault_Queue_Listener(IBus bus, Address queueName)
        {
            var path = NServiceBus.Utils.MsmqUtilities.GetFullPath(queueName);
            Queue = new MessageQueue(path, QueueAccessMode.PeekAndAdmin);
            var mpf = new System.Messaging.MessagePropertyFilter();
            mpf.SetAll();
            Queue.MessageReadPropertyFilter = mpf;

            Queue.PeekCompleted += queue_PeekCompleted;
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

        void queue_PeekCompleted(object sender, PeekCompletedEventArgs e)
        {
            var tm = NServiceBus.Utils.MsmqUtilities.Convert( e.Message);

            if (tm.Is_Failed_Message())
            {
                var failed = tm.To_NServiceBus_Failed_Message();
                Bus.Publish(failed);
            }
        }

        public override void Start()
        {
            Peek_Result = Queue.BeginPeek();
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

        //void Queue_PeekCompleted(object sender, PeekCompletedEventArgs e)
        //{
        //    try
        //    {
        //       // Queue.EndPeek(Peek_Result);
        //        //using (var transaction = new TransactionScope())
        //        using(var transaction = new MessageQueueTransaction())
        //        {
        //            transaction.Begin();
        //            var message = Queue.Receive(TimeSpan.Zero,  transaction );
                 
        //            var tm = NServiceBus.Utils.MsmqUtilities.Convert(message);

        //            var audit = tm.To_NServiceBus_Audit_Message();
        //            Bus.SendLocal(audit);

        //            transaction.Commit();
        //        }
        //    }
        //    catch (Exception ex) 
        //    {
        //        Debug.WriteLine(ex.Message);
        //        Debug.WriteLine(ex.StackTrace);
        //    }
        //    Queue.BeginPeek();
        //}

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
                // abort if processing fails
                transaction.Abort();
            }
            finally
            {
                // start watching for another message
                queue.BeginPeek();
            }
        }

        public override void Start()
        {
            Peek_Result = Queue.BeginPeek();
        }
    }
}
