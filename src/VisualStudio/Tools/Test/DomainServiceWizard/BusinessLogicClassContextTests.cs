using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NorthwindModel;
using DescriptionAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;
using TestHelper = OpenRiaServices.VisualStudio.DomainServices.Tools.Test.Utilities.TestHelper;
using System;

namespace OpenRiaServices.VisualStudio.DomainServices.Tools.Test
{
    [TestClass]
    public class BusinessLogicClassContextTests
    {
        [TestMethod]
        [Description("BusinessLogicClassContext ctor initializes correctly")]
        public void BusinessLogicClass_Context_Ctor()
        {
            // Try the ctor that takes a context list
            LinqToEntitiesContext efContext = new LinqToEntitiesContext(typeof(NorthwindEntities));
            Assert.AreEqual("NorthwindEntities (Entity Framework)", efContext.Name);
            Assert.IsTrue(efContext.IsClientAccessEnabled);
        }


        [TestMethod]
        [Description("BusinessLogicClassContext entities are created on demand")]
        public void BusinessLogicClass_Context_Entities_Lazy_Load()
        {
            // Try the ctor that takes a context list
            LinqToEntitiesContext efContext = new LinqToEntitiesContext(typeof(NorthwindEntities));
            IEnumerable<BusinessLogicEntity> entities = efContext.Entities;
            Assert.IsNotNull(entities);
            Assert.IsTrue(entities.Any());
            foreach (BusinessLogicEntity entity in entities)
            {
                Assert.AreEqual(typeof(LinqToEntitiesEntity), entity.GetType());
                Assert.IsFalse(entity.IsEditable);
                Assert.IsFalse(entity.IsIncluded);
                Assert.IsTrue(entity.CanBeEdited);
                Assert.IsTrue(entity.CanBeIncluded);
                Assert.IsNotNull(entity.ClrType);
                Assert.IsNotNull(entity.Name);
            }
        }

        [TestMethod]
        [Description("BusinessLogicClassContext Name+DAL is correct")]
        public void BusinessLogicClass_Context_DAL_Name()
        {
            BusinessLogicContext context = new BusinessLogicContext(typeof(object), "Foo");
            Assert.AreEqual("Foo", context.NameAndDataAccessLayerName);

            LinqToEntitiesContext efContext = new LinqToEntitiesContext(typeof(NorthwindEntities));
            Assert.AreEqual("NorthwindEntities (Entity Framework)", efContext.NameAndDataAccessLayerName);
        }

        [TestMethod]
        [Description("BusinessLogicContext has no problem with derived ObjectContext")]
        public void BusinessLogicClass_Context_Inheritance()
        {
            LinqToEntitiesContext lteContext = new LinqToEntitiesContext(typeof(DataModels.ScenarioModels.ObjectContextInheritanceScenarios));
            Assert.IsGreaterThan(0, lteContext.Entities.Count());
        }

        [TestMethod]
        [Description("Tests if LinqToEntitiesDbContext works properly for DbContext entities")]
        public void BusinessLogicClass_Context_EFDbContextTest()
        {
            LinqToEntitiesDbContext dbContext = new LinqToEntitiesDbContext(typeof(DbContextModels.Northwind.DbCtxNorthwindEntities));
            Assert.AreEqual(11, dbContext.Entities.Count());
            Assert.IsTrue(dbContext.NeedToGenerateMetadataClasses);
        }

        //[TestMethod]
        //[Description("Tests if LinqToEntitiesDbContext works properly for EFCore entities")]
        //public void BusinessLogicClass_Context_EFCoreDbContextTest()
        //{
        //    // TODO: This won't work since LinqToEntitiesDbContext uses EF6 metadata model
        //    // Something similar to LinqToEntitiesDbContext but using EF Core's IModel must be built
        //    //LinqToEntitiesDbContext dbContext = new LinqToEntitiesDbContext(typeof(EFCoreModels.Northwind.EFCoreDbCtxNorthwindEntities));
        //    //Assert.AreEqual(11, dbContext.Entities.Count());
        //    //Assert.IsTrue(dbContext.NeedToGenerateMetadataClasses);
        //    throw new NotImplementedException();
        //}

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
