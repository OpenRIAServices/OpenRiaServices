using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Silverlight;
using Microsoft.VisualStudio.TemplateWizard;

namespace OpenRiaServices.VisualStudio.DomainServices.Tools
{
    public class RiaServicesLibraryWizard : IWizard
    {
        private DTE2 _dte2;
        private Project _slClassLibProject;
        private SolutionFolder _activeSolutionFolder;
        private Dictionary<string, string> _replacementsDictionary;

        #region IWizard Members

        public void BeforeOpeningFile(ProjectItem projectItem)
        {
        }

        public void ProjectFinishedGenerating(Project project)
        {
            this._slClassLibProject = project;
        }

        public void ProjectItemFinishedGenerating(ProjectItem projectItem)
        {
        }

        public void RunFinished()
        {
            Solution2 solution2 = (Solution2)this._dte2.Solution;
            string slClassLibProjectPath = this._slClassLibProject.FullName;
            string classLibName = this._replacementsDictionary["$safeprojectname$"];

            // Determine whether the SL project was created in a Solution Folder.
            // If the user explicitly asked to Add Project under a Solution Folder,
            // it will be non-null.  However if they ask to Create New Project under
            // a Solution Folder but change their mind to say "Add to Solution", 
            // they will end up with the Silverlight project as a child of the SLN.
            ProjectItem projectItem = this._slClassLibProject.ParentProjectItem;
            ProjectItems projectItems = projectItem == null ? null : projectItem.Collection;
            Project parentProject = projectItems == null ? null : projectItems.Parent as Project;
            SolutionFolder slProjectParentSolutionFolder = (parentProject != null && parentProject.Kind == ProjectKinds.vsProjectKindSolutionFolder)
                                                            ? parentProject.Object as SolutionFolder
                                                            : null;

            // If the SL project was created in a Solution Folder, it wins because we cannot move it (see below).
            // However if the SL project was created as a child of the Solution, we have a choice.  If a Solution Folder
            // was active when the user added the template, that is the one we will use.  But if there was no active
            // Solution Folder, then we unconditionally create a new Solution Folder as a child of the Solution.
            SolutionFolder libFolder = slProjectParentSolutionFolder ?? this._activeSolutionFolder;
            if (libFolder == null)
            {
                try
                {
                    // SL project was created directly under the Solution.  Create a Solution Folder
                    // to hold the pair of projects.
                    libFolder = (SolutionFolder)((Project)solution2.AddSolutionFolder(classLibName)).Object;
                }
                catch (COMException)
                {
                    libFolder = null;
                }
            }

            bool isVb = this._slClassLibProject.CodeModel.Language.Equals(CodeModelLanguageConstants.vsCMLanguageVB, StringComparison.OrdinalIgnoreCase);
            string language = isVb ? "VisualBasic" : "CSharp";

            // CSDMain 228876
            // Appending the FrameworkVersion to the file name when calling GetProjectTemplate is an undocumented way to request a specific $targetframeworkversion$ token
            // to become available to the child template.  Without doing this, the default target framework value is used, which for VS 11 is 4.5.
            // Reference: http://www.visualstudiodev.com/visual-studio-extensibility/using-automation-to-create-templates-using-different-framework-versions-in-vs2008-23148.shtml
            string templateName = "ClassLibrary.zip|FrameworkVersion=" + this._replacementsDictionary["$targetframeworkversion$"];
            string netClassLibProjectTemplate = solution2.GetProjectTemplate(templateName, language);
            string netClassLibProjectName = classLibName + ".Web";
            string destination = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(slClassLibProjectPath)), netClassLibProjectName);

            // This code executes if we either created our own SolutionFolder or are using
            // the one the user chose.
            if (libFolder != null)
            {
                // Create the .NET class library in whichever SolutionFolder we decided to use above
                libFolder.AddFromTemplate(netClassLibProjectTemplate, destination, netClassLibProjectName);

                // If the SL project was created as a child of the Solution, we need to move it
                // into our new Solution Folder.  However, if it was created in a Solution Folder,
                // we leave it as is.  Dev10 bug 893488 disallows moving the SL project from one
                // Solution Folder to another, so this strategy avoids that issue.
                if (slProjectParentSolutionFolder == null)
                {
                    // Move the Silverlight library under the folder
                    solution2.Remove(this._slClassLibProject);

                    this._slClassLibProject = libFolder.AddFromFile(slClassLibProjectPath);

                }
            }
            else
            {
                solution2.AddFromTemplate(netClassLibProjectTemplate, destination, netClassLibProjectName, false);
            }


            // Link the two class libraries together

            string extension = Path.GetExtension(slClassLibProjectPath);
            IVsSolution ivsSolution = (IVsSolution)Package.GetGlobalService(typeof(SVsSolution));
            IVsHierarchy hierarchy;
            ivsSolution.GetProjectOfUniqueName(_slClassLibProject.UniqueName, out hierarchy);
            IVsBuildPropertyStorage buildPropertyStorage = (IVsBuildPropertyStorage)hierarchy;
            buildPropertyStorage.SetPropertyValue("LinkedOpenRiaServerProject", null,
                (uint)_PersistStorageType.PST_PROJECT_FILE,
                Path.Combine("..", Path.Combine(netClassLibProjectName, netClassLibProjectName + extension)));


        }

        public void RunStarted(object automationObject,
            Dictionary<string, string> replacementsDictionary,
            WizardRunKind runKind, object[] customParams)
        {
            this._dte2 = (DTE2)automationObject;
            this._replacementsDictionary = replacementsDictionary;
            this._replacementsDictionary["$targetsilverlightversion$"] = TemplateUtilities.GetSilverlightVersion(automationObject);
            this._dte2.Globals["safeclientprojectname"] = replacementsDictionary["$safeprojectname$"];

            // Determine whether the user has asked to add this to an existing Solution Folder.
            // We do this at startup because the active project will be changed during the creation
            // of the template.  If _activeSolutionFolder is null, it means the user did not ask
            // to create these libraries under a SolutionFolder.
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
                this._activeSolutionFolder = activeProject.Object as SolutionFolder;
                System.Diagnostics.Debug.Assert(this._activeSolutionFolder != null, "Failed to cast dynamic oject to SolutionFolder");
            }
        }

        public bool ShouldAddProjectItem(string filePath)
        {
            return true;
        }

        #endregion
    }
}
