using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.VisualBasic;

namespace OpenRiaServices.DomainServices.Tools.Test
{
    internal class CompilerHelper
    {
        // The version of Silverlight we use in registry keys below
        private const string SLVER = "v5.0";
        private static Dictionary<string, PortableExecutableReference> s_referenceCache = new Dictionary<string, PortableExecutableReference>();

        private static ParseOptions _cSharpParseOptions = new CSharpParseOptions(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp5,  preprocessorSymbols: new
                    [] { "SILVERLIGHT" });

        /// <summary>
        /// Invokes CSC to build the given files against the given set of references
        /// </summary>
        /// <param name="files"></param>
        /// <param name="referenceAssemblies"></param>
        /// <param name="lang"></param>
        /// <param name="documentationFile">If nonblank, the documentation file to generate during the compile.</param>
        public static bool CompileCSharpSource(IEnumerable<string> files, IEnumerable<string> referenceAssemblies, string documentationFile)
        {
            var stream = CompileCSharpSilverlightAssembly("tempFile", files, referenceAssemblies, documentationFile);

            // The Compile method will throw on error, this method always returns true
            stream.Dispose();
            return true;
        }

        public static SourceText LoadFile(string filename)
        {
            using (var file = File.OpenRead(filename))
            {
                return SourceText.From(file);
            }
        }

        public static SyntaxTree ParseCSharpFile(string filename, ParseOptions options)
        {
            var stringText = LoadFile(filename);
            return Microsoft.CodeAnalysis.CSharp.SyntaxFactory.ParseSyntaxTree(stringText, options, filename);
        }

        public static SyntaxTree ParseVBFile(string filename, ParseOptions options)
        {
            var stringText = LoadFile(filename);
            return Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory.ParseSyntaxTree(stringText, options, filename);
        }

        public static MemoryStream CompileCSharpSilverlightAssembly(string assemblyName,
            IEnumerable<string> files,
            IEnumerable<string> referenceAssemblies,
            string documentationFile = null)
        {
            List<MetadataReference> references = GetMetadataReferences(referenceAssemblies);

            try
            {
                // Parse files
                List<SyntaxTree> syntaxTrees = new List<SyntaxTree>();
                foreach (var file in files)
                    syntaxTrees.Add(ParseCSharpFile(file, _cSharpParseOptions));

                // Do compilation when parsing succeeded
                var compileOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
                Compilation compilation = CSharpCompilation.Create(assemblyName, syntaxTrees, references, compileOptions);

                return Compile(compilation, documentationFile);
            }
            catch (Exception ex)
            {
                Assert.Fail("Exception occurred invoking CSC task on '{0}' \r\n {1}", files.FirstOrDefault(), ex);
                // We will never get here since assert will throw
                return null;
            }
        }

        public static MemoryStream CompileVBSilverlightAssembly(string assemblyName,
            IEnumerable<string> files,
            IEnumerable<string> referenceAssemblies,
            string documentationFile = null)
        {
            List<MetadataReference> references = GetMetadataReferences(referenceAssemblies);

            try
            {
                var parseOptions = new VisualBasicParseOptions(Microsoft.CodeAnalysis.VisualBasic.LanguageVersion.VisualBasic14,
                    preprocessorSymbols: new
                    [] { new KeyValuePair<string, object>("SILVERLIGHT", 1) });
                references.Add(GetVisualBasicReference());

                // Parse files
                List<SyntaxTree> syntaxTrees = new List<SyntaxTree>();
                foreach (var file in files)
                    syntaxTrees.Add(ParseVBFile(file, parseOptions));

                // Do compilation when parsing succeeded
                var compileOptions = new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
                Compilation compilation = VisualBasicCompilation.Create(assemblyName, syntaxTrees, references, compileOptions);

                // Same file
                return Compile(compilation, documentationFile);
            }
            catch (Exception ex)
            {
                Assert.Fail("Exception occurred invoking CSC task on '{0}' \r\n {1}", files.FirstOrDefault(), ex);
                // We will never get here since assert will throw
                return null;
            }
        }

        private static PortableExecutableReference
            GetReference(string filename)
        {
            PortableExecutableReference reference;

            if (s_referenceCache.TryGetValue(filename, out reference))
            {
                return reference;
            }

            reference = MetadataReference.CreateFromFile(filename);
            s_referenceCache.Add(filename, reference);
            return reference;
        }

        private static PortableExecutableReference GetVisualBasicReference()
        {
            return GetReference(Path.Combine(GetSilverlightSdkReferenceAssembliesPath(), "Microsoft.VisualBasic.dll"));
        }

        private static MemoryStream Compile(Compilation compilation, string documentationFile)
        {
            var memoryStream = new MemoryStream();
            using (Stream documentationStream = (documentationFile != null) ? File.OpenWrite(documentationFile) : null)
            {
                var emitResult = compilation.Emit(memoryStream, null, documentationStream);
                if (!emitResult.Success)
                {
                    Assert.Fail("Failed to compile assembly \r\n {0}", string.Join(" \r\n", emitResult.Diagnostics));
                }
                return memoryStream;
            }
        }

        private static List<MetadataReference> GetMetadataReferences(IEnumerable<string> referenceAssemblies)
        {
            List<MetadataReference> references = new List<MetadataReference>();
            foreach (string s in referenceAssemblies)
                references.Add(GetReference(s));
            return references;
        }

        /// <summary>
        /// Invokes VBC to build the given files against the given set of references
        /// </summary>
        /// <param name="files"></param>
        /// <param name="referenceAssemblies"></param>
        /// <param name="documentationFile">If nonblank, the documentation file to generate during the compile.</param>
        public static bool CompileVisualBasicSource(IEnumerable<string> files, IEnumerable<string> referenceAssemblies, string rootNamespace, string documentationFile)
        {
            var stream = CompileVBSilverlightAssembly("tempFile", files, referenceAssemblies, documentationFile);

            // The Compile method will throw on error, this method always returns true
            stream.Dispose();
            return true;
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
            string testProjectDir = Path.GetFullPath(Path.Combine(projectDir, @"..\..\OpenRiaServices.DomainServices.Client.Web\Framework\Silverlight"));

            string projectOutputDir = Path.Combine(testProjectDir, outputPath);
            string testProjectFile = Path.Combine(testProjectDir, @"OpenRiaServices.DomainServices.Client.Web.csproj");
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
