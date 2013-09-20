using System.ComponentModel.DataAnnotations;
using System.ServiceModel.DomainServices.Server.Test.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.ServiceModel.DomainServices.Tools.Test
{
    using DescriptionAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;

    /// <summary>
    /// Summary description for domain service code gen
    /// </summary>
    [TestClass]
    public class CodeGenEditableAttributeTests
    {
        [TestMethod]
        [Description("DomainService with [Editable(...)] code gens properly")]
        public void CodeGen_Attribute_EditableAttribute()
        {
            string generatedCode = TestHelper.GenerateCodeAssertSuccess("C#", typeof(Mock_CG_Attr_Entity_Editable_DomainService));
            TestHelper.AssertGeneratedCodeContains(generatedCode, "[Editable(true)]  public string EditableTrue");
            TestHelper.AssertGeneratedCodeContains(generatedCode, "[Editable(false)] public string EditableFalse");
            TestHelper.AssertGeneratedCodeContains(generatedCode, "[Editable(true)]  public string EditableTrue_AllowInitialValueTrue");
            TestHelper.AssertGeneratedCodeContains(generatedCode, "[Editable(true, AllowInitialValue=false)] public string EditableTrue_AllowInitialValueFalse");
            TestHelper.AssertGeneratedCodeContains(generatedCode, "[Editable(false, AllowInitialValue=true)]  public string EditableFalse_AllowInitialValueTrue");
            TestHelper.AssertGeneratedCodeContains(generatedCode, "[Editable(false)] public string EditableFalse_AllowInitialValueFalse");
        }
    }

    public class Mock_CG_Attr_Entity_Editable_DomainService : GenericDomainService<Mock_CG_Attr_Entity_Editable> { }

    public partial class Mock_CG_Attr_Entity_Editable
    {
        [Key]
        public int KeyField { get; set; }

        [Editable(true)]
        public string EditableTrue { get; set; }

        [Editable(false)]
        public string EditableFalse { get; set; }

        [Editable(true, AllowInitialValue = true)]
        public string EditableTrue_AllowInitialValueTrue { get; set; }

        [Editable(true, AllowInitialValue = false)]
        public string EditableTrue_AllowInitialValueFalse { get; set; }

        [Editable(false, AllowInitialValue = true)]
        public string EditableFalse_AllowInitialValueTrue { get; set; }

        [Editable(false, AllowInitialValue = false)]
        public string EditableFalse_AllowInitialValueFalse { get; set; }
    }
}
