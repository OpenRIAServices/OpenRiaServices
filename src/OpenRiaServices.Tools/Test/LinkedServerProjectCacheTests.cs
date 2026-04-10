using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.Client.Test;
using ConsoleLogger = OpenRiaServices.Server.Test.Utilities.ConsoleLogger;

namespace OpenRiaServices.Tools.Test
{
    /// <summary>
    /// Tests for custom build task to generate client proxies
    /// </summary>
    [TestClass]
    public class LinkedServerProjectCacheTests
    {
        public LinkedServerProjectCacheTests()
        {
        }

        [TestMethod]
        [Description("Tests all positive and negative ctor patterns for LinkedServerProjectCacheTests")]
        public void LinkedServerProjectCache_Ctor()
        {
            LinkedServerProjectCache cache;

            ConsoleLogger logger = new ConsoleLogger();
            using (ProjectFileReader projectFileReader = new ProjectFileReader(logger))
            {
                // Null project  file throws
                ExceptionHelper.ExpectArgumentNullExceptionStandard(() => cache = new LinkedServerProjectCache(null, "breadCrumb", logger, projectFileReader), "rootProjectPath");
                ExceptionHelper.ExpectArgumentNullExceptionStandard(() => cache = new LinkedServerProjectCache(String.Empty, "breadCrumb", logger, projectFileReader), "rootProjectPath");

                // Null logger throws
                ExceptionHelper.ExpectArgumentNullExceptionStandard(() => cache = new LinkedServerProjectCache("proj", null, logger, projectFileReader), "historyFilePath");
                ExceptionHelper.ExpectArgumentNullExceptionStandard(() => cache = new LinkedServerProjectCache("proj", String.Empty, logger, projectFileReader), "historyFilePath");

                // Null logger throws
                ExceptionHelper.ExpectArgumentNullExceptionStandard(() => cache = new LinkedServerProjectCache("proj", "breadCrumb", null, projectFileReader), "logger");

                // Null projectFileLogger throws
                ExceptionHelper.ExpectArgumentNullExceptionStandard(() => cache = new LinkedServerProjectCache("proj", "breadCrumb", logger, null), "projectFileReader");

                // Valid ctor succeeds
                cache = new LinkedServerProjectCache("proj", "breadCrumb", logger, projectFileReader);
            }
        }

        [TestMethod]
        [Description("Tests indexer for ProjectSourceFileCache")]
        public void LinkedServerProjectCache_Indexer()
        {
            ConsoleLogger logger = new ConsoleLogger();
            using (ProjectFileReader projectFileReader = new ProjectFileReader(logger))
            {
                string projectFile = this.CreateMockProjectFile();
                string historyFile = this.CreateMockHistoryFile();
                LinkedServerProjectCache cache = new LinkedServerProjectCache(projectFile, historyFile, logger, projectFileReader);

                try
                {
                    // First access allocates empty instance
                    Assert.IsNotNull(cache.LinkedServerProjectsByProject, "Expected non null dictionary after ctor");
                    Assert.IsEmpty(cache.LinkedServerProjectsByProject, "Expected empty cache");

                    // Null indexer parameter throws on sets
                    ExceptionHelper.ExpectArgumentNullExceptionStandard((() => cache[null] = "x"), "projectPath");
                    ExceptionHelper.ExpectArgumentNullExceptionStandard((() => cache[string.Empty] = "x"), "projectPath");

                    // Null indexer parameter throws on gets
                    string unused;
                    ExceptionHelper.ExpectArgumentNullExceptionStandard((() => unused = cache[null]), "projectPath");
                    ExceptionHelper.ExpectArgumentNullExceptionStandard((() => unused = cache[string.Empty]), "projectPath");

                    // Indexer setter can be called with valid values
                    cache["proj1"] = "proj1.Web";
                    cache["proj2"] = "proj2.Web";

                    // Nulls are permitted
                    cache["proj3"] = null;

                    Assert.HasCount(3, cache.LinkedServerProjectsByProject, "Expected this many entries in cache");

                    Assert.AreEqual("proj1.Web", cache["proj1"]);
                    Assert.AreEqual("proj2.Web", cache["proj2"]);
                    Assert.IsNull(cache["proj3"], "Null should have been allowed in cache");

                    // Cache is case insensitive.  Should overwrite entry differing only in case
                    cache["PrOj1"] = "PrOj1.wEb";
                    Assert.HasCount(3, cache.LinkedServerProjectsByProject, "Key differing only in case should have overwritten existing one");
                    Assert.AreEqual("PrOj1.wEb", cache["PrOj1"]);
                }
                finally
                {
                    File.Delete(projectFile);
                    File.Delete(historyFile);
                }
            }
        }

        [TestMethod]
        [Description("Tests project references and RIA link references for LinkedServerProjectCache")]
        public void LinkedServerProjectCache_RiaLinks()
        {
            ConsoleLogger logger = new ConsoleLogger();
            using (ProjectFileReader projectFileReader = new ProjectFileReader(logger))
            {
                string projectFile = this.CreateMockProjectFile();
                string historyFile = this.CreateMockHistoryFile();

                try
                {
                    LinkedServerProjectCache cache = new LinkedServerProjectCache(projectFile, historyFile, logger, projectFileReader);

                    cache["p1"] = "w1";
                    cache["p2"] = null;
                    cache["p3"] = "w3";
                    cache["p4"] = string.Empty;

                    Assert.HasCount(4, cache.LinkedServerProjectsByProject, "Should have had this many items in cache");

                    List<string> projectRefs = cache.ProjectReferences.ToList();
                    Assert.HasCount(4, projectRefs, "Expected to have this many project references in cache");

                    Assert.Contains("p1", projectRefs, "expected p1 to be in list of project refs");
                    Assert.Contains("p2", projectRefs, "expected p2 to be in list of project refs");
                    Assert.Contains("p3", projectRefs, "expected p3 to be in list of project refs");
                    Assert.Contains("p4", projectRefs, "expected p4 to be in list of project refs");

                    List<string> riaLinks = cache.LinkedServerProjects.ToList();
                    Assert.HasCount(2, riaLinks, "Expected this many project references to have RIA Links");
                    Assert.Contains("w1", riaLinks, "expected p1 to be in list of RIA links");
                    Assert.Contains("w3", riaLinks, "expected p3 to be in list of RIA links");
                }
                finally
                {
                    File.Delete(projectFile);
                    File.Delete(historyFile);
                }
            }
        }

        [TestMethod]
        [Description("Tests ability to load and save history file for LinkedServerProjectCache")]
        public void LinkedServerProjectCache_LoadAndSaveFile()
        {
            ConsoleLogger logger = new ConsoleLogger();
            using (ProjectFileReader projectFileReader = new ProjectFileReader(logger))
            {

                string historyFile = this.CreateMockHistoryFile();
                string projectFile = this.CreateMockProjectFile();
                string p1 = this.CreateMockProjectFile();
                string p2 = this.CreateMockProjectFile();
                string p3 = this.CreateMockProjectFile();
                string p4 = this.CreateMockProjectFile();

                string w1 = this.CreateMockProjectFile();
                string w3 = this.CreateMockProjectFile();

                LinkedServerProjectCache cache = new LinkedServerProjectCache(projectFile, historyFile, logger, projectFileReader);

                cache[p1] = w1;
                cache[p2] = null;
                cache[p3] = w3;
                cache[p4] = string.Empty;

                Assert.IsFalse(cache.IsFileCacheCurrent, "expected file cache not to be current after set some properties");

                try
                {
                    // Save to the cache and verify side effects of that
                    bool success = cache.SaveCacheToFile();
                    Assert.IsTrue(success, "Expected successful save of cache to history file");
                    Assert.IsTrue(File.Exists(historyFile), "Expected history file " + historyFile + " to have been written");

                    Assert.IsTrue(cache.IsFileCacheCurrent, "Expected cache to be considered current");

                    // zap our cache and read back in from file
                    cache = new LinkedServerProjectCache(projectFile, historyFile, logger, projectFileReader);

                    // The following methods and properties will lazy load the cache
                    Assert.HasCount(4, cache.LinkedServerProjectsByProject, "Should have had this many items in cache");

                    List<string> projectRefs = cache.ProjectReferences.ToList();
                    Assert.HasCount(4, projectRefs, "Expected to have this many project references in cache");

                    Assert.Contains(p1, projectRefs, "expected p1 to be in list of project refs");
                    Assert.Contains(p2, projectRefs, "expected p2 to be in list of project refs");
                    Assert.Contains(p3, projectRefs, "expected p3 to be in list of project refs");
                    Assert.Contains(p4, projectRefs, "expected p4 to be in list of project refs");

                    List<string> riaLinks = cache.LinkedServerProjects.ToList();
                    Assert.HasCount(2, riaLinks, "Expected this many project references to have RIA Links");
                    Assert.Contains(w1, riaLinks, "expected w1 to be in list of RIA links");
                    Assert.Contains(w3, riaLinks, "expected w3 to be in list of RIA links");
                }
                finally
                {
                    File.Delete(historyFile);
                    File.Delete(projectFile);
                    File.Delete(p1);
                    File.Delete(p2);
                    File.Delete(p3);
                    File.Delete(p4);
                    File.Delete(w1);
                    File.Delete(w3);
                }
            }
        }

        [TestMethod]
        [Description("Tests ability to load a real MSBuild project for LinkedServerProjectCache")]
        public void LinkedServerProjectCache_Load_Real_Project()
        {
            ConsoleLogger logger = new ConsoleLogger();
            using (ProjectFileReader projectFileReader = new ProjectFileReader(logger))
            {

                // We will create valid MSBuild projects with this shape:
                //  - projectFile: contains 3 project references
                //  - 2 of the 3 projects referenced point to unique RIA Link server project
                //  - 1 of the 3 projects references points to the same RIA Link project as another
                //     (this tests the ability of our model to handle multiple clients pointing to the same server)
                string serverProject1 = this.CreateMockProjectFile();
                string serverProject2 = this.CreateMockProjectFile();

                string refProject1Contents = string.Format(@"
<Project ToolsVersion=""4.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup>
    <LinkedOpenRiaServerProject>{0}</LinkedOpenRiaServerProject>
  </PropertyGroup>
</Project>", serverProject1);

                string refProject2Contents = string.Format(@"
<Project ToolsVersion=""4.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup>
    <LinkedOpenRiaServerProject>{0}</LinkedOpenRiaServerProject>
  </PropertyGroup>
</Project>", serverProject2);

                string refProject1 = this.CreateTempFile("csproj", refProject1Contents);
                string refProject2 = this.CreateTempFile("csproj", refProject2Contents);
                string refProject3 = this.CreateTempFile("csproj", refProject2Contents);

                string projectContents = string.Format(@"
<Project ToolsVersion=""4.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <ItemGroup>
    <ProjectReference Include=""{0}""></ProjectReference>
    <ProjectReference Include=""{1}""></ProjectReference>
    <ProjectReference Include=""{2}""></ProjectReference>
  </ItemGroup>
</Project>", refProject1, refProject2, refProject3);

                string projectFile = this.CreateTempFile("csproj", projectContents);
                string historyFile = this.CreateTempFile("txt", null);  // <-- null prevents us from creating file on disk
                try
                {
                    LinkedServerProjectCache cache = new LinkedServerProjectCache(projectFile, historyFile, logger, projectFileReader);

                    // Validate we see our 3 project references
                    List<string> projectRefs = cache.ProjectReferences.ToList();
                    Assert.HasCount(3, projectRefs, "Expected this many project references");
                    Assert.Contains(refProject1, projectRefs, "Expected ref project 1 in project references.");
                    Assert.Contains(refProject2, projectRefs, "Expected ref project 2 in project references.");
                    Assert.Contains(refProject3, projectRefs, "Expected ref project 3 in project references.");

                    // Validate that we extracted the RIA Links for those project references
                    List<string> riaLinks = cache.LinkedServerProjects.ToList();
                    Assert.HasCount(2, riaLinks, "Expected to find 2 RIA links");
                    Assert.Contains(serverProject1, riaLinks, "Expected server project 1 RIA link");
                    Assert.Contains(serverProject2, riaLinks, "Expected server project 2 RIA link");

                    // Validate that we can ask for the source of each RIA Link target
                    List<string> sources = cache.GetLinkedServerProjectSources(serverProject1).ToList();
                    Assert.HasCount(1, sources, "Expected 1 source to for RIA link to server project 1");
                    Assert.Contains(refProject1, sources, "Expected refProject 1 to be shown as RIA Link source 1");

                    sources = cache.GetLinkedServerProjectSources(serverProject2).ToList();
                    Assert.HasCount(2, sources, "Expected 2 sources to for RIA link to server project 2");
                    Assert.Contains(refProject2, sources, "Expected refProject 2 to be shown as RIA Link source 2");
                    Assert.Contains(refProject3, sources, "Expected refProject 2 to be shown as RIA Link source 2");
                }
                finally
                {
                    File.Delete(projectFile);
                    File.Delete(historyFile);
                    File.Delete(refProject1);
                    File.Delete(refProject2);
                    File.Delete(refProject3);
                    File.Delete(serverProject1);
                    File.Delete(serverProject2);

                }
            }
        }

        private string CreateTempFile(string extension, string contents)
        {
            string fileName = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            if (!string.IsNullOrEmpty(extension))
            {
                fileName += ("." + extension);
            }
            if (contents != null)
            {
                File.WriteAllText(fileName, contents);
            }
            return fileName;
        }

        private string CreateMockProjectFile()
        {
            return CreateTempFile("csproj", string.Empty);
        }

        private string CreateMockHistoryFile()
        {
            return CreateTempFile("txt", String.Empty);
        }
    }
}
