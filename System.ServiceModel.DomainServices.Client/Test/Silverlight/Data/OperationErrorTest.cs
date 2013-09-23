using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// Note: this test only lives on the client because VSTT starts hanging when running unit tests
// if this test is linked into the server test project. For now, we only need to run from the client
// since there is no behavior difference of ValidationResultInfo on client/server.

namespace OpenRiaServices.DomainServices.Client.Test
{
    [TestClass]
    public class OperationErrorTest
    {
        [TestMethod]
        [Description("Verify ValidationResultInfo getters and setters")]
        public void OperationError_Sanity()
        {
            // test default ctor
            ValidationResultInfo error = new ValidationResultInfo();
            Assert.IsNull(error.Message);
            Assert.AreEqual<int>(0, error.ErrorCode);
            Assert.IsNull(error.StackTrace);

            // test ctor with message/member argument
            error = new ValidationResultInfo("my error message", new string[] { "foo" });
            Assert.AreEqual("my error message", error.Message);
            Assert.AreEqual<int>(0, error.ErrorCode);
            Assert.IsNull(error.StackTrace);
            Assert.AreEqual(1, error.SourceMemberNames.Count());
            Assert.IsTrue(error.SourceMemberNames.Contains("foo"));

            // test ctor with all arguments
            error = new ValidationResultInfo("message", 10, "stacktrace", new string[] { "foo" });
            Assert.AreEqual("message", error.Message);
            Assert.AreEqual(10, error.ErrorCode);
            Assert.AreEqual("stacktrace", error.StackTrace);
            Assert.AreEqual(1, error.SourceMemberNames.Count());
            Assert.IsTrue(error.SourceMemberNames.Contains("foo"));
        }

        [TestMethod]
        [Description("Verify ValidationResultInfo implements IEquatable properly")]
        public void OperationError_IEquatable()
        {
            ValidationResultInfo error1 = new ValidationResultInfo("message", 10, "stacktrace", new string[] { "foo" });
            ValidationResultInfo error2 = new ValidationResultInfo("message", 10, "stacktrace", new string[] { "foo" });
            ValidationResultInfo error3 = new ValidationResultInfo("message2", 10, "stacktrace", new string[] { "foo" });
            ValidationResultInfo error4 = new ValidationResultInfo("message2", 2, null, new string[] { "foo" });

            // compare same object ref
            Assert.IsTrue(((IEquatable<ValidationResultInfo>)error1).Equals(error1));

            // compare different object ref but same data fields
            Assert.IsTrue(((IEquatable<ValidationResultInfo>)error1).Equals(error2));

            // compare objects with 1 different field
            Assert.IsFalse(((IEquatable<ValidationResultInfo>)error1).Equals(error3));

            // compare objects with all different fields
            Assert.IsFalse(((IEquatable<ValidationResultInfo>)error1).Equals(error4));

            // verify List.Contains uses our implementation of IEquatable.Equals
            List<ValidationResultInfo> errors = new List<ValidationResultInfo>() { error1, error3, error4 };
            Assert.IsTrue(errors.Contains(error2));
        }
    }
}
