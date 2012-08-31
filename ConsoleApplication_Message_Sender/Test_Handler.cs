using NServiceBus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConsoleApplication_Message_Sender
{
    public class Test_Handler: IHandleMessages<object>
    {
        public void Handle(object message)
        {
            //throw new My_Custsom_Exception("my very reason", message);
        }
    }

    public class My_Custsom_Exception : Exception
    {
        public object Message { get; set; }
        public My_Custsom_Exception(string message, object msg):base(message)
        {
            Message = msg;
        }
    }
}
