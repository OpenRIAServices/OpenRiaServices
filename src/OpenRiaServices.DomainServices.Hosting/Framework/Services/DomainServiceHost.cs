using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Security.Principal;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Description;
using OpenRiaServices.DomainServices.Server;
using System.Web;
using System.Web.Configuration;

namespace OpenRiaServices.DomainServices.Hosting
{
    /// <summary>
    /// Provides a host for domain services.
    /// </summary>
    public class DomainServiceHost : ServiceHost, IServiceProvider
    {
        private static readonly HashSet<string> _allowedSchemes = new HashSet<string>() { Uri.UriSchemeHttp, Uri.UriSchemeHttps };
        private static readonly HashSet<string> _allowedSecureSchemes = new HashSet<string>() { Uri.UriSchemeHttps };
        private readonly DomainServiceDescription _domainServiceDescription;

        /// <summary>
        /// Initializes a new instance of the <see cref="DomainServiceHost"/> class with
        /// the type of service and its base addresses specified.
        /// </summary>
        /// <param name="domainServiceType">The type of the <see cref="DomainService"/> to host.</param>
        /// <param name="baseAddresses">
        /// An array of type <see cref="System.Uri"/> that contains the base addresses for 
        /// the hosted service.
        /// </param>
        public DomainServiceHost(Type domainServiceType, params Uri[] baseAddresses)
        {
            if (domainServiceType == null)
            {
                throw new ArgumentNullException("domainServiceType");
            }

            if (baseAddresses == null)
            {
                throw new ArgumentNullException("baseAddresses");
            }

            EnableClientAccessAttribute att = (EnableClientAccessAttribute)TypeDescriptor.GetAttributes(domainServiceType)[typeof(EnableClientAccessAttribute)];

            // Filter out all non-HTTP addresses.
            HashSet<string> allowedSchemes = DomainServiceHost._allowedSchemes;

            // Additionally filter out HTTP addresses if this DomainService requires a secure endpoint.
            if (att != null && att.RequiresSecureEndpoint)
            {
                allowedSchemes = DomainServiceHost._allowedSecureSchemes;
            }

            // Apply the filter.
            baseAddresses = baseAddresses.Where(addr => allowedSchemes.Contains(addr.Scheme)).ToArray();

            this._domainServiceDescription = DomainServiceDescription.GetDescription(domainServiceType);
            this.InitializeDescription(domainServiceType, new UriSchemeKeyedCollection(baseAddresses));
        }

        /// <summary>
        /// Gets a service.
        /// </summary>
        /// <param name="serviceType">The type of service to get.</param>
        /// <returns>The service.</returns>
        public object GetService(Type serviceType)
        {
            if (serviceType == null)
            {
                throw new ArgumentNullException("serviceType");
            }

            if (serviceType == typeof(IPrincipal))
            {
                return HttpContext.Current.User;
            }

            if (serviceType == typeof(HttpContext))
            {
                return HttpContext.Current;
            }

            if (serviceType == typeof(HttpContextBase))
            {
                return new HttpContextWrapper(HttpContext.Current);
            }

            return null;
        }

        /// <summary>
        /// Creates a description of the service hosted.
        /// </summary>
        /// <param name="implementedContracts">
        /// The <see cref="IDictionary&lt;TKey,TValue&gt;"/> with key pairs of
        /// type (string, <see cref="ContractDescription"/>) that contains the 
        /// keyed-contracts of the hosted service that have been implemented.
        /// </param>
        /// <returns>A <see cref="ServiceDescription"/> of the hosted service.</returns>
        protected override ServiceDescription CreateDescription(out IDictionary<string, ContractDescription> implementedContracts)
        {
            try
            {
                Type domainServiceType = this._domainServiceDescription.DomainServiceType;
                ServiceDescription serviceDesc = ServiceDescription.GetService(domainServiceType);

                implementedContracts = new Dictionary<string, ContractDescription>();

                DomainServicesSection config = DomainServicesSection.Current;
                foreach (ProviderSettings provider in config.Endpoints)
                {
                    DomainServiceEndpointFactory endpointFactory = DomainServiceHost.CreateEndpointFactoryInstance(provider);
                    foreach (ServiceEndpoint endpoint in endpointFactory.CreateEndpoints(this._domainServiceDescription, this))
                    {
                        string contractName = endpoint.Contract.ConfigurationName;

                        ContractDescription contract;
                        if (implementedContracts.TryGetValue(contractName, out contract) && contract != endpoint.Contract)
                        {
                            throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resource.DomainServiceHost_DuplicateContractName, contract.ConfigurationName));
                        }

                        // Register the contract.
                        implementedContracts[endpoint.Contract.ConfigurationName] = endpoint.Contract;

                        // Register the endpoint.
                        serviceDesc.Endpoints.Add(endpoint);
                    }
                }

                return serviceDesc;
            }
            catch (Exception ex)
            {
                DiagnosticUtility.ServiceException(ex);
                throw;
            }
        }

        /// <summary>
        /// Loads the service description information from the configuration file and 
        /// applies it to the runtime being constructed.
        /// </summary>
        protected override void ApplyConfiguration()
        {
            base.ApplyConfiguration();
            this.AddDefaultBehaviors();

            foreach (ServiceEndpoint endpoint in this.Description.Endpoints)
            {
#if DEBUG
                if (Debugger.IsAttached)
                {
                    endpoint.Binding.OpenTimeout = TimeSpan.FromMinutes(5);
                }
#endif
            }
        }

        /// <summary>
        /// Adds the default service and contract behaviors for a domain service.
        /// </summary>
        protected virtual void AddDefaultBehaviors()
        {
            // Force ASP.NET compat mode.
            AspNetCompatibilityRequirementsAttribute aspNetCompatModeBehavior = ServiceUtility.EnsureBehavior<AspNetCompatibilityRequirementsAttribute>(this.Description);
            aspNetCompatModeBehavior.RequirementsMode = AspNetCompatibilityRequirementsMode.Required;

            // Force default service behavior.
            ServiceBehaviorAttribute serviceBehavior = ServiceUtility.EnsureBehavior<ServiceBehaviorAttribute>(this.Description);
            serviceBehavior.InstanceContextMode = InstanceContextMode.PerCall;
            serviceBehavior.IncludeExceptionDetailInFaults = true;
            serviceBehavior.AddressFilterMode = AddressFilterMode.Any;

            // Force metadata to be available through HTTP GET.
            ServiceMetadataBehavior serviceMetadataBehavior = ServiceUtility.EnsureBehavior<ServiceMetadataBehavior>(this.Description);
            serviceMetadataBehavior.HttpGetEnabled = this.BaseAddresses.Any(a => a.Scheme.Equals(Uri.UriSchemeHttp));
            serviceMetadataBehavior.HttpsGetEnabled = this.BaseAddresses.Any(a => a.Scheme.Equals(Uri.UriSchemeHttps));
        }

        /// <summary>
        /// Creates a <see cref="DomainServiceEndpointFactory"/> from a <see cref="ProviderSettings"/> object.
        /// </summary>
        /// <param name="provider">The <see cref="ProviderSettings"/> object.</param>
        /// <returns>A <see cref="DomainServiceEndpointFactory"/>.</returns>
        private static DomainServiceEndpointFactory CreateEndpointFactoryInstance(ProviderSettings provider)
        {
            Type endpointFactoryType = Type.GetType(provider.Type, /* throwOnError */ true);
            DomainServiceEndpointFactory endpointFactory = (DomainServiceEndpointFactory)Activator.CreateInstance(endpointFactoryType);
            endpointFactory.Name = provider.Name;
            endpointFactory.Parameters = provider.Parameters;
            return endpointFactory;
        }
    }
}
