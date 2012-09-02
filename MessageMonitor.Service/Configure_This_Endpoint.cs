using NServiceBus;
using System;
using System.Security.Principal;
using Autofac;

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

            //Configure.Instance.Configurer.ConfigureComponent<Mutator>(DependencyLifecycle.InstancePerCall);
        }

        
        public void Run()
        {
            //Bus.Subscribe<NServiceBus_Audit_Message>();

            var queueName = Address.Parse("MessageMonitorAudit");

            NServiceBus.Utils.MsmqUtilities.CreateQueueIfNecessary(queueName, WindowsIdentity.GetCurrent().Name);

            MSMQ_Multi_Queue_Notification_Listener.Init(Bus);
            var faultMonitor = MSMQ_Multi_Queue_Notification_Listener.Instance();

            var audit = new NServiceBus_MSMQ_Audit_Queue_Listener(Bus, queueName);
            audit.Start();

            //var fault = new NServiceBus_MSMQ_Fault_Queue_Listener(Bus, "error");
            //audit.Start();

            var container = MessageMonitor.Infrastructure.Container.Instance();
            var service = container.Resolve<MessageMonitor.Services.NServiceBus_Audit_Handler>();
            service.Statistics();
        }

        public void Stop()
        {
            
        }
    }
}
