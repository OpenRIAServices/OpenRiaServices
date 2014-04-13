using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using OpenRiaServices.DomainServices.Tools;

namespace OpenRiaServices.DomainServices.Server.Test.Utilities
{
    /// <summary>
    /// Console-based logger that implements <see cref="ILogger"/> interface,
    /// which can be consumed by code generator to log messages
    /// </summary>
    [System.Security.SecuritySafeCritical]
    public sealed class ConsoleLogger : ILoggingService
    {
        private List<string> _errorMsgs = new List<string>();
        private List<string> _warningMsgs = new List<string>();
        private List<string> _infoMsgs = new List<string>();
        private List<LogPacket> _errorPackets = new List<LogPacket>();
        private List<LogPacket> _warningPackets = new List<LogPacket>();

        /// <summary>
        /// Gets a value indicating whether any errors were logged.
        /// </summary>
        public bool HasLoggedErrors
        {
            get
            {
                return (this._errorMsgs.Count > 0);
            }
        }

        /// <summary>
        /// Log error message to Console
        /// </summary>
        /// <param name="message">message to be logged</param>
        public void LogError(string message)
        {
            Console.WriteLine(string.Format(CultureInfo.CurrentCulture, "Error: {0}", message));
            _errorMsgs.Add(message);
        }

        /// <summary>
        /// Log exception message to Console
        /// </summary>
        /// <param name="message">message to be logged</param>
        public void LogException(Exception ex)
        {
            Console.WriteLine(string.Format(CultureInfo.CurrentCulture, "Error: {0}", ex.Message));
            _errorMsgs.Add(ex.Message);
        }

        /// <summary>
        /// Log warning message to Console
        /// </summary>
        /// <param name="message">message to be logged</param>
        public void LogWarning(string message)
        {
            Console.WriteLine(string.Format(CultureInfo.CurrentCulture, "Warning: {0}", message));
            _warningMsgs.Add(message);
        }

        /// <summary>
        /// Log a message to Console
        /// </summary>
        /// <param name="message">message to be logged</param>
        public void LogMessage(string message)
        {
            Console.WriteLine(message);
            _infoMsgs.Add(message);
        }

        public void LogError(string message, string subCategory, string errorCode, string helpString, string file, int lineNumber, int columnNumber, int endLineNumber, int endColumnNumber)
        {
            this.LogError(message);
            LogPacket packet = new LogPacket()
            {
                Message = message,
                Subcategory = subCategory,
                ErrorCode = errorCode,
                HelpString = helpString,
                File = file,
                LineNumber = lineNumber,
                ColumnNumber = columnNumber,
                EndLineNumber = endLineNumber,
                EndColumnNumber = endColumnNumber
            };
            this._errorPackets.Add(packet);
        }

        public void LogWarning(string message, string subCategory, string errorCode, string helpString, string file, int lineNumber, int columnNumber, int endLineNumber, int endColumnNumber)
        {
            this.LogWarning(message);
            this.LogError(message);
            LogPacket packet = new LogPacket()
            {
                Message = message,
                Subcategory = subCategory,
                ErrorCode = errorCode,
                HelpString = helpString,
                File = file,
                LineNumber = lineNumber,
                ColumnNumber = columnNumber,
                EndLineNumber = endLineNumber,
                EndColumnNumber = endColumnNumber
            };
            this._warningPackets.Add(packet);
        }

        /// <summary>
        /// Gets a list of messages previously passed to LogError calls
        /// </summary>
        public List<string> ErrorMessages
        {
            get { return this._errorMsgs; }
        }

        /// <summary>
        /// Gets the list of <see cref="LogPacket"/>s logged as errors.
        /// </summary>
        public List<LogPacket> ErrorPackets
        {
            get
            {
                return this._errorPackets;
            }
        }

        /// <summary>
        /// Gets the list of <see cref="LogPacket"/>s logged as warnings.
        /// </summary>
        public List<LogPacket> WarningPackets
        {
            get
            {
                return this._warningPackets;
            }
        }


        /// <summary>
        /// Gets <see cref="ErrorMessages"/> as a single string
        /// </summary>
        public string Errors
        {
            get
            {
                return BuildString(this._errorMsgs);
            }
        }

        /// <summary>
        /// Gets a list of messages previously passed to LogWarning calls
        /// </summary>
        public List<string> WarningMessages
        {
            get { return this._warningMsgs; }
        }

        /// <summary>
        /// Gets <see cref="WarningMessages"/> as a single string
        /// </summary>
        public string Warnings
        {
            get
            {
                return BuildString(this._warningMsgs);
            }
        }

        /// <summary>
        /// Gets a list of messages previously passed to LogMessage calls
        /// </summary>
        public List<string> InfoMessages
        {
            get { return this._infoMsgs; }
        }

        /// <summary>
        /// Gets <see cref="InfoMessages"/> as a single string
        /// </summary>
        public string Infos
        {
            get
            {
                return BuildString(this._infoMsgs);
            }
        }

        /// <summary>
        /// Clears the internally cached messages collections
        /// </summary>
        public void Reset()
        {
            this._errorMsgs.Clear();
            this._warningMsgs.Clear();
            this._infoMsgs.Clear();
        }

        private static string BuildString(List<string> list)
        {
            StringBuilder sb = new StringBuilder();
            foreach (string s in list)
            {
                sb.AppendLine(s);
            }
            return sb.ToString();
        }

        public class LogPacket
        {
            public string Message { get; set; }
            public string Subcategory { get; set; }
            public string File { get; set; }
            public string HelpString { get; set; }
            public string ErrorCode { get; set; }
            public int LineNumber { get; set; }
            public int ColumnNumber { get; set; }
            public int EndLineNumber { get; set; }
            public int EndColumnNumber { get; set; }

            public override string ToString()
            {
                return "Message: " + this.Message + Environment.NewLine +
                       "Subcat: " + this.Subcategory + Environment.NewLine +
                       "Help: " + this.HelpString + Environment.NewLine +
                       "File: " + this.File + Environment.NewLine +
                       "Error: " + this.ErrorCode + Environment.NewLine +
                       "Line: " + this.LineNumber + Environment.NewLine +
                       "Col: " + this.ColumnNumber + Environment.NewLine +
                       "EndLine: " + this.EndLineNumber + Environment.NewLine +
                       "EndCol: " + this.EndColumnNumber + Environment.NewLine;
            }
        }
    }
}
