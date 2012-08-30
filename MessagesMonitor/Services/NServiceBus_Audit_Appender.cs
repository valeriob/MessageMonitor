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
    }
}
