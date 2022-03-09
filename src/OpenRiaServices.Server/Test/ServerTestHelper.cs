using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;

namespace OpenRiaServices.Server.Test
{
    /// <summary>
    /// Class containing server side test helpers
    /// </summary>
    public class ServerTestHelper
    {
        /// <summary>
        /// Creates an instance of the specified provider type, initialized with
        /// a mock operation context.
        /// </summary>
        /// <typeparam name="T">The Type of provider to create and initialize</typeparam>
        /// <param name="operationType">The operation type</param>
        /// <returns>The provider instance</returns>
        public static T CreateInitializedDomainService<T>(DomainOperationType operationType) where T : DomainService
        {
            return (T)CreateInitializedDomainService(typeof(T), operationType);
        }

        /// <summary>
        /// Creates an instance of the specified provider type, initialized with
        /// a mock operation context.
        /// </summary>
        /// <param name="providerType">The Type of provider to create and initialize</param>
        /// <param name="operationType">The operation type</param>
        /// <returns>The provider instance</returns>
        public static DomainService CreateInitializedDomainService(Type providerType, DomainOperationType operationType)
        {
            DomainService provider = (DomainService)Activator.CreateInstance(providerType);

            // create a fully functional, authenticated test context
            MockUser mockUser = new MockUser("test_user");
            mockUser.IsAuthenticated = true;

            var user = new GenericPrincipal(new GenericIdentity("test_user"), null);
            MockDataService dataService = new MockDataService();
            DomainServiceContext operationContext = new DomainServiceContext(dataService, user, operationType);

            provider.Initialize(operationContext);
            return provider;
        }
    }
}
