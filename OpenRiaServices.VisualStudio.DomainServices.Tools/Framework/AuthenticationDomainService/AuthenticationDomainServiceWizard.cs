using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.ManagedInterfaces9;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TemplateWizard;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace OpenRiaServices.VisualStudio.DomainServices.Tools
{
    class AuthenticationDomainServiceWizard : IWizard
    {
        #region IWizard Members

        private DTE2 _dte2;
        private Project _project;

        public void BeforeOpeningFile(ProjectItem projectItem)
        {
        }

        public void ProjectFinishedGenerating(Project project)
        {
        }

        public void ProjectItemFinishedGenerating(ProjectItem projectItem)
        {
            this._project = projectItem.ContainingProject;
        }

        public void RunFinished()
        {
            IOleServiceProvider oleServiceProvider = this.Dte2 as IOleServiceProvider;
            IVsApplicationConfigurationManager cfgMgr = null;
            IVsHierarchy vsHierarchy = null;

            using (ServiceProvider sp = new ServiceProvider(oleServiceProvider))
            {
                // Get the solution 
                IVsSolution sln = sp.GetService(typeof(IVsSolution)) as IVsSolution;
                if (sln == null)
                {
                    return;
                }

                cfgMgr = sp.GetService(typeof(IVsApplicationConfigurationManager)) as IVsApplicationConfigurationManager;
                if (cfgMgr == null)
                {
                    return;
                }

                int result = sln.GetProjectOfUniqueName(this._project.UniqueName, out vsHierarchy);
                if (result != 0 || vsHierarchy == null)
                {
                    return;
                }
            }

            // Return the current application's configuration file by using 
            // the IVsApplicationConfiguration APIs. Make sure that the 
            // instance that is returned is disposed of correctly in order 
            // to clean up any event hooks or docdatas.
            // Note that this interface is aware of source control and text buffers, so it
            // works even if the file is currently open and modified.
            using (IVsApplicationConfiguration appCfg = cfgMgr.GetApplicationConfiguration(vsHierarchy, Microsoft.VisualStudio.VSConstants.VSITEMID_ROOT))
            {
                // Do not do anything unless the file already exists, else we will create an empty one
                if (appCfg != null && appCfg.FileExists())
                {
                    System.Configuration.Configuration cfg = appCfg.LoadConfiguration();
                    if (cfg != null)
                    {
                        WebConfigUtil webConfigUtil = new WebConfigUtil(cfg);

                        // First check whether any work needs to done
                        bool addHttpModule = webConfigUtil.DoWeNeedToAddHttpModule();
                        bool addModuleToWebServer = webConfigUtil.DoWeNeedToAddModuleToWebServer();
                        bool setAspNetCompatiblity = !webConfigUtil.IsAspNetCompatibilityEnabled();
                        bool setMultipleSiteBindingsEnabled = !webConfigUtil.IsMultipleSiteBindingsEnabled();
                        bool addValidationSection = webConfigUtil.DoWeNeedToValidateIntegratedModeToWebServer();

                        // Modify the file only if we decided work is required
                        if (addHttpModule || addModuleToWebServer || setAspNetCompatiblity || setMultipleSiteBindingsEnabled || addValidationSection)
                        {
                            string domainServiceModuleName = WebConfigUtil.GetDomainServiceModuleTypeName();

                            // Check the file out from Source Code Control if it exists.
                            appCfg.QueryEditConfiguration();

                            if (addHttpModule)
                            {
                                webConfigUtil.AddHttpModule(domainServiceModuleName);
                            }

                            if (addModuleToWebServer)
                            {
                                webConfigUtil.AddModuleToWebServer(domainServiceModuleName);
                            }

                            if (setAspNetCompatiblity)
                            {
                                webConfigUtil.SetAspNetCompatibilityEnabled(true);
                            }

                            if (setMultipleSiteBindingsEnabled)
                            {
                                webConfigUtil.SetMultipleSiteBindingsEnabled(true);
                            }

                            if (addValidationSection)
                            {
                                webConfigUtil.AddValidateIntegratedModeToWebServer();
                            }

                            cfg.Save();
                        }
                    }
                }
            }
        }

        public void RunStarted(object automationObject, Dictionary<string, string> replacementsDictionary, WizardRunKind runKind, object[] customParams)
        {
            this._dte2 = (DTE2)automationObject;

            // Ensure the user entered a non-null file name
            string fileName = replacementsDictionary["$rootname$"];
            fileName = fileName.Trim();
            if (string.IsNullOrEmpty(fileName))
            {
                this.TerminateWizard(Resources.WizardError_Empty_Filename);
            }

            // Class name is file name minus extension.  Validate not empty.
            string className = Path.GetFileNameWithoutExtension(fileName);
            if (string.IsNullOrEmpty(className))
            {
                this.TerminateWizard(Resources.WizardError_Empty_Filename);
            }
        }

        public bool ShouldAddProjectItem(string filePath)
        {
            return true;
        }

        /// <summary>
        /// Gets the DTE2 for this wizard.  It will never be null.
        /// </summary>
        /// <exception cref="WizardCancelledException"> is thrown if no DTE2 is available.</exception>
        private DTE2 Dte2
        {
            get
            {
                if (this._dte2 == null)
                {
                    this.TerminateWizard(Resources.WizardError_No_DTE);
                }
                return this._dte2;
            }
        }

        /// <summary>
        /// Obtains from VS a service of the given type.
        /// </summary>
        /// <param name="serviceType">The type of service to obtain</param>
        /// <returns>The service instance or null.</returns>
        /// <exception cref="WizardCancelledException"> is thrown if there is no active DTE2.</exception>
        private object GetService(Type serviceType)
        {
            Microsoft.VisualStudio.OLE.Interop.IServiceProvider vsServiceProvider = this.Dte2 as Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
            if (vsServiceProvider == null)
            {
                return null;
            }

            using (ServiceProvider serviceProvider = new ServiceProvider(vsServiceProvider))
            {
                return serviceProvider.GetService(serviceType);
            }
        }

        /// <summary>
        /// Immediately terminates the wizard, optionally displaying an error message to the user.
        /// </summary>
        /// <param name="errorMessage">It not empty, an error message to display to user before termination</param>
        private void TerminateWizard(string errorMessage)
        {
            if (!string.IsNullOrEmpty(errorMessage))
            {
                this.ShowError(errorMessage);
                throw new WizardCancelledException(errorMessage);
            }
            throw new WizardCancelledException();
        }

        /// <summary>
        /// Displays the given error message in a modal message box.
        /// </summary>
        /// <param name="errorMessage">The error message</param>
        private void ShowError(string errorMessage)
        {
            IUIService uiService = (IUIService)this.GetService(typeof(IUIService));
            if (uiService != null)
            {
                MessageBoxOptions options = 0;
                System.Windows.Forms.IWin32Window parentWnd = uiService.GetDialogOwnerWindow();
                if (System.Threading.Thread.CurrentThread.CurrentUICulture.TextInfo.IsRightToLeft)
                {
                    options |= MessageBoxOptions.RightAlign;
                    options |= MessageBoxOptions.RtlReading;
                }

                MessageBox.Show(parentWnd, errorMessage, Resources.WizardError_Caption, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, options);
            }
            else
            {
                System.Diagnostics.Debug.Fail("Could not obtain IUIService");
            }
        }

        #endregion
    }
}
