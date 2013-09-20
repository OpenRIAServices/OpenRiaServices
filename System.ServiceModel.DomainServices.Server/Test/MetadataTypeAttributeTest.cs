using System.ComponentModel.DataAnnotations;
using System.Windows.Data.Test.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Web.DomainObjects.Test
{
    [TestClass]
    public class MetadataTypeAttributeTest {
        [TestMethod]
        public void Constructor_NullValue() {
            ExceptionHelper.ExpectArgumentNullException(delegate() {
                new MetadataTypeAttribute(null);
            }, "metadataClassType");
        }
    }
}
