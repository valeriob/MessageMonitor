using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageMonitor.Service
{
    public class NServiceBus_Failed_Message
    {
        public string Version { get; set; }
        public DateTime? TimeSent { get; set; }
        public string[] EnclosedMessageTypes { get; set; }
        public string WinIdName { get; set; }
        public string CorrId { get; set; }

        public NserviceBus_ExceptionInfo ExceptionInfo { get; set; }


        public string OriginalId { get; set; }
        public string FailedQ { get; set; }
        public DateTime? TimeOfFailure { get; set; }


        public string XmlBody { get; set; }
        public List<Header> Other_Headers { get; set; }

        //public string Message_Id { get; set; }
        //public string CorrelationId { get; set; }
        //public string IdForCorrelation { get; set; }

        //public string MessageIntent { get; set; }
        //public string ReplyToAddress { get; set; }
        //public DateTime? TimeSent { get; set; }
    }

    public class NserviceBus_ExceptionInfo
    {
        public string Reason { get; set; }
        public string ExceptionType { get; set; }
        public string HelpLink { get; set; }
        public string Message { get; set; }
        public string Source { get; set; }
        public string StackTrace { get; set; }
        public string InnerExceptionType { get; set; }
    }
}
