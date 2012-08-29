using MessagesMonitor.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MessagesMonitor.EventStore
{
    public class Read_Commits : Query
    {

    }

    public class CommitDto
    {
        public Guid Id { get; set; }

    }
}
