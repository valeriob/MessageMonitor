using NServiceBus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConsoleApplication_Message_Sender
{
    public class Program
    {
        static void Main(string[] args)
        {
            var bus = Configure.With()
               .DefineEndpointName("test")
              .DefaultBuilder()
                //.DefiningMessagesAs((m)=> m.GetType() == typeof(Test_Message))

              .XmlSerializer()
              .MsmqTransport()
              .UnicastBus()
              .CreateBus()
              .Start(()=> Configure.Instance.ForInstallationOn<NServiceBus.Installation.Environments.Windows>().Install());
    

            var address = Address.Parse("MessageMonitorAudit");
            //bus.Send(address, new object[] { new Test_Message(), new Real_Message()});
            //bus.Send(address, new Test_Message() );

          //  bus.Send( new object[] { new Test_Message(), new Real_Message() });
            //bus.Send( new Test_Message());

            Console.ReadLine();
        }
    }


    public class Test_Message :IMessage
    {
        public DateTime Date { get; set; }
        public int Int { get; set; }
        public string String { get; set; }
        public int[] Array { get; set; }
    }

    public class Real_Message : IMessage
    {
        public string Body { get; set; }
    }
}
