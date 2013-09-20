using System.Collections.Generic;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.TemplateWizard;
using IServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace Microsoft.VisualStudio.ServiceModel.DomainServices.Tools
{
    public class BusinessApplicationServerTemplateWizard : IWizard
    {
        private DTE2 _dte2;
        EnvDTE.Project _project;

        #region IWizard Members

        public void BeforeOpeningFile(ProjectItem projectItem)
        {
        }

        public void ProjectFinishedGenerating(Project project)
        {
            this._project = project;
        }

        public void ProjectItemFinishedGenerating(ProjectItem projectItem)
        {
        }

        public void RunFinished()
        {
#if VS10
#else
            // ported from $/DevDiv/PU/WPT/venus/mvw/Wizard/TemplateWizard.cs
            const int LOCALHOST = 1;
            const int NOTWEBSITE = -1;

            // WebSiteType = 1 for HTTP/FileSystem; WebSiteType=2 for FTPWebSite, WebSiteType=3 for FPSE
            const int FTPWEBSITE = 2;
            const int FPSEWEBSITE = 3;

            int webSiteType = NOTWEBSITE;

            bool isLocalHost = false;
            bool isIISExpress = false;

            webSiteType = MVWUtilities.GetProjectProperty<int>(this._project, "WebSiteType", NOTWEBSITE);
            if (webSiteType == NOTWEBSITE)
            {
                isLocalHost = MVWUtilities.GetProjectProperty<bool>(this._project, "WebApplication.UseIIS", false);
                if (isLocalHost)
                {
                    isIISExpress = MVWUtilities.GetProjectProperty<bool>(this._project, "WebApplication.IsUsingIISExpress", false);
                }
            }
            else
            {
                isLocalHost = (LOCALHOST == webSiteType);
                if (isLocalHost)
                {
                    isIISExpress = MVWUtilities.GetProjectProperty<bool>(this._project, "IsUsingIISExpress", false);
                }
            }

            string webConfigPath = this._project.ProjectItems.Item("Web.config").FileNames[0];
            bool isDataSourceLocalDB = webSiteType != FTPWEBSITE && webSiteType != FPSEWEBSITE && (!isLocalHost || isIISExpress);
            using (LocalDBUtil localdb = new LocalDBUtil((IServiceProvider)this._dte2, webConfigPath))
            {
                localdb.UpdateDBConnectionStringsForNewProject(isDataSourceLocalDB, this._project.Name);
            }
#endif
        }

        public void RunStarted(object automationObject,
            Dictionary<string, string> replacementsDictionary,
            WizardRunKind runKind, object[] customParams)
        {
            // read the client project name from the global storage, we had cached it in the root wizard
            this._dte2 = (DTE2)automationObject;
            replacementsDictionary["$safeclientprojectname$"] = (string)this._dte2.Globals["safeclientprojectname"];

            // Add a '$'-free replacement token so that if we need to templatize a file name we can check
            // the file in TFS
            replacementsDictionary["__safeclientprojectname__"] = replacementsDictionary["$safeclientprojectname$"];

            // Add a replacement token for the Silverlight Runtime Version that is installed on this machine
            // If the version number could not be found, then apply our default.
            replacementsDictionary["$runtime_version$"] = GetSilverlightRuntimeVersion(automationObject) ??
                string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0.0}", TemplateUtilities.DefaultSilverlightVersion);
        }

        public bool ShouldAddProjectItem(string filePath)
        {
            return true;
        }
        #endregion

        /// <summary>
        /// Gets the Silverlight Runtime Version installed on the local machine.
        /// </summary>
        /// <param name="automationObject">Service provider.</param>
        /// <returns>
        /// A string representing a Silverlight version number.  When the full version number is
        /// found on the machine, it will be returned.  When not found, <c>null</c> will be returned.
        /// </returns>
        private static string GetSilverlightRuntimeVersion(object automationObject)
        {
            // Gets the latest version of Silverlight supported by the tools
            string silverlightToolsVersion = TemplateUtilities.GetSilverlightVersion(automationObject);

            // Open up the sub-key for the Silverlight SDK's Reference Assemblies for the specified version
            using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                string.Format(System.Globalization.CultureInfo.InvariantCulture, @"SOFTWARE\Microsoft\Microsoft SDKs\Silverlight\{0}\ReferenceAssemblies", silverlightToolsVersion)))
            {
                if (key != null)
                {
                    // Attempt to get the runtime version
                    string str = key.GetValue("SLRuntimeInstallVersion") as string;

                    if (!string.IsNullOrEmpty(str))
                    {
                        // Return it if found
                        return str;
                    }
                }
            }

            // If we couldn't find the version in the registry, return null so the caller can
            // decide how to handle that scenario.
            return null;
        }
    }
}
