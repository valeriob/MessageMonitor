using Raven.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MessageMonitor.Infrastructure;
using Raven.Client.Linq;

namespace MessageMonitor.Services
{
    public class NServiceBus_Audit_Handler
    {
        IDocumentSession Session;
        public NServiceBus_Audit_Handler(IDocumentSession session)
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
            return Session.Query<NServiceBus_Audit_Message>()
                .Skip(query.Skip)
                .Take(query.Take)
                .To_Audit_Message_SinteticDto()
                .ToList();
        }

        public void Statistics()
        {
            RavenQueryStatistics stats;
            var query = Session.Query<Group_Result, Group_Result_Index>()
                .Statistics(out stats);
            var asd = query.ToList();
        }
    }
    public class Paging 
    {
        public int Skip { get; set; }
        public int Take { get; set; }
    }
    public class Browse_Audit : Paging
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
