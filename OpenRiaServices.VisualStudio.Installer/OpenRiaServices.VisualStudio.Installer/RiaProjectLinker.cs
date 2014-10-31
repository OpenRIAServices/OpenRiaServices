using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EnvDTE;
using Microsoft.Build.Evaluation;
using OpenRiaServices.VisualStudio.Installer.Helpers;
using Project = EnvDTE.Project;

namespace OpenRiaServices.VisualStudio.Installer
{
    public class RIAProjectLinker
    {
        private const string LinkedPropertyName = "LinkedOpenRiaServerProject";
        private const string DisableFastUpToDateCheckPropertyName = "DisableFastUpToDateCheck";

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
                ProjectProperty disableFastUpToDateCheck = this.GetDisableFastUpToDateCheck(this._riaProject);
                if (disableFastUpToDateCheck == null)
                {
                    return null;
                }
                return new bool?(disableFastUpToDateCheck.EvaluatedValue == "true");
            }
            set
            {
                if (!value.HasValue || !value.Value)
                {
                    ProjectProperty disableFastUpToDateCheck = this.GetDisableFastUpToDateCheck(this._riaProject.AsMSBuildProject());
                    if (disableFastUpToDateCheck != null)
                    {
                        this._riaProject.AsMSBuildProject().RemoveProperty(disableFastUpToDateCheck);
                    }
                }
                else
                {
                    this._riaProject.AsMSBuildProject().SetProperty("DisableFastUpToDateCheck", "true");
                }
            }
        }




        private static Project FindProjectFromLocation(IEnumerable<Project> projects, string absoluteLocation)
        {
            return projects
                .FirstOrDefault(p => Path.GetFullPath(absoluteLocation) == Path.GetFullPath(p.FullName));
        }

        private ProjectProperty GetDisableFastUpToDateCheck(Project p)
        {
            return p.AsMSBuildProject().GetProperty(DisableFastUpToDateCheckPropertyName);


        }

        private ProjectProperty GetLinkedProperty(Project p)
        {
            return p.AsMSBuildProject().GetProperty(LinkedPropertyName);
   

        }

        private ProjectProperty GetDisableFastUpToDateCheck(Microsoft.Build.Evaluation.Project p)
        {
            return p.GetProperty(DisableFastUpToDateCheckPropertyName);
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
