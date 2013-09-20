using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web.Compilation;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.ServiceModel.DomainServices.Tools.Validation;

namespace Microsoft.ServiceModel.DomainServices.Tools
{
    /// <summary>
    /// Task that validates the integrity of the <see cref="System.ServiceModel.DomainServices.Server.DomainService"/>s exposed by the target Web Application
    /// </summary>
    public class ValidateDomainServicesTask : Task
    {
        #region Member Fields

        private readonly ILoggingService _loggingService;
        private string _projectDirectory;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidateDomainServicesTask"/>
        /// </summary>
        public ValidateDomainServicesTask()
        {
            this._loggingService = new TaskLoggingHelperLoggingService(this.Log);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the path to the project file
        /// </summary>
        [Required]
        public string ProjectPath { get; set; }

        /// <summary>
        /// Gets or sets the assembly containing <see cref="System.ServiceModel.DomainServices.Server.DomainService"/> types
        /// </summary>
        [Required]
        public ITaskItem Assembly { get; set; }

        /// <summary>
        /// Gets or sets the list of reference assemblies that may contains <see cref="System.ServiceModel.DomainServices.Server.DomainService"/> types
        /// </summary>
        [Required]
        public ITaskItem[] ReferenceAssemblies { get; set; }

        /// <summary>
        /// Gets the absolute path to the project directory
        /// </summary>
        private string ProjectDirectory
        {
            get
            {
                if (this._projectDirectory == null)
                {
                    this._projectDirectory = Path.GetDirectoryName(this.ProjectPath);
                }
                return this._projectDirectory;
            }
        }

        /// <summary>
        /// Gets the <see cref="ILoggingService"/> for this task
        /// </summary>
        private ILoggingService LoggingService
        {
            get { return this._loggingService; }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Returns the absolute filename of the <see cref="ITaskItem"/>
        /// </summary>
        /// <param name="taskItem">The item to get the name for</param>
        /// <returns>The absolute filename</returns>
        private string GetFileName(ITaskItem taskItem)
        {
            string fileName = taskItem.ItemSpec;
            if (!Path.IsPathRooted(fileName))
            {
                fileName = Path.GetFullPath(Path.Combine(this.ProjectDirectory, fileName));
            }
            return fileName;
        }

        /// <summary>
        /// Invoked by MSBuild to run this task
        /// </summary>
        /// <returns><c>true</c> if task succeeds</returns>
        public override bool Execute()
        {
            this._projectDirectory = null;

            this.LoggingService.LogMessage(string.Format(CultureInfo.CurrentCulture, "DomainService Validation starting for project {0}", Path.GetFileName(this.ProjectPath)));
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            this.ValidateDomainServices();

            stopWatch.Stop();
            double secondsAsDouble = stopWatch.ElapsedMilliseconds / 1000.0;
            this.LoggingService.LogMessage(string.Format(CultureInfo.CurrentCulture, "DomainService Validation finished in {0} seconds", secondsAsDouble));

            // Any error sent to the log constitutes failure
            return !this.LoggingService.HasLoggedErrors;
        }

        /// <summary>
        /// Validates the integrity of the <see cref="System.ServiceModel.DomainServices.Server.DomainService"/>s exposed by the target Web Application
        /// in a separate <see cref="AppDomain"/>
        /// </summary>
        private void ValidateDomainServices()
        {
            IEnumerable<string> assemblies =
                new[] { this.GetFileName(this.Assembly) } .Concat(
                this.ReferenceAssemblies.Select(i => this.GetFileName(i)));

            this.WarnIfAssembliesDontExist(assemblies);

            using (ClientBuildManager cbm = new ClientBuildManager(/* appVirtualDir */ "/", this.ProjectDirectory))
            {
                // Surface a HttpRuntime initialization error that would otherwise manifest as a NullReferenceException
                // This can occur when the build environment is configured incorrectly
                if (System.Web.Hosting.HostingEnvironment.InitializationException != null)
                {
                    throw new InvalidOperationException(
                        Resource.HttpRuntimeInitializationError,
                        System.Web.Hosting.HostingEnvironment.InitializationException);
                }

                using (DomainServiceValidator validator = (DomainServiceValidator)cbm.CreateObject(typeof(DomainServiceValidator), false))
                {
                    // Transfer control to Web Application AppDomain to invoke the validator
                    validator.Validate(assemblies.ToArray(), this.LoggingService);
                }
            }
        }

        /// <summary>
        /// Checks the existence of files and logs a warning message for each that does not exist
        /// </summary>
        /// <param name="assemblies">The set of assembly names to test</param>
        private void WarnIfAssembliesDontExist(IEnumerable<string> assemblies)
        {
            foreach (string assembly in assemblies)
            {
                if (!File.Exists(assembly))
                {
                    this.LoggingService.LogWarning(string.Format(CultureInfo.CurrentCulture, Resource.ClientCodeGen_Input_Assembly_Not_Found, assembly));
                }
            }
        }

        #endregion

        #region Nested Classes

        /// <summary>
        /// <see cref="ILoggingService"/> implementation that writes to a <see cref="TaskLoggingHelper"/> and 
        /// handles cross-appdomain logging requests
        /// </summary>
        private class TaskLoggingHelperLoggingService : MarshalByRefObject, ILoggingService
        {
            private bool _hasLoggedErrors;
            private TaskLoggingHelper _log;

            public TaskLoggingHelperLoggingService(TaskLoggingHelper log)
            {
                if (log == null)
                {
                    throw new ArgumentNullException("log");
                }

                this._log = log;
            }

            public TaskLoggingHelper Log
            {
                get { return this._log; }
            }

            public void LogError(string message, string subcategory, string errorCode, string helpKeyword, string file, int lineNumber, int columnNumber, int endLineNumber, int endColumnNumber)
            {
                this._hasLoggedErrors = true;
                this.Log.LogError(subcategory, errorCode, helpKeyword, file, lineNumber, columnNumber, endLineNumber, endColumnNumber, message);
            }

            public void LogWarning(string message, string subcategory, string errorCode, string helpKeyword, string file, int lineNumber, int columnNumber, int endLineNumber, int endColumnNumber)
            {
                this.Log.LogWarning(subcategory, errorCode, helpKeyword, file, lineNumber, columnNumber, endLineNumber, endColumnNumber, message);
            }

            public bool HasLoggedErrors
            {
                get { return this._hasLoggedErrors; }
            }

            public void LogError(string message)
            {
                this._hasLoggedErrors = true;
                this.Log.LogError(message);
            }

            public void LogWarning(string message)
            {
                this.Log.LogWarning(message);
            }

            public void LogMessage(string message)
            {
                this.Log.LogMessage(message);
            }
        }

        #endregion
    }
}