using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EnvDTE;
using Microsoft.Build.Evaluation;
using OpenRiaServices.DomainServices.Tools;
using OpenRiaServices.VisualStudio.Installer.Helpers;
using Project = EnvDTE.Project;

namespace OpenRiaServices.VisualStudio.Installer
{
    public class RIAProjectLinker
    {
        private const string LinkedPropertyName = "LinkedOpenRiaServerProject";
        private const string DisableFastUpToDateCheckPropertyName = "DisableFastUpToDateCheck";
        private const string OpenRiaGenerateApplicationContextPropertyName = "OpenRiaGenerateApplicationContext";
        private const string OpenRiaSharedFilesModePropertyName = "OpenRiaSharedFilesMode";
        private const string OpenRiaClientUseFullTypeNamesPropertyName = "OpenRiaClientUseFullTypeNames";

        private readonly Project _riaProject;
        private readonly DTE _dte;

        public RIAProjectLinker(Project riaProject, DTE dte)
        {
            _riaProject = riaProject;
            _dte = dte;
        }

        /// <summary>
        /// Gets the linked ria project. Returns null if one isn't set or if the relative file path is invalid.
        /// </summary>
        public Project LinkedProject
        {
            get
            {
                Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
                var property = GetLinkedProperty(_riaProject);
                if (property == null)
                {
                    return null;
                }
                var linkedProjectLocation = property.EvaluatedValue;

                //absolutize the relative path of linkedProjectLocation from RiaProjectDir
                var absoluteLinkedProjectLocation = linkedProjectLocation.AbsolutePathFrom(RiaProjectDir());
                var childProjects = _dte.Solution.GetSupportedChildProjects();
                return FindProjectFromLocation(childProjects, absoluteLinkedProjectLocation);
            }
            set
            {
                if (value == null)
                {
                    var linkedProperty = GetLinkedProperty(_riaProject.AsMSBuildProject());
                    if (linkedProperty == null)
                    {
                        return;
                    }

                    //it exists but we delete it
                    _riaProject.AsMSBuildProject().RemoveProperty(linkedProperty);
                }
                else
                {
                    var relativePath = RiaProjectDir().RelativePathTo(value.FullName);
                    _riaProject.AsMSBuildProject().SetProperty(LinkedPropertyName, relativePath);
                }
            }
        }

        public bool? DisableFastUpToDateCheck
        {
            get
            {
                return GetBooleanProperty(DisableFastUpToDateCheckPropertyName);
            }
            set
            {
                SetBooleanProperty(DisableFastUpToDateCheckPropertyName, value);
            }
        }

        public bool? OpenRiaGenerateApplicationContext
        {
            get
            {
                return GetBooleanProperty(OpenRiaGenerateApplicationContextPropertyName);
            }
            set
            {
                SetBooleanProperty(OpenRiaGenerateApplicationContextPropertyName, value);
            }
        }

        public bool? OpenRiaClientUseFullTypeNames
        {
            get
            {
                return GetBooleanProperty(OpenRiaClientUseFullTypeNamesPropertyName);
            }
            set
            {
                SetBooleanProperty(OpenRiaClientUseFullTypeNamesPropertyName, value);
            }
        }

        public OpenRiaSharedFilesMode? OpenRiaSharedFilesMode
        {
            get => GetEnumProperty<OpenRiaSharedFilesMode>(OpenRiaSharedFilesModePropertyName);
            set => SetEnumProperty<OpenRiaSharedFilesMode>(OpenRiaSharedFilesModePropertyName, value);
        }

        private bool? GetBooleanProperty(string name)
        {
            var project = this._riaProject.AsMSBuildProject();
            ProjectProperty projectProperty = project.GetProperty(name);
            if (projectProperty == null)
            {
                return null;
            }
            return new bool?(projectProperty.EvaluatedValue == "true");
        }

        private void SetBooleanProperty(string name, bool? value)
        {
            var project = this._riaProject.AsMSBuildProject();

            if (value == null)
            {
                ProjectProperty projectProperty = project.GetProperty(name);
                if (projectProperty != null)
                {
                    project.RemoveProperty(projectProperty);
                }
            }
            else if (value == false)
            {
                project.SetProperty(name, "false");
            }
            else
            {
                project.SetProperty(name, "true");
            }
        }


        private TEnum? GetEnumProperty<TEnum>(string name) where TEnum  : struct
        {
            var project = this._riaProject.AsMSBuildProject();
            string value = project.GetProperty(name)?.EvaluatedValue;
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            TEnum result;
            if (Enum.TryParse(value, true, out result))
                return result;
            else
                return null;
        }

        private void SetEnumProperty<TEnum>(string name, TEnum? value) where TEnum : struct
        {
            var project = this._riaProject.AsMSBuildProject();

            if (value == null)
            {
                ProjectProperty projectProperty = project.GetProperty(name);
                if (projectProperty != null)
                {
                    project.RemoveProperty(projectProperty);
                }
            }
            else
            {
                project.SetProperty(name, value.ToString());
            }
        }

        private static Project FindProjectFromLocation(IEnumerable<Project> projects, string absoluteLocation)
        {
            return projects
                .FirstOrDefault(p => Path.GetFullPath(absoluteLocation) == Path.GetFullPath(p.FullName));
        }

        private ProjectProperty GetLinkedProperty(Project p)
        {
            return p.AsMSBuildProject().GetProperty(LinkedPropertyName);
        }

        private ProjectProperty GetLinkedProperty(Microsoft.Build.Evaluation.Project p)
        {
            return p.GetProperty(LinkedPropertyName);
        }

        private string RiaProjectDir()
        {
            var riaProjectLocation = _riaProject.FullName;
            var riaProjectDir = Path.GetDirectoryName(riaProjectLocation);
            return riaProjectDir;
        }
    }
}
