using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using OpenRiaServices.Server.Test.Utilities;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32;
using OpenRiaServices.Tools.TextTemplate.CSharpGenerators;
using OpenRiaServices.Tools.Test;
using Microsoft.CodeAnalysis.Text;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace OpenRiaServices.Tools.TextTemplate.Test
{
    internal class T4AssemblyGenerator : IDisposable
    {
        private readonly bool _isCSharp;
        private string _outputAssemblyName;
        private readonly IEnumerable<Type> _domainServiceTypes;
        private DomainServiceCatalog _domainServiceCatalog;
        private MockBuildEngine _mockBuildEngine;
        private MockSharedCodeService _mockSharedCodeService;
        private string _generatedCode;
        private Assembly _generatedAssembly;
        private IList<string> _referenceAssemblies;
        private string _generatedCodeFile;
        private Type[] _generatedTypes;
        private string _userCode;
        private string _userCodeFile;
        private readonly bool _useFullTypeNames;
        private MetadataLoadContext _metadataLoadContext;

        public T4AssemblyGenerator(bool isCSharp, IEnumerable<Type> domainServiceTypes) :
            this(isCSharp, false, domainServiceTypes)
        {
        }

        public T4AssemblyGenerator(bool isCSharp, bool useFullTypeNames, IEnumerable<Type> domainServiceTypes)
        {
            var paths = domainServiceTypes.Select(t => t.Assembly.Location).ToHashSet();
            string[] runtimeAssemblies = Directory.GetFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll");
            paths.UnionWith(runtimeAssemblies);
            var resolver = new Resolver(paths);
            _metadataLoadContext = new MetadataLoadContext(resolver);
            this._isCSharp = isCSharp;
            this._useFullTypeNames = useFullTypeNames;
            this._domainServiceTypes = domainServiceTypes;
        }


        internal bool IsCSharp
        {
            get
            {
                return this._isCSharp;
            }
        }

        internal bool UseFullTypeNames
        {
            get
            {
                return this._useFullTypeNames;
            }
        }

        internal string OutputAssemblyName
        {
            get
            {
                if (this._outputAssemblyName == null)
                {
                    this._outputAssemblyName = Path.GetTempFileName() + ".dll";
                }
                return this._outputAssemblyName;
            }
        }

        internal string UserCode
        {
            get
            {
                return this._userCode;
            }
            private set
            {
                this._userCode = value;
            }
        }

        internal string UserCodeFile
        {
            get
            {
                if (this._userCodeFile == null)
                {
                    if (!string.IsNullOrEmpty(this.UserCode))
                    {
                        this._userCodeFile = Path.GetTempFileName();
                        File.WriteAllText(this._userCodeFile, this.UserCode);
                    }
                }
                return this._userCodeFile;
            }
        }


        internal MockBuildEngine MockBuildEngine
        {
            get
            {
                if (this._mockBuildEngine == null)
                {
                    this._mockBuildEngine = new MockBuildEngine();
                }
                return this._mockBuildEngine;
            }
        }

        internal MockSharedCodeService MockSharedCodeService
        {
            get
            {
                if (this._mockSharedCodeService == null)
                {
                    this._mockSharedCodeService = new MockSharedCodeService(Array.Empty<Type>(), Array.Empty<MethodBase>(), Array.Empty<string>());
                }
                return this._mockSharedCodeService;
            }
        }


        internal ConsoleLogger ConsoleLogger
        {
            get
            {
                return this.MockBuildEngine.ConsoleLogger;
            }
        }

        internal IList<string> ReferenceAssemblies
        {
            get
            {
                if (this._referenceAssemblies == null)
                {
                    this._referenceAssemblies = CompilerHelper.GetClientAssemblies(string.Empty);
                }
                return this._referenceAssemblies;
            }
        }

        internal string GeneratedCode
        {
            get
            {
                if (this._generatedCode == null)
                {
                    this._generatedCode = this.GenerateCode();
                }
                return this._generatedCode;
            }
        }

        internal DomainServiceCatalog DomainServiceCatalog
        {
            get
            {
                if (this._domainServiceCatalog == null)
                {
                    // slightly orthogonal, but they are set together
                    this._generatedCode = this.GenerateCode();
                }
                return this._domainServiceCatalog;
            }
        }
        internal Assembly GeneratedAssembly
        {
            get
            {
                if (this._generatedAssembly == null)
                {
                    this._generatedAssembly = this.GenerateAssembly();
                }
                return this._generatedAssembly;
            }
        }

        internal Type[] GeneratedTypes
        {
            get
            {
                if (this._generatedTypes == null)
                {
                    this._generatedTypes = this.GeneratedAssembly.GetExportedTypes();
                }
                return this._generatedTypes;
            }
        }

        internal string GeneratedTypeNames
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                foreach (Type t in this.GeneratedTypes)
                {
                    sb.AppendLine("    " + t.FullName);
                }
                return sb.ToString();
            }
        }

        /// <summary>
        /// Adds the specified source code into the next compilation
        /// request.  This allows a test to inject source code into
        /// the compile to test things like partial methods.
        /// </summary>
        /// <param name="userCode"></param>
        internal void AddUserCode(string userCode)
        {
            string s = this.UserCode ?? string.Empty;
            this.UserCode = s + userCode;
        }

        /// <summary>
        /// Converts the full type name of a server-side type to
        /// the name in the generated code.  This handles the invisible
        /// prepend of the VS namespace.
        /// </summary>
        /// <param name="fullTypeName"></param>
        /// <returns></returns>
        internal string GetGeneratedTypeName(string fullTypeName)
        {
            return this._isCSharp ? fullTypeName : "TestRootNS." + fullTypeName;
        }

        internal Type GetGeneratedType(string fullTypeName)
        {
            fullTypeName = GetGeneratedTypeName(fullTypeName);

            foreach (Type t in this.GeneratedTypes)
            {
                if (string.Equals(fullTypeName, t.FullName, StringComparison.OrdinalIgnoreCase))
                {
                    return t;
                }
            }
            return null;
        }

        private string GeneratedCodeFile
        {
            get
            {
                if (this._generatedCodeFile == null)
                {
                    this._generatedCodeFile = Path.GetTempFileName();
                    File.WriteAllText(this._generatedCodeFile, this.GeneratedCode);
                }
                return this._generatedCodeFile;
            }
        }

        private string GenerateCode()
        {
            ClientCodeGenerationOptions options = new ClientCodeGenerationOptions()
            {
                Language = this._isCSharp ? "C#" : "VisualBasic",
                ClientProjectPath = "MockProject.proj",
                ClientRootNamespace = "TestRootNS",
                UseFullTypeNames = this._useFullTypeNames
            };

            MockCodeGenerationHost host = TestHelper.CreateMockCodeGenerationHost(this.ConsoleLogger, this.MockSharedCodeService);
            ClientCodeGenerator generator = (ClientCodeGenerator)new CSharpClientCodeGenerator();
            this._domainServiceCatalog = new DomainServiceCatalog(this._domainServiceTypes, this.ConsoleLogger);

            string generatedCode = generator.GenerateCode(host, this._domainServiceCatalog.DomainServiceDescriptions, options);
            return generatedCode;
        }

        private Assembly GenerateAssembly()
        {
            // Failure to generate code results in no assembly
            if (string.IsNullOrEmpty(this.GeneratedCode))
            {
                return null;
            }

            MemoryStream generatedAssembly = CompileSource();
            Assert.IsNotNull(generatedAssembly, "Expected compile to succeed");

            Assembly assy = null;
            Dictionary<string, Assembly> loadedAssemblies = new Dictionary<string, Assembly>();

            try
            {
                foreach (string refAssyName in this.ReferenceAssemblies)
                {
                    if (refAssyName.Contains("mscorlib"))
                    {
                        continue;
                    }
                    try
                    {
                        Assembly refAssy = _metadataLoadContext.LoadFromAssemblyPath(refAssyName);
                        loadedAssemblies[refAssy.FullName] = refAssy;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(" failed to load " + refAssyName + ":\r\n" + ex.Message);
                    }
                }
                assy = _metadataLoadContext.LoadFromByteArray(generatedAssembly.ToArray());
                Assert.IsNotNull(assy);

                AssemblyName[] refNames = assy.GetReferencedAssemblies();
                foreach (AssemblyName refName in refNames)
                {
                    if (refName.FullName.Contains("mscorlib"))
                    {
                        continue;
                    }
                    if (!loadedAssemblies.ContainsKey(refName.FullName))
                    {
                        try
                        {
                            Assembly refAssy;
                            if (refName.Name.Contains("Client"))
                                refAssy = _metadataLoadContext.LoadFromAssemblyPath("C:\\Users\\crmhli\\source\\repos\\OpenRiaServices\\src\\OpenRiaServices.Tools.TextTemplate\\Test\\bin\\Debug\\net472\\OpenRiaServices.Client.dll");
                            else
                                refAssy = _metadataLoadContext.LoadFromAssemblyName(refName);
                            loadedAssemblies[refName.FullName] = refAssy;
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine(" failed to load " + refName + ":\r\n" + ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Assert.Fail("Encountered exception doing reflection only loads:\r\n" + ex.Message);
            }

            return assy;
        }


        private MemoryStream CompileSource()
        {
            string assemblyname = Path.GetFileNameWithoutExtension(this.OutputAssemblyName);
            var contents = new List<SourceText>(capacity: 2);
            contents.Add(SourceText.From(GeneratedCode));
            if (!string.IsNullOrEmpty(UserCode))
                contents.Add(SourceText.From(UserCode));

            if (this._isCSharp)
            {
                return CompilerHelper.CompileCSharpSilverlightAssembly(assemblyname, contents, referenceAssemblies: ReferenceAssemblies);
            }
            else
            {
                return CompilerHelper.CompileVBSilverlightAssembly(assemblyname, contents, ReferenceAssemblies, rootNamespace: "TestRootNS", documentationFile: null);
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            this._generatedTypes = null;
            this._generatedAssembly = null;
            this.SafeDelete(this._generatedCodeFile);
            this.SafeDelete(this._userCodeFile);
            _metadataLoadContext?.Dispose();
            if (this._generatedAssembly != null)
            {
                this.SafeDelete(this._generatedAssembly.Location);
            }
        }

        #endregion

        private void SafeDelete(string file)
        {
            if (!string.IsNullOrEmpty(file) && File.Exists(file))
            {
                try
                {
                    File.Delete(file);
                    System.Diagnostics.Debug.WriteLine("Deleted test file: " + file);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Could not delete " + file + ":\r\n" + ex.Message);
                }
            }
        }
    }
}
