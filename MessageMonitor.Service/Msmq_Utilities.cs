using System;
using System.Collections.Generic;
using System.Linq;
using System.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace MessageMonitor.Service
{
    public static class Msmq_Utilities
    {
        public static bool Queue_Contains_Messages(this MessageQueue queue)
        {
            try
            {
                var msg = queue.Peek(TimeSpan.Zero);
                return true;
            }
            catch (MessageQueueException ex)
            {
                bool isEmpty = ex.MessageQueueErrorCode == MessageQueueErrorCode.MessageNotFound;
                return !isEmpty;
            }
        }

    }
}
