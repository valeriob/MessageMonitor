using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageMonitor
{
    public class NServiceBus_Audit_Message 
    {
        public string Id { get; set; }

        public string Version { get; set; }
        public DateTime? TimeSent { get; set; }
        public string[] EnclosedMessageTypes { get; set; }
        public string WinIdName { get; set; }
        public string CorrId { get; set; }
        public DateTime? ProcessingStarted { get; set; }
        public DateTime? ProcessingEnded { get; set; }
        public string OriginatingAddress { get; set; }

        public string XmlBody { get; set; }
        public List<Header> Headers { get; set; }

        public string Message_Id { get; set; }
        public string CorrelationId { get; set; }
        public string IdForCorrelation { get; set; }

        public string MessageIntent { get; set; }
        public string ReplyToAddress { get; set; }
    }
    public class Header
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }
}
