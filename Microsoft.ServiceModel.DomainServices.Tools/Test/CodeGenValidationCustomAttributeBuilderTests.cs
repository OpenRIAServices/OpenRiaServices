using System;
using System.ComponentModel.DataAnnotations;
using OpenRiaServices.DomainServices.Server.Test.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestDescription = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;

namespace OpenRiaServices.DomainServices.Tools.Test
{
    /// <summary>
    /// Summary description for domain service code gen
    /// </summary>
    [TestClass]
    public class CodeGenValidationCustomAttributeBuilderTests
    {
        [TestMethod]
        [TestDescription("Code gen for [StringLength] with valid resource type and name succeeds")]
        public void CodeGen_Attribute_StringLength_Valid_ResourceType_And_Name()
        {
            MockSharedCodeService sts = TestHelper.CreateCommonMockSharedCodeService();

            string generatedCode = TestHelper.GenerateCodeAssertSuccess("C#", new Type[] { typeof(Mock_CG_Attr_Entity_StringLength_Valid_DomainService) }, null, sts);
            TestHelper.AssertGeneratedCodeContains(generatedCode,
                "[StringLength(10, ErrorMessageResourceName=\"TheResource\", ErrorMessageResourceType=typeof(Mock_CG_Attr_Entity_StringLength_ResourceType))] " +
                "public string StringProperty");
        }

        [TestMethod]
        [TestDescription("Code gen for [StringLength] with valid resource type but invalid property name fails.")]
        public void CodeGen_Attribute_StringLength_Invalid_PropertyName()
        {
            string generatedCode = TestHelper.GenerateCodeAssertSuccess("C#", typeof(Mock_CG_Attr_Entity_StringLength_Invalid_PropertyName_DomainService));
            TestHelper.AssertGeneratedCodeContains(generatedCode,
                " // " + Resource.ClientCodeGen_Attribute_FailedToGenerate +
                " // " + 
                " // - " + string.Format(Resource.ClientCodeGen_ValidationAttribute_ResourcePropertyNotFound, typeof(StringLengthAttribute), "InvalidPropertyName", typeof(Mock_CG_Attr_Entity_StringLength_ResourceType)) +
                " // [StringLengthAttribute(10, ErrorMessageResourceName = \"InvalidPropertyName\", ErrorMessageResourceType = typeof(OpenRiaServices.DomainServices.Tools.Test.Mock_CG_Attr_Entity_StringLength_ResourceType))]" +
                " // [DataMember()] public string StringProperty");
        }

        [TestMethod]
        [TestDescription("Code gen for [StringLength] with missing resource type fails")]
        public void CodeGen_Attribute_StringLength_Fail_No_ResourceType()
        {
            string generatedCode = TestHelper.GenerateCodeAssertSuccess("C#", typeof(Mock_CG_Attr_Entity_StringLength_Missing_ResourceType_DomainService));

            TestHelper.AssertGeneratedCodeContains(
                generatedCode,
                Resource.ClientCodeGen_Attribute_FailedToGenerate,
                string.Format(Resource.ClientCodeGen_ValidationAttribute_Requires_ResourceType_And_Name, typeof(StringLengthAttribute), "<unspecified>", "TheResource"));
        }

        /// <summary>
        /// Code gen for [StringLength] with missing resource name fails.
        /// </summary>
        [TestMethod]
        [TestDescription("Code gen for [StringLength] with missing resource name fails.")]
        public void CodeGen_Attribute_StringLength_Fail_No_ResourceName()
        {
            string generatedCode = TestHelper.GenerateCodeAssertSuccess("C#", typeof(Mock_CG_Attr_Entity_StringLength_Missing_ResourceName_DomainService));

            TestHelper.AssertGeneratedCodeContains(generatedCode,
                " // " + Resource.ClientCodeGen_Attribute_FailedToGenerate +
                " // " +
                " // - " + string.Format(Resource.ClientCodeGen_ValidationAttribute_Requires_ResourceType_And_Name, typeof(StringLengthAttribute), "Mock_CG_Attr_Entity_StringLength_ResourceType", "<unspecified>") +
                " // [StringLengthAttribute(10, ErrorMessageResourceType = typeof(OpenRiaServices.DomainServices.Tools.Test.Mock_CG_Attr_Entity_StringLength_ResourceType))]" +
                " // ");
        }

        /// <summary>
        /// Code gen for [StringLength] with both resource identifiers missing.
        /// </summary>
        [TestMethod]
        [TestDescription("Code gen for [StringLength] with both resource identifiers missing.")]
        public void CodeGen_Attribute_StringLength_Valid_MissingBoth()
        {
            string generatedCode = TestHelper.GenerateCodeAssertSuccess("C#", typeof(Mock_CG_Attr_Entity_StringLength_Missing_Both_DomainService));
        }
    }

    #region Mock Entities

    public class Mock_CG_Attr_Entity_StringLength_Missing_ResourceType_DomainService : GenericDomainService<Mock_CG_Attr_Entity_StringLength_Missing_ResourceType> { }

    public partial class Mock_CG_Attr_Entity_StringLength_Missing_ResourceType
    {
        [Key]
        public int KeyField { get; set; }

        // ErrorMessageResourceType is missing from [StringLength]
        [StringLength(10, ErrorMessageResourceName = "TheResource")]
        public string StringProperty { get; set; }
    }

    public class Mock_CG_Attr_Entity_StringLength_Missing_ResourceName_DomainService : GenericDomainService<Mock_CG_Attr_Entity_StringLength_Missing_ResourceName> { }

    public partial class Mock_CG_Attr_Entity_StringLength_Missing_ResourceName
    {
        [Key]
        public int KeyField { get; set; }

        // ErrorMessageResourceName is missing from [StringLength]
        [StringLength(10, ErrorMessageResourceType = typeof(Mock_CG_Attr_Entity_StringLength_ResourceType))]
        public string StringProperty { get; set; }
    }

    public class Mock_CG_Attr_Entity_StringLength_Missing_Both_DomainService : GenericDomainService<Mock_CG_Attr_Entity_StringLength_Missing_Both> { }

    public partial class Mock_CG_Attr_Entity_StringLength_Missing_Both
    {
        [Key]
        public int KeyField { get; set; }

        // ErrorMessageResourceName is missing from [StringLength]
        [StringLength(10)]
        public string StringProperty { get; set; }
    }

    public class Mock_CG_Attr_Entity_StringLength_Valid_DomainService : GenericDomainService<Mock_CG_Attr_Entity_StringLength_Valid> { }

    public partial class Mock_CG_Attr_Entity_StringLength_Valid
    {
        [Key]
        public int KeyField { get; set; }

        [StringLength(10, ErrorMessageResourceType = typeof(Mock_CG_Attr_Entity_StringLength_ResourceType), ErrorMessageResourceName = "TheResource")]
        public string StringProperty { get; set; }
    }

    public class Mock_CG_Attr_Entity_StringLength_Invalid_PropertyName_DomainService : GenericDomainService<Mock_CG_Attr_Entity_StringLength_Invalid_PropertyName> { }

    public partial class Mock_CG_Attr_Entity_StringLength_Invalid_PropertyName
    {
        [Key]
        public int KeyField { get; set; }

        [StringLength(10, ErrorMessageResourceType = typeof(Mock_CG_Attr_Entity_StringLength_ResourceType), ErrorMessageResourceName = "InvalidPropertyName")]
        public string StringProperty { get; set; }
    }

    public class Mock_CG_Attr_Entity_StringLength_ResourceType
    {
        public static string TheResource { get { return "theResource"; } }
    }

    #endregion Mock Entities
}
