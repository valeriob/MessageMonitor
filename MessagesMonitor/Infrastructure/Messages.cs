﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MessageMonitor.Infrastructure
{
    public interface Query
    { 
    }
    public interface Message
    {
    }

    public interface Command : Message
    { }


    public interface Event : Message
    { }
}
