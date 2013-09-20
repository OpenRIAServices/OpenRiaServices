using System;
using System.Collections.Generic;
using System.ServiceModel.DomainServices.Server;
using System.ServiceModel.DomainServices.Hosting;

namespace Microsoft.ServiceModel.DomainServices.Tools.Test
{
    /// <summary>
    /// A generic <see cref="DomainService"/> used for unit testing.
    /// </summary>
    /// <typeparam name="T">The Type of entity to return in a Query method.</typeparam>
    [EnableClientAccess]
    public class GenericDomainService<T> : DomainService
    {
        [Query]
        public IEnumerable<T> Get()
        {
            throw new NotImplementedException();
        }
    }
}
