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

        public string XmlBody { get; set; }
        public List<Header> Headers { get; set; }

        public string Message_Id { get; set; }
        public string CorrelationId { get; set; }
        public string IdForCorrelation { get; set; }

        public string MessageIntent { get; set; }
        public string ReplyToAddress { get; set; }
        public DateTime? TimeSent { get; set; }
    }
    public class Header
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }
}
