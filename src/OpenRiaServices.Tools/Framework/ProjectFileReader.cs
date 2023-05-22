using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Exceptions;

namespace OpenRiaServices.Tools
{
    /// <summary>
    /// This utility class offers basic support for reading MSBuild
    /// project files.
    /// </summary>
    internal class ProjectFileReader : IDisposable
    {
        private readonly ILogger _logger;
        private ProjectCollection _projectCollection = new ProjectCollection();

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to use for warnings and errors.</param>
        internal ProjectFileReader(ILogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            this._logger = logger;
        }

        /// <summary>
        /// Returns the relative path of the given <paramref name="path"/>
        /// relative to <paramref name="projectPath"/>.
        /// </summary>
        /// <param name="path">The path we want made into a relative path.</param>
        /// <param name="projectPath">The project file we want it relative to.</param>
        /// <returns>The relative path if it is possible, else the unmodified <paramref name="path"/>.</returns>
        internal static string ConvertToRelativePath(string path, string projectPath)
        {
            string resultPath = path;
            if (Path.IsPathRooted(path))
            {
                string projectDirectory = Path.GetFullPath(Path.GetDirectoryName(projectPath));
                resultPath = Path.GetFullPath(path);
                if (resultPath.StartsWith(projectDirectory, StringComparison.OrdinalIgnoreCase) &&
                    resultPath.Length > projectDirectory.Length &&
                    resultPath[projectDirectory.Length] == Path.DirectorySeparatorChar)
                {
                    resultPath = resultPath.Substring(projectDirectory.Length + 1);
                }
            }
            return resultPath;
        }

        /// <summary>
        /// Returns the full path of the given <paramref name="path"/> relative to the
        /// given <paramref name="projectPath"/>.
        /// </summary>
        /// <param name="path">The path to convert (if not rooted).</param>
        /// <param name="projectPath">The path to use as the root.</param>
        /// <returns>The full path of the input <paramref name="path"/>.</returns>
        internal static string ConvertToFullPath(string path, string projectPath)
        {
            if (!Path.IsPathRooted(path))
            {
                path = Path.Combine(Path.GetDirectoryName(projectPath), path);
            }
            return Path.GetFullPath(path);
        }

        /// <summary>
        /// Retrieves a <see cref="Project"/> instance from the given project path.
        /// </summary>
        /// <param name="projectPath">Full path name to the project file.</param>
        /// <returns>The project instance or null if it does not exist</returns>
        internal Project LoadProject(string projectPath)
        {
            if (string.IsNullOrEmpty(projectPath))
            {
                throw new ArgumentNullException(nameof(projectPath));
            }

            if (!File.Exists(projectPath))
            {
                this._logger.LogWarning(string.Format(CultureInfo.CurrentCulture, Resource.Project_Does_Not_Exist, projectPath));
                return null;
            }

            Project project = null;

            try
            {
                project = this._projectCollection.LoadedProjects.Where(p => string.Equals(p.FullPath, projectPath, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                if (project == null)
                {
                    project = this._projectCollection.LoadProject(projectPath);

                    // Check if 'IsCrossTargetingBuild' is true
                    // * this means 'TargetFramework' is missing, but 'TargetFrameworks' are not
                    if (project.GetProperty("IsCrossTargetingBuild")?.EvaluatedValue == "true")
                    {
                        var targetFrameworks = project.GetProperty("TargetFrameworks")?.EvaluatedValue;
                        if (targetFrameworks != null)
                        {
                            // fallback to first item (ideally we should loop all or take based on referenced project)
                            // if only a single target without ";" then that will be the the single result of split
                            var firstFramework = targetFrameworks.Split(';')[0].Trim();
                            project.SetGlobalProperty("TargetFramework", firstFramework);
                            this._logger.LogMessage(string.Format(Resource.Project_Is_MultiTarget_Using_TargetFramework, projectPath, firstFramework));
                        }

                        project.ReevaluateIfNecessary();
                    }
                }
            }
            catch (InvalidOperationException ioe)
            {
                this._logger.LogWarning(string.Format(CultureInfo.CurrentCulture, Resource.Failed_To_Open_Project, projectPath, ioe.Message));
            }
            catch (InvalidProjectFileException ipfe)
            {
                this._logger.LogWarning(string.Format(CultureInfo.CurrentCulture, Resource.Failed_To_Open_Project, projectPath, ipfe.Message));
            }

            return project;
        }

        /// <summary>
        /// Retrieves the named property from the specified project
        /// </summary>
        /// <param name="projectPath">Full path to the project file.</param>
        /// <param name="propertyName">Name of the property to retrieve</param>
        /// <returns>The value of the property, or null if it does not exist.</returns>
        internal string GetPropertyValue(string projectPath, string propertyName)
        {
            string propertyValue = null;

            Project project = this.LoadProject(projectPath);
            if (project != null)
            {
                propertyValue = project.GetPropertyValue(propertyName);
            }
            return propertyValue;
        }

        /// <summary>
        /// Returns the list of all project references found in the given <paramref name="projectPath"/>
        /// </summary>
        /// <remarks>
        /// This method unconditionally opens an MSBuild object model on the given project
        /// to extract the set of project references.  It does not cache.  Use with care.
        /// </remarks>
        /// <param name="projectPath">Full path to the project to open.</param>
        /// <returns>The list of full project file names referred by by the given project.</returns>
        internal IEnumerable<string> LoadProjectReferences(string projectPath)
        {
            IEnumerable<string> projects = Array.Empty<string>();

            Project project = this.LoadProject(projectPath);
            if (project == null)
            {
                return projects;
            }

            this._logger.LogMessage(string.Format(CultureInfo.CurrentCulture, Resource.Analyzing_Project_References, Path.GetFileName(projectPath)));

            projects = project.GetItems("ProjectReference").Select(i => ConvertToFullPath(i.EvaluatedInclude, projectPath));

            // Tell the user what project references we found
            if (projects.Any())
            {
                StringBuilder sb = new StringBuilder(string.Format(CultureInfo.CurrentCulture, Resource.Project_References_Found, Path.GetFileName(projectPath)));
                foreach (string p in projects)
                {
                    sb.AppendLine();
                    sb.Append("    " + p);
                }
                this._logger.LogMessage(sb.ToString());
            }

            return projects;
        }

        /// <summary>
        /// Retrieves the list of source file names from the specified project.
        /// </summary>
        /// <param name="projectPath">Full path to the project file.</param>
        /// <returns>A non-null (but possibly empty) list of full paths to files.</returns>
        internal IEnumerable<string> LoadSourceFilesFromProject(string projectPath)
        {
            IEnumerable<string> sources = Array.Empty<string>();
            Project project = this.LoadProject(projectPath);
            if (project == null)
            {
                return sources;
            }

            // Tell the user.  This helps us see when we use the cache and when we don't
            this._logger.LogMessage(string.Format(CultureInfo.CurrentCulture, Resource.Analyzing_Project_Files, Path.GetFileName(projectPath)));

            sources = project.GetItems("Compile").Select(i => ConvertToFullPath(i.EvaluatedInclude, projectPath))
                .ToArray();

            return sources;
        }

        #region IDisposable Members

        /// <summary>
        /// Override of IDisposable.Dispose to handle implementation details of dispose
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);

            if (this._projectCollection != null)
            {
                this._projectCollection.Dispose();
                this._projectCollection = null;
            }
        }
        #endregion
    }
}
