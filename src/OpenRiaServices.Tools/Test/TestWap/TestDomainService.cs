using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OpenRiaServices;
using OpenRiaServices.Server;

namespace TestWap
{
    [EnableClientAccess]
    [DomainIdentifier("TestId", CodeProcessor = typeof(TestCodeProcessor))]
    public class TestDomainService : DomainService
    {
        public IQueryable<TestEntity> GetTestEntities() { return null; }
    }
}