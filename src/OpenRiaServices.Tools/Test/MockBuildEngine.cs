using System.Collections;
using Microsoft.Build.Framework;
using ConsoleLogger = OpenRiaServices.Server.Test.Utilities.ConsoleLogger;

namespace OpenRiaServices.Tools.Test
{
    class MockBuildEngine : IBuildEngine
    {
        private readonly ConsoleLogger _consoleLogger = new ConsoleLogger();

        public ConsoleLogger ConsoleLogger
        {
            get
            {
                return this._consoleLogger;
            }
        }

        #region IBuildEngine Members

        public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties, IDictionary targetOutputs)
        {
            return false;
        }

        public int ColumnNumberOfTaskNode
        {
            get { return 0; }
        }

        public bool ContinueOnError
        {
            get { return true; }
        }

        public int LineNumberOfTaskNode
        {
            get { return 0; }
        }

        public void LogCustomEvent(CustomBuildEventArgs e)
        {
        }

        public void LogErrorEvent(BuildErrorEventArgs e)
        {
            this.ConsoleLogger.LogError(e.Message);
        }

        public void LogMessageEvent(BuildMessageEventArgs e)
        {
            this.ConsoleLogger.LogMessage(e.Message);
        }

        public void LogWarningEvent(BuildWarningEventArgs e)
        {
            this.ConsoleLogger.LogWarning(e.Message);
        }

        public string ProjectFileOfTaskNode
        {
            get { return null; }
        }

        #endregion
    }
}
