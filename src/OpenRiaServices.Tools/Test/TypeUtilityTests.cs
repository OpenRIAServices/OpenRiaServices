extern alias SystemWebDomainServices;
extern alias TextTemplate;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpenRiaServices.Tools.Test
{
    [TestClass]
    public class TypeUtilityTests
    {
        [TestMethod]
        [Description("Checks that Open Ria Services assembly is identified as a system assembly.")]
        public void TestOpenRiaServicesAssemblyIsSystemAssembly()
        {
            var assemblyName = typeof (TextTemplate::OpenRiaServices.Tools.TextTemplate.ClientCodeGenerator).Assembly.FullName;
            bool result = SystemWebDomainServices::OpenRiaServices.TypeUtility.IsSystemAssembly(assemblyName);
            Assert.IsTrue(result, "The assembly " + assemblyName + " is not identified as a system assembly");
        }

        [TestMethod]
        public void SimpleStructCannotContainComplexOrEntityMembers()
        {
            Assert.IsFalse(SystemWebDomainServices::OpenRiaServices.TypeUtility.IsSimpleStructType(typeof(StructWithComplexMember)));
            Assert.IsFalse(SystemWebDomainServices::OpenRiaServices.TypeUtility.IsPredefinedType(typeof(StructWithComplexMember)));
            Assert.IsFalse(SystemWebDomainServices::OpenRiaServices.TypeUtility.IsSimpleStructType(typeof(StructWithEntityMember)));
            Assert.IsFalse(SystemWebDomainServices::OpenRiaServices.TypeUtility.IsPredefinedType(typeof(StructWithEntityMember)));
        }

        public struct StructWithComplexMember
        {
            public TestComplexObject Value { get; set; }
        }

        public struct StructWithEntityMember
        {
            public TestEntity Value { get; set; }
        }

        public class TestComplexObject
        {
            public string Value { get; set; }
        }

        public class TestEntity
        {
            public int Id { get; set; }
        }
    }
}