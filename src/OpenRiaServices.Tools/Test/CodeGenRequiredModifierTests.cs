#if NET7_0_OR_GREATER //required modifier does not work before net7
using System.ComponentModel.DataAnnotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace OpenRiaServices.Tools.Test;

/// <summary>
/// CodeGen tests for the required modifier
/// </summary>
[TestClass]
public class CodeGenRequiredModifierTests
{
    [TestMethod]
    [Description("CodeGen does not apply compiler exclusive attribute for required")]
    public void CodeGen_Required_Modifier_Dont_Use_Compiler_Attributes()
    {
        MockSharedCodeService sts = TestHelper.CreateCommonMockSharedCodeService();
        string generatedCode = TestHelper.GenerateCodeAssertSuccess("C#", new Type[] {typeof(Mock_CG_Required_Entity_DomainService) }, null, sts);
        TestHelper.AssertGeneratedCodeDoesNotContain(generatedCode, "RequiredMember");
    }
}

public class Mock_CG_Required_Entity_DomainService : GenericDomainService<Mock_CG_Required_Entity> { }

public class Mock_CG_Required_Entity
{
    [Key]
    public required string RequiredProperty { get; set; }
}
#endif
