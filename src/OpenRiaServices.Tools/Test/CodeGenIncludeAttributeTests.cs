using System.ComponentModel.DataAnnotations;
using System.Globalization;
using OpenRiaServices.Server;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpenRiaServices.Tools.Test
{
    using DescriptionAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;
    using IgnoreAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.IgnoreAttribute;

    /// <summary>
    /// Summary description for domain service code gen
    /// </summary>
    [TestClass]
    public class CodeGenIncludeAttributeTests
    {
        [TestMethod]
        [Description("DomainService with [Include] succeeds")]
        public void CodeGen_Attribute_IncludeAttribute_Succeeds()
        {
            string generatedCode = TestHelper.GenerateCodeAssertSuccess("C#", typeof(Mock_CG_Attr_Entity_Include_Succeed_DomainService));

            // Generated property getter for Entity2
            TestHelper.AssertGeneratedCodeContains(generatedCode, "public Mock_CG_Attr_Entity_Include_Succeed_2 Entity2");
            
            // Generated partial type for included entity
            TestHelper.AssertGeneratedCodeContains(generatedCode, "public sealed partial class Mock_CG_Attr_Entity_Include_Succeed_2 : Entity");
        }

        [TestMethod]
        [Description("DomainService with [Include] yields error for non-entity types")]
        public void CodeGen_Attribute_IncludeAttribute_Fail_Not_Entity()
        {
            string error = string.Format(OpenRiaServices.Server.Resource.Invalid_Include_Invalid_Entity, "NotAnEntity", typeof(Mock_CG_Attr_Entity_Include).Name, "String", OpenRiaServices.Server.Resource.EntityTypes_Cannot_Be_Primitives);
            TestHelper.GenerateCodeAssertFailure("C#", typeof(Mock_CG_Attr_Entity_Include_DomainService), error);
        }

        [TestMethod]
        [Description("DomainService with [Include] on non-existant buddy class property yields error for non-entity types")]
        public void CodeGen_Attribute_IncludeAttribute_Fail_Buddy_Class_Wrong_Property()
        {
            // This resource is internal to DataAnnotations, so we evaluate correctness only for EN_US
            if (OpenRiaServices.Client.Test.UnitTestHelper.EnglishBuildAndOS)
            {
                string error = "The associated metadata type for type '" + typeof(Mock_CG_Attr_Entity_Include_Buddy).FullName + "' contains the following unknown properties or fields: NotAProperty. Please make sure that the names of these members match the names of the properties on the main type.";
                TestHelper.GenerateCodeAssertFailure("C#", typeof(Mock_CG_Attr_Entity_Include_Buddy_DomainService), error);
            }
        }

        [TestMethod]
        [Description("DomainService with projections on both sides of a bi-directional association works correctly (no StackOverflowException)")]
        public void CodeGen_Attribute_IncludeAttribute_Bidirectional_Projection()
        {
            string generatedCode = TestHelper.GenerateCodeAssertSuccess("C#", typeof(Mock_CG_Attr_Entity_Include_Bidirectional_Projection_DomainService));
        }

        [TestMethod]
        [Description("DomainService with a projection of a projection on a bi-directional association works as expected")]
        public void CodeGen_Attribute_IncludeAttribute_Bidirectional_Projection_Of_Projection()
        {
            string generatedCode = TestHelper.GenerateCodeAssertSuccess("C#", typeof(Mock_CG_Attr_Entity_Include_Bidirectional_Projection_Of_Projection_DomainService));
            TestHelper.AssertGeneratedCodeContains(generatedCode, "ProjectedTargetProp");
            TestHelper.AssertGeneratedCodeContains(generatedCode, "ProjectedSourceProp");
        }

        [TestMethod]
        [Description("DomainService with [Include] and [Exclude] succeeds but emits warning")]
        public void CodeGen_Attribute_IncludeAttribute_Exclude_Succeeds_With_Warning()
        {
            string warning = string.Format(OpenRiaServices.Tools.Resource.ClientCodeGen_Cannot_Have_Include_And_Exclude, "Entity2", typeof(Mock_CG_Attr_Entity_Include_Exclude));
            string generatedCode = TestHelper.GenerateCodeAssertWarnings("C#", typeof(Mock_CG_Attr_Entity_Include_Exclude_DomainService), warning);
        }

        [TestMethod]
        [Description("DomainService with multiple [Include]s on a property generates all")]
        public void CodeGen_Attribute_IncludeAttribute_Multiple()
        {
            string generatedCode = TestHelper.GenerateCodeAssertSuccess("C#", typeof(Mock_CG_Attr_Entity_Include_Multiple_DomainService));
            TestHelper.AssertGeneratedCodeContains(generatedCode,
                                                    @"private string _member1",
                                                    @"private string _member2",
                                                    @"private string _path1",
                                                    @"private string _path2",
                                                    @"public string Path1",
                                                    @"public string Path2");

        }
    }

    public class Mock_CG_Attr_Entity_Include_Succeed_DomainService : GenericDomainService<Mock_CG_Attr_Entity_Include_Succeed> { }

    public partial class Mock_CG_Attr_Entity_Include_Succeed
    {
        [Key]
        public int KeyField { get; set; }

        [Include]
        [Association("Mock_CG_Attr_Entity_Include_Succeed_2", "FK", "Key2Field", IsForeignKey = true)]
        public Mock_CG_Attr_Entity_Include_Succeed_2 Entity2 { get; set; }

        public int FK
        {
            get;
            set;
        }
    }

    public partial class Mock_CG_Attr_Entity_Include_Succeed_2
    {
        [Key]
        public int Key2Field { get; set; }

        public string String2Property { get; set; }

        public string Path1 { get; set; }
        public string Path2 { get; set; }
    }

    public class Mock_CG_Attr_Entity_Include_Multiple_DomainService : GenericDomainService<Mock_CG_Attr_Entity_Include_Multiple> { }

    public partial class Mock_CG_Attr_Entity_Include_Multiple
    {
        [Key]
        public int KeyField { get; set; }

        public string Path1 { get; set; }
        public string Path2 { get; set; }

        [Include]
        [Association("Mock_CG_Attr_Entity_Include_Succeed_2", "FK", "Key2Field", IsForeignKey = true)]
        [Include("Path1", "member1")]
        [Include("Path2", "member2")]
        public Mock_CG_Attr_Entity_Include_Succeed_2 Entity2 { get; set; }

        public int FK
        {
            get;
            set;
        }
    }

    public class Mock_CG_Attr_Entity_Include_Exclude_DomainService : GenericDomainService<Mock_CG_Attr_Entity_Include_Exclude> { }

    public partial class Mock_CG_Attr_Entity_Include_Exclude
    {
        [Key]
        public int KeyField { get; set; }

        [Include]
        [Association("Mock_CG_Attr_Entity_Include_Exclude_2", "FK", "Key2Field", IsForeignKey = true)]
        [Exclude]   // can't have both
        public Mock_CG_Attr_Entity_Include_Exclude_2 Entity2 { get; set; }

        public int FK
        {
            get;
            set;
        }
    }

    public partial class Mock_CG_Attr_Entity_Include_Exclude_2
    {
        [Key]
        public int Key2Field { get; set; }

        public string String2Property { get; set; }
    }

    public class Mock_CG_Attr_Entity_Include_DomainService : GenericDomainService<Mock_CG_Attr_Entity_Include> { }

    public partial class Mock_CG_Attr_Entity_Include
    {
        [Key]
        public int KeyField { get; set; }

        [Include]
        public string NotAnEntity { get; set; }
    }

    public class Mock_CG_Attr_Entity_Include_Buddy_DomainService : GenericDomainService<Mock_CG_Attr_Entity_Include_Buddy> { }

    [MetadataType(typeof(Include_Buddy))]
    public partial class Mock_CG_Attr_Entity_Include_Buddy
    {
        [Key]
        public int KeyField { get; set; }

        public string StringProperty { get; set; }
    }

    public static class Include_Buddy
    {
        [Include]
        public static object NotAProperty;
    }

    public class Mock_CG_Attr_Entity_Include_Bidirectional_Projection_DomainService : GenericDomainService<Mock_CG_Attr_Entity_Include_Bidirectional_Projection_Source> { }

    public class Mock_CG_Attr_Entity_Include_Bidirectional_Projection_Source
    {
        [Key]
        public string TheKey { get; set; }

        public string SourceProperty { get; set; } // we'll project this

        public string OtherId { get; set; } // FK to Other

        [Association("Derived_Other", "OtherId", "Id", IsForeignKey = true)]
        [Include]
        [Include("TargetProperty", "ProjectedTargetProp")] // project a field from target's root
        public Mock_CG_Attr_Entity_Include_Bidirectional_Projection_Target Target { get; set; }
    }

    public class Mock_CG_Attr_Entity_Include_Bidirectional_Projection_Target
    {
        [Key]
        public string Id { get; set; } // PK

        public string TargetProperty { get; set; }

        public string SourceKey { get; set; } // FK to source

        [Include("SourceProperty", "ProjectedSourceProp")]
        [Association("Derived_Other", "SourceKey", "TheKey")]
        public Mock_CG_Attr_Entity_Include_Bidirectional_Projection_Source Source { get; set; }
    }

    public class Mock_CG_Attr_Entity_Include_Bidirectional_Projection_Of_Projection_DomainService : GenericDomainService<Mock_CG_Attr_Entity_Include_Bidirectional_Projection_Of_Projection_Source> { }

    public class Mock_CG_Attr_Entity_Include_Bidirectional_Projection_Of_Projection_Source
    {
        [Key]
        public string TheKey { get; set; }

        public string SourceProperty { get; set; } // we'll project this

        public string OtherId { get; set; } // FK to Other

        [Association("Derived_Other", "OtherId", "Id", IsForeignKey = true)]
        [Include]
        [Include("TargetProperty", "ProjectedTargetProp")] // project a field from target's root
        public Mock_CG_Attr_Entity_Include_Bidirectional_Projection_Of_Projection_Target Target { get; set; }
    }

    public class Mock_CG_Attr_Entity_Include_Bidirectional_Projection_Of_Projection_Target
    {
        [Key]
        public string Id { get; set; } // PK

        public string TargetProperty { get; set; }

        public string SourceKey { get; set; } // FK to source

        [Include("ProjectedTargetProp", "ProjectedSourceProp")]
        [Association("Derived_Other", "SourceKey", "TheKey")]
        public Mock_CG_Attr_Entity_Include_Bidirectional_Projection_Of_Projection_Source Source { get; set; }
    }
}
