using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.Server.Test.Utilities;
using OpenRiaServices.Tools.Logging;

namespace OpenRiaServices.Tools.Test
{
    [TestClass]
    public class CrossProcessLoggingTests
    {
        [TestMethod]
        public void LogsAreForwarded()
        {
            using var server = new CrossProcessLoggingServer();
            using (var client = new CrossProcessLoggingWriter(server.PipeName))
            {
                var log = (ILoggingService)client;
                log.LogMessage("Message");
                log.LogWarning("Warning");
                log.LogError("Error");
                log.LogError("LongError", "SubCat1", "errorCode", "helpKeyword", "file", lineNumber: 1, columnNumber: 2, endLineNumber: 3, endColumnNumber: 4);
                log.LogWarning("LongWarning", "SubCat2", "errorCode2", "helpKeyword2", "file2", lineNumber: 5, columnNumber: 6, endLineNumber: 7, endColumnNumber: 8);
            }

            var destination = new ConsoleLogger();
            server.WriteLogsTo(destination);

            CollectionAssert.AreEqual(new[] { "Message" }, destination.InfoMessages);
            CollectionAssert.AreEqual(new[] { "Warning", "LongWarning" }, destination.WarningMessages);
            CollectionAssert.AreEqual(new[] { "Error", "LongError" }, destination.ErrorMessages);
            CollectionAssert.AreEqual(new[] { new ConsoleLogger.LogPacket()
                {
                    Message = "LongError",
                    Subcategory = "SubCat1",
                    ErrorCode = "errorCode",
                    HelpString = "helpKeyword",
                    File = "file",
                    LineNumber = 1,
                    ColumnNumber = 2,
                    EndLineNumber = 3,
                    EndColumnNumber = 4,
                }
            }, destination.ErrorPackets);
            CollectionAssert.AreEqual(new[] { new ConsoleLogger.LogPacket()
            {
                    Message = "LongWarning",
                    Subcategory = "SubCat2",
                    ErrorCode = "errorCode2",
                    HelpString = "helpKeyword2",
                    File = "file2",
                    LineNumber = 5,
                    ColumnNumber = 6,
                    EndLineNumber = 7,
                    EndColumnNumber = 8,
                }
            }, destination.WarningPackets);
        }

        [TestMethod]
        public void ExceptionsAreForwarded()
        {
            using var server = new CrossProcessLoggingServer();
            Exception ex;
            try
            {
                // Throw exeption to setup callstack, required for net framework
                throw new ArgumentException("exception message", new InvalidOperationException("inner message"));
            }
            catch (Exception e)
            {
                ex = e;
            }

            using (var client = new CrossProcessLoggingWriter(server.PipeName))
            {
                var log = (ILoggingService)client;
                log.LogException(ex);
            }

            var destination = new ConsoleLogger();
            server.WriteLogsTo(destination);

            string errorMessage = destination.ErrorMessages.Single();
            StringAssert.Contains(errorMessage, ex.Message);
            StringAssert.Contains(errorMessage, ex.InnerException.Message);
            StringAssert.Contains(errorMessage, ex.StackTrace);
        }
    }
}
