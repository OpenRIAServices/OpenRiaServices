using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel.Activation;
using System.ServiceModel.Description;
using OpenRiaServices.Server;
using System.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.Hosting.Wcf;

namespace OpenRiaServices.Hosting.UnitTests
{
    /// <summary>
    /// Tests <see cref="QueryProcessor"/> members.
    /// </summary>
    [TestClass]
    public class QueryProcessorTests
    {
        [TestMethod]
        [Description("Tests QueryProcessor optimization when to flatten graph based on associations")]
        public void QueryProcessor_RequiresFlattening()
        {
            DomainServiceDescription description = DomainServiceDescription.GetDescription(typeof(QueryProcessorTest_DomainService));

            // An entity with associations only on derived types requires flattening
            Assert.IsTrue(QueryProcessor.RequiresFlattening(description, typeof(QueryProcessorTest_Entity)), "Associations on derived type should have signalled flattening was required");
            
            // An entity with associations requires flattening
            Assert.IsTrue(QueryProcessor.RequiresFlattening(description, typeof(QueryProcessorTest_EntityDerived)), "Associations on type should have signalled flattening was required");

            // An entity with no associations does not require flattening
            Assert.IsFalse(QueryProcessor.RequiresFlattening(description, typeof(QueryProcessorTest_EntityNoDerived)), "Associations on type without associations should have signalled flattening was not required");
        }
    }

    // This domain service deliberately creates an entity hierarchy
    // where the root has no associations but a derived type does.
    // This allows the flatten graph test logic to be tested.
    [EnableClientAccess]
    public class QueryProcessorTest_DomainService : DomainService
    {
        public IQueryable<QueryProcessorTest_Entity> GetEntities()
        {
            return null;
        }

        public IQueryable<QueryProcessorTest_EntityNoDerived> GetEntitiesNoDerived()
        {
            return null;
        }
    }

    [KnownType(typeof(QueryProcessorTest_EntityDerived))]
    public class QueryProcessorTest_Entity
    {
        [Key]
        public int Key
        {
            get;
            set;
        }
    }

    public class QueryProcessorTest_EntityDerived : QueryProcessorTest_Entity
    {
        [Include]
        [Association("Derived_to_Association", "Key", "Key")]
        public QueryProcessorTest_EntityAssociated Detail { get; set; }
    }

    public class QueryProcessorTest_EntityAssociated
    {
        [Key]
        public int Key
        {
            get;
            set;
        }

        [Include]
        [Association("Derived_to_Association", "Key", "Key", IsForeignKey=true)]
        public QueryProcessorTest_EntityDerived Master { get; set; }

    }

    public class QueryProcessorTest_EntityNoDerived
    {
        [Key]
        public int Key
        {
            get;
            set;
        }
    }
}
