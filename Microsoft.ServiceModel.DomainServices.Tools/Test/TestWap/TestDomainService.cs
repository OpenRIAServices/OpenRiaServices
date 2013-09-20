using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ServiceModel.DomainServices;
using System.ServiceModel.DomainServices.Hosting;
using System.ServiceModel.DomainServices.Server;

namespace TestWap
{
    [EnableClientAccess]
    [DomainIdentifier("TestId", CodeProcessor = typeof(TestCodeProcessor))]
    public class TestDomainService : DomainService
    {
        public IQueryable<TestEntity> GetTestEntities() { return null; }
    }
}