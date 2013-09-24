using System;
using System.Collections.Generic;
using OpenRiaServices.DomainServices.Server;
using OpenRiaServices.DomainServices.Hosting;

namespace OpenRiaServices.DomainServices.Tools.Test
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
