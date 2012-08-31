using NServiceBus;
using System;
using System.Security.Principal;

namespace MessageMonitor.Service
{
    public class Configure_This_Endpoint : IConfigureThisEndpoint, 
        IWantCustomInitialization,
        IWantToRunAtStartup
    {
        public IBus Bus { get; set; }

        public void Init()
        {
            var container = MessageMonitor.Infrastructure.Container.Instance();

            Configure.With()
               .DefineEndpointName("MessageMonitor")
               .AutofacBuilder(container)
               .Log4Net()
               .DefiningMessagesAs(type => type == typeof(NServiceBus_Audit_Message))
               .XmlSerializer()
               .MsmqSubscriptionStorage()
               .MsmqTransport()
               .UnicastBus()
               
               //.DoNotAutoSubscribe()
               //.AllowSubscribeToSelf();
               .LoadMessageHandlers();

            Configure.Instance.Configurer.ConfigureComponent<Mutator>(DependencyLifecycle.InstancePerCall);
        }

        
        public void Run()
        {
            //Bus.Subscribe<NServiceBus_Audit_Message>();

            var queueName = Address.Parse("MessageMonitorAudit");

            NServiceBus.Utils.MsmqUtilities.CreateQueueIfNecessary(queueName, WindowsIdentity.GetCurrent().Name);

            var audit = new NServiceBus_MSMQ_Audit_Queue_Listener(Bus, queueName);
            audit.Start();

            //var fault = new NServiceBus_MSMQ_Fault_Queue_Listener(Bus, "error");
            //audit.Start();
        }

        public void Stop()
        {
            
        }
    }
}
