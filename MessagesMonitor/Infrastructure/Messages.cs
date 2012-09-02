using System;
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
        Guid Id { get; }
    }

    public interface Command : Message
    {

    }


    public interface Event : Message
    {
        DateTime Timestamp { get; }
    }

    public abstract class BaseEvent : Event
    {
        public Guid Id { get; set; }
        public DateTime Timestamp { get; set; }

        public static TEvent Build<TEvent>() where TEvent : BaseEvent, new()
        {
            var @event = Activator.CreateInstance<TEvent>();
            @event.Id = Guid.NewGuid();
            @event.Timestamp = DateTime.Now;
            return @event;
        }
    }

}
