using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRiaServices.DomainServices.Server;
using OpenRiaServices.DomainServices.Hosting;
using ServerClassLib2;

namespace ServerClassLib
{
    [EnableClientAccess]
    public class TestDomainService : DomainService
    {
        public IEnumerable<TestEntity> GetTestEntities()
        {
            return Array.Empty<TestEntity>();
        }

        public void CustomUpdateTestEntity(TestEntity entity, TestComplexType complexType)
        {
        }

        public TestEntity_CL2 GetTestEntity_CL2()
        {
            return null;
        }
    }
}
