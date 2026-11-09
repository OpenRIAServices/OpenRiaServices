extern alias SSmDsClient;
using System;
using System.Reflection;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpenRiaServices.Test
{
    using ExceptionHandlingUtility = SSmDsClient::OpenRiaServices.ExceptionHandlingUtility;

    [TestClass]
    public class ExceptionHandlingUtilityTests
    {
        [TestMethod]
        [Description("Check if IsFatal returns true for all fatal exceptions")]
        public void CheckFatalExceptions()
        {            
            Assert.IsTrue(ExceptionHandlingUtility.IsFatal(new OutOfMemoryException()));
#if !SILVERLIGHT
            Assert.IsFalse(ExceptionHandlingUtility.IsFatal(new InsufficientMemoryException()));
#endif

            Assert.IsTrue(ExceptionHandlingUtility.IsFatal(new TypeInitializationException("foo", new OutOfMemoryException())));
            Assert.IsTrue(ExceptionHandlingUtility.IsFatal(new TargetInvocationException(new OutOfMemoryException())));
        }
    }
}
