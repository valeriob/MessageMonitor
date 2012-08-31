using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageMonitor.Services
{
    public static class Extensions
    {
        public static IQueryable<Audit_Message_SinteticDto> To_Audit_Message_SinteticDto(this IQueryable<NServiceBus_Audit_Message> source)
        {
            return source.Select(s => new Audit_Message_SinteticDto 
            { 
                
            });
        }
    }
}
