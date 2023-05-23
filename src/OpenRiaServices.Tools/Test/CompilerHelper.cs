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
using System.Collections.Immutable;

namespace OpenRiaServices.Tools.Test
{
    public class CompilerHelper
    {
        // The version of Silverlight we use in registry keys below
        private const string SLVER = "v5.0";
        private static Dictionary<string, PortableExecutableReference> s_referenceCache = new Dictionary<string, PortableExecutableReference>();

        private static ParseOptions s_cSharpParseOptions = new CSharpParseOptions(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp5, preprocessorSymbols: new
                    [] { "SILVERLIGHT" });

        private static ParseOptions s_VbParseOptions = new VisualBasicParseOptions(Microsoft.CodeAnalysis.VisualBasic.LanguageVersion.VisualBasic14,
                    preprocessorSymbols: new
                    [] { new KeyValuePair<string, object>("SILVERLIGHT", 1) });

        /// <summary>
        /// Invokes CSharp compilation of the given files against the given set of references
        /// </summary>
        /// <param name="files"></param>
        /// <param name="referenceAssemblies"></param>
        /// <param name="documentationFile">If nonblank, the documentation file to generate during the compile.</param>
        public static bool CompileCSharpSourceFromFiles(IEnumerable<string> files, IEnumerable<string> referenceAssemblies, string documentationFile)
        {
            var sources = files.Select(filename => LoadFile(filename));

            // The Compile method will throw on error, this method always returns true on success
            using (var stream = CompileCSharpSilverlightAssembly("tempFile", sources, referenceAssemblies, documentationFile: documentationFile))
                return true;
        }

        /// <summary>
        /// Invokes VisualBasic compilation of the given files against the given set of references
        /// </summary>
        /// <param name="files"></param>
        /// <param name="referenceAssemblies"></param>
        /// <param name="documentationFile">If nonblank, the documentation file to generate during the compile.</param>
        /// <param name="rootNamespace">the projects rootNamespace</param>
        public static bool CompileVisualBasicSourceFromFiles(IEnumerable<string> files, IEnumerable<string> referenceAssemblies, string rootNamespace, string documentationFile)
        {
            var sources = files.Select(filename => LoadFile(filename));

            // The Compile method will throw on error, this method always returns true on success
            using (var stream = CompileVBSilverlightAssembly("tempFile", sources, referenceAssemblies, rootNamespace, documentationFile))
                return true;
        }

        /// <summary>
        /// Perform CSharp 'Silverlight' compilation of the given source files and refernces to produce
        /// an in memory assembly.
        /// </summary>
        /// <param name="assemblyName"></param>
        /// <param name="sources"></param>
        /// <param name="referenceAssemblies"></param>
        /// <param name="documentationFile"></param>
        /// <returns></returns>
        public static MemoryStream CompileCSharpSilverlightAssembly(string assemblyName,
            IEnumerable<SourceText> sources,
            IEnumerable<string> referenceAssemblies,
            string documentationFile = null)
        {
            List<MetadataReference> references = LoadReferences(referenceAssemblies);

            try
            {
                // Parse files
                var syntaxTrees = sources
                    .Select(text => Microsoft.CodeAnalysis.CSharp.SyntaxFactory.ParseSyntaxTree(text, s_cSharpParseOptions))
                    .ToList();

                // Do compilation when parsing succeeded
                var compileOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                    assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default);
                Compilation compilation = CSharpCompilation.Create(assemblyName, syntaxTrees, references, compileOptions);

                return Compile(compilation, documentationFile);
            }
            catch (UnitTestAssertException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Assert.Fail("Exception occurred on Csharp compilation. \nError: {0}", ex);
                // We will never get here since assert will throw
                return null;
            }
        }

        /// <summary>
        /// Perform VB 'Silverlight' compilation of the given source files and refernces to produce
        /// an in memory assembly.
        /// </summary>
        /// <param name="assemblyName"></param>
        /// <param name="sources"></param>
        /// <param name="referenceAssemblies"></param>
        /// <param name="documentationFile"></param>
        /// <param name="files">todo: describe files parameter on CompileVBSilverlightAssembly</param>
        /// <param name="rootNamespace">todo: describe rootNamespace parameter on CompileVBSilverlightAssembly</param>
        /// <returns></returns>
        public static MemoryStream CompileVBSilverlightAssembly(string assemblyName,
            IEnumerable<SourceText> sources,
            IEnumerable<string> referenceAssemblies,
            string rootNamespace,
            string documentationFile = null)
        {
            List<MetadataReference> references = LoadReferences(referenceAssemblies);
#if NETFRAMEWORK // Not required for Net6
            references.Add(GetVisualBasicReference());
#endif

            try
            {
                // Parse files
                var syntaxTrees = sources
                    .Select(text => Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory.ParseSyntaxTree(text, s_VbParseOptions))
                    .ToList();

                // Do compilation when parsing succeeded
                var compileOptions = new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                    rootNamespace: rootNamespace,
                    assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default);
                Compilation compilation = VisualBasicCompilation.Create(assemblyName, syntaxTrees, references, compileOptions);

                // Same file
                return Compile(compilation, documentationFile);
            }
            catch (UnitTestAssertException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Assert.Fail("Exception occurred invoking compiling VB sources. \nError: {0}", ex);
                // We will never get here since assert will throw
                return null;
            }
        }

        private static SourceText LoadFile(string filename)
        {
            using (var file = File.OpenRead(filename))
            {
                return SourceText.From(file);
            }
        }

        public static SyntaxTree ParseVBFile(string filename, ParseOptions options)
        {
            var stringText = LoadFile(filename);
            return Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory.ParseSyntaxTree(stringText, options, filename);
        }

        /// <summary>
        /// Loads a referenced dll from file so it can be used for compilation.
        /// Files are cached to allow reuse
        /// </summary>
        /// <param name="filename">full path to dll</param>
        private static PortableExecutableReference LoadReference(string filename)
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

        /// <summary>
        /// Loads the special Microsoft.VisualBasic.dll required for several VB specific operations
        /// </summary>
        private static PortableExecutableReference GetVisualBasicReference()
        {
            return LoadReference(typeof(int).Assembly.Location.Replace("mscorlib", "Microsoft.VisualBasic"));
        }

        /// <summary>
        /// Perform actual compilation and return the resulting assembly as a <see cref="MemoryStream"/>.
        /// Assert (Throws) that compilation succeeds so can 
        /// </summary>
        /// <param name="compilation"></param>
        /// <param name="documentationFile"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Load referenced dll files
        /// </summary>
        /// <param name="referenceAssemblies">The sources to load (must be full path)</param>
        private static List<MetadataReference> LoadReferences(IEnumerable<string> referenceAssemblies)
        {
            var references = new List<MetadataReference>();
            foreach (string s in referenceAssemblies)
                references.Add(LoadReference(s));
            return references;
        }



        /// <summary>
        /// Extract the list of assemblies both generated and referenced by Client.
        /// Not coincidently, this list is what a client project needs to reference.
        /// </summary>
        /// <returns></returns>
        public static List<string> GetClientAssemblies(string relativeTestDir)
        {
            string projectPath = string.Empty;  // path to current project
            string outputPath = string.Empty;   // output path for current project, used to infer output path of test project
            TestHelper.GetProjectPaths(relativeTestDir, out projectPath, out outputPath);

            // Our current project's folder
            string projectDir = Path.GetDirectoryName(projectPath);

            // Folder of project we want to build
            string testProjectDir = Path.GetFullPath(Path.Combine(projectDir, @"..\..\OpenRiaServices.Client.Web\Framework"));

            string projectOutputDir = Path.Combine(testProjectDir, outputPath);
            string testProjectFile = Path.Combine(testProjectDir, @"OpenRiaServices.Client.Web.csproj");
            Assert.IsTrue(File.Exists(testProjectFile), "This test could not find its required project at " + testProjectFile);

            // Retrieve all the assembly references from the test project (follows project-to-project references too)
            List<string> assemblies = MsBuildHelper.GetReferenceAssemblies(testProjectFile);
            string outputAssembly = MsBuildHelper.GetOutputAssembly(testProjectFile);
            if (!string.IsNullOrEmpty(outputAssembly))
            {
                assemblies.Add(outputAssembly);
            }

            return assemblies;

        }
    }
}
