using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Cities;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpenRiaServices.Client.Test
{
    [TestClass]
    public class DomainOperationExceptionTests
    {
        [TestMethod]
        [Description("Validates all ctors")]
        public void DomainOperationException_Ctors()
        {
            // Parameterless ctor
            DomainOperationException doe = new DomainOperationException();
            Assert.IsNotNull(doe.Message, "Default msg s/n/b null");
            Assert.IsNull(doe.InnerException, "InnerException s/b null");
            Assert.IsNull(doe.StackTrace, "Default stack trace s/b null");
            Assert.AreEqual(0, doe.ErrorCode, "Error code s/b 0");
            Assert.AreEqual(OperationErrorStatus.ServerError, doe.Status, "Default status s/b ServerError");
            Assert.IsFalse(doe.ValidationErrors.Any(), "default validationErrors should be empty");

            // ctor(message)
            doe = new DomainOperationException("message");
            Assert.AreEqual("message", doe.Message, "ctor(msg) failed");
            Assert.IsNull(doe.InnerException, "InnerException s/b null");
            Assert.IsNull(doe.StackTrace, "Default stack trace s/b null");
            Assert.AreEqual(0, doe.ErrorCode, "Error code s/b 0");
            Assert.AreEqual(OperationErrorStatus.ServerError, doe.Status, "Default status s/b ServerError");
            Assert.IsFalse(doe.ValidationErrors.Any(), "default validationErrors should be empty");

            // ctor(message, status)
            doe = new DomainOperationException("message", OperationErrorStatus.Unauthorized);
            Assert.AreEqual("message", doe.Message, "ctor(msg) failed");
            Assert.IsNull(doe.InnerException, "InnerException s/b null");
            Assert.IsNull(doe.StackTrace, "Default stack trace s/b null");
            Assert.AreEqual(0, doe.ErrorCode, "Error code s/b 0");
            Assert.AreEqual(OperationErrorStatus.Unauthorized, doe.Status, "ctor(msg, status) failed status");
            Assert.IsFalse(doe.ValidationErrors.Any(), "default validationErrors should be empty");

            // ctor(message, status, errCode)
            doe = new DomainOperationException("message", OperationErrorStatus.Unauthorized, 5);
            Assert.AreEqual("message", doe.Message, "ctor(msg) failed");
            Assert.IsNull(doe.InnerException, "InnerException s/b null");
            Assert.IsNull(doe.StackTrace, "Default stack trace s/b null");
            Assert.AreEqual(5, doe.ErrorCode, "Error code failed");
            Assert.AreEqual(OperationErrorStatus.Unauthorized, doe.Status, "ctor(msg, status) failed status");
            Assert.IsFalse(doe.ValidationErrors.Any(), "default validationErrors should be empty");

            // ctor(message, status, errCode, stackTrace)
            doe = new DomainOperationException("message", OperationErrorStatus.Unauthorized, 5, "stackTrace");
            Assert.AreEqual("message", doe.Message, "ctor(msg) failed");
            Assert.IsNull(doe.InnerException, "InnerException s/b null");
            Assert.AreEqual("stackTrace", doe.StackTrace, "StackTrace failed");
            Assert.AreEqual(5, doe.ErrorCode, "Error code failed");
            Assert.AreEqual(OperationErrorStatus.Unauthorized, doe.Status, "ctor(msg, status) failed status");
            Assert.IsFalse(doe.ValidationErrors.Any(), "default validationErrors should be empty");

            // ctor(message, innerException)
            InvalidOperationException ioe = new InvalidOperationException("ioe");
            doe = new DomainOperationException("message", ioe);
            Assert.AreEqual("message", doe.Message, "ctor(msg) failed");
            Assert.AreSame(ioe, doe.InnerException, "InnerException failed");
            Assert.IsNull(doe.StackTrace, "Default stack trace s/b null");
            Assert.AreEqual(0, doe.ErrorCode, "Error code s/b 0");
            Assert.AreEqual(OperationErrorStatus.ServerError, doe.Status, "Default status s/b ServerError");
            Assert.IsFalse(doe.ValidationErrors.Any(), "default validationErrors should be empty");

            // ctor(message, doe)
            DomainOperationException doe2 = new DomainOperationException("message", OperationErrorStatus.Unauthorized, 5, "stackTrace");
            doe = new DomainOperationException("mm", doe2);
            Assert.AreEqual("mm", doe.Message, "ctor(doe) failed message");
            Assert.IsNull(doe.InnerException, "InnerException s/b null");
            Assert.AreEqual("stackTrace", doe.StackTrace, "StackTrace failed");
            Assert.AreEqual(5, doe.ErrorCode, "Error code failed");
            Assert.AreEqual(OperationErrorStatus.Unauthorized, doe.Status, "ctor(msg, status) failed status");
            Assert.IsFalse(doe.ValidationErrors.Any(), "default validationErrors should be empty");

            // ctor(message, validationerrors)
            var validationErrors = new List<ValidationResult>() {new ValidationResult("validation message")};
            doe = new DomainOperationException("message", validationErrors);
            Assert.AreEqual("message", doe.Message, "ctor(message, validationerrors) failed message");
            Assert.IsNull(doe.InnerException, "InnerException s/b null");
            Assert.IsNull(doe.StackTrace, "Default stack trace s/b null");
            Assert.AreEqual(0, doe.ErrorCode, "Error code s/b 0");
            Assert.AreEqual(OperationErrorStatus.ValidationFailed, doe.Status, "ctor(message, validationerrors) status s/b ValidationFailed");
            CollectionAssert.AreEqual(validationErrors, doe.ValidationErrors.ToList());
        }

        [TestMethod]
        [Description("Validates all settable properties")]
        public void DomainOperationException__Properties()
        {
            DomainOperationException doe = new DomainOperationException();

            doe.ErrorCode = 5;
            Assert.AreEqual(5, doe.ErrorCode, "failed to set ErrorCode");

            doe.Status = OperationErrorStatus.ValidationFailed;
            Assert.AreEqual(OperationErrorStatus.ValidationFailed, doe.Status, "failed to set Status");
        }

    }
}
