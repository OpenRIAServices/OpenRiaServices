using System.ComponentModel.DataAnnotations;
using System.Globalization;
using OpenRiaServices.DomainServices.Server.Test.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

public class Mock_CG_Attr_Entity_Missing_Namespace_DomainService : OpenRiaServices.DomainServices.Tools.Test.GenericDomainService<Mock_CG_Attr_Entity_Missing_Namespace> { }

public partial class Mock_CG_Attr_Entity_Missing_Namespace
{
    [Key]
    public string StringProperty { get; set; }
}

namespace OpenRiaServices.DomainServices.Tools.Test
{
    /// <summary>
    /// Summary description for domain service code gen
    /// </summary>
    [TestClass]
    public class CodeGenNamespaceTests
    {
        [TestMethod]
        [Description("DomainService with Entity outside of namespace fails")]
        public void CodeGen_Namespace_Fails_Missing()
        {
            string error = string.Format(CultureInfo.CurrentCulture, Resource.ClientCodeGen_Namespace_Required, "Mock_CG_Attr_Entity_Missing_Namespace");
            TestHelper.GenerateCodeAssertFailure("C#", typeof(Mock_CG_Attr_Entity_Missing_Namespace_DomainService), error);
        }
    }
}
