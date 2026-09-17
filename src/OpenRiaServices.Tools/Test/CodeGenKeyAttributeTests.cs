using System;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using OpenRiaServices.Server.Test.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimpleStructs;

namespace OpenRiaServices.Tools.Test
{
    /// <summary>
    /// Test that have to deal with [Key] attribute on entities
    /// </summary>
    [TestClass]
    public class CodeGenKeyAttributeTests
    {
        [TestMethod]
        [Description("DomainService with Entity missing [Key] property fails")]
        public void CodeGen_Attribute_KeyAttribute_Fail_Missing()
        {
            ConsoleLogger logger = new ConsoleLogger();
            string generatedCode = TestHelper.GenerateCode("C#", typeof(Mock_CG_Attr_Entity_Missing_Key_DomainService), logger);
            Assert.IsTrue(string.IsNullOrEmpty(generatedCode));
            TestHelper.AssertContainsErrors(logger, string.Format(OpenRiaServices.Server.Resource.Entity_Has_No_Key_Properties, typeof(Mock_CG_Attr_Entity_Missing_Key).Name, typeof(Mock_CG_Attr_Entity_Missing_Key_DomainService).Name));
        }

        [TestMethod]
        [Description("GetIdentity method is not generated for an Entity with KeyAttribute SharedAttribute on the same property")]
        public void CodeGen_Attribute_KeyAttribute_SharedAttribute()
        {
            ConsoleLogger logger = new ConsoleLogger();

            // For this test, consider K2 shared and K1 not shared
            ISharedCodeService sts = new MockSharedCodeService(
                    new Type[] { typeof(Mock_CG_Attr_Entity_Shared_Key) },
                    new MethodInfo[] {  typeof(Mock_CG_Attr_Entity_Shared_Key).GetProperty("K2").GetGetMethod() },
                    Array.Empty<string>());

            string generatedCode = TestHelper.GenerateCode("C#", new Type[] { typeof(Mock_CG_Attr_Entity_Shared_Key_DomainService) }, logger, sts);
            Assert.IsFalse(string.IsNullOrEmpty(generatedCode));
            TestHelper.AssertGeneratedCodeDoesNotContain(generatedCode, "GetIdentity");
        }

        [TestMethod]
        public void CodeGen_Attribute_KeyAttribute_SimpleStructWithExcludedMember_Fails()
        {
            ConsoleLogger logger = new ConsoleLogger();
            ISharedCodeService sharedCodeService = new MockSharedCodeService(
                new[] { typeof(Mock_CG_ExcludedMemberSimpleStruct) },
                new[] { typeof(Mock_CG_Attr_Entity_ExcludedSimpleStructKey).GetProperty(nameof(Mock_CG_Attr_Entity_ExcludedSimpleStructKey.K1)).GetGetMethod() },
                Array.Empty<string>());

            string generatedCode = TestHelper.GenerateCode(
                "C#",
                new[] { typeof(Mock_CG_Attr_Entity_ExcludedSimpleStructKey_DomainService) },
                logger,
                sharedCodeService);

            Assert.IsTrue(string.IsNullOrEmpty(generatedCode));
            TestHelper.AssertContainsErrors(
                logger,
                string.Format(
                    Resource.EntityCodeGen_EntityKey_KeyTypeNotSupported,
                    typeof(Mock_CG_Attr_Entity_ExcludedSimpleStructKey),
                    nameof(Mock_CG_Attr_Entity_ExcludedSimpleStructKey.K1),
                    typeof(Mock_CG_ExcludedMemberSimpleStruct)));
        }

    }

    public class Mock_CG_Attr_Entity_Missing_Key_DomainService : GenericDomainService<Mock_CG_Attr_Entity_Missing_Key> { }

    public partial class Mock_CG_Attr_Entity_Missing_Key
    {
        public string StringProperty { get; set; }
    }

    public class Mock_CG_Attr_Entity_Shared_Key_DomainService : GenericDomainService<Mock_CG_Attr_Entity_Shared_Key> { }

    public partial class Mock_CG_Attr_Entity_Shared_Key
    {
        [Key]
        public int K1 { get; set; }

        [Key]
        public string K2 { get; set; }
    }

    public class Mock_CG_Attr_Entity_ExcludedSimpleStructKey_DomainService : GenericDomainService<Mock_CG_Attr_Entity_ExcludedSimpleStructKey> { }

    public partial class Mock_CG_Attr_Entity_ExcludedSimpleStructKey
    {
        [Key]
        public Mock_CG_ExcludedMemberSimpleStruct K1 { get; set; }
    }

}
