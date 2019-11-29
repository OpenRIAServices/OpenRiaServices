using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ServiceModel.Description;
using OpenRiaServices.DomainServices.Server;

namespace OpenRiaServices.DomainServices.Hosting
{
    /// <summary>
    /// Base class for <see cref="DomainService"/> endpoint factories.
    /// </summary>
    public abstract class DomainServiceEndpointFactory
    {
        private string _name = string.Empty;
        private NameValueCollection _parameters;

        /// <summary>
        /// Initializes a new instance of the <see cref="DomainServiceEndpointFactory"/> class.
        /// </summary>
        protected DomainServiceEndpointFactory()
        {
        }

        /// <summary>
        /// Gets or sets the name of the endpoint.
        /// </summary>
        /// <remarks>Default value is an empty string. This property doesn't accept <c>null</c> values.</remarks>
        public string Name
        {
            get
            {
                return this._name;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                this._name = value;
            }
        }

        /// <summary>
        /// Gets or sets a collection of key/value parameter pairs.
        /// </summary>
        public NameValueCollection Parameters
        {
            get
            {
                if (this._parameters == null)
                {
                    this._parameters = new NameValueCollection();
                }

                return this._parameters;
            }
            internal set
            {
                this._parameters = value;
            }
        }

        /// <summary>
        /// Creates endpoints based on the specified description.
        /// </summary>
        /// <param name="description">The <see cref="DomainServiceDescription"/> of the <see cref="DomainService"/> to create the endpoints for.</param>
        /// <param name="serviceHost">The service host for which the endpoints will be created.</param>
        /// <param name="contractDescription">the default domain serice contract description</param>
        /// <returns>The endpoints that were created.</returns>
        public virtual IEnumerable<ServiceEndpoint> CreateEndpoints(DomainServiceDescription description, DomainServiceHost serviceHost, ContractDescription contractDescription)
        {
            var endpoints = new List<ServiceEndpoint>();
            foreach (Uri address in serviceHost.BaseAddresses)
            {
                endpoints.Add(this.CreateEndpointForAddress(contractDescription, address));
            }
            return endpoints;
        }

        /// <summary>
        /// Creates an endpoint based on the specified address.
        /// </summary>
        /// <param name="contract">The endpoint's contract.</param>
        /// <param name="address">The endpoint's base address.</param>
        /// <returns>An endpoint.</returns>
        protected virtual ServiceEndpoint CreateEndpointForAddress(ContractDescription contract, Uri address) => throw new NotImplementedException();
    }
}
