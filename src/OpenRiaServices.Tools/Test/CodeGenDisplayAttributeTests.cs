using System.ComponentModel.DataAnnotations;
using OpenRiaServices.Server.Test.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestDomainServices;
using System;
using System.Globalization;

namespace OpenRiaServices.Tools.Test
{
    /// <summary>
    /// Summary description for domain service code gen
    /// </summary>
    [TestClass]
    public class CodeGenDisplayAttributeTests
    {
        [TestMethod]
        [Description("DomainService with Resourced [Display] strings succeeds")]
        public void CodeGen_Attribute_DisplayAttribute_Resourced()
        {
            MockSharedCodeService sts = TestHelper.CreateCommonMockSharedCodeService();
            string generatedCode = TestHelper.GenerateCodeAssertSuccess("C#", new Type[] {typeof(Mock_CG_DisplayAttr_Entity_Shared_ResourceType_DomainService)}, null, sts);
            TestHelper.AssertGeneratedCodeContains(generatedCode, @"[Display(Description=""Resource4"", Name=""Resource2"", Prompt=""Resource3"", ResourceType=typeof(Mock_CG_DisplayAttr_Shared_ResourceType), ShortName=""Resource1"")]");
            TestHelper.AssertGeneratedCodeContains(generatedCode, @"[Display(Description=""Literal4"", Name=""Literal2"", Prompt=""Literal3"", ShortName=""Literal1"")]");
        }

        [TestMethod]
        [Description("DomainService with Resourced [Display] strings in a different namespace succeeds")]
        public void CodeGen_Attribute_DisplayAttribute_Resourced_DifferentNamespace()
        {
            MockSharedCodeService sts = TestHelper.CreateCommonMockSharedCodeService();
            string generatedCode = TestHelper.GenerateCodeAssertSuccess("C#", new Type[] {typeof(Mock_CG_DisplayAttr_Entity_Shared_ResourceType_DifferentNamespace_DomainService) }, null, sts);
            TestHelper.AssertGeneratedCodeContains(generatedCode, @"[Display(Description=""Resource4"", Name=""Resource2"", Prompt=""Resource3"", ResourceType=typeof(Mock_CG_DisplayAttr_Shared_ResourceType_DifferentNamespace), ShortName=""Resource1"")]");
            TestHelper.AssertGeneratedCodeContains(generatedCode, @"[Display(Description=""Literal4"", Name=""Literal2"", Prompt=""Literal3"", ShortName=""Literal1"")]");
        }

        [TestMethod]
        [Description("DomainService with Resourced [Display] strings logs error if resource type is private")]
        public void CodeGen_Attribute_DisplayAttribute_Fail_Private_ResourceType()
        {
            ConsoleLogger logger = new ConsoleLogger();
            string generatedCode = TestHelper.GenerateCode("C#", typeof(Mock_CG_DisplayAttr_Entity_Private_ResourceType_DomainService), logger);

            string expectedExceptionMessage = "Cannot retrieve property 'Name' because localization failed.  Type 'OpenRiaServices.Tools.Test.Mock_CG_DisplayAttr_Private_ResourceType' is not public or does not contain a public static string property with the name 'Resource2'.";

            AttributeBuilderException expectedException = new AttributeBuilderException(
                    new InvalidOperationException(expectedExceptionMessage),
                    typeof(DisplayAttribute),
                    "Name");

            string expectedBuildWarning = string.Format(
                    CultureInfo.CurrentCulture,
                    Resource.ClientCodeGen_Attribute_ThrewException_CodeTypeMember,
                    expectedException.Message,
                    "TheResourcedProperty",
                    typeof(Mock_CG_DisplayAttr_Entity_Private_ResourceType).Name,
                    expectedException.InnerException.Message);

            TestHelper.AssertGeneratedCodeContains(generatedCode, expectedException.Message);
            TestHelper.AssertContainsWarnings(logger, expectedBuildWarning);
        }
    }

    public class Mock_CG_DisplayAttr_Entity_Shared_ResourceType_DomainService : GenericDomainService<Mock_CG_DisplayAttr_Entity_Shared_ResourceType> { }

    public partial class Mock_CG_DisplayAttr_Entity_Shared_ResourceType
    {
        [Key]
        // Demonstrates fully resourced display with all resources corresponding to properties
        [Display(ResourceType = typeof(Mock_CG_DisplayAttr_Shared_ResourceType), ShortName = "Resource1", Name = "Resource2", Prompt = "Resource3", Description = "Resource4")]
        public string TheResourcedProperty { get; set; }

        // Demonstrates non-resourced literals
        [Display(ShortName = "Literal1", Name = "Literal2", Prompt = "Literal3", Description = "Literal4")]
        public string TheLiteralProperty { get; set; }
    }

    public class Mock_CG_DisplayAttr_Entity_Shared_ResourceType_DifferentNamespace_DomainService : GenericDomainService<Mock_CG_DisplayAttr_Entity_Shared_ResourceType_DifferentNamespace> { }

    public partial class Mock_CG_DisplayAttr_Entity_Shared_ResourceType_DifferentNamespace
    {
        [Key]
        // Demonstrates fully resourced display with all resources corresponding to properties
        [Display(ResourceType = typeof(DifferentNamespace.Mock_CG_DisplayAttr_Shared_ResourceType_DifferentNamespace), ShortName = "Resource1", Name = "Resource2", Prompt = "Resource3", Description = "Resource4")]
        public string TheResourcedProperty { get; set; }

        // Demonstrates non-resourced literals
        [Display(ShortName = "Literal1", Name = "Literal2", Prompt = "Literal3", Description = "Literal4")]
        public string TheLiteralProperty { get; set; }
    }

    public class Mock_CG_DisplayAttr_Shared_ResourceType
    {
        public static string Resource1 { get { return "string1"; } }
        public static string Resource2 { get { return "string2"; } }
        public static string Resource3 { get { return "string3"; } }
        public static string Resource4 { get { return "string4"; } }
    }

    internal class Mock_CG_DisplayAttr_Private_ResourceType
    {
        public static string Resource1 { get { return "string1"; } }
        public static string Resource2 { get { return "string2"; } }
        public static string Resource3 { get { return "string3"; } }
        public static string Resource4 { get { return "string4"; } }
    }

    public partial class Mock_CG_DisplayAttr_Entity_Unshared_ResourceType
    {
        public Mock_CG_DisplayAttr_Entity_Unshared_ResourceType() { }

        [Key]
        [Display(ResourceType = typeof(Mock_CG_DisplayAttr_Unshared_ResourceType), ShortName = "Resource1", Name = "Resource2", Prompt = "Resource3", Description = "Resource4")]
        public string TheResourcedProperty { get; set; }

        [Display(ShortName = "Literal1", Name = "Literal2", Prompt = "Literal3", Description = "Literal4")]
        public string TheLiteralProperty { get; set; }

    }

    public class Mock_CG_DisplayAttr_Entity_Private_ResourceType_DomainService : GenericDomainService<Mock_CG_DisplayAttr_Entity_Private_ResourceType> { }

    public partial class Mock_CG_DisplayAttr_Entity_Private_ResourceType
    {
        public Mock_CG_DisplayAttr_Entity_Private_ResourceType() { }

        [Key]
        [Display(ResourceType = typeof(Mock_CG_DisplayAttr_Private_ResourceType), ShortName = "Resource1", Name = "Resource2", Prompt = "Resource3", Description = "Resource4")]
        public string TheResourcedProperty { get; set; }
    }

    public class Mock_CG_DisplayAttr_Unshared_ResourceType
    {
        public static string Resource1 { get { return "string1"; } }
        public static string Resource2 { get { return "string2"; } }
        public static string Resource3 { get { return "string3"; } }
        public static string Resource4 { get { return "string4"; } }
    }

    public class Mock_CG_Attr_Shared_ResourceType
    {
        public static string StringResource { get { return "fred"; } }
    }

}

namespace DifferentNamespace
{
    public class Mock_CG_DisplayAttr_Shared_ResourceType_DifferentNamespace
    {
        public static string Resource1 { get { return "string1"; } }
        public static string Resource2 { get { return "string2"; } }
        public static string Resource3 { get { return "string3"; } }
        public static string Resource4 { get { return "string4"; } }
    }
}