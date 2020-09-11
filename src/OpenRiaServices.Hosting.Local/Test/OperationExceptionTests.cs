using System.Collections.Generic;
using System.Linq;
using OpenRiaServices.Client.Test;
using OpenRiaServices.Server;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpenRiaServices.Hosting.Local.Test
{
    using TestDescription = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;

    [TestClass]
    public class OperationExceptionTests
    {
        [TestMethod]
        [TestDescription("Verifies that the OperationException constructors report argument errors as expected.")]
        public void Constructor_InvalidArgs()
        {
            ValidationResultInfo nullError = null;
            ExceptionHelper.ExpectArgumentNullException(
                () => new OperationException("message", nullError),
                "operationError");

            IEnumerable<ValidationResultInfo> nullErrors = null;
            ExceptionHelper.ExpectArgumentNullException(
                () => new OperationException("message", nullErrors),
                "operationErrors");
        }

        [TestMethod]
        [TestDescription("Verifies that the OperationException constructors set state as expected.")]
        public void Constructor_SetState()
        {
            ValidationResultInfo error = new ValidationResultInfo();
            ValidationResultInfo[] errors = new[] { error };
            OperationException exception;

            exception = new OperationException("message", error);

            Assert.AreEqual("message", exception.Message);
            Assert.AreEqual(1, exception.OperationErrors.Count());
            Assert.AreSame(error, exception.OperationErrors.Single());

            exception = new OperationException("message", errors);

            Assert.AreEqual("message", exception.Message);
            Assert.AreEqual(1, exception.OperationErrors.Count());
            Assert.AreSame(error, exception.OperationErrors.Single());
        }
    }
}
