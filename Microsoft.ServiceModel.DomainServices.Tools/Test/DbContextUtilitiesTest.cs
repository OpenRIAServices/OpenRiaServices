using System;
using System.Data.Entity;
using System.Reflection;
using CodeFirstModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.ServiceModel.DomainServices.Tools.Test
{
    [TestClass]
    public class DbContextUtilitiesTest
    {
        [TestMethod]
        [Description("Test various utility methods in DbContextUtilities")]
        public void TestDbContextUtilitiesMethods()
        {
            Type dbContextTypeRef = DbContextUtilities.GetDbContextTypeReference(typeof(EFCFNorthwindEntities));
            Assert.AreEqual(typeof(DbContext), dbContextTypeRef);

            Type dbContextType = DbContextUtilities.GetDbContextType(typeof(TestDomainServices.EFCF.Northwind));
            Assert.AreEqual(typeof(EFCFNorthwindEntities), dbContextType);

            Type dbSetType = DbContextUtilities.LoadTypeFromAssembly(typeof(DbContext).Assembly, typeof(DbSet<>).FullName);
            Assert.IsNotNull(dbSetType);
            Assert.AreEqual(dbSetType, typeof(DbSet<>));
        }

        [TestMethod]
        [Description("Compares DbContextUtilities.CompareWithSystemType() method")]
        public void TestCompareSystemTypesUtilityMethod()
        {
            Assert.IsTrue(DbContextUtilities.CompareWithSystemType(typeof(DbSet<>), typeof(DbSet<>).FullName));
            Assert.IsFalse(DbContextUtilities.CompareWithSystemType(typeof(TestDomainServices.RoundtripOriginal_TestEntity), typeof(TestDomainServices.RoundtripOriginal_TestEntity).FullName));
            Assert.IsFalse(DbContextUtilities.CompareWithSystemType(null, typeof(DbSet<>).FullName));
            Assert.IsFalse(DbContextUtilities.CompareWithSystemType(typeof(DbSet<>), null));
        }
    }
}
