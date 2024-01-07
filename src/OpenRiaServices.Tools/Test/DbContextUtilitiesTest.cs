using System;
#if NETCOREAPP
using Microsoft.EntityFrameworkCore;
#else
using System.Data.Entity;
#endif
using System.Reflection;
using CodeFirstModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpenRiaServices.Tools.Test
{
    [TestClass]
    public class DbContextUtilitiesTest
    {
        [TestMethod]
        [Description("Test various utility methods in DbContextUtilities")]
        public void TestDbContextUtilitiesMethods()
        {
#if NETCOREAPP
            Type dbContextTypeRef = DbContextUtilities.GetDbContextTypeReference(typeof(EFCoreModels.Northwind.EFCoreDbCtxNorthwindEntities));
#else
            Type dbContextTypeRef = DbContextUtilities.GetDbContextTypeReference(typeof(EFCFNorthwindEntities));
#endif
            Assert.AreEqual(typeof(DbContext), dbContextTypeRef);

#if NETCOREAPP
            Type dbContextType = DbContextUtilities.GetDbContextType(typeof(TestDomainServices.EFCore.Northwind));
            Assert.AreEqual(typeof(EFCoreModels.Northwind.EFCoreDbCtxNorthwindEntities), dbContextType);
#else
            Type dbContextType = DbContextUtilities.GetDbContextType(typeof(TestDomainServices.EFCF.Northwind));
            Assert.AreEqual(typeof(EFCFNorthwindEntities), dbContextType);
#endif


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
