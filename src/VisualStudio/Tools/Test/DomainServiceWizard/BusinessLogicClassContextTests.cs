using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using OpenRiaServices.Server.Test.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NorthwindModel;
using DescriptionAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;
using TestHelper = OpenRiaServices.VisualStudio.DomainServices.Tools.Test.Utilities.TestHelper;

namespace OpenRiaServices.VisualStudio.DomainServices.Tools.Test
{
    [TestClass]
    public class BusinessLogicClassContextTests
    {
        [ClassInitialize()]
        public static void MyClassInitialize(TestContext testContext)
        {
            UnitTestTraceListener.Initialize(testContext, true);
            TestHelper.EnsureL2SSupport(true);
        }

        [ClassCleanup()]
        public static void MyClassCleanup()
        {
            UnitTestTraceListener.Reset();
            TestHelper.EnsureL2SSupport(false);
        }

        [TestMethod]
        [Description("BusinessLogicClassContext ctor initializes correctly")]
        public void BusinessLogicClass_Context_Ctor()
        {
            // Try the ctor that takes a context list
            LinqToSqlContext ltsContext = new LinqToSqlContext(typeof(DataTests.Northwind.LTS.NorthwindDataContext));
            Assert.AreEqual("NorthwindDataContext (LINQ to SQL)", ltsContext.Name);
            Assert.IsTrue(ltsContext.IsClientAccessEnabled);
        }


        [TestMethod]
        [Description("BusinessLogicClassContext entities are created on demand")]
        public void BusinessLogicClass_Context_Entities_Lazy_Load()
        {
            // Try the ctor that takes a context list
            LinqToSqlContext ltsContext = new LinqToSqlContext(typeof(DataTests.Northwind.LTS.NorthwindDataContext));
            IEnumerable<BusinessLogicEntity> entities = ltsContext.Entities;
            Assert.IsNotNull(entities);
            Assert.IsTrue(entities.Any());
            foreach (BusinessLogicEntity entity in entities)
            {
                Assert.AreEqual(typeof(LinqToSqlEntity), entity.GetType());
                Assert.IsFalse(entity.IsEditable);
                Assert.IsFalse(entity.IsIncluded);
                Assert.IsTrue(entity.CanBeEdited);
                Assert.IsTrue(entity.CanBeIncluded);
                Assert.IsNotNull(entity.ClrType);
                Assert.IsNotNull(entity.Name);
            }
        }

        [TestMethod]
        [WorkItem(180787)]
        [Description("Ensure that non-entity classes are excluded from the Entities list on Linq to Sql contexts")]
        public void BusinessLogicClass_Context_Exclude_Non_Entities()
        {
            LinqToSqlContext ltsContext = new LinqToSqlContext(typeof(DataTests.Scenarios.LTS.Northwind_ExternalMapping.Northwind));
            IEnumerable<BusinessLogicEntity> entities = ltsContext.Entities;
            Assert.IsNotNull(entities);
            Assert.IsTrue(entities.Any());

            // CategorySalesFor1997 is not an entity type
            Assert.IsFalse(entities.Any(e => e.ClrType == typeof(DataTests.Scenarios.LTS.Northwind_ExternalMapping.CategorySalesFor1997)));
        }

        [TestMethod]
        [Description("BusinessLogicClassContext Name+DAL is correct")]
        public void BusinessLogicClass_Context_DAL_Name()
        {
            BusinessLogicContext context = new BusinessLogicContext(typeof(object), "Foo");
            Assert.AreEqual("Foo", context.NameAndDataAccessLayerName);

            LinqToSqlContext ltsContext = new LinqToSqlContext(typeof(DataTests.Northwind.LTS.NorthwindDataContext));
            Assert.AreEqual("NorthwindDataContext (LINQ to SQL)", ltsContext.NameAndDataAccessLayerName);

            LinqToEntitiesContext efContext = new LinqToEntitiesContext(typeof(NorthwindEntities));
            Assert.AreEqual("NorthwindEntities (Entity Framework)", efContext.NameAndDataAccessLayerName);
        }

        [TestMethod]
        [Description("BusinessLogicContext catches the error if underlying L2S Context throws")]
        public void BusinessLogicClass_DataContext_Exception_Caught()
        {
            LinqToSqlContext ltsContext = new LinqToSqlContext(typeof(DataModels.ScenarioModels.DataContextInstantiationScenarios));
            // This call triggers an exception that should be caught
            Assert.IsNotNull(ltsContext.Entities);
            Assert.AreEqual(0, ltsContext.Entities.Count(), "Error context should have zero entities");
        }

        [TestMethod]
        [Description("BusinessLogicContext has no problem with derived ObjectContext and DataContext")]
        public void BusinessLogicClass_Context_Inheritance()
        {
            LinqToSqlContext ltsContext = new LinqToSqlContext(typeof(DataModels.ScenarioModels.DataContextInheritanceScenarios));
            Assert.IsTrue(ltsContext.Entities.Count() > 0);

            LinqToEntitiesContext lteContext = new LinqToEntitiesContext(typeof(DataModels.ScenarioModels.ObjectContextInheritanceScenarios));
            Assert.IsTrue(lteContext.Entities.Count() > 0);
        }

        [TestMethod]
        [Description("Tests if LinqToEntitiesDbContext works properly for DbContext entities")]
        public void BusinessLogicClass_Context_EFDbContextTest()
        {
            LinqToEntitiesDbContext dbContext = new LinqToEntitiesDbContext(typeof(DbContextModels.Northwind.DbCtxNorthwindEntities));
            Assert.AreEqual(11, dbContext.Entities.Count());
            Assert.IsTrue(dbContext.NeedToGenerateMetadataClasses);
        }

        [TestMethod]
        [Description("Tests if LinqToEntitiesDbContext works properly for EF CodeFirst entities")]
        public void BusinessLogicClass_Context_EFCFDbContextTest()
        {
            LinqToEntitiesDbContext efcfContext = new LinqToEntitiesDbContext(typeof(CodeFirstModels.EFCFNorthwindEntities));
            Assert.AreEqual(11, efcfContext.Entities.Count());
            Assert.IsFalse(efcfContext.NeedToGenerateMetadataClasses);
        }
    }
}
