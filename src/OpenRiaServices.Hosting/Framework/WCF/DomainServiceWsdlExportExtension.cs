using System.Runtime.Serialization;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;

namespace OpenRiaServices.Hosting
{
    internal sealed class DomainServiceWsdlExportExtension : IContractBehavior, IWsdlExportExtension
    {
        private readonly DomainServiceSerializationSurrogate surrogate;

        public DomainServiceWsdlExportExtension(DomainServiceSerializationSurrogate surrogate)
        {
            this.surrogate = surrogate;
        }

        public void ExportContract(WsdlExporter exporter, WsdlContractConversionContext context)
        {
            XsdDataContractExporter xsdInventoryExporter;
            object dataContractExporter;
            if (!exporter.State.TryGetValue(typeof(XsdDataContractExporter), out dataContractExporter))
            {
                xsdInventoryExporter = new XsdDataContractExporter(exporter.GeneratedXmlSchemas);
                exporter.State.Add(typeof(XsdDataContractExporter), xsdInventoryExporter);
            }
            else
            {
                xsdInventoryExporter = (XsdDataContractExporter)dataContractExporter;
            }

            if (xsdInventoryExporter.Options == null)
            {
                xsdInventoryExporter.Options = new ExportOptions();
            }

            xsdInventoryExporter.Options.DataContractSurrogate = this.surrogate;
        }

        public void ExportEndpoint(WsdlExporter exporter, WsdlEndpointConversionContext context)
        {
        }

        void IContractBehavior.AddBindingParameters(ContractDescription contractDescription, ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {
        }

        void IContractBehavior.ApplyClientBehavior(ContractDescription contractDescription, ServiceEndpoint endpoint, System.ServiceModel.Dispatcher.ClientRuntime clientRuntime)
        {
        }

        void IContractBehavior.ApplyDispatchBehavior(ContractDescription contractDescription, ServiceEndpoint endpoint, System.ServiceModel.Dispatcher.DispatchRuntime dispatchRuntime)
        {
        }

        void IContractBehavior.Validate(ContractDescription contractDescription, ServiceEndpoint endpoint)
        {
        }
    }
}
