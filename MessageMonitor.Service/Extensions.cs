using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Messaging;
using System.Text;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Faults;
using NServiceBus.Serialization;
using NServiceBus.Serializers.XML;
using NServiceBus.Unicast.Transport;

namespace MessageMonitor.Service
{
    public static class Extensions
    {
        public static bool Is_Failed_Message(this TransportMessage msg)
        {
            return msg.Headers.Any(h => h.Key == FaultsHeaderKeys.FailedQ);
        }

        public static V Try_Get_And_Remove<K, V>(this IDictionary<K, V> dictionary, K key)
        {
            if (dictionary.ContainsKey(key))
            {
                var value = dictionary[key];
                dictionary.Remove(key);
                return value;
            }
            return default(V);
        }

        public static DateTime? To_DateTime(this string value)
        {
            DateTime date;
            if (DateTime.TryParse(value, out date))
                return date;
            else return null;
        }


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
