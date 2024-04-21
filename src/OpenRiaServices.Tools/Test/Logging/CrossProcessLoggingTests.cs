using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
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
            using (var client = new CrossProcessLoggingWriter(server.ClientSafePipeHandle))
            {
                var log = (ILoggingService)client;
                log.LogMessage("Message");
                log.LogWarning("Warning");
                log.LogError("Error");
                log.LogError("LongError", "SubCat1", "errorCode", "helpKeyword", "file", lineNumber: 1, columnNumber: 2, endLineNumber: 3, endColumnNumber: 4);
                log.LogWarning("LongWarning", "SubCat2", "errorCode2", "helpKeyword2", "file2", lineNumber: 5, columnNumber: 6, endLineNumber: 7, endColumnNumber: 8);
            }

            var destination = new ConsoleLogger();
            server.WriteLogsTo(destination, CancellationToken.None);

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

        /// <summary>
        /// Validate that important complex exceptions are unwrapped so all "inner" details are part of message
        /// </summary>
        [TestMethod]
        public void ExceptionsAreForwarded()
        {
            using var server = new CrossProcessLoggingServer();
            Exception ex;
            Type[] typeLoadClasses = [typeof(CrossProcessLoggingServer), typeof(CrossProcessLoggingWriter)];
            List<Exception> allExceptions = new();

            try
            {
                allExceptions.Add(new ArgumentNullException("AME:param", "ANE:message"));
                allExceptions.Add(new InvalidOperationException("IOE:message"));

                allExceptions.Add(new ReflectionTypeLoadException(typeLoadClasses, allExceptions.ToArray(), "RTE:message"));
                allExceptions.Add(new ArgumentException("AE:message", allExceptions.Last()));
                allExceptions.Add(new InvalidCastException("ICE:message"));

                allExceptions.Add(new AggregateException("AGG:message", allExceptions[4], allExceptions[3]));

                // Initialize callstack
                throw allExceptions.Last();
            }
            catch (AggregateException e)
            {
                ex = e;
            }

            using (var client = new CrossProcessLoggingWriter(server.ClientSafePipeHandle))
            {
                var log = (ILoggingService)client;
                log.LogException(ex);
            }

            var destination = new ConsoleLogger();
            server.WriteLogsTo(destination, CancellationToken.None);

            string errorMessage = destination.ErrorMessages.Single();

            StringAssert.Contains(errorMessage, ex.StackTrace);

            // Exception Type and message should be logged for all exceptions in the hierarchy
            foreach (var exception in allExceptions) 
            {
                StringAssert.Contains(errorMessage, exception.Message);
                StringAssert.Contains(errorMessage, exception.GetType().Name);

                if (ex is ArgumentException ae)
                    StringAssert.Contains(errorMessage, ae.ParamName);
            }

            foreach(var type in typeLoadClasses)
            {
                StringAssert.Contains(errorMessage, type.FullName);
            }
        }
    }
}
