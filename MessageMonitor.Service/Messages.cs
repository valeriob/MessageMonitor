using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageMonitor.Service.Messages
{
    public class Queue_Alarm
    {
        public Guid Id { get; set; }
        public DateTime Timestamp { get; set; }

        public string Queue_Name { get; set; }
    }

    public class Start_Monitoring_Queue
    {
        public Guid Id { get; set; }
        public string Queue_Name { get; set; }
    }
}
