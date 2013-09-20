using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel.DomainServices.Server.Test.Utilities;
using Microsoft.Build.Framework;
using Microsoft.Build.Tasks;
using Microsoft.Build.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32;

namespace Microsoft.ServiceModel.DomainServices.Tools.Test
{
    internal class CompilerHelper
    {
        // The version of Silverlight we use in registry keys below
        private const string SLVER = "v4.0";

        /// <summary>
        /// Invokes CSC to build the given files against the given set of references
        /// </summary>
        /// <param name="files"></param>
        /// <param name="referenceAssemblies"></param>
        /// <param name="lang"></param>
        /// <param name="documentationFile">If nonblank, the documentation file to generate during the compile.</param>
        public static bool CompileCSharpSource(IEnumerable<string> files, IEnumerable<string> referenceAssemblies, string documentationFile)
        {
            List<ITaskItem> sources = new List<ITaskItem>();
            foreach (string f in files)
                sources.Add(new TaskItem(f));

            List<ITaskItem> references = new List<ITaskItem>();
            foreach (string s in referenceAssemblies)
                references.Add(new TaskItem(s));

            Csc csc = new Csc();
            MockBuildEngine buildEngine = new MockBuildEngine();
            csc.BuildEngine = buildEngine;  // needed before task can log

            csc.NoStandardLib = true;   // don't include std lib stuff -- we're feeding it silverlight
            csc.NoConfig = true;        // don't load the csc.rsp file to get references
            csc.TargetType = "library";
            csc.Sources = sources.ToArray();
            csc.References = references.ToArray();
            csc.DefineConstants += "SILVERLIGHT";
            if (!string.IsNullOrEmpty(documentationFile))
            {
                csc.DocumentationFile = documentationFile;
            }
 
            bool result = false;
            try
            {
                result = csc.Execute();
            }
            catch (Exception ex)
            {
                Assert.Fail("Exception occurred invoking CSC task on " + sources[0].ItemSpec + ":\r\n" + ex);
            }
            
            Assert.IsTrue(result, "CSC failed to compile " + sources[0].ItemSpec + ":\r\n" + buildEngine.ConsoleLogger.Errors);
            return result;
        }

        /// <summary>
        /// Invokes VBC to build the given files against the given set of references
        /// </summary>
        /// <param name="files"></param>
        /// <param name="referenceAssemblies"></param>
        /// <param name="documentationFile">If nonblank, the documentation file to generate during the compile.</param>
        public static bool CompileVisualBasicSource(IEnumerable<string> files, IEnumerable<string> referenceAssemblies, string rootNamespace, string documentationFile)
        {
            List<ITaskItem> sources = new List<ITaskItem>();
            foreach (string f in files)
                sources.Add(new TaskItem(f));

            // Transform references into a list of ITaskItems.
            // Here, we skip over mscorlib explicitly because this is already included as a project reference.
            List<ITaskItem> references =
                referenceAssemblies
                    .Where(reference => !reference.EndsWith("mscorlib.dll", StringComparison.Ordinal))
                    .Select<string, ITaskItem>(reference => new TaskItem(reference) as ITaskItem)
                    .ToList();

            Vbc vbc = new Vbc();
            MockBuildEngine buildEngine = new MockBuildEngine();
            vbc.BuildEngine = buildEngine;  // needed before task can log

            vbc.NoStandardLib = true;   // don't include std lib stuff -- we're feeding it silverlight
            vbc.NoConfig = true;        // don't load the vbc.rsp file to get references
            vbc.TargetType = "library";
            vbc.Sources = sources.ToArray();
            vbc.References = references.ToArray();
            vbc.SdkPath = GetSilverlightSdkReferenceAssembliesPath();
            vbc.DefineConstants += "SILVERLIGHT";
            if (!string.IsNullOrEmpty(rootNamespace))
            {
                vbc.RootNamespace = rootNamespace;
            }

            if (!string.IsNullOrEmpty(documentationFile))
            {
                vbc.DocumentationFile = documentationFile;
            }

            bool result = false;
            try
            {
                result = vbc.Execute();
            }
            catch (Exception ex)
            {
                Assert.Fail("Exception occurred invoking VBC task on " + sources[0].ItemSpec + ":\r\n" + ex);
            }

            Assert.IsTrue(result, "VBC failed to compile " + sources[0].ItemSpec + ":\r\n" + buildEngine.ConsoleLogger.Errors);
            return result;
        }

        /// <summary>
        /// Extract the list of assemblies both generated and referenced by SilverlightClient.
        /// Not coincidently, this list is what a client project needs to reference.
        /// </summary>
        /// <returns></returns>
        public static List<string> GetSilverlightClientAssemblies(string relativeTestDir)
        {
            List<string> assemblies = new List<string>();

            string projectPath = string.Empty;  // path to current project
            string outputPath = string.Empty;   // output path for current project, used to infer output path of test project
            TestHelper.GetProjectPaths(relativeTestDir, out projectPath, out outputPath);

            // Our current project's folder
            string projectDir = Path.GetDirectoryName(projectPath);

            // Folder of project we want to build
            string testProjectDir = Path.GetFullPath(Path.Combine(projectDir, @"..\..\System.ServiceModel.DomainServices.Client.Web\Framework\Silverlight"));

            string projectOutputDir = Path.Combine(testProjectDir, outputPath);
            string testProjectFile = Path.Combine(testProjectDir, @"System.ServiceModel.DomainServices.Client.Web.csproj");
            Assert.IsTrue(File.Exists(testProjectFile), "This test could not find its required project at " + testProjectFile);

            // Retrieve all the assembly references from the test project (follows project-to-project references too)
            MsBuildHelper.GetReferenceAssemblies(testProjectFile, assemblies);
            string outputAssembly = MsBuildHelper.GetOutputAssembly(testProjectFile);
            if (!string.IsNullOrEmpty(outputAssembly))
            {
                assemblies.Add(outputAssembly);
            }

            // add other required SL assemblies
            assemblies.Add(Path.Combine(GetSilverlightSdkInstallPath(), "Libraries\\Client\\System.Xml.Linq.dll"));

            return assemblies;
        }

        /// <summary>
        /// Returns the path to the Silverlight SDK reference assemblies directory.
        /// </summary>
        /// <returns>The reference assemblies path for Silverlight.</returns>
        internal static string GetSilverlightSdkReferenceAssembliesPath()
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Microsoft SDKs\Silverlight\" + SLVER + @"\ReferenceAssemblies"))
                {
                    if (key != null)
                    {
                        return (string)key.GetValue("SLRuntimeInstallPath");
                    }
                }
            }
            catch
            {
            }
            return null;
        }

        /// <summary>
        /// Returns the path to the Silverlight SDK install directory.
        /// </summary>
        /// <returns>The SDK install path for Silverlight.</returns>
        internal static string GetSilverlightSdkInstallPath()
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Microsoft SDKs\Silverlight\" + SLVER + @"\Install Path"))
                {
                    if (key != null)
                    {
                        return (string)key.GetValue("Install Path");
                    }
                }
            }
            catch
            {
            }
            return null;
        }
    }
}
