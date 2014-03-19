using System;
using System.Collections.Generic;
using System.Linq;
using EnvDTE;
using Microsoft.Build.Evaluation;
using Project = EnvDTE.Project;
using MsBuildProject = Microsoft.Build.Evaluation.Project;
using ProjectItem = EnvDTE.ProjectItem;
namespace OpenRiaServices.VisualStudio.Installer.Helpers
{
    /// <summary>
    /// 
    /// </summary>
    /// <from>NuGet</from>
    public static class ProjectExtensions
    {
        public static bool IsUnloaded(this Project project)
        {
            return VsConstants.UnloadedProjectTypeGuid.Equals(project.Kind, StringComparison.OrdinalIgnoreCase);
        }

        public static MsBuildProject AsMSBuildProject(this Project project)
        {
            return ProjectCollection.GlobalProjectCollection.GetLoadedProjects(project.FullName).FirstOrDefault() ??
                   ProjectCollection.GlobalProjectCollection.LoadProject(project.FullName);
        }

        /// <summary>
        /// Recursively retrieves all supported child projects of a virtual folder.
        /// </summary>
        /// <param name="project">The root container project</param>
        public static IEnumerable<Project> GetSupportedChildProjects(this Project project)
        {
            if (!project.IsSolutionFolder())
            {
                yield break;
            }

            var containerProjects = new Queue<Project>();
            containerProjects.Enqueue(project);

            while (containerProjects.Any())
            {
                var containerProject = containerProjects.Dequeue();
                foreach (ProjectItem item in containerProject.ProjectItems)
                {
                    var nestedProject = item.SubProject;
                    if (nestedProject == null)
                    {
                        continue;
                    }
                    else if (nestedProject.IsSupported())
                    {
                        yield return nestedProject;
                    }
                    else if (nestedProject.IsSolutionFolder())
                    {
                        containerProjects.Enqueue(nestedProject);
                    }
                }
            }
        }

        public static IEnumerable<Project> GetSupportedChildProjects(this Solution solution)
        {
            return solution.Projects.OfType<Project>().SelectMany(i => i.GetSupportedChildProjects());
        }

        public static bool IsSolutionFolder(this Project project)
        {
            return project.Kind != null && project.Kind.Equals(VsConstants.VsProjectItemKindSolutionFolder, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsTopLevelSolutionFolder(this Project project)
        {
            return IsSolutionFolder(project) && project.ParentProjectItem == null;
        }

    }


}
