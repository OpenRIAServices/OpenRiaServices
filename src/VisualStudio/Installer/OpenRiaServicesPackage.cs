using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using EnvDTE;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using OpenRiaServices.VisualStudio.Installer.Dialog;
using OpenRiaServices.VisualStudio.Installer.Helpers;
using System.Threading;
using Microsoft.VisualStudio.Threading;
using System.Reflection;

namespace OpenRiaServices.VisualStudio.Installer
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the 
    /// IVsPackage interface and uses the registration attributes defined in the framework to 
    /// register itself and its components with the shell.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    // We must load package to setup assembly resolve in order to be able to load zip files wich reference unsigned dll
    [ProvideAutoLoad(VSConstants.UICONTEXT.ShellInitialized_string, PackageAutoLoadFlags.BackgroundLoad)]
    // This attribute is used to register the information needed to show this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(GuidList.guidVisualStudio_MenuExtensionPkgString)]
    public sealed class OpenRiaServicesPackage : AsyncPackage
    {
        private uint _solutionNotBuildingAndNotDebuggingContextCookie;
        private IVsMonitorSelection _vsMonitorSelection;
        private OleMenuCommandService _mcs;

        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        public OpenRiaServicesPackage()
        {
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));
        }


        /////////////////////////////////////////////////////////////////////////////
        // Overridden Package Implementation

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        // This method is run automatically the first time the command is being executed

        protected override async System.Threading.Tasks.Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            SetupBindingRedirectForOldZipTemplates();
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this.ToString()));

            await base.InitializeAsync(cancellationToken, progress);

            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            _mcs = await GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != _mcs)
            {
                CommandID menuCommandID = new CommandID(GuidList.guidVisualStudio_MenuExtensionCmdSet, (int)PkgCmdIDList.cmdidLinkOpenRiaServicesProject);
                MenuCommand menuItem = new OleMenuCommand(LinkRiaProjectCallback, null, BeforeQueryStatusForAddPackageDialog, menuCommandID);
                _mcs.AddCommand(menuItem);
            }
        }

        /// <summary>
        /// Resolve references from old templates in zip files to unsigned tooling assembly with references
        /// to the actual boundled strong named assembly.
        /// </summary>
        private void SetupBindingRedirectForOldZipTemplates()
        {
            AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolveUnsignedToolsWithSigned;
        }

        /// <summary>
        /// Resolve references from old templates in zip files to unsigned tooling assembly with references
        /// to the actual boundled strong named assembly.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private static Assembly AssemblyResolveUnsignedToolsWithSigned(object sender, ResolveEventArgs args)
        {
            var nameComparison = StringComparison.OrdinalIgnoreCase;

            if (args.Name != null
                && args.Name.StartsWith("OpenRiaServices.VisualStudio.DomainServices.Tools", nameComparison)
                && args.Name.IndexOf("PublicKeyToken=null", nameComparison) != -1)
            {
                return typeof(DomainServices.Tools.DomainServiceClassWizard).Assembly;
            }

            return null;
        }

        private bool IsSolutionExistsAndNotDebuggingAndNotBuilding()
        {
            int pfActive;
            int result = VsMonitorSelection.IsCmdUIContextActive(_solutionNotBuildingAndNotDebuggingContextCookie, out pfActive);
            return (result == VSConstants.S_OK && pfActive > 0);
        }

        private void BeforeQueryStatusForAddPackageDialog(object sender, EventArgs args)
        {
            OleMenuCommand command = (OleMenuCommand)sender;
            command.Visible = IsSolutionExistsAndNotDebuggingAndNotBuilding() && HasActiveLoadedSupportedProject;
            command.Enabled = command.Visible;
        }

        /// <summary>
        /// This function is the callback used to execute a command when the a menu item is clicked.
        /// See the Initialize method to see how the menu item is associated to this function using
        /// the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        private void LinkRiaProjectCallback(object sender, EventArgs e)
        {
            Project project = VsMonitorSelection.GetActiveProject();

            if (project != null && !project.IsUnloaded() && project.IsSupported())
            {
                ShowLinkedProjectDialog(project);
            }
            else
            {
                // show error message when no supported project is selected.
                string projectName = project != null ? project.Name : String.Empty;

                string errorMessage = String.IsNullOrEmpty(projectName)
                    ? Resources.NoProjectSelected
                    : String.Format(CultureInfo.CurrentCulture, Resources.DTE_ProjectUnsupported, projectName);

                MessageHelper.ShowWarningMessage(errorMessage, Resources.ErrorDialogBoxTitle);
            }
        }

        private static void ShowLinkedProjectDialog(Project project)
        {
            DialogWindow window = GetVSRiaLinkWindow(project);

            window.ShowModal();
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        private static DialogWindow GetVSRiaLinkWindow(Project project)
        {
            return new LinkRiaDialogWindow(project);
        }

        private bool HasActiveLoadedSupportedProject
        {
            get
            {
                Project project = VsMonitorSelection.GetActiveProject();
                return project != null && !project.IsUnloaded() && project.IsSupported();
            }
        }


        private IVsMonitorSelection VsMonitorSelection
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                if (_vsMonitorSelection == null)
                {
                    // get the UI context cookie for the debugging mode
                    _vsMonitorSelection = (IVsMonitorSelection)GetService(typeof(IVsMonitorSelection));

                    // get the solution not building and not debugging cookie
                    Guid guid = VSConstants.UICONTEXT.SolutionExistsAndNotBuildingAndNotDebugging_guid;
                    _vsMonitorSelection.GetCmdUIContextCookie(ref guid, out _solutionNotBuildingAndNotDebuggingContextCookie);
                }
                return _vsMonitorSelection;
            }
        }

    }
}
