using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using OpenRiaServices.DomainServices.Client.Test;
using OpenRiaServices.DomainServices.Server.Test.Utilities;
using Microsoft.Build.BuildEngine;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ConsoleLogger = OpenRiaServices.DomainServices.Server.Test.Utilities.ConsoleLogger;

namespace OpenRiaServices.DomainServices.Tools.Test
{
    /// <summary>
    /// Tests for custom build task to generate client proxies
    /// </summary>
    [TestClass]
    public class ProjectSourceFileCacheTests
    {
        public ProjectSourceFileCacheTests()
        {
        }

        [TestMethod]
        [Description("Tests all positive and negative ctor patterns for ProjectSourceFileCache")]
        public void ProjectSourceFileCache_Ctor()
        {
            ProjectSourceFileCache cache;

            ConsoleLogger logger = new ConsoleLogger();
            using (ProjectFileReader projectFileReader = new ProjectFileReader(logger))
            {
                // Null/empty server project throws
                ExceptionHelper.ExpectArgumentNullExceptionStandard(() => cache = new ProjectSourceFileCache(null, "breadCrumb", logger, projectFileReader), "rootProjectPath");
                ExceptionHelper.ExpectArgumentNullExceptionStandard(() => cache = new ProjectSourceFileCache(string.Empty, "breadCrumb", logger, projectFileReader), "rootProjectPath");

                // Null/empty bread crumb file throws
                ExceptionHelper.ExpectArgumentNullExceptionStandard(() => cache = new ProjectSourceFileCache("proj", null, logger, projectFileReader), "historyFilePath");
                ExceptionHelper.ExpectArgumentNullExceptionStandard(() => cache = new ProjectSourceFileCache("proj", string.Empty, logger, projectFileReader), "historyFilePath");

                // Null logger throws
                ExceptionHelper.ExpectArgumentNullExceptionStandard(() => cache = new ProjectSourceFileCache("proj", "breadCrumb", null, projectFileReader), "logger");

                // Null projctFileReader throws
                ExceptionHelper.ExpectArgumentNullExceptionStandard(() => cache = new ProjectSourceFileCache("proj", "breadCrumb", logger, null), "projectFileReader");

                // Valid ctor succeeds
                cache = new ProjectSourceFileCache("proj", "breadCrumb", logger, projectFileReader);
            }
        }

        [TestMethod]
        [Description("Tests indexer for ProjectSourceFileCache")]
        public void ProjectSourceFileCache_Indexer()
        {
            ConsoleLogger logger = new ConsoleLogger();
            using (ProjectFileReader projectFileReader = new ProjectFileReader(logger))
            {

                ProjectSourceFileCache cache = new ProjectSourceFileCache("proj", "breadCrumb", logger, projectFileReader);

                // First access allocates empty instance
                Assert.IsNotNull(cache.SourceFilesByProject);
                Assert.AreEqual(0, cache.SourceFilesByProject.Count, "Expected empty cache");

                // Indexer setter can be called
                cache["proj"] = new string[] { "a", "b", "c" };

                // Which invalidates the currency of the cache
                Assert.IsFalse(cache.IsFileCacheCurrent, "Adding to the cache should have marked the file cache as out of date.");

                // And indexer getter returns the inputs
                IEnumerable<string> files = cache["proj"];
                Assert.IsNotNull(files);
                Assert.AreEqual(3, files.Count());
                Assert.IsTrue(files.Contains("a"));
                Assert.IsTrue(files.Contains("b"));
                Assert.IsTrue(files.Contains("c"));

                // Cache is case insensitive.  Should overwrite entry differing only in case
                cache["PrOj"] = new string[] { "d", "e" };
                Assert.AreEqual(1, cache.SourceFilesByProject.Count, "Key differing only in case should have overwritten existing one");
                files = cache["PrOj"];
                Assert.IsNotNull(files);

                // And can read out again in different case
                files = cache["pRoJ"];
                Assert.IsNotNull(files);

                // Null value clears entry
                cache["proj"] = null;
                files = cache["proj"];
                Assert.IsNull(files);

                // ArgNull exception on null/empty index setter
                ExceptionHelper.ExpectArgumentNullExceptionStandard(() => cache[null] = new string[] { "a" }, "projectPath");
                ExceptionHelper.ExpectArgumentNullExceptionStandard(() => cache[string.Empty] = new string[] { "a" }, "projectPath");

                // ArgNull exception on null/empty index getter
                ExceptionHelper.ExpectArgumentNullExceptionStandard(() => files = cache[null], "projectPath");
                ExceptionHelper.ExpectArgumentNullExceptionStandard(() => files = cache[string.Empty], "projectPath");
            }
        }

        [TestMethod]
        [Description("Tests ProjectSourceFileCache.GetAllKnownProjects")]
        public void ProjectSourceFileCache_AllKnownProjects()
        {
            ConsoleLogger logger = new ConsoleLogger();
            using (ProjectFileReader projectFileReader = new ProjectFileReader(logger))
            {
                ProjectSourceFileCache cache = new ProjectSourceFileCache("proj", "breadCrumb", logger, projectFileReader);

                cache["p1"] = new string[] { "a", "b" };
                cache["p2"] = new string[] { "c" };

                IEnumerable<string> projects = cache.GetAllKnownProjects();
                Assert.IsNotNull(projects);
                Assert.AreEqual(2, projects.Count());
                Assert.IsTrue(projects.Contains("p1"));
                Assert.IsTrue(projects.Contains("p2"));
            }
        }

        [TestMethod]
        [Description("ProjectSourceFileCache warns about non-existent project file")]
        public void ProjectSourceFileCache_Nonexistent_Project_File_Warns()
        {
            ConsoleLogger logger = new ConsoleLogger();
            string badProjectPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".csproj");
            using (ProjectFileReader projectFileReader = new ProjectFileReader(logger))
            {
                try
                {
                    ProjectSourceFileCache cache = new ProjectSourceFileCache(badProjectPath, "breadCrumb", logger, projectFileReader);
                    IEnumerable<string> projects = cache.GetAllKnownProjects();

                    string warningMessage = string.Format(CultureInfo.CurrentCulture, Resource.Project_Does_Not_Exist, badProjectPath);

                    TestHelper.AssertContainsWarnings(logger, new string[] { warningMessage });

                }
                finally
                {
                    File.Delete(badProjectPath);
                }
            }
        }

        [TestMethod]
        [Description("ProjectSourceFileCache warns about non-existent project file when asking for its source files")]
        public void ProjectSourceFileCache_Nonexistent_Project_File_SourceFiles_Warns()
        {
            ConsoleLogger logger = new ConsoleLogger();
            using (ProjectFileReader projectFileReader = new ProjectFileReader(logger))
            {
                string badProjectPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".csproj");

                try
                {
                    ProjectSourceFileCache cache = new ProjectSourceFileCache("proj", "breadCrumb", logger, projectFileReader);
                    IEnumerable<string> sourceFiles = projectFileReader.LoadSourceFilesFromProject(badProjectPath);

                    string warningMessage = string.Format(CultureInfo.CurrentCulture, Resource.Project_Does_Not_Exist, badProjectPath);

                    TestHelper.AssertContainsWarnings(logger, new string[] { warningMessage });

                }
                finally
                {
                    File.Delete(badProjectPath);
                }
            }
        }

        [TestMethod]
        [Description("ProjectSourceFileCache warns about invalid project file")]
        public void ProjectSourceFileCache_Bad_Project_File_Warns()
        {
            ConsoleLogger logger = new ConsoleLogger();
            using (ProjectFileReader projectFileReader = new ProjectFileReader(logger))
            {
                string badProjectPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".csproj");
                File.WriteAllText(badProjectPath, "neener neener");

                try
                {
                    ProjectSourceFileCache cache = new ProjectSourceFileCache(badProjectPath, "breadCrumb", logger, projectFileReader);
                    IEnumerable<string> projects = cache.GetAllKnownProjects();

                    // Simulate the exception so we get the exact text
                    string warningMessage = null;
                    try
                    {
                        Engine engine = new Engine();
                        Project project = new Project(engine);
                        project.Load(badProjectPath);

                    }
                    catch (InvalidProjectFileException ipfe)
                    {
                        warningMessage = string.Format(CultureInfo.CurrentCulture, Resource.Failed_To_Open_Project, badProjectPath, ipfe.Message);
                    }

                    TestHelper.AssertContainsWarnings(logger, new string[] { warningMessage });

                }
                finally
                {
                    File.Delete(badProjectPath);
                }
            }
        }

        [TestMethod]
        [Description("ProjectSourceFileCache warns about invalid project file when asking for source files")]
        public void ProjectSourceFileCache_Bad_Project_File_Warns_SourceFiles()
        {
            ConsoleLogger logger = new ConsoleLogger();
            using (ProjectFileReader projectFileReader = new ProjectFileReader(logger))
            {
                string badProjectPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".csproj");
                File.WriteAllText(badProjectPath, "neener neener");

                try
                {
                    ProjectSourceFileCache cache = new ProjectSourceFileCache(badProjectPath, "breadCrumb", logger, projectFileReader);
                    IEnumerable<string> files = projectFileReader.LoadSourceFilesFromProject(badProjectPath);

                    // Simulate the exception so we get the exact text
                    string warningMessage = null;
                    try
                    {
                        Engine engine = new Engine();
                        Project project = new Project(engine);
                        project.Load(badProjectPath);

                    }
                    catch (InvalidProjectFileException ipfe)
                    {
                        warningMessage = string.Format(CultureInfo.CurrentCulture, Resource.Failed_To_Open_Project, badProjectPath, ipfe.Message);
                    }

                    TestHelper.AssertContainsWarnings(logger, new string[] { warningMessage });

                }
                finally
                {
                    File.Delete(badProjectPath);
                }
            }
        }

        [TestMethod]
        [Description("ProjectSourceFileCache warns about failed write to breadcrumb file")]
        public void ProjectSourceFileCache_Failed_BreadCrumb_Save_Warns()
        {
            ConsoleLogger logger = new ConsoleLogger();
            using (ProjectFileReader projectFileReader = new ProjectFileReader(logger))
            {
                string projectPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".csproj");
                string breadCrumbPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".txt");
                string csFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".cs");

                // Need file on disk only for timestamp
                File.WriteAllText(projectPath, "neener neener");
                File.WriteAllText(breadCrumbPath, "bread crumbs");
                File.WriteAllText(csFile, "//");

                try
                {
                    ProjectSourceFileCache cache = new ProjectSourceFileCache(projectPath, breadCrumbPath, logger, projectFileReader);
                    cache.SourceFilesByProject[projectPath] = new string[] { csFile };

                    // Setup for failure
                    File.SetAttributes(breadCrumbPath, FileAttributes.ReadOnly);

                    // Ask to write to readonly file -- should fail
                    cache.SaveCacheToFile();

                    // Simulate the exception so we get the exact text
                    string warningMessage = null;
                    try
                    {

                        // this should fail
                        File.WriteAllText(breadCrumbPath, "stuff");
                    }
                    catch (UnauthorizedAccessException uae)
                    {
                        warningMessage = string.Format(CultureInfo.CurrentCulture, Resource.Failed_To_Write_File, breadCrumbPath, uae.Message);
                    }

                    TestHelper.AssertContainsWarnings(logger, new string[] { warningMessage });

                }
                finally
                {
                    File.Delete(projectPath);
                    File.SetAttributes(breadCrumbPath, File.GetAttributes(breadCrumbPath) & ~FileAttributes.ReadOnly);
                    File.Delete(breadCrumbPath);
                    File.Delete(csFile);
                }
            }
        }

        [TestMethod]
        [Description("Tests ProjectSourceFileCache loads files for project")]
        [DeploymentItem(@"OpenRiaServices.DomainServices.Tools\Test\ProjectPath.txt", "SSFC")]
        public void ProjectSourceFileCache_Loads_Real_Project()
        {
            string projectPath = null;
            string outputPath = null;
            TestHelper.GetProjectPaths("SSFC", out projectPath, out outputPath);
            string serverProjectPath = CodeGenHelper.ServerClassLibProjectPath(projectPath);
            string breadCrumbFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".txt");
            ConsoleLogger logger = new ConsoleLogger();
            using (ProjectFileReader projectFileReader = new ProjectFileReader(logger))
            {

                try
                {
                    // Instantiate the cache.
                    ProjectSourceFileCache cache = new ProjectSourceFileCache(serverProjectPath, breadCrumbFile, logger, projectFileReader);

                    // Initially, it will not have loaded anything
                    Assert.AreEqual(0, cache.SourceFilesByProject.Count);

                    // -------------------------------------------------------------
                    // Validate cache is loaded correctly from .csproj files
                    // -------------------------------------------------------------
                    this.ValidateProjectSourceFileCache(cache, projectPath);

                    // Ask to write out the breadcrumb file
                    Assert.IsFalse(File.Exists(breadCrumbFile));
                    bool success = cache.SaveCacheToFile();
                    Assert.IsTrue(success);
                    Assert.IsTrue(File.Exists(breadCrumbFile));

                    // Clear the cache and force it to read from breadcrumb
                    cache.SourceFilesByProject.Clear();

                    success = cache.LoadCacheFromFile();
                    Assert.IsTrue(success, "Failed to load from breadCrumb file");
                    Assert.IsTrue(cache.IsFileCacheCurrent, "Loading from file should have marked cache as current with file.");

                    // -------------------------------------------------------------
                    // Validate cache is loaded correctly from breadcrumb file
                    // -------------------------------------------------------------
                    this.ValidateProjectSourceFileCache(cache, projectPath);

                    // Now mark the breadcrumb file as if it had been written before the ServerClassLib project
                    cache.SourceFilesByProject.Clear();

                    DateTime serverLastWriteTime = File.GetLastWriteTime(serverProjectPath);
                    DateTime beforeServerLastWriteTime = new DateTime(serverLastWriteTime.Ticks - 1000);
                    File.SetLastWriteTime(breadCrumbFile, beforeServerLastWriteTime);

                    // -------------------------------------------------------------
                    // Validate cache is *not* loaded if timestamp is before project's
                    // -------------------------------------------------------------
                    success = cache.LoadCacheFromFile();
                    Assert.IsFalse(success, "Expected breadCrumbFile time stamp to be caught and reject load");
                    Assert.IsFalse(cache.IsFileCacheCurrent, "Failed load from file should have marked cache as *not* current with file.");

                    // Cache should still be empty
                    Assert.AreEqual(0, cache.SourceFilesByProject.Count);

                    // -------------------------------------------------------------
                    // Validate cache loaded in presence of out-of-date breadcrumb
                    // file loads correctly
                    // -------------------------------------------------------------
                    this.ValidateProjectSourceFileCache(cache, projectPath);

                    Assert.IsFalse(cache.IsFileCacheCurrent, "Loading from project should have marked cache as *not* current with file.");
                }
                finally
                {
                    if (File.Exists(breadCrumbFile))
                        File.Delete(breadCrumbFile);
                }
            }
        }

        /// <summary>
        /// Helper class to validate the cache contains what we expect it to
        /// contain after loading the real ServerClassLib project
        /// </summary>
        /// <param name="cache"></param>
        /// <param name="projectPath"></param>
        private void ValidateProjectSourceFileCache(ProjectSourceFileCache cache, string projectPath)
        {
            string serverProjectPath = CodeGenHelper.ServerClassLibProjectPath(projectPath);
            string server2ProjectPath = CodeGenHelper.ServerClassLib2ProjectPath(projectPath);

            string[] expectedServerFiles = new string[] { "TestEntity.shared.cs" };
            string[] expectedServer2Files = new string[] { "ServerClassLib2.shared.cs" };

            // Ask cache for all known projects.  It should open the .csproj and find project references
            IEnumerable<string> projects = cache.GetAllKnownProjects();
            Assert.IsNotNull(projects);

            // We expect to find ServerClassLib and ServerClassLib2 in the set of known projects.
            // There may be others due to normal project references, but we don't care about them
            Assert.IsTrue(projects.Contains(serverProjectPath), "Expected to find " + serverProjectPath + " in list of known projects");
            Assert.IsTrue(projects.Contains(server2ProjectPath), "Expected to find " + server2ProjectPath + " in list of known projects");

            IEnumerable<string> serverFiles = cache.GetSourceFilesInProject(serverProjectPath);
            Assert.IsNotNull(serverFiles);
            Assert.IsTrue(serverFiles.Count() >= expectedServerFiles.Length);
            foreach (string file in expectedServerFiles)
            {
                string expectedFile = Path.Combine(Path.GetDirectoryName(serverProjectPath), file);
                Assert.IsTrue(serverFiles.Contains(expectedFile), "Expected to see " + expectedFile + " in list of server files");
            }

            IEnumerable<string> server2Files = cache.GetSourceFilesInProject(server2ProjectPath);
            Assert.IsNotNull(server2Files);
            Assert.IsTrue(server2Files.Count() >= expectedServer2Files.Length);
            foreach (string file in expectedServer2Files)
            {
                string expectedFile = Path.Combine(Path.GetDirectoryName(server2ProjectPath), file);
                Assert.IsTrue(server2Files.Contains(expectedFile), "Expected to see " + expectedFile + " in list of server files");
            }

        }

    }
}
