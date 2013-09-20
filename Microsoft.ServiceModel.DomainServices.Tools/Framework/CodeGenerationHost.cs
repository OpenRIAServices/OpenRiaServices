using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.ServiceModel.DomainServices.Tools.SharedTypes;

namespace Microsoft.ServiceModel.DomainServices.Tools
{
    /// <summary>
    /// Internal implementation of <see cref="ICodeGenerationHost"/> used for client
    /// proxy code generation.
    /// </summary>
    internal class CodeGenerationHost : ICodeGenerationHost
    {
        private ILoggingService _loggingService;
        private ISharedCodeService _sharedCodeService;

        /// <summary>
        /// Initializes a new instance of the <see cref="CodeGenerationHost"/> class.
        /// </summary>
        /// <param name="loggingService">The <see cref="ILoggingService"/> object used to report all errors and warnings.</param>
        /// <param name="sharedCodeService">The <see cref="ISharedCodeService"/> object used to determine which code elements
        /// are shared between server and client projects.</param>
        internal CodeGenerationHost(ILoggingService loggingService, ISharedCodeService sharedCodeService)
        {
            Debug.Assert(loggingService != null, "loggingService cannot be null");
            Debug.Assert(sharedCodeService != null, "sharedCodeService cannot be null");

            this._loggingService = loggingService;
            this._sharedCodeService = sharedCodeService;
        }

        private ILoggingService LoggingService { get { return this._loggingService; } }

        private ISharedCodeService SharedCodeService { get { return this._sharedCodeService; } }

        #region ILogger members

        public bool HasLoggedErrors
        {
            get { return this.LoggingService.HasLoggedErrors; }
        }

        public void LogError(string message)
        {
            this.LoggingService.LogError(message);
        }

        public void LogWarning(string message)
        {
            this.LoggingService.LogWarning(message);
        }

        public void LogMessage(string message)
        {
            this.LoggingService.LogMessage(message);
        }

        #endregion // ILogger members

        #region ILoggingService members
        public void LogError(string message, string subcategory, string errorCode, string helpKeyword, string file, int lineNumber, int columnNumber, int endLineNumber, int endColumnNumber)
        {
            this.LoggingService.LogError(message, subcategory, errorCode, helpKeyword, file, lineNumber, columnNumber, endLineNumber, endColumnNumber);
        }

        public void LogWarning(string message, string subcategory, string errorCode, string helpKeyword, string file, int lineNumber, int columnNumber, int endLineNumber, int endColumnNumber)
        {
            this.LoggingService.LogWarning(message, subcategory, errorCode, helpKeyword, file, lineNumber, columnNumber, endLineNumber, endColumnNumber);
        }


        #endregion // ILoggingService members

        #region ISharedCodeService members

        public CodeMemberShareKind GetTypeShareKind(string typeName)
        {
            return this.SharedCodeService.GetTypeShareKind(typeName);
        }

        public CodeMemberShareKind GetPropertyShareKind(string typeName, string propertyName)
        {
            return this.SharedCodeService.GetPropertyShareKind(typeName, propertyName);
        }

        public CodeMemberShareKind GetMethodShareKind(string typeName, string methodName, IEnumerable<string> parameterTypeNames)
        {
            return this.SharedCodeService.GetMethodShareKind(typeName, methodName, parameterTypeNames);
        }

        #endregion // ISharedCodeService members
    }
}
