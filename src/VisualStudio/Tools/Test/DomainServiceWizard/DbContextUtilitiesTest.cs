using OpenRiaServices.Tools;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpenRiaServices.VisualStudio.DomainServices.Tools.Test.DomainServiceWizard
{
    [TestClass]
    public class DbContextUtilitiesTest
    {
        [TestMethod]
        [Description("Tests if IsDbContext() method returns true for DbContext types and false otherwise.")]
        public void TestIsDbContext()
        {
            Assert.IsTrue(typeof(CodeFirstModels.EFCFNorthwindEntities).IsDbContext());
            Assert.IsTrue(typeof(DbContextModels.Northwind.DbCtxNorthwindEntities).IsDbContext());
            Assert.IsFalse(typeof(NorthwindModel.NorthwindEntities).IsDbContext());
            Assert.IsFalse(typeof(ITestInterface).IsDbContext());
        }

        [TestMethod]
        [Description("Tests if the IsContextType() method returns true for DbContext/DataContext/ObjectContext types and false otherwise.")]
        public void TestIsContextType()
        {
            bool isDbContext;
            
            Assert.IsTrue(DomainServiceClassWizard.IsContextType(typeof(CodeFirstModels.EFCFNorthwindEntities), true, true, out isDbContext));
            Assert.IsTrue(isDbContext);

            Assert.IsTrue(DomainServiceClassWizard.IsContextType(typeof(NorthwindModel.NorthwindEntities), true, true, out isDbContext));
            Assert.IsFalse(isDbContext);
            
            Assert.IsTrue(DomainServiceClassWizard.IsContextType(typeof(DataTests.AdventureWorks.LTS.AdventureWorks), true, true, out isDbContext));
            Assert.IsFalse(isDbContext);
            
            Assert.IsFalse(DomainServiceClassWizard.IsContextType(typeof(CodeFirstModels.EFCFNorthwindEntities), true, false, out isDbContext));
            Assert.IsTrue(isDbContext);

            Assert.IsFalse(DomainServiceClassWizard.IsContextType(typeof(DataTests.AdventureWorks.LTS.AdventureWorks), false, true, out isDbContext));
            Assert.IsFalse(isDbContext);
            
            Assert.IsFalse(DomainServiceClassWizard.IsContextType(typeof(TestDomainServices.A), true, true, out isDbContext));
            Assert.IsFalse(isDbContext);
            
            Assert.IsFalse(DomainServiceClassWizard.IsContextType(typeof(TestDomainServices.EF.Northwind), true, true, out isDbContext));
            Assert.IsFalse(isDbContext);
        }

        [TestMethod]
        [Description("Tests if GetDbContext() returns the DbContext type if the current type derives from it and null otherwise.")]
        public void TestGetDbContextType()
        {
            OpenRiaServices.Tools.DbContextUtilities.ResetDbContextTypeReference();
            Assert.IsNull(OpenRiaServices.Tools.DbContextUtilities.GetDbContextTypeReference(typeof(ITestInterface)));

            OpenRiaServices.Tools.DbContextUtilities.ResetDbContextTypeReference();
            Assert.IsNotNull(OpenRiaServices.Tools.DbContextUtilities.GetDbContextTypeReference(typeof(CodeFirstModels.EFCFNorthwindEntities)));
        }
    }
    
    public interface ITestInterface
    {
        void DummyMethod();
    }
}
