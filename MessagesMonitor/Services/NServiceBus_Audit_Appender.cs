using Raven.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageMonitor.Services
{
    public class NServiceBus_Audit_Appender
    {
        IDocumentSession Session;
        public NServiceBus_Audit_Appender(IDocumentSession session)
        {
            Session = session;
        }


        public void Append(NServiceBus_Audit_Message message)
        {
            Session.Store(message);
            Session.SaveChanges();
        }

        public IEnumerable<Audit_Message_SinteticDto> Query(Browse_Audit query)
        {
            return Session.Query<Audit_Message_SinteticDto>()
                .ToList();
        }
    }

    public class Browse_Audit
    {
        public DateTime? From { get; set; }
        public DateTime? UpTo { get; set; }

        public string Queue { get; set; }
        public string Computer { get; set; }
    }
    public class Audit_Message_SinteticDto
    {
        public string XmlBody { get; set; }
        public List<Header> Headers { get; set; }

        public string MessageIntent { get; set; }
        public string ReplyToAddress { get; set; }
        public DateTime? TimeSent { get; set; }
    }
}
