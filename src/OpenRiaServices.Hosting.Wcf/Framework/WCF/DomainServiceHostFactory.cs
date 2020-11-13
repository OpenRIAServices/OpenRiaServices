using System;
using System.ServiceModel;
using System.ServiceModel.Activation;

namespace OpenRiaServices.Hosting.Wcf
{
    /// <summary>
    /// Factory that provides instances of <see cref="DomainServiceHost"/> in managed 
    /// hosting environments where the host instance is created dynamically in response 
    /// to incoming messages.
    /// </summary>
    public class DomainServiceHostFactory : ServiceHostFactory
    {
        /// <summary>
        /// Creates a <see cref="System.ServiceModel.ServiceHost"/> for a specified type of service 
        /// with a specific base address.
        /// </summary>
        /// <param name="serviceType">Specifies the type of service to host.</param>
        /// <param name="baseAddresses">
        /// The <see cref="System.Array"/> of type <see cref="System.Uri"/> that contains the base 
        /// addresses for the service hosted.</param>
        /// <returns>
        /// A <see cref="System.ServiceModel.ServiceHost"/> for the type of service specified with 
        /// a specific base address.
        /// </returns>
        protected override ServiceHost CreateServiceHost(Type serviceType, Uri[] baseAddresses)
        {
            return new DomainServiceHost(serviceType, baseAddresses);
        }
    }
}
