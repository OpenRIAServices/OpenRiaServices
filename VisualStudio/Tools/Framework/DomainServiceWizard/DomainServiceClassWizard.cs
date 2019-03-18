using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Data.Linq;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using System.Windows.Interop;
using EnvDTE;
using EnvDTE80;
using OpenRiaServices.DomainServices.Tools;
using Microsoft.VisualStudio.ManagedInterfaces9;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TemplateWizard;
using VSLangProj;
using Microsoft.VisualStudio.Shell.Design;

namespace OpenRiaServices.VisualStudio.DomainServices.Tools
{
    /// <summary>
    /// Business Logic Class wizard.
    /// </summary>
    /// <remarks>
    /// This wizard is invoked by VS in response to creating a new Business Logic Class item template.
    /// It manages the user interaction to select which entities to use to create the domain service.
    /// </remarks>
    public partial class DomainServiceClassWizard : IWizard
    {
        private bool _generateMetadataFile;
        private DTE2 _dte2;
        private Project _project;

        // The dialog is a member only so the test helper can locate it
        BusinessLogicClassDialog _dialog;

        private GeneratedCode _businessLogicCode;
        private GeneratedCode _metadataCode;
        private bool _isClientAccessEnabled;
        private bool _isODataEndpointEnabled;
      
        /// <summary>
        /// See <see cref="IWizard.BeforeOpeningFile"/>
        /// </summary>
        /// <param name="projectItem">See documentation.</param>
        public void BeforeOpeningFile(ProjectItem projectItem)
        {
        }

        /// <summary>
        /// See <see cref="IWizard.ProjectFinishedGenerating"/>
        /// </summary>
        /// <param name="project">See documentation.</param>
        public void ProjectFinishedGenerating(Project project)
        {
        }

        /// <summary>
        /// See <see cref="IWizard.ProjectItemFinishedGenerating"/>
        /// </summary>
        /// <param name="projectItem">See documentation.</param>
        public void ProjectItemFinishedGenerating(ProjectItem projectItem)
        {
        }

        public void RunFinished()
        {
            // Update the web.config if necessary
            this.UpdateConfiguration();
        }

        public void RunStarted(object automationObject,
            Dictionary<string, string> replacementsDictionary,
            WizardRunKind runKind, object[] customParams)
        {
            // This instance may be reused -- reinitialize each time
            this._project = null;

            this._dte2 = automationObject as DTE2;
            if (this._dte2 == null)
            {
                this.TerminateWizard(Resources.WizardError_No_DTE);
            }

            // Get active project.  Throws if null.
            Project project = this.ActiveProject;

            ITypeDiscoveryService typeDiscoveryService = this.GetTypeDiscoveryService(project);
            if (typeDiscoveryService == null)
            {
                this.TerminateWizard(Resources.BusinessLogicClass_Error_No_TypeDiscoveryService);
            }

            // Check if the project has a reference to the assembly that has DbDomainService, supporting DbContext.
            VSProject vsproj = project.Object as VSProject;
            bool allowDbContext = false;
            if (vsproj != null) 
            {
                allowDbContext = vsproj.References.Cast<Reference>().Any(r => String.Equals(r.Name, BusinessLogicClassConstants.DbDomainServiceAssemblyShortName));
            }

            // Get the list of available ObjectContexts, DataContexts, and optionally DbContexts.
            bool foundDbContext = false;
            IEnumerable<Type> candidates = this.GetCandidateTypes(typeDiscoveryService, allowDbContext, out foundDbContext);

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

            // We infer language from extension
            string extension = Path.GetExtension(fileName);
            bool isVb = extension.EndsWith("vb", StringComparison.OrdinalIgnoreCase);
            string language = isVb ? "VB" : "C#";

            Property rootNamespaceProperty = project.Properties.Item("RootNamespace");
            string namespaceName = rootNamespaceProperty == null ? null : (string)(rootNamespaceProperty.Value);
            if (string.IsNullOrEmpty(namespaceName))
            {
                this.TerminateWizard(Resources.BusinessLogicClass_Error_No_RootNamespace);
            }

            // Extract VB root namespace for code gen.
            // If non-empty, it means we want to avoid generating namespaces that begin with it.
            string rootNamespace = isVb ? namespaceName : string.Empty;

            // Get the name of the assembly produced by the current project
            Property assemblyNameProperty = project.Properties.Item("AssemblyName");
            string assemblyName = assemblyNameProperty == null ? null : (string)(assemblyNameProperty.Value);
            if (string.IsNullOrEmpty(assemblyName))
            {
                this.TerminateWizard(Resources.BusinessLogicClass_Error_No_AssemblyName);
            }

            // We extract all the context types from the TypeDiscovery service and will pass
            // those to another AppDomain for analysis
            IEnumerable<Type> candidateTypes = candidates.Where(t => CodeGenUtilities.IsValidGenericTypeParam(t));

            // We need the project path for the ClientBuildManager source folder
            string projectPath = project.FullName;
            if (string.IsNullOrEmpty(projectPath))
            {
                this.TerminateWizard(Resources.BusinessLogicClass_No_Project_Path);
            }
            string projectDirectory = Path.GetDirectoryName(projectPath);
            try
            {
                IVsHelp help = this.GetService(typeof(IVsHelp)) as IVsHelp;
                // Strategy: we instantiate the contexts in another AppDomain so they can load the assembly outside of the current
                // Visual Studio root AppDomain.  The main reason for this is the ability to reopen the DSWizard onto modified
                // client assemblies and see modified types.  If they were loaded into the root AppDomain, we would not be able to
                // reload.  The BusinessLogicViewModel is IDisposable and controls the other AppDomain's lifetime.
                using (BusinessLogicViewModel businessLogicViewModel = new BusinessLogicViewModel(projectDirectory, className, language, rootNamespace, assemblyName, candidateTypes, help))
                {
                    businessLogicViewModel.ShowDbContextWarning = false; // foundDbContext; //Removed by CDB //TODO: remove commented out section

                    // Intercept exceptions to report to VS UI.
                    businessLogicViewModel.ExceptionHandler = delegate(Exception ex)
                    {
                        this.ShowError(ex.Message);
                        throw ex;
                    };

                    // Invoke the wizard UI now
                    this._dialog = new BusinessLogicClassDialog();
                    this._dialog.Model = businessLogicViewModel;

                    IVsUIShell uiShell = this.GetService(typeof(IVsUIShell)) as IVsUIShell;
                    IntPtr dialogOwnerHwnd = default(IntPtr);
                    int result = uiShell.GetDialogOwnerHwnd(out dialogOwnerHwnd);
                    if (result == 0 && dialogOwnerHwnd != default(IntPtr))
                    {
                        WindowInteropHelper windowHelper = new WindowInteropHelper(this._dialog);
                        windowHelper.Owner = dialogOwnerHwnd;
                    }
                    this._dialog.ShowInTaskbar = false;

                    this._dialog.ShowDialog();
                    bool success = this._dialog.DialogResult.HasValue && this._dialog.DialogResult.Value;
                    this._dialog.Model = null;

                    // If user cancels dialog, cancel wizard
                    if (!success)
                    {
                        throw new WizardCancelledException();
                    }

                    // Capture some model state to we can dispose the contexts and models
                    ContextViewModel currentContext = businessLogicViewModel.CurrentContextViewModel;
                    this._isClientAccessEnabled = currentContext != null && currentContext.IsClientAccessEnabled;
                    this._isODataEndpointEnabled = currentContext != null && currentContext.IsODataEndpointEnabled;

                    // Compute a namespace that includes folder names
                    namespaceName = this.ComputeNamespace();

                    // User said OK -- so let's generate the code
                    GeneratedCode generatedCode = businessLogicViewModel.GenerateBusinessLogicClass(namespaceName);

                    replacementsDictionary.Add("$generatedcode$", generatedCode.SourceCode);
                    this.AddReferences(generatedCode.References);
                    this._businessLogicCode = generatedCode;

                    // If user elected metadata classes, do that into a separate file
                    if (businessLogicViewModel.IsMetadataClassGenerationRequested)
                    {
                        // The null namespace asks to generate into the entity types own namespaces
                        GeneratedCode generatedMetadataCode = businessLogicViewModel.GenerateMetadataClasses(null /* optionalSuffix */);
                        replacementsDictionary.Add("$generatedmetadatacode$", generatedMetadataCode.SourceCode);
                        this.AddReferences(generatedMetadataCode.References);
                        this._generateMetadataFile = generatedMetadataCode.SourceCode.Length > 0;
                        this._metadataCode = generatedMetadataCode;
                    }
                    else
                    {
                        this._generateMetadataFile = false;
                    }
                }
            }
            finally
            {
                this._dialog = null;
            }
        }

        public bool ShouldAddProjectItem(string filePath)
        {
            // We hook this callback point to avoid generating the metadata file if we decided not to generate it
            bool isAssociatedMetadataFile = IsAssociatedMetadataFile(Path.GetFileName(filePath));
            return (isAssociatedMetadataFile ? this._generateMetadataFile : true);
        }

        /// <summary>
        /// Gets the DTE2 for this wizard.  It will never be null.
        /// </summary>
        /// <exception cref="WizardCancelledException"> is thrown if no DTE2 is available.</exception>
        private DTE2 DTE2
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
        /// Returns the active project.  It will never be null.
        /// </summary>
        /// <exception cref="WizardCancelledException"> is thrown if there is no active project. </exception>
        private Project ActiveProject
        {
            get
            {
                if (this._project == null)
                {
                    Array projects = (Array)this.DTE2.ActiveSolutionProjects;
                    this._project = projects.OfType<Project>().FirstOrDefault();

                    if (this._project == null)
                    {
                        this.TerminateWizard(Resources.BusinessLogicClass_Error_No_Project);
                    }
                }
                return this._project;
            }
        }

        /// <summary>
        /// Returns <c>true</c> if the given file name is for the "buddy class"
        /// </summary>
        /// <param name="fileName">The full name of the file to test</param>
        /// <returns><c>true</c> if the given name is the name of the buddy class</returns>
        private static bool IsAssociatedMetadataFile(string fileName)
        {
            return (fileName.IndexOf(".metadata.", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        /// <summary>
        /// Obtains from VS a service of the given type.
        /// </summary>
        /// <param name="serviceType">The type of service to obtain</param>
        /// <returns>The service instance or null.</returns>
        /// <exception cref="WizardCancelledException"> is thrown if there is no active DTE2.</exception>
        private object GetService(Type serviceType)
        {
            Microsoft.VisualStudio.OLE.Interop.IServiceProvider vsServiceProvider = this.DTE2 as Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
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
        /// Retrieves the IVsHierarchy associated with the given project.  It will never be null.
        /// </summary>
        /// <param name="project">The project.  It cannot be null.</param>
        /// <returns>The VS hierarchy.  It will not be null.</returns>
        /// <exception cref="WizardCancelledException"> is thrown if no hierarchy is available.</exception>
        private IVsHierarchy GetVsHierarchy(Project project)
        {
            IVsSolution vsSolution = this.GetService(typeof(SVsSolution)) as IVsSolution;
            if (vsSolution == null)
            {
                this.TerminateWizard(Resources.BusinessLogicClass_Error_No_Hierarchy);
            }

            IVsHierarchy vsHierarchy = null;
            int result = vsSolution.GetProjectOfUniqueName(project.UniqueName, out vsHierarchy);
            if (result != 0 || vsHierarchy == null)
            {
                this.TerminateWizard(Resources.BusinessLogicClass_Error_No_Hierarchy);
            }
            return vsHierarchy;
        }

        /// <summary>
        /// Returns the type discovery service for the given project
        /// </summary>
        /// <param name="project">The project to use for the vsHierarchy</param>
        /// <returns>The type discovery service or null if it cannot be found</returns>
        /// <exception cref="WizardCancelledException"> is thrown if no hierarchy is available.</exception>
        private ITypeDiscoveryService GetTypeDiscoveryService(Project project)
        {
            DynamicTypeService dts = this.GetService(typeof(DynamicTypeService)) as DynamicTypeService;
            if (dts == null)
            {
                return null;
            }

            // Get the hierarchy.  Throws if not available.
            IVsHierarchy vsHierarchy = this.GetVsHierarchy(project);

            ITypeDiscoveryService tds = dts.GetTypeDiscoveryService(vsHierarchy);
            return tds;
        }

        /// <summary>
        /// Using the specified type discovery service, this method locates all the possible
        /// context types (currently LTS and EDM only)
        /// </summary>
        /// <param name="typeDiscoveryService">The type discovery service to use.  It cannot be null.</param>
        /// <param name="allowDbContext">A boolean indicating if DbContext is an allowable context type</param>
        /// <returns>A non-null (but possibly empty) list of candidate types</returns>
        private IEnumerable<Type> GetCandidateTypes(ITypeDiscoveryService typeDiscoveryService, bool allowDbContext, out bool foundDbContext)
        {
            foundDbContext = false;

            List<Type> candidates = new List<Type>();

            VSProject currentProject = this.ActiveProject.Object as VSProject;
            if (currentProject == null || typeDiscoveryService == null)
            {
                return candidates;
            }

            List<Reference> references = new List<Reference>();
            foreach (Reference reference in currentProject.References)
            {
                if (reference != null && !string.IsNullOrEmpty(reference.Name) && reference.Type == prjReferenceType.prjReferenceTypeAssembly)
                {
                    references.Add(reference);
                }
            }

            // Cache the value of LinqToSqlContext.EnableDataContextTypes locally since the property cannot be cached.
            bool dataContextEnabled = LinqToSqlContext.EnableDataContextTypes;

            // Get all types contained in and referenced by the project. From that list, we find the types interesting to us.
            foreach (Type t in typeDiscoveryService.GetTypes(typeof(object), true))
            {
                bool typeIsDbContext;
                if (DomainServiceClassWizard.IsContextType(t, dataContextEnabled, allowDbContext, out typeIsDbContext))
                {
                    if (IsVisibleInCurrentProject(t, currentProject, references))
                    {
                        candidates.Add(t);
                    }
                }
                foundDbContext |= typeIsDbContext;
            }

            return candidates;
        }

        /// <summary>
        /// Returns <c>true</c> if the type is an ObjectContext, DbContext or DataContext. 
        /// </summary>
        /// <param name="t">The type to be checked.</param>
        /// <param name="dataContextEnabled">Flag indicating if the datacontext is enabled.</param>
        /// <param name="allowDbContext">A boolean indicating if DbContext is an allowable context type</param>
        /// <returns><c>true</c> if the type is one of the context types and <c>false</c> otherwise.</returns>
        public static bool IsContextType(Type t, bool dataContextEnabled, bool allowDbContext, out bool isDbContext)
        {
            isDbContext = false;

            // We are looking for ObjectContext, DbContext or DataContext types only. So we skip Value types and interfaces for performance.
            if (!t.IsValueType && !t.IsInterface)
            {

                isDbContext = IsDbContext(t); //typeof(DbContext).IsAssignableFrom(t);
                if (typeof(ObjectContext).IsAssignableFrom(t) ||
                        (allowDbContext && isDbContext) ||
                        (dataContextEnabled && typeof(DataContext).IsAssignableFrom(t)))
                {
                    return true;
                }
            }
            return false;
        }
        //This method checks for DbContext without directly checking the type
        static bool IsDbContext(Type t)
        {
            if (t == null)
                return false;
            if (t.Name == "DbContext")
                return true;
            else
            {
                return IsDbContext(t.BaseType);
            }
        }

        /// <summary>
        /// This method determines whether the given type comes from the current project, its temporary assemblies or references.
        /// </summary>
        /// <param name="t">The type in question</param>
        /// <param name="vsProject">The project we planning to add domain service to</param>
        /// <param name="projectReferences">References collection for the project</param>
        /// <returns><c>true</c> if the type is visible, <c>false</c> otherwise</returns>
        private static bool IsVisibleInCurrentProject(Type t, VSProject vsProject, IEnumerable<Reference> projectReferences)
        {
            // If we couldn't retirve project or references info -- return true so that the type will appear in the wizard
            if (t == null || vsProject == null || projectReferences == null)
            {
                return true;
            }

            Assembly typeAssembly = t.Assembly;
            string typeAssemblyName = typeAssembly.GetName().Name;
            Project project = vsProject.Project;

            // Check if type comes from this project's output assembly
            string projectAssemblyName = (string)project.Properties.Item("AssemblyName").Value;
            if (typeAssemblyName.Equals(projectAssemblyName, StringComparison.Ordinal))
            {
                return true;
            }

            // Check if type's assembly is located under project's obj directory
            string relativeObjDirectory = project.ConfigurationManager.ActiveConfiguration.Properties.Item("IntermediatePath").Value as string;
            string projectObjDirectory = Path.Combine(Path.GetDirectoryName(project.FullName), relativeObjDirectory);

            if (typeAssembly.Location.StartsWith(projectObjDirectory, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Check if type comes from one of the project's references
            foreach (Reference reference in projectReferences)
            {
                // If Identity property contains tailing '\0' -- remove it.
                string referenceName = reference.Identity;
                
                if (referenceName.EndsWith(Char.ToString((char)0), StringComparison.Ordinal))
                {
                    referenceName = referenceName.Substring(0, referenceName.Length - 1);
                }

                if (referenceName.Equals(typeAssemblyName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
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
        public void ShowError(string errorMessage)
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

        /// <summary>
        /// Updates the web.config as necessary to permit the generated domain service to be found.
        /// </summary>
        private void UpdateConfiguration()
        {
            // There is no work to do unless the user has selected some context
            // and chosen to expose the DomainService via [EnableClientAccess] or
            // to expose an OData endpoint.
            if (!this._isClientAccessEnabled && !this._isODataEndpointEnabled)
            {
                return;
            }

            // Get active project.  Throws if not available.
            Project project = this.ActiveProject;

            // Get hierarchy.  Throws if not available.
            IVsHierarchy vsHierarchy = this.GetVsHierarchy(project);

            IVsApplicationConfigurationManager cfgMgr = this.GetService(typeof(IVsApplicationConfigurationManager)) as IVsApplicationConfigurationManager;
            if (cfgMgr == null)
            {
                this.TerminateWizard(Resources.BusinessLogicClass_Error_No_ConfigurationManager);
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
                        bool addODataEndpoint = this._isODataEndpointEnabled && !webConfigUtil.IsEndpointDeclared(BusinessLogicClassConstants.ODataEndpointName);

                        // Modify the file only if we decided work is required
                        if (addHttpModule || addModuleToWebServer || setAspNetCompatiblity || setMultipleSiteBindingsEnabled || addValidationSection || addODataEndpoint)
                        {
                            string domainServiceFactoryName = WebConfigUtil.GetDomainServiceModuleTypeName();

                            // Check the file out from Source Code Control if it exists.
                            appCfg.QueryEditConfiguration();

                            if (addHttpModule)
                            {
                                webConfigUtil.AddHttpModule(domainServiceFactoryName);
                            }

                            if (addModuleToWebServer)
                            {
                                webConfigUtil.AddModuleToWebServer(domainServiceFactoryName);
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

                            if (addODataEndpoint)
                            {
                                string odataEndpointFactoryName = WebConfigUtil.GetODataEndpointFactoryTypeName();
                                webConfigUtil.AddEndpointDeclaration(BusinessLogicClassConstants.ODataEndpointName, odataEndpointFactoryName);
                            }
                            cfg.Save();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Adds (or verifies we already have) all the given assembly references in the active project
        /// </summary>
        /// <param name="references">Set of references to add to active project</param>
        private void AddReferences(IEnumerable<string> references)
        {
            if (references.Any())
            {
                // Get active project -- throws if not available
                Project project = this.ActiveProject;
                VSLangProj.VSProject vsProject = project.Object as VSLangProj.VSProject;
                if (vsProject != null)
                {
                    // Unconditionally add every reference.
                    // The References.Add method contains the logic to check for an existing
                    // reference of the same identity, and if found, will not change anything.
                    foreach (string assemblyReference in references)
                    {
                        VSLangProj.Reference reference = vsProject.References.Add(assemblyReference);

                        // The assembly reference string can have the full assembly identity format ('AssemblyName, Version=xxxx, Culture ...')
                        // or it can be the full assembly file path.
                        // If the assembly reference is a full path, assume it is not in the GAC and copy it locally to the project
                        // so it can find it and also for bin-deployment support.
                        if (ShouldCopyLocal(assemblyReference))
                        {
                            reference.CopyLocal = true;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Determines whether the specified assembly should be copied into the project or not.
        /// We special-case some assemblies that are not loaded from the GAC to be able to use them in the project
        /// and to support bin-deployment.  In this case the assemblyReference contains the assembly full path
        /// instead of the assembly identity information.
        /// </summary>
        /// <param name="assemblyReference">The assembly reference string</param>
        /// <returns>True if the specified assembly should be copied into the project, false otherwise.</returns>
        private static bool ShouldCopyLocal(string assemblyReference)
        {
            if (File.Exists(assemblyReference))
            {
                return assemblyReference.ToUpperInvariant().Contains(BusinessLogicClassConstants.LinqToSqlDomainServiceAssemblyName.ToUpperInvariant());
            }

            return false;
        }

        /// <summary>
        /// Computes the namespace to use for the generated code.
        /// </summary>
        /// <remarks>The returned value will be a combination of the project's root namespace
        /// plus any folder names the user selected prior to asking to add this template.
        /// </remarks>
        /// <returns>The namespace to use for code generation</returns>
        private string ComputeNamespace()
        {
            ProjectItems projItems = null;

            // If we have a folder selected, we need to use that.
            // Note that the project itself appears as a selected item
            // with a null ProjectItem.  And, yes, the selection collection is 1-based.
            int selectedCount = this._dte2.SelectedItems.Count;
            if (selectedCount == 1)
            {
                SelectedItem selItem = this._dte2.SelectedItems.Item(1);
                if (selItem.ProjectItem != null)
                {
                    projItems = selItem.ProjectItem.ProjectItems;

                    if (projItems != null && projItems.Kind != EnvDTE.Constants.vsProjectItemKindPhysicalFolder)
                    {
                        projItems = null; // not a folder.
                    }
                }
            }
            // If nothing was selected, the active project is our base
            if (projItems == null)
            {
                projItems = this.ActiveProject.ProjectItems;
            }

            // Use algorithm taken verbatim from templateWiz.cs
            return FindNSOfItem(projItems);
        }

        #region Copied from TemplateWiz.cs

        //// Code in this region was copied verbatim from templateWiz.cs,
        //// the VS code that invokes the IWizard interfaces and performs
        //// parameter replacement in the template.
        ////
        //// This code is duplicated here for just one reason -- the existing
        //// template implementation does not compute $rootnamespace$ at a point
        //// where we can use it, and it is required for our code generation.
        ////
        //// From the IWizard's perspective, $rootnamespace$ is not set in the
        //// replacements dictionary until ProjectItemFinishedGenerating is called.
        //// And by then, parameter substitution in the template has been done.

        /// <summary>
        /// Returns the namespace to use for the specified project items
        /// </summary>
        /// <param name="projItems">The project items used to compute the namespace.</param>
        /// <returns>The string value to use for the root namespace</returns>
        /// <remarks>Project resource</remarks>
        private static string FindNSOfItem(EnvDTE.ProjectItems projItems)
        {
            string extension = Path.GetExtension(projItems.ContainingProject.FileName);
            if ((String.Equals(extension, ".csproj", System.StringComparison.OrdinalIgnoreCase) == false) && (String.Equals(extension, ".vjsproj", System.StringComparison.OrdinalIgnoreCase) == false))
            {
                if (projItems.ContainingProject.Object is VSLangProj.VSProject)
                {
                    return projItems.ContainingProject.Properties.Item("RootNamespace").Value.ToString();
                }
                return MakeNameCompliant(Path.GetFileNameWithoutExtension(projItems.ContainingProject.FileName));
            }

            if (projItems is EnvDTE.Project)
            {
                return projItems.ContainingProject.Properties.Item("RootNamespace").Value.ToString();
            }
            else
            {
                string rootNamespace = string.Empty;
                while (projItems.Kind == EnvDTE.Constants.vsProjectItemKindPhysicalFolder)
                {
                    if (projItems.Parent is EnvDTE.Project)
                    {
                        if (rootNamespace.Length == 0)
                        {
                            return projItems.ContainingProject.Properties.Item("RootNamespace").Value.ToString();
                        }
                        else
                        {
                            return projItems.ContainingProject.Properties.Item("RootNamespace").Value.ToString() + "." + rootNamespace;
                        }
                    }
                    else
                    {
                        if (rootNamespace.Length == 0)
                        {
                            rootNamespace = MakeNameCompliant(((EnvDTE.ProjectItem)projItems.Parent).Name);
                        }
                        else
                        {
                            rootNamespace = MakeNameCompliant(((EnvDTE.ProjectItem)projItems.Parent).Name) + "." + rootNamespace;
                        }
                        projItems = ((EnvDTE.ProjectItem)projItems.Parent).Collection;
                    }
                }
                return rootNamespace;
            }
        }

        private static string MakeNameCompliant(string name)
        {
            if (String.IsNullOrEmpty(name))
            {
                return string.Empty;
            }

            if (Char.IsDigit(name[0]))
            {
                name = "_" + name;
            }

            for (int i = 0; i < name.Length; i++)
            {
                UnicodeCategory cat = char.GetUnicodeCategory(name[i]);

                //TODO: For VC, we need the same method for making the name compliant, except we need to remove the check for '.' below.
                if ((cat != UnicodeCategory.UppercaseLetter) && (cat != UnicodeCategory.LowercaseLetter) &&
                    (cat != UnicodeCategory.OtherLetter) && (cat != UnicodeCategory.ConnectorPunctuation) &&
                    (cat != UnicodeCategory.ModifierLetter) && (cat != UnicodeCategory.NonSpacingMark) &&
                    (cat != UnicodeCategory.SpacingCombiningMark) && (cat != UnicodeCategory.TitlecaseLetter) &&
                    (cat != UnicodeCategory.Format) && (cat != UnicodeCategory.LetterNumber) &&
                    (cat != UnicodeCategory.DecimalDigitNumber) &&
                    (name[i] != '.') && (name[i] != '_'))
                {
                    name = name.Replace(name[i], '_');
                }
            }
            return name;
        }

        #endregion Copied from TemplateWiz.cs
    }
}
