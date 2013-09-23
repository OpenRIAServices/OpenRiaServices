using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OpenRiaServices.DomainServices;
using OpenRiaServices.DomainServices.Hosting;
using OpenRiaServices.DomainServices.Server;

namespace TestWap
{
    [EnableClientAccess]
    [DomainIdentifier("TestId", CodeProcessor = typeof(TestCodeProcessor))]
    public class TestDomainService : DomainService
    {
        public IQueryable<TestEntity> GetTestEntities() { return null; }
    }
}