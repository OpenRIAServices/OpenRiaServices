using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Windows.Controls.Tests
{
    /// <summary>
    /// Tests <see cref="FilterDescriptor"/> members.
    /// </summary>
    [TestClass]
    public class FilterDescriptorTests : UnitTestBase
    {
        [TestMethod]
        [Description("Ensure all properties are set from the constructors")]
        public void FilterDescriptor_Constructors_Set_Properties()
        {
            FilterDescriptor fd = new FilterDescriptor("MyPropertyPath", FilterOperator.IsEqualTo, "MyValue");
            Assert.AreEqual("MyPropertyPath", fd.PropertyPath, "PropertyPath wasn't set properly");
            Assert.AreEqual(FilterOperator.IsEqualTo, fd.Operator, "Operator wasn't set properly");
            Assert.AreEqual("MyValue", fd.Value, "Value wasn't constructed and set properly");
        }
    }
}
