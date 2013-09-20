namespace System.ServiceModel.DomainServices.Hosting
{
    #region Namespaces
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;
    using System.ServiceModel.Description;
    using System.ServiceModel.DomainServices.Hosting.OData;
    using System.ServiceModel.DomainServices.Server;
    using System.ServiceModel.Web;
    using System.Text;
    #endregion

    /// <summary>
    /// Represents a Domain Data Service endpoint factory for <see cref="DomainService"/>s.
    /// </summary>
    public class ODataEndpointFactory : DomainServiceEndpointFactory
    {
        /// <summary>Data Service metadata object corresponding to domain service description.</summary>
        private DomainDataServiceMetadata domainDataServiceMetadata;

        /// <summary>
        /// Creates endpoints based on the specified description.
        /// </summary>
        /// <param name="description">The <see cref="DomainServiceDescription"/> of the <see cref="DomainService"/> to create the endpoints for.</param>
        /// <param name="serviceHost">The service host for which the endpoints will be created.</param>
        /// <returns>The endpoints that were created.</returns>
        public override IEnumerable<ServiceEndpoint> CreateEndpoints(DomainServiceDescription description, DomainServiceHost serviceHost)
        {
            Debug.Assert(this.Name != null, "Name has not been set.");
            Debug.Assert(this.domainDataServiceMetadata == null, "this.domainDataServiceMetadata == null");

            if (description.Attributes[typeof(EnableClientAccessAttribute)] != null)
            {
                // The metadata object will tell us which operations we should expose for the OData end point.
                this.domainDataServiceMetadata = new OData.DomainDataServiceMetadata(description);

                // The OData endpoint doesn't expose all operations in the DomainService, but only operations with parameter and return types
                // which are supported by the data service.  WCF doesn't allow us to create a contract without any operation.
                if (this.domainDataServiceMetadata.Sets.Count > 0 || this.domainDataServiceMetadata.ServiceOperations.Count > 0)
                {
                    // Infer the contract from Domain service description.
                    ContractDescription contract = this.CreateContract(description);

                    // Make endpoints that expose the inferred contract on the given base addresses.
                    foreach (Uri address in serviceHost.BaseAddresses)
                    {
                        yield return this.CreateEndpointForAddress(description, contract, address);
                    }
                }
            }
        }

        /// <summary>
        /// Creates a contract description for the domain data service endpoint based on the domain service description.
        /// </summary>
        /// <param name="description">Domain data service description.</param>
        /// <returns>Contract description for the domain data service endpoint.</returns>
        private ContractDescription CreateContract(DomainServiceDescription description)
        {
            Type domainServiceType = description.DomainServiceType;

            ServiceDescription serviceDesc = ServiceDescription.GetService(domainServiceType);

            // Use names from [ServiceContract], if specified.
            ServiceContractAttribute sca = TypeDescriptor.GetAttributes(domainServiceType)[typeof(ServiceContractAttribute)] as ServiceContractAttribute;
            if (sca != null)
            {
                if (!String.IsNullOrEmpty(sca.Name))
                {
                    serviceDesc.Name = sca.Name;
                }
                if (!String.IsNullOrEmpty(sca.Namespace))
                {
                    serviceDesc.Name = sca.Namespace;
                }
            }

            ContractDescription contractDesc = new ContractDescription(serviceDesc.Name + this.Name, serviceDesc.Namespace)
            {
                ConfigurationName = serviceDesc.ConfigurationName + this.Name,
                ContractType = domainServiceType
            };

            // Disable metadata generation for the domain data service contract.
            contractDesc.Behaviors.Add(new ServiceMetadataContractBehavior(true));

            // Add domain service behavior which takes care of instantiating DomainServices.
            ServiceUtils.EnsureBehavior<DomainDataServiceContractBehavior>(contractDesc);

            // Load the ContractDescription from the DomainServiceDescription.
            this.LoadContractDescription(contractDesc, description);

            return contractDesc;
        }

        /// <summary>
        /// Creates an endpoint based on the specified address.
        /// </summary>
        /// <param name="domainServiceDescription">Domain service description from which the <paramref name="contract"/> was inferred.</param>
        /// <param name="contract">The endpoint's contract.</param>
        /// <param name="address">The endpoint's base address.</param>
        /// <returns>An endpoint.</returns>
        private ServiceEndpoint CreateEndpointForAddress(DomainServiceDescription domainServiceDescription, ContractDescription contract, Uri address)
        {
            WebHttpBinding binding = new WebHttpBinding();
            binding.MaxReceivedMessageSize = Int32.MaxValue;
            ServiceUtils.SetReaderQuotas(binding.ReaderQuotas);

            if (address.Scheme.Equals(Uri.UriSchemeHttps))
            {
                binding.Security.Mode = WebHttpSecurityMode.Transport;
            }
            else if (ServiceUtils.CredentialType != HttpClientCredentialType.None)
            {
                binding.Security.Mode = WebHttpSecurityMode.TransportCredentialOnly;
            }

            if (ServiceUtils.CredentialType != HttpClientCredentialType.None)
            {
                binding.Security.Transport.ClientCredentialType = ServiceUtils.CredentialType;
            }

            ServiceEndpoint ep = new ServiceEndpoint(contract, binding, new EndpointAddress(address.OriginalString + "/" + this.Name));

            // Data service end point has data service behavior.
            ep.Behaviors.Add(new DomainDataServiceEndpointBehavior()
            {
                DomainDataServiceMetadata = this.domainDataServiceMetadata,
                DefaultBodyStyle = WebMessageBodyStyle.Wrapped
            });

            return ep;
        }

        /// <summary>
        /// Create operation corresponding to given DomainService query operation.
        /// </summary>
        /// <param name="declaringContract">Contract to which operation will belong.</param>
        /// <param name="operation">DomainService query operation.</param>
        /// <returns>Created operation.</returns>
        private static OperationDescription CreateQueryOperationDescription(ContractDescription declaringContract, DomainOperationEntry operation)
        {
            OperationDescription operationDesc = ODataEndpointFactory.CreateOperationDescription(declaringContract, operation);

            // Change the return type to QueryResult<TEntity>.
            operationDesc.Messages[1].Body.ReturnValue.Type = typeof(IEnumerable<>).MakeGenericType(operation.AssociatedType);

            return operationDesc;
        }

        /// <summary>
        /// Create operation corresponding to given DomainService operation.
        /// </summary>
        /// <param name="declaringContract">Contract to which operation will belong.</param>
        /// <param name="operation">DomainService operation.</param>
        /// <returns>Created operation.</returns>
        private static OperationDescription CreateOperationDescription(ContractDescription declaringContract, DomainOperationEntry operation)
        {
            OperationDescription operationDesc = new OperationDescription(operation.Name, declaringContract);

            // Propagate behaviors.
            foreach (IOperationBehavior behavior in operation.Attributes.OfType<IOperationBehavior>())
            {
                operationDesc.Behaviors.Add(behavior);
            }

            // Add standard web behaviors behaviors.
            if ((operation.Operation == DomainOperation.Query && ((QueryAttribute)operation.OperationAttribute).HasSideEffects) ||
                (operation.Operation == DomainOperation.Invoke && ((InvokeAttribute)operation.OperationAttribute).HasSideEffects))
            {
                // For operations with side-effects i.e. with WebInvoke attribute, we need to build a default GET like
                // URI template so that, the parameter processing is taken care of by the WebHttpBehavior selector.
                WebInvokeAttribute attrib = ServiceUtils.EnsureBehavior<WebInvokeAttribute>(operationDesc);
                if (attrib.UriTemplate == null)
                {
                    attrib.UriTemplate = ODataEndpointFactory.BuildDefaultUriTemplate(operation);
                }
            }
            else
            {
                ServiceUtils.EnsureBehavior<WebGetAttribute>(operationDesc);
            }

            string action = ServiceUtils.GetRequestMessageAction(declaringContract, operationDesc.Name, null);

            // Define operation input.
            MessageDescription inputMessageDesc = new MessageDescription(action, MessageDirection.Input);
            inputMessageDesc.Body.WrapperName = operationDesc.Name;
            inputMessageDesc.Body.WrapperNamespace = ServiceUtils.DefaultNamespace;

            for (int i = 0; i < operation.Parameters.Count; i++)
            {
                DomainOperationParameter parameter = operation.Parameters[i];

                MessagePartDescription parameterPartDesc = new MessagePartDescription(parameter.Name, ServiceUtils.DefaultNamespace)
                {
                    Index = i,
                    Type = TypeUtils.GetClientType(parameter.ParameterType)
                };
                inputMessageDesc.Body.Parts.Add(parameterPartDesc);
            }

            operationDesc.Messages.Add(inputMessageDesc);

            // Define operation output.
            string responseAction = ServiceUtils.GetResponseMessageAction(declaringContract, operationDesc.Name, null);

            MessageDescription outputMessageDesc = new MessageDescription(responseAction, MessageDirection.Output);
            outputMessageDesc.Body.WrapperName = operationDesc.Name + "Response";
            outputMessageDesc.Body.WrapperNamespace = ServiceUtils.DefaultNamespace;

            if (operation.ReturnType != typeof(void))
            {
                outputMessageDesc.Body.ReturnValue = new MessagePartDescription(operationDesc.Name + "Result", ServiceUtils.DefaultNamespace)
                {
                    Type = TypeUtils.GetClientType(operation.ReturnType)
                };
            }
            operationDesc.Messages.Add(outputMessageDesc);

            return operationDesc;
        }

        /// <summary>
        /// Builds the default URI temaplate to be used for side-effecting (POST) operations.
        /// </summary>
        /// <param name="operation">Domain operation.</param>
        /// <returns>string representing the default URI temaplate.</returns>
        private static string BuildDefaultUriTemplate(DomainOperationEntry operation)
        {
            StringBuilder builder = new StringBuilder(operation.Name);
            if (operation.Parameters.Count > 0)
            {
                builder.Append("?");
                foreach (var parameter in operation.Parameters)
                {
                    builder.Append(parameter.Name);
                    builder.Append("={");
                    builder.Append(parameter.Name);
                    builder.Append("}&");
                }
                builder.Remove(builder.Length - 1, 1);
            }

            return builder.ToString();
        }

        /// <summary>
        /// Populates a contract description from a domain service description.
        /// </summary>
        /// <param name="contractDesc">Contract description to populate.</param>
        /// <param name="domainServiceDescription">Domain service description.</param>
        private void LoadContractDescription(ContractDescription contractDesc, DomainServiceDescription domainServiceDescription)
        {
            OperationDescription operationDesc;
            Debug.Assert(this.domainDataServiceMetadata != null, "this.domainDataServiceMetadata != null");

            // Create contract operations by inferring them from the [Query] & [Invoke] methods on the domain service.
            foreach (DomainOperationEntry operation in domainServiceDescription.DomainOperationEntries)
            {
                if (this.domainDataServiceMetadata.Sets.ContainsKey(operation.Name) || this.domainDataServiceMetadata.ServiceOperations.ContainsKey(operation.Name))
                {
                    switch (operation.Operation)
                    {
                        case DomainOperation.Query:
                            operationDesc = ODataEndpointFactory.CreateQueryOperationDescription(contractDesc, operation);
                            Type queryOperationType = typeof(DomainDataServiceQueryOperationBehavior<>).MakeGenericType(operation.AssociatedType);
                            // Add as first behavior such that our operation invoker is the first in the chain.
                            operationDesc.Behaviors.Insert(0, (IOperationBehavior)Activator.CreateInstance(queryOperationType, operation));
                            contractDesc.Operations.Add(operationDesc);
                            break;
                        case DomainOperation.Invoke:
                            operationDesc = ODataEndpointFactory.CreateOperationDescription(contractDesc, operation);
                            // Add as first behavior such that our operation invoker is the first in the chain.
                            operationDesc.Behaviors.Insert(0, new DomainDataServiceInvokeOperationBehavior(operation));
                            contractDesc.Operations.Add(operationDesc);
                            break;
                        default:
                            break;
                    }
                }
            }
        }
    }
}