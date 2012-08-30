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
            //throw new NotImplementedException();
        }
    }
}
