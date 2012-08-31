using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Faults;
using NServiceBus.Serialization;
using NServiceBus.Serializers.XML;
using NServiceBus.Unicast.Transport;

namespace MessageMonitor.Service
{
    public static class Mapping_Extensions
    {
        public static NServiceBus_Failed_Message To_NServiceBus_Failed_Message(this TransportMessage msg)
        {
            var headers = msg.Headers.ToDictionary(d => d.Key, r => r.Value);

            var failed= new NServiceBus_Failed_Message 
            {
                Version = headers.Try_Get_And_Remove(Headers.NServiceBusVersion),
                WinIdName = headers.Try_Get_And_Remove(Headers.WindowsIdentityName),
                CorrId = headers.Try_Get_And_Remove(TransportHeaderKeys.IdForCorrelation),
                OriginalId = headers.Try_Get_And_Remove(TransportHeaderKeys.OriginalId),
                FailedQ = headers.Try_Get_And_Remove(FaultsHeaderKeys.FailedQ), 
                XmlBody = Encoding.UTF8.GetString(msg.Body),
                
               TimeSent = headers.Try_Get_And_Remove(NServiceBus.Unicast.Monitoring.Headers.TimeSent).To_DateTime(),
               TimeOfFailure = headers.Try_Get_And_Remove("NServiceBus.TimeOfFailure").To_DateTime(),
            };

            failed.ExceptionInfo = new NserviceBus_ExceptionInfo 
            {
                Reason = headers.Try_Get_And_Remove("NServiceBus.ExceptionInfo.Reason"),
                ExceptionType = headers.Try_Get_And_Remove("NServiceBus.ExceptionInfo.ExceptionType"),
                HelpLink = headers.Try_Get_And_Remove("NServiceBus.ExceptionInfo.HelpLink"),
                Message = headers.Try_Get_And_Remove("NServiceBus.ExceptionInfo.Message"),
                Source = headers.Try_Get_And_Remove("NServiceBus.ExceptionInfo.Source"),
                StackTrace = headers.Try_Get_And_Remove("NServiceBus.ExceptionInfo.StackTrace"),
                InnerExceptionType = headers.Try_Get_And_Remove("NServiceBus.ExceptionInfo.InnerExceptionType")
            };

            failed.TimeSent = headers.Try_Get_And_Remove(NServiceBus.Unicast.Monitoring.Headers.TimeSent).To_DateTime();
            failed.TimeOfFailure = headers.Try_Get_And_Remove("NServiceBus.TimeOfFailure").To_DateTime();

            failed.Other_Headers = headers.Select(h => new Header { Name = h.Key, Value = h.Value }).ToList();

            return failed;
        }


        public static NServiceBus_Audit_Message To_NServiceBus_Audit_Message(this TransportMessage transportMessage)
        {
            var body = Encoding.UTF8.GetString(transportMessage.Body);
            var headers = transportMessage.Headers.ToDictionary(d => d.Key, r => r.Value);

            var audit = new NServiceBus_Audit_Message
            {
                Version = headers.Try_Get_And_Remove(Headers.NServiceBusVersion),
                WinIdName = headers.Try_Get_And_Remove(Headers.WindowsIdentityName),
                CorrId = headers.Try_Get_And_Remove(TransportHeaderKeys.IdForCorrelation),
                TimeSent = headers.Try_Get_And_Remove(NServiceBus.Unicast.Monitoring.Headers.TimeSent).To_DateTime(),
                ProcessingStarted = headers.Try_Get_And_Remove(NServiceBus.Unicast.Monitoring.Headers.ProcessingStarted).To_DateTime(),
                ProcessingEnded = headers.Try_Get_And_Remove(NServiceBus.Unicast.Monitoring.Headers.ProcessingEnded).To_DateTime(),
                OriginatingAddress = headers.Try_Get_And_Remove("NServiceBus.OriginatingAddress"),

                XmlBody = body,

                Message_Id = transportMessage.Id,
                CorrelationId = transportMessage.CorrelationId,
                IdForCorrelation = transportMessage.IdForCorrelation,
                MessageIntent = transportMessage.MessageIntent + "",
                ReplyToAddress = transportMessage.ReplyToAddress.ToString(),
            };

            audit.Headers = headers.Select(p => new Header { Name = p.Key, Value = p.Value }).ToList();

             return audit;
        }

    }
}
