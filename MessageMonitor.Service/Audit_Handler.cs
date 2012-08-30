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
    public class Audit_Handler : IHandleMessages<NServiceBus_Audit_Message>
    {
        public NServiceBus_Audit_Appender Service { get; set; }

        public void Handle(NServiceBus_Audit_Message message)
        {
            Service.Append(message);
        }
    }

    public class Mutator : NServiceBus.MessageMutator.IMutateIncomingTransportMessages, NServiceBus.MessageMutator.IMutateIncomingMessages
    {
        public void MutateIncoming(NServiceBus.Unicast.Transport.TransportMessage transportMessage)
        {
            var body = Encoding.UTF8.GetString(transportMessage.Body);

            var serializer = new XmlMessageSerializer(new MessageMapper());

            using (var buffer = new MemoryStream())
            {
                serializer.Serialize(new[] { new NServiceBus_Audit_Message 
                { 
                    XmlBody = body, 
                    Headers = transportMessage.Headers.Select(p=> new Header{ Name= p.Key, Value = p.Value}).ToList(),
                    Message_Id = transportMessage.Id,
                    CorrelationId = transportMessage.CorrelationId, 
                    IdForCorrelation = transportMessage.IdForCorrelation,
                    MessageIntent = transportMessage.MessageIntent +"",
                    ReplyToAddress = transportMessage.ReplyToAddress.ToString(), 
                    TimeSent = transportMessage.TimeSent,
                } }, buffer);

                transportMessage.Body = buffer.GetBuffer();
            }
        }

        public object MutateIncoming(object message)
        {
            return message;
        }
    }

}
