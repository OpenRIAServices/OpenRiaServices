namespace Microsoft.ServiceModel.DomainServices.Tools
{
    /// <summary>
    /// General purpose interface to allow errors and warnings to be logged
    /// with sufficient information to identify the location of the error in
    /// source code.
    /// </summary>
    internal interface ILoggingService : ILogger
    {
        /// <summary>
        /// Logs the given message as an error, together with information about the source location.
        /// </summary>
        /// <param name="message">The message to log as an error.</param>
        /// <param name="subcategory">The optional description of the error type.</param>
        /// <param name="errorCode">The optional error code.</param>
        /// <param name="helpKeyword">The optional help keyword.</param>
        /// <param name="file">The optional path to the file containing the error.</param>
        /// <param name="lineNumber">The zero-relative line number in the <paramref name="file"/> where the error begins.</param>
        /// <param name="columnNumber">The zero-relative column number in the <paramref name="file"/> where the error begins.</param>
        /// <param name="endLineNumber">The zero-relative line number in the <paramref name="file"/> where the error ends.</param>
        /// <param name="endColumnNumber">The zero-relative column number in the <paramref name="file"/> where the error ends.</param>
        void LogError(string message, string subcategory, string errorCode, string helpKeyword, string file, int lineNumber, int columnNumber, int endLineNumber, int endColumnNumber);

        /// <summary>
        /// Logs the given message as an warning, together with information about the source location.
        /// </summary>
        /// <param name="message">The message to log as an error.</param>
        /// <param name="subcategory">The optional description of the error type.</param>
        /// <param name="errorCode">The optional error code.</param>
        /// <param name="helpKeyword">The optional help keyword.</param>      
        /// <param name="file">The optional path to the file containing the error.</param>
        /// <param name="lineNumber">The zero-relative line number in the <paramref name="file"/> where the error begins.</param>
        /// <param name="columnNumber">The zero-relative column number in the <paramref name="file"/> where the error begins.</param>
        /// <param name="endLineNumber">The zero-relative line number in the <paramref name="file"/> where the error ends.</param>
        /// <param name="endColumnNumber">The zero-relative column number in the <paramref name="file"/> where the error ends.</param>
        void LogWarning(string message, string subcategory, string errorCode, string helpKeyword, string file, int lineNumber, int columnNumber, int endLineNumber, int endColumnNumber);
    }
}
