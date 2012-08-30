using MessageMonitor.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MessageMonitor.EventStore
{
    public class Read_Commits : Query
    {

    }

    public class CommitDto
    {
        public Guid Id { get; set; }

    }
}
