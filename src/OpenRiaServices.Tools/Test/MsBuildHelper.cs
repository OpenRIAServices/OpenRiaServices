﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpenRiaServices.Tools.Test
{
    /// <summary>
    /// Helper class for common MSBuild tasks
    /// </summary>
    public static class MsBuildHelper
    {
        private static readonly Dictionary<string, IList<string>> s_ReferenceAssembliesByProjectPath = new Dictionary<string, IList<string>>();

        /// <summary>
        /// Extract the list of assemblies both generated and referenced by the named project.
        /// </summary>
        /// <returns></returns>
        public static List<string> GetReferenceAssemblies(string projectPath)
        {
            IList<string> cachedAssemblies;

            lock (s_ReferenceAssembliesByProjectPath)
            {
                if (!s_ReferenceAssembliesByProjectPath.TryGetValue(projectPath, out cachedAssemblies))
                {
                    cachedAssemblies = new List<string>();
                    GetReferenceAssemblies(projectPath, cachedAssemblies);

                    s_ReferenceAssembliesByProjectPath.Add(projectPath, cachedAssemblies);
                }
            }

            // Create a new copy to prevent modifications to original list
            return new List<string>(cachedAssemblies);
        }

        /// <summary>
        /// Adds the assembly references from the given project to the given list
        /// </summary>
        /// <param name="projectPath">Absolute path to the project file itself</param>
        /// <param name="assemblies">List to add assembly names to</param>
        public static void GetReferenceAssemblies(string projectPath, IList<string> assemblies)
        {
            projectPath = Path.GetFullPath(projectPath);

            using (var project = LoadProject(projectPath))
            {
                // Ask to be told of generated outputs
                var log = new ErrorLogger();

#if NET6_0_OR_GREATER
                var results = project.Build(new string[] { "ResolveAssemblyReferences" /* "ResolvePackageDependenciesForBuild" */}, new[] { log });
                Assert.AreEqual(null, results.Exception, "Build should not have exception result");
                Assert.AreEqual(string.Empty, string.Join("\n", log.Errors));
                Assert.AreEqual(BuildResultCode.Success, results.OverallResult, "ResolveLockFileReferences failed");

                foreach (var reference in project.ProjectInstance.GetItems("Reference"))
                {
                    string assemblyPath = GetFullPath(projectPath, reference);

                    if (!assemblies.Contains(assemblyPath))
                        assemblies.Add(assemblyPath);
                }

                foreach (var reference in project.ProjectInstance.GetItems("_ResolvedProjectReferencePaths"))
                {
                    string outputAssembly = GetFullPath(projectPath, reference);

                    if (!string.IsNullOrEmpty(outputAssembly) && !assemblies.Contains(outputAssembly))
                        assemblies.Add(outputAssembly);
                }
#else
                var results = project.Build(new string[] { "ResolveAssemblyReferences" }, new[] { log });
                Assert.AreEqual(string.Empty, string.Join("\n", log.Errors));
                Assert.AreEqual(BuildResultCode.Success, results.OverallResult, "ResolveAssemblyReferences failed");

                foreach (var reference in project.ProjectInstance.GetItems("_ResolveAssemblyReferenceResolvedFiles"))
                {
                    string assemblyPath = GetFullPath(projectPath, reference);

                    if (!assemblies.Contains(assemblyPath))
                        assemblies.Add(assemblyPath);
                }

                foreach (var reference in project.ProjectInstance.GetItems("_ResolvedProjectReferencePaths"))
                {
                    string outputAssembly = GetFullPath(projectPath, reference);
                    
                    if (!string.IsNullOrEmpty(outputAssembly) && !assemblies.Contains(outputAssembly))
                        assemblies.Add(outputAssembly);
                }
#endif


            }

            MakeFullPaths(assemblies, Path.GetDirectoryName(projectPath));
        }

        private static string GetFullPath(string projectPath, ProjectItemInstance reference)
        {
            string otherProjectPath = reference.EvaluatedInclude;
            if (!Path.IsPathRooted(otherProjectPath))
            {
                otherProjectPath = Path.Combine(Path.GetDirectoryName(projectPath), otherProjectPath);
            }

            return otherProjectPath;
        }

        /// <summary>
        /// Gets the absolute path of the output assembly generated by the specified project
        /// </summary>
        /// <param name="projectPath">Absolute path to the project file</param>
        /// <returns>Absolute path to the generated output assembly (which may or may not exist)</returns>
        public static string GetOutputAssembly(string projectPath)
        {
            string outputAssembly = null;
            projectPath = Path.GetFullPath(projectPath);

            string outputPath, assemblyName, outputType, target;

            using (var project = LoadProject(projectPath))
            {
                outputPath = project.GetPropertyValue("OutputPath");
                assemblyName = project.GetPropertyValue("AssemblyName");
                outputType = project.GetPropertyValue("OutputType");
                target = project.GetPropertyValue("TargetFramework");
            }

            if (!Path.IsPathRooted(outputPath))
                outputPath = Path.Combine(Path.GetDirectoryName(projectPath), outputPath);
            outputAssembly = Path.Combine(outputPath, assemblyName);
            outputAssembly = Path.GetFullPath(outputAssembly);

#if NET6_0_OR_GREATER
            // TODO: change here or not ?
            string extension = ".dll";
#else
            string extension = outputType.Equals("Exe", StringComparison.InvariantCultureIgnoreCase) ? ".exe" : ".dll";
#endif
            outputAssembly += extension;
            var fullPath = MakeFullPath(outputAssembly, Path.GetDirectoryName(projectPath));
            if (!File.Exists(fullPath))
            {
                var assemblyPart = "\\" + assemblyName + extension;
                var alternativePath = fullPath.Replace(assemblyPart, "\\" + target + assemblyPart);
                if (File.Exists(alternativePath))
                    return alternativePath;
            }


            return fullPath;
        }

        internal static ProjectWrapper LoadProject(string projectPath)
        {
            var projectCollection = new ProjectCollection();
            projectCollection.SetGlobalProperty("Configuration", GetConfiguration());

            var project = projectCollection.LoadProject(projectPath, projectCollection.DefaultToolsVersion);
            project.SetProperty("BuildProjectReferences", "false");

            if (project.GetProperty("TargetFramework") == null)
            {
                var targetFrameworks = project.GetProperty("TargetFrameworks")?.EvaluatedValue;
                if (targetFrameworks != null)
                {
                    if (!targetFrameworks.Contains(';'))
                    {
                        project.SetGlobalProperty("TargetFramework", targetFrameworks);
                    }
                    else
                    {
                        var frameworks = targetFrameworks.Split(';');
                        var version = (TargetFrameworkAttribute)Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(System.Runtime.Versioning.TargetFrameworkAttribute), false).SingleOrDefault();
                        var isNet472 = version.FrameworkName == ".NETFramework,Version=v4.7.2";
                        var framework = isNet472 ? "net472" : frameworks.First(f => f != "net472");
                        project.SetGlobalProperty("TargetFramework", framework);
                    }
#if NET6_0_OR_GREATER
                    var actualFramework = project.GetProperty("TargetFramework");
                    Assert.IsTrue(actualFramework.EvaluatedValue == "net6.0");
#endif
                }
            }

            return new ProjectWrapper(project);
        }

        private static string GetConfiguration()
        {
#if DEBUG
            return "Debug";
#else
            return "Release";
#endif
        }

        /// <summary>
        /// Gets the source files used by the given project
        /// </summary>
        /// <param name="projectPath">Absolute path to the project file itself</param>
        public static List<string> GetSourceFiles(string projectPath)
        {
            List<string> items = new List<string>();

            projectPath = Path.GetFullPath(projectPath);

            using (var project = LoadProject(projectPath))
            {
                foreach (var buildItem in project.GetItems("Compile"))
                {
                    items.Add(buildItem.EvaluatedInclude);
                }
            }

            MakeFullPaths(items, Path.GetDirectoryName(projectPath));
            return items;
        }

        /// <summary>
        /// Expands any relative paths to be full paths, using the given base directory
        /// </summary>
        /// <param name="files"></param>
        /// <param name="baseDir"></param>
        public static void MakeFullPaths(IList<string> files, string baseDir)
        {
            for (int i = 0; i < files.Count; ++i)
            {
                files[i] = MakeFullPath(files[i], baseDir);
            }
        }

        public static string MakeFullPath(string file, string baseDir)
        {
            if (!Path.IsPathRooted(file))
            {
                file = Path.Combine(baseDir, file);
            }
            if (file.Contains(".."))
            {
                file = Path.GetFullPath(file);
            }
            return file;
        }

        /// <summary>
        /// Converts a collection of strings to a collection of task items.
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public static List<ITaskItem> AsTaskItems(IEnumerable<string> items)
        {
            List<ITaskItem> result = new List<ITaskItem>(items.Count());
            foreach (string s in items)
            {
                result.Add(new TaskItem(s));
            }
            return result;
        }

        public sealed class ProjectWrapper : IDisposable
        {
            private ProjectInstance _projectInstance;

            private BuildManager BuildManager => BuildManager.DefaultBuildManager;

            public Project Project { get; }
            public ProjectInstance ProjectInstance
            {
                get
                {
                    return (_projectInstance) ?? (_projectInstance = BuildManager.GetProjectInstanceForBuild(Project));
                }
            }

            public ProjectWrapper(Project project)
            {
                this.Project = project;
            }

            public BuildResult Build(string[] targets, IEnumerable<Microsoft.Build.Framework.ILogger> loggers = null)
            {
                var parameters = new BuildParameters()
                {
                    GlobalProperties = new Dictionary<string, string>()
                     {
                         {"Configuration", "Debug" },
                     },
                    DisableInProcNode = true,
                    Loggers = loggers,                    
                };

                var projectInstance = BuildManager.GetProjectInstanceForBuild(Project);
                return BuildManager.Build(parameters, new BuildRequestData(projectInstance, targets));
            }

            internal string GetPropertyValue(string v)
            {
                return this.Project.GetPropertyValue(v);
            }

            internal ICollection<ProjectItemInstance> GetItems(string v)
            {
                return this.ProjectInstance.GetItems(v);
            }

            #region IDisposable Support
            private bool disposedValue = false; // To detect redundant calls

            private void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        _projectInstance = null;
                        BuildManager.ResetCaches();
                        //if (_buildManager != null)
                        //{
                        //    _buildManager.ResetCaches();
                        //    _buildManager.Dispose();

                        //}

                        // Unload project to remove it from cached static data
                        var projectCollection = Project.ProjectCollection;
                        projectCollection.UnloadProject(Project);
                        projectCollection.UnloadAllProjects();
                        projectCollection.UnregisterAllLoggers();
                        projectCollection.RemoveAllToolsets();
                        projectCollection.Dispose();
                    }

                    disposedValue = true;
                }
            }

            // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
            // ~ProjectWrapper() {
            //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            //   Dispose(false);
            // }

            // This code added to correctly implement the disposable pattern.
            public void Dispose()
            {
                // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
                Dispose(true);
                // TODO: uncomment the following line if the finalizer is overridden above.
                // GC.SuppressFinalize(this);
            }
            #endregion
        }

        private class ErrorLogger : Microsoft.Build.Framework.ILogger
        {
            private readonly List<string> _errors = new List<string>();

            public void Initialize(IEventSource eventSource)
            {
                //eventSource.ErrorRaised += (s, a) => this._errors.Add($"{a.File}({a.LineNumber},{a.ColumnNumber}): error {a.Code}: {a.Message}");
                //eventSource.WarningRaised += (s, a) => this._errors.Add($"{a.File}({a.LineNumber},{a.ColumnNumber}): error {a.Code}: {a.Message}");
                eventSource.AnyEventRaised += (s, a) => this._errors.Add(a.Message);
            }

            public void Shutdown() { }

            public IEnumerable<string> Errors { get { return this._errors; } }
            public string Parameters { get; set; }
            public LoggerVerbosity Verbosity { get; set; }
        }
    }
}
