using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;

using EnvDTE;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.Win32;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using OpenRiaServices.VisualStudio.Installer.Dialog;
using OpenRiaServices.VisualStudio.Installer.Helpers;

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
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the information needed to show this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(GuidList.guidVisualStudio_MenuExtensionPkgString)]
    public sealed class OpenRiaServicesPackage : Package
    {
        private uint _solutionNotBuildingAndNotDebuggingContextCookie;
        private DTE _dte;
        private DTEEvents _dteEvents;
        private IVsMonitorSelection _vsMonitorSelection;
        private OleMenuCommand _managePackageDialogCommand;
        private OleMenuCommand _managePackageForSolutionDialogCommand;
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
        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            Debug.WriteLine (string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this.ToString()));
            base.Initialize();
            AddMenuCommandHandlers();

        }

        #endregion

        private void AddMenuCommandHandlers()
        {
            _mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != _mcs)
            {
                CommandID menuCommandID = new CommandID(GuidList.guidVisualStudio_MenuExtensionCmdSet, (int)PkgCmdIDList.cmdidLinkOpenRiaServicesProject);
                MenuCommand menuItem = new OleMenuCommand(LinkRiaProjectCallback, null, BeforeQueryStatusForAddPackageDialog, menuCommandID);
                _mcs.AddCommand(menuItem);
            }
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
