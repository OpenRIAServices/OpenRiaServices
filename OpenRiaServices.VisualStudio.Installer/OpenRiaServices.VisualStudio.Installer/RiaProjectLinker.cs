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
        private const string LINKED_PROPERTY_NAME = "LinkedOpenRiaServerProject";
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
                    _riaProject.AsMSBuildProject().SetProperty(LINKED_PROPERTY_NAME, relativePath);
                }

            }
        }


        private static Project FindProjectFromLocation(IEnumerable<Project> projects, string absoluteLocation)
        {
            return projects
                .FirstOrDefault(p => Path.GetFullPath(absoluteLocation) == Path.GetFullPath(p.FullName));
        }

        private ProjectProperty GetLinkedProperty(Project p)
        {
            return p.AsMSBuildProject().GetProperty(LINKED_PROPERTY_NAME);
   

        }

        private ProjectProperty GetLinkedProperty(Microsoft.Build.Evaluation.Project p)
        {
            return p.GetProperty(LINKED_PROPERTY_NAME);
        }

        private string RiaProjectDir()
        {
            var riaProjectLocation = _riaProject.FullName;
            var riaProjectDir = Path.GetDirectoryName(riaProjectLocation);
            return riaProjectDir;
        }



    }
}
