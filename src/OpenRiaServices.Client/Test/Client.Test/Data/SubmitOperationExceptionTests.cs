using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharedEntities;

namespace OpenRiaServices.Client.Test
{
    [TestClass]
    public class SubmitOperationExceptionTests
    {
        [TestMethod]
        [Description("Validates all ctors")]
        public void DomainOperationException_Ctors()
        {
            var entities = new List<Entity> {new EntityX()};
            var emtpy = new ReadOnlyCollection<Entity>(entities);
            var changeSet = new EntityChangeSet(emtpy, emtpy, emtpy);

            // ctor(changeSet, message, status)
            var soe = new SubmitOperationException(changeSet, "message", OperationErrorStatus.Unauthorized);
            Assert.AreEqual(changeSet, soe.ChangeSet);
            Assert.AreEqual("message", soe.Message, "ctor(msg) failed");
            Assert.IsNull(soe.InnerException, "InnerException s/b null");
            Assert.IsNull(soe.StackTrace, "Default stack trace s/b null");
            Assert.AreEqual(0, soe.ErrorCode, "Error code s/b 0");
            Assert.AreEqual(OperationErrorStatus.Unauthorized, soe.Status, "ctor(msg, status) failed status");
            Assert.IsFalse(soe.ValidationErrors.Any(), "default validationErrors should be empty");

            // ctor(changeSet, message, innerException)
            var ioe = new InvalidOperationException("ioe");
            soe = new SubmitOperationException(changeSet, "message", ioe);
            Assert.AreEqual(changeSet, soe.ChangeSet);
            Assert.AreEqual("message", soe.Message, "ctor(msg) failed");
            Assert.AreSame(ioe, soe.InnerException, "InnerException failed");
            Assert.IsNull(soe.StackTrace, "Default stack trace s/b null");
            Assert.AreEqual(0, soe.ErrorCode, "Error code s/b 0");
            Assert.AreEqual(OperationErrorStatus.ServerError, soe.Status, "Default status s/b ServerError");
            Assert.IsFalse(soe.ValidationErrors.Any(), "default validationErrors should be empty");

            // ctor(changeSet, message, doe)
            var doe2 = new DomainOperationException("message", OperationErrorStatus.Unauthorized, 5, "stackTrace");
            soe = new SubmitOperationException(changeSet, "mm", doe2);
            Assert.AreEqual(changeSet, soe.ChangeSet); 
            Assert.AreEqual("mm", soe.Message, "ctor(doe) failed message");
            Assert.IsNull(soe.InnerException, "InnerException s/b null");
            Assert.AreEqual("stackTrace", soe.StackTrace, "StackTrace failed");
            Assert.AreEqual(5, soe.ErrorCode, "Error code failed");
            Assert.AreEqual(OperationErrorStatus.Unauthorized, soe.Status, "ctor(msg, status) failed status");
            Assert.IsFalse(soe.ValidationErrors.Any(), "default validationErrors should be empty");
        }
    }
}
