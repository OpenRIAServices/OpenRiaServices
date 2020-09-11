using System.ComponentModel.DataAnnotations;
using OpenRiaServices.Server.Test.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpenRiaServices.Tools.Test
{
    using DescriptionAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;

    /// <summary>
    /// Summary description for domain service code gen
    /// </summary>
    [TestClass]
    public class CodeGenUIHintAttributeTests
    {
        [TestMethod]
        [Description("DomainService with [UIHint] code gens properly")]
        public void CodeGen_Attribute_UIHint()
        {
            string generatedCode = TestHelper.GenerateCodeAssertSuccess("C#", typeof(Mock_CG_Attr_Entity_UIHint_DomainService));
            TestHelper.AssertGeneratedCodeContains(generatedCode, @"[UIHint(""theUIHint"", ""thePresentationLayer"")]");
        }

        [TestMethod]
        [Description("DomainService with [UIHint] and set of control parameters code gens properly")]
        public void CodeGen_Attribute_UIHint_ControlParameters()
        {
            string generatedCode = TestHelper.GenerateCodeAssertSuccess("C#", typeof(Mock_CG_Attr_Entity_UIHint_ControlParameters_DomainService));
            TestHelper.AssertGeneratedCodeContains(generatedCode, 
                            @"[UIHint(""theUIHint"", ""thePresentationLayer"",",
                            @", ""key1"", 100",
                            @", ""key2"", ((double)(2D))");      // odd syntax reflect CodeDom workaround
        }

        [TestMethod]
        [Description("DomainService with [UIHint] and odd number of control parameters should fail gracefully")]
        [Ignore] // currently cannot run until Oryx fixes UiHint to not throw in its ctor
        public void CodeGen_Attribute_UIHint_ControlParameters_Fail_Odd_Count()
        {
            ConsoleLogger logger = new ConsoleLogger();
            string generatedCode = TestHelper.GenerateCode("C#", typeof(Mock_CG_Attr_Entity_UIHint_ControlParameters_Odd_DomainService), logger);
            TestHelper.AssertContainsErrors(logger, "xxx");
            Assert.AreEqual(string.Empty, generatedCode);
        }
    }

    public class Mock_CG_Attr_Entity_UIHint_DomainService : GenericDomainService<Mock_CG_Attr_Entity_UIHint> { }

    public partial class Mock_CG_Attr_Entity_UIHint
    {
        [Key]
        public int KeyField { get; set; }

        [UIHint("theUIHint", "thePresentationLayer")]
        public string StringProperty { get; set; }
    }

    public class Mock_CG_Attr_Entity_UIHint_ControlParameters_DomainService : GenericDomainService<Mock_CG_Attr_Entity_UIHint_ControlParameters> { }

    public partial class Mock_CG_Attr_Entity_UIHint_ControlParameters
    {
        [Key]
        public int KeyField { get; set; }

        [UIHint("theUIHint", "thePresentationLayer", "key1", 100, "key2", 2.0)]
        public string StringProperty { get; set; }
    }

    public class Mock_CG_Attr_Entity_UIHint_ControlParameters_Odd_DomainService : GenericDomainService<Mock_CG_Attr_Entity_UIHint_ControlParameters_Odd> { }

    public partial class Mock_CG_Attr_Entity_UIHint_ControlParameters_Odd
    {
        [Key]
        public int KeyField { get; set; }

        [UIHint("theUIHint", "thePresentationLayer", "key1", 100, "key2")]
        public string StringProperty { get; set; }
    }
}
