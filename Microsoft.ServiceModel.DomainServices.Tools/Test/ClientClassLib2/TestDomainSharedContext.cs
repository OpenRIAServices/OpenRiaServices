using System;
using System.ServiceModel.DomainServices.Client;

namespace ServerClassLib
{
    /// <summary>
    /// This class serves as a DomainContext that has already been generated.
    /// It tests the ability of the code generator to determine a domain context
    /// already exists on the client.  It is never instantiated.
    /// </summary>
    public class TestDomainSharedContext : DomainContext
    {
        public TestDomainSharedContext()
            : base(null)
        {
        }

        protected override EntityContainer CreateEntityContainer()
        {
            throw new NotImplementedException();
        }
    }
}