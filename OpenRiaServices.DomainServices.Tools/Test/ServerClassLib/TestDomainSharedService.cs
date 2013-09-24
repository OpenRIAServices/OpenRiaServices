using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRiaServices.DomainServices.Server;
using OpenRiaServices.DomainServices.Hosting;

namespace ServerClassLib
{
    /// <summary>
    /// This DomainService is used to test for the pre-existence of
    /// a DomainContext in the client.
    /// </summary>
    [EnableClientAccess]
    public class TestDomainSharedService : DomainService
    {
        public IEnumerable<TestEntity2> GetTestEntities()
        {
            return new TestEntity2[0];
        }
    }
}
