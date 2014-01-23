using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TemplateWizard;
using Microsoft.VisualStudio.Web.Silverlight;

namespace OpenRiaServices.VisualStudio.DomainServices.Tools
{
    using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;



    public class BusinessApplicationProjectTemplateWizard : IWizard
    {
        private DTE2 _dte2;
        private Dictionary<string, string> _replacementsDictionary;
        private object[] _customParams;
        private Solution2 _solution2;
        private Project _selectedSolutionFolder;
        private string _webProjectName;

        #region IWizard Members

        public void BeforeOpeningFile(ProjectItem projectItem)
        {
        }

        public void ProjectFinishedGenerating(Project project)
        {
        }

        public void ProjectItemFinishedGenerating(ProjectItem projectItem)
        {
        }

        public void RunFinished()
        {
            Project silverlightProject = this.GetSilverlightProject();

            // Here we create the web project from template.
            // The location of web template is calculated as a relative path from current(client) template,
            // which is passed as customParams[0] in current version of VS/VWD(undocumented).
            if (this._customParams.Length > 0 && this._customParams[0] != null)
            {
                string templateDir = Path.GetDirectoryName((string)this._customParams[0]);
#if VS10
                string webTemplateDir = Path.Combine(templateDir, "BA.Web.10.0");
#else
                string webTemplateDir = Path.Combine(templateDir, "BA.Web");
#endif
                if (!Directory.Exists(webTemplateDir))
                {
                    // In V1 SP1, we switched to a shorter directory name to avoid a lot of messy path-length
                    // issues. However, some older templates might still have the original directory name.
                    webTemplateDir = Path.Combine(templateDir, "BusinessApplication.Web");
                }

                string webTemplate = Path.Combine(webTemplateDir, "server.vstemplate");

                // CSDMain 228876
                // Custom parameters can be appended to the path when calling AddFromTemplate.  We need to use something other than
                // $targetframeworkversion$ since we're not calling GetProjectTemplate and $targetframeworkversion$ is already baked in
                // as the default when calling AddFromTemplate in this manner.  The project file and web.config reference
                // $targetwebframeworkversion$ to complete this.
                webTemplate += "|$targetwebframeworkversion$=" + this._replacementsDictionary["$targetframeworkversion$"];

                string destination = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(silverlightProject.FullName)), this._webProjectName);
                this._solution2.AddFromTemplate(webTemplate, destination, this._webProjectName, false);
            }

            Project webProject = this.GetWebProject();

            if (webProject != null)
            {
                // Set the WAP as the startup project
                this._dte2.Solution.SolutionBuild.StartupProjects = webProject.UniqueName;
                Properties props = webProject.Properties;

                // Set the start page
                ProjectItem testPageItem = this.GetAspxTestPage(webProject);
                if (testPageItem != null)
                {
                    props.Item("WebApplication.StartPageUrl").Value = testPageItem.Name;
                    props.Item("WebApplication.DebugStartAction").Value = 1; // StartAction.SpecificPage
                }

                // Link the server project to the client
                if (silverlightProject != null)
                {
                    string projectReference = webProject.FullName;
                    if ((webProject.FullName.Length > 0) && Path.IsPathRooted(webProject.FullName))
                    {
                        projectReference = MakeProjectPathRelative(projectReference, silverlightProject.FullName);
                    }

                    
                    IVsSolution ivsSolution = (IVsSolution)Package.GetGlobalService(typeof(SVsSolution));
                    IVsHierarchy hierarchy;
                    ivsSolution.GetProjectOfUniqueName(silverlightProject.UniqueName, out hierarchy);
                    IVsBuildPropertyStorage buildPropertyStorage = (IVsBuildPropertyStorage)hierarchy;
                    buildPropertyStorage.SetPropertyValue("LinkedOpenRiaServerProject", null,
                        (uint)_PersistStorageType.PST_PROJECT_FILE,
                        projectReference);


                    // Add this client to the list of clients in the server project

                    // Get the IVsHierarchy for each one from the solution
                    IOleServiceProvider oleServiceProvider = this._dte2 as IOleServiceProvider;
                    IVsSolution sln = null;

                    using (ServiceProvider sp = new ServiceProvider(oleServiceProvider))
                    {
                        // Get the solution 
                        sln = sp.GetService(typeof(IVsSolution)) as IVsSolution;
                        System.Diagnostics.Debug.Assert(sln != null, "Unable to get solution object.");
                    }

                    // Get the hierarchies for each project
                    IVsHierarchy webHierarchy;
                    int result;
                    result = sln.GetProjectOfUniqueName(webProject.UniqueName, out webHierarchy);
                    System.Diagnostics.Debug.Assert(result == 0, "Unexpected failure.");

                    if (result == 0 && webHierarchy != null)
                    {
                        IVsHierarchy silverlightHierarchy;
                        result = sln.GetProjectOfUniqueName(silverlightProject.UniqueName, out silverlightHierarchy);
                        System.Diagnostics.Debug.Assert(result == 0, "Unexpected failure.");
                        if (result == 0)
                        {

                            // Cast the server one to a silverlight project consumer
                            IVsSilverlightProjectConsumer spc = webHierarchy as IVsSilverlightProjectConsumer;

                            // Create the Silverlight link 
                            spc.LinkToSilverlightProject("ClientBin", // destination folder 
                                        true, //enable silverlight debugging 
                                        false, //use cfg specific folders
                                        silverlightHierarchy as IVsSilverlightProject);
                        }
                    }
                }
            }



            // Add Links to .resx files
            FileInfo webProjectProjectFile = new FileInfo(webProject.FullName);
            string webProjectDirectory = webProjectProjectFile.DirectoryName;

            ProjectItem webResourcesFolder = silverlightProject.ProjectItems.AddFolder("Web", null).ProjectItems.AddFolder("Resources", null);

            foreach (string resxFile in Directory.GetFiles(Path.Combine(webProjectDirectory, "Resources"), "*.resx"))
            {
                ProjectItem link = webResourcesFolder.ProjectItems.AddFromFile(resxFile);
                link.Properties.Item("CustomTool").Value = "PublicResXFileCodeGenerator";
            }

            // We always want to build the RIA Services Projects because we need a couple of things to happen:
            // 1. CodeGen needs to occur to make the generated classes available in the client project, allowing it to compile
            // 2. We need the client project to be built so that its controls can be referenced in XAML
            // Without the build occurring here, VB solutions immediately showed build errors in the error list, and C#
            // solutions would show errors as soon as any of the XAML files were opened.
            // We build the silverlight project, which also causes VS to build the Web project because it is a dependency, 
            // instead of the whole solution. That way, if there are additional projects in the solution, as is the case 
            // with the Azure BAT template, those projects won't get built because of the RIA projects.
            var sb = this._solution2.SolutionBuild;
            if (silverlightProject != null)
            {
                sb.BuildProject(sb.ActiveConfiguration.Name, silverlightProject.UniqueName, /*WaitForBuildToFInish*/ true);
            }

            ProjectItem mainPage = this._solution2.FindProjectItem("MainPage.xaml");
            System.Diagnostics.Debug.Assert(mainPage != null, "MainPage.xaml should always exist in the Silverlight project.");
            mainPage.Open(EnvDTE.Constants.vsViewKindPrimary);
        }

        public static string MakeProjectPathRelative(string fullPath, string basePath)
        {
            string localBasePath = basePath;
            string localFullPath = fullPath;
            string relativePath = null;
            if (!localBasePath.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            {
                localBasePath = localBasePath + Path.DirectorySeparatorChar.ToString();
            }
            localFullPath = localFullPath.ToLowerInvariant();
            localBasePath = localBasePath.ToLowerInvariant();
            while (!string.IsNullOrEmpty(localBasePath))
            {
                if (localFullPath.StartsWith(localBasePath, StringComparison.Ordinal))
                {
                    relativePath = relativePath + fullPath.Remove(0, localBasePath.Length);
                    if (relativePath == Path.DirectorySeparatorChar.ToString())
                    {
                        relativePath = "";
                    }
                    return relativePath;
                }
                localBasePath = localBasePath.Remove(localBasePath.Length - 1);
                int lastIndex = localBasePath.LastIndexOf(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal);
                if (-1 != lastIndex)
                {
                    localBasePath = localBasePath.Remove(lastIndex + 1);
                    relativePath = relativePath + ".." + Path.DirectorySeparatorChar.ToString();
                }
                else
                {
                    return fullPath;
                }
            }
            return fullPath;
        }

        public void RunStarted(object automationObject,
            Dictionary<string, string> replacementsDictionary,
            WizardRunKind runKind, object[] customParams)
        {
            this._dte2 = (DTE2)automationObject;
            this._solution2 = (Solution2)this._dte2.Solution;
            this._replacementsDictionary = replacementsDictionary;
            this._replacementsDictionary["$targetsilverlightversion$"] = TemplateUtilities.GetSilverlightVersion(automationObject);
            this._dte2.Globals["safeclientprojectname"] = replacementsDictionary["$safeprojectname$"];
            this._webProjectName = this._replacementsDictionary["$safeprojectname$"] + ".Web";
            this._customParams = customParams;

            // Determine whether the user has asked to add this to an existing Solution Folder.
            // We do this at startup because the active project will be changed during the creation
            // of the template.  If _selectedSolutionFolder is null, it means the user did not ask
            // to create the business application under a SolutionFolder.
            Array projects = null;
            try
            {
                projects = (Array)this._dte2.ActiveSolutionProjects;
            }
            catch (COMException)
            {
            }
            Project activeProject = projects == null ? null : projects.OfType<Project>().FirstOrDefault();
            if (activeProject != null && activeProject.Kind == ProjectKinds.vsProjectKindSolutionFolder)
            {
                this._selectedSolutionFolder = activeProject;
            }
        }

        public bool ShouldAddProjectItem(string filePath)
        {
            return true;
        }

        #endregion

        /// <summary>
        /// Finds and returns the web project in the active solution or folder.
        /// </summary>
        /// <returns>The web project or <c>null</c> if not found.</returns>
        private Project GetWebProject()
        {
            return this.GetProject(this._webProjectName);
        }

        /// <summary>
        /// Finds and returns the Silverlight project in the active solution or folder.
        /// </summary>
        /// <returns>The Silverlight project or <c>null</c> if not found.</returns>
        private Project GetSilverlightProject()
        {
            string silverlightProjectName = this._replacementsDictionary["$safeprojectname$"];
            return this.GetProject(silverlightProjectName);
        }

        /// <summary>
        /// Finds and returns the project matching the specified name.
        /// </summary>
        /// <remarks>
        /// Will find the project by scanning the current solution or selected
        /// solution folder.
        /// </remarks>
        /// <param name="projectName">The name of the project to find.</param>
        /// <returns>The <see cref="Project"/>, or <c>null</c> if not found.</returns>
        private Project GetProject(string projectName)
        {
            Project project = null;

            // If there was a selected solution folder, then first attempt to find the project there,
            // but if it's not found there, then we will fall back to the root of the solution. This
            // can occur if the File->New Project/Add To Solution option was used.  This behavior
            // matches that of the other Silverlight project templates, ignoring the solution folder
            // when File->New Project is used.
            if (this._selectedSolutionFolder != null)
            {
                // Filter the project items down to the items that are Projects themselves, which gives us
                // the list of projects contained in the solution folder. We don't need to traverse nested
                // solution folders, as this._selectedSolutionFolder directly references the target folder.
                IEnumerable<ProjectItem> projects = this._selectedSolutionFolder.ProjectItems.Cast<ProjectItem>().Where(p => p.SubProject != null);
                project = GetProject(projects.Select(p => p.SubProject), projectName);
            }

            // Find the Silverlight project in the root solution if it wasn't found in a solution folder.
            return project ?? GetProject(this._dte2.Solution.Projects.Cast<Project>(), projectName);
        }

        /// <summary>
        /// Finds and returns the project matching the specified name.
        /// </summary>
        /// <param name="projects">The list of projects that scan.</param>
        /// <param name="projectName">The name of the project to find.</param>
        /// <returns>The <see cref="Project"/>, or <c>null</c> if not found.</returns>
        private static Project GetProject(IEnumerable<Project> projects, string projectName)
        {
            foreach (Project project in projects)
            {
                try
                {
                    if (Path.GetFileNameWithoutExtension(project.FullName).Equals(projectName))
                    {
                        return project;
                    }
                }
                catch (NotImplementedException)
                {
                    // The project is probably not loaded, but whatever the reason
                    // It's definitely not the BAT :)
                }
            }

            return null;
        }

        /// <summary>
        /// Finds and returns the BusinessApplicationTestPage.aspx test page.
        /// </summary>
        /// <param name="project">The project containing the test page.</param>
        /// <returns>Test page project item or <c>null</c> if not found.</returns>
        private ProjectItem GetAspxTestPage(Project project)
        {
            string testPage = this._dte2.Globals["safeclientprojectname"] + "TestPage.aspx";
            return project.ProjectItems.Cast<ProjectItem>().FirstOrDefault(item => item.Name.Equals(testPage, StringComparison.OrdinalIgnoreCase));
        }
    }
}
