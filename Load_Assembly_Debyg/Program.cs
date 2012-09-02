using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Load_Assembly_Debyg
{
    class Program
    {
        static void Main(string[] args)
        {
            var test = new MessageMonitor.Test.UnitTest1();
            test.TestMethod1();
        }
    }
}
