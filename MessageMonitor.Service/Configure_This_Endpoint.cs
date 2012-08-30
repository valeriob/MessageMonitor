using NServiceBus;
using System;

namespace MessageMonitor.Service
{
    public class Configure_This_Endpoint : IConfigureThisEndpoint, IWantCustomInitialization
    {
        public void Init()
        {
            var container = MessageMonitor.Infrastructure.Container.Instance();

            Configure.With()
               .DefineEndpointName("MessageMonitorAudit")
               .AutofacBuilder(container)
               .DefiningMessagesAs(type => type == typeof(NServiceBus_Audit_Message))
               .XmlSerializer()
               .MsmqTransport()
               .UnicastBus()
               .LoadMessageHandlers()
               .CreateBus()
               .Start();

            Configure.Instance.Configurer.ConfigureComponent<Mutator>(DependencyLifecycle.InstancePerCall);
        }
    }
}
