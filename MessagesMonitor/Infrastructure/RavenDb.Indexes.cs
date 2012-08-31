using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Raven.Client.Indexes;

namespace MessageMonitor.Infrastructure
{
    public class OriginatingAddress_Result
    {
        public string OriginatingAddress { get; set; }
        public int Count { get; set; }
    }
    public class OriginatingAddress_Index : AbstractIndexCreationTask<NServiceBus_Audit_Message, OriginatingAddress_Result>
    {
        public OriginatingAddress_Index()
        {
            Map = docs => from doc in docs
                          select new OriginatingAddress_Result
                          {
                              OriginatingAddress = doc.OriginatingAddress,
                              Count = 1
                          };
            Reduce = results => from result in results
                                group result by result.OriginatingAddress into g
                                select new OriginatingAddress_Result
                                {
                                    OriginatingAddress = g.Key,
                                    Count = g.Sum(x => x.Count)
                                };
        }
    }

    public class Group_Result
    {
        public string OriginatingAddress { get; set; }
        public string EnclosedMessageTypes { get; set; }
        public int Count { get; set; }
    }
    public class Group_Result_Index : AbstractIndexCreationTask<NServiceBus_Audit_Message, Group_Result>
    {
        public Group_Result_Index()
        {
        
            //    Map = docs => from detail in docs.SelectMany(d => d.EnclosedMessageTypes.Select(t => new { EnclosedMessageTypes = t, d.OriginatingAddress }))
        //                  select new Group_Result
        //                  {
        //                      OriginatingAddress = detail.OriginatingAddress,
        //                      EnclosedMessageTypes = detail.EnclosedMessageTypes,
        //                      Count = 1
        //                  };
            
            Map = docs => from doc in docs
                          from messageType in doc.EnclosedMessageTypes
                          select new 
                          {
                              OriginatingAddress = doc.OriginatingAddress,
                              EnclosedMessageTypes = messageType,
                              Count = 1
                          };
            Reduce = results => from result in results
                                group result by new { result.EnclosedMessageTypes, result.OriginatingAddress } into g
                                select new 
                                {
                                    OriginatingAddress = g.Key.OriginatingAddress,  
                                    EnclosedMessageTypes = g.Key.EnclosedMessageTypes,
                                    Count = g.Sum(x => x.Count)
                                };
        }
    }




}
