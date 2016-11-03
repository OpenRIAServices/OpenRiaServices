using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using OpenRiaServices.DomainServices.Server.Test.Utilities;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Tasks;
using Microsoft.Build.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32;

namespace OpenRiaServices.DomainServices.Tools.Test
{
    internal class AssemblyGenerator : IDisposable
    {
        private readonly string _relativeTestDir;
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


        public AssemblyGenerator(string relativeTestDir, bool isCSharp, IEnumerable<Type> domainServiceTypes) :
            this(relativeTestDir, isCSharp, false, domainServiceTypes)
        {
        }

        public AssemblyGenerator(string relativeTestDir, bool isCSharp, bool useFullTypeNames, IEnumerable<Type> domainServiceTypes)
        {
            Assert.IsFalse(string.IsNullOrEmpty(relativeTestDir), "relativeTestDir required");

            this._relativeTestDir = relativeTestDir;
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
                    this._mockSharedCodeService = new MockSharedCodeService(new Type[0], new MethodBase[0], new string[0]);
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
                    this._referenceAssemblies = CompilerHelper.GetSilverlightClientAssemblies(this._relativeTestDir);
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

        /// <summary>
        /// Returns <c>true</c> if the given type is a <see cref="Nullable"/>
        /// </summary>
        /// <param name="type">The type to test</param>
        /// <returns><c>true</c> if the given type is a nullable type</returns>
        public static bool IsNullableType(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        /// <summary>
        /// If the given type is <see cref="Nullable"/>, returns the element type,
        /// otherwise simply returns the input type
        /// </summary>
        /// <param name="type">The type to test that may or may not be Nullable</param>
        /// <returns>Either the input type or, if it was Nullable, its element type</returns>
        public static Type GetNonNullableType(Type type)
        {
            return IsNullableType(type) ? type.GetGenericArguments()[0] : type;
        }

        /// <summary>
        /// <summary>
        /// Returns the list of <see cref="CustomAttributeData"/> for the custom attributes
        /// attached to the given type of the given attribute type.
        /// </summary>
        /// <param name="memberInfo"></param>
        /// <param name="attributeType"></param>
        /// <returns></returns>
        internal static IList<CustomAttributeData> GetCustomAttributeData(MemberInfo memberInfo, Type attributeType)
        {
            List<CustomAttributeData> result = new List<CustomAttributeData>(); ;

            IList<CustomAttributeData> attrs = CustomAttributeData.GetCustomAttributes(memberInfo);
            foreach (CustomAttributeData cad in attrs)
            {
                Type attrType = cad.Constructor.DeclaringType;
                if (string.Equals(attrType.FullName, attributeType.FullName, StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(cad);
                }
            }
            return result;
        }

        /// <summary>
        /// Helper method to extract a named value from a <see cref="CustomAttributeData"/> instance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="attribute"></param>
        /// <param name="valueName"></param>
        /// <returns></returns>
        internal static T GetCustomAttributeValue<T>(CustomAttributeData attribute, string valueName)
        {
            T value;
            if (TryGetCustomAttributeValue<T>(attribute, valueName, out value))
            {
                return value;
            }
            Assert.Fail("Failed to find a value named " + valueName + " in the CustomAttributeData " + attribute);
            return default(T);
        }

        /// <summary>
        /// Helper method to extract a named value from a <see cref="CustomAttributeData"/> instance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="attribute"></param>
        /// <param name="valueName"></param>
        /// <param name="value">Output parameter to receive value</param>
        /// <returns><c>true</c> if the value was found</returns>
        internal static bool TryGetCustomAttributeValue<T>(CustomAttributeData attribute, string valueName, out T value)
        {
            value = default(T);
            ConstructorInfo ctor = attribute.Constructor;
            var ctorArgs = attribute.ConstructorArguments;
            if (ctor != null && ctorArgs != null && ctorArgs.Count > 0)
            {
                int ctorArgIndex = -1;
                ParameterInfo[] pInfos = ctor.GetParameters();
                for (int i = 0; i < pInfos.Length; ++i)
                {
                    if (string.Equals(valueName, pInfos[i].Name, StringComparison.OrdinalIgnoreCase))
                    {
                        ctorArgIndex = i;
                        break;
                    }
                }

                if (ctorArgIndex >= 0)
                {
                    var ctorArg = ctorArgs[ctorArgIndex];
                    if (typeof(T).IsAssignableFrom(ctorArg.ArgumentType))
                    {
                        value = (T)ctorArg.Value;
                        return true;
                    }
                }
            }

            foreach (var namedArg in attribute.NamedArguments)
            {
                if (string.Equals(valueName, namedArg.MemberInfo.Name, StringComparison.OrdinalIgnoreCase))
                {
                    if (typeof(T).IsAssignableFrom(namedArg.TypedValue.ArgumentType))
                    {
                        value = (T)namedArg.TypedValue.Value;
                        return true;
                    }
                }
            }

            return false;
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
            CodeDomClientCodeGenerator generator = (this._isCSharp) 
                                                        ? (CodeDomClientCodeGenerator)new CSharpCodeDomClientCodeGenerator() 
                                                        : (CodeDomClientCodeGenerator) new VisualBasicCodeDomClientCodeGenerator();
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

            string generatedAssemblyFileName = this._isCSharp ? this.CompileCSharpSource() : this.CompileVisualBasicSource();
            if (string.IsNullOrEmpty(generatedAssemblyFileName))
            {
                Assert.Fail("Expected compile to succeed");
            }

            Assembly assy = null;
            Dictionary<AssemblyName, Assembly> loadedAssemblies = new Dictionary<AssemblyName, Assembly>();

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
                        Assembly refAssy = Assembly.ReflectionOnlyLoadFrom(refAssyName);
                        loadedAssemblies[refAssy.GetName()] = refAssy;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(" failed to load " + refAssyName + ":\r\n" + ex.Message);
                    }
                }

                assy = Assembly.ReflectionOnlyLoadFrom(generatedAssemblyFileName);
                Assert.IsNotNull(assy);

                AssemblyName[] refNames = assy.GetReferencedAssemblies();
                foreach (AssemblyName refName in refNames)
                {
                    if (refName.FullName.Contains("mscorlib"))
                    {
                        continue;
                    }
                    if (!loadedAssemblies.ContainsKey(refName))
                    {
                        try
                        {
                            Assembly refAssy = Assembly.ReflectionOnlyLoad(refName.FullName);
                            loadedAssemblies[refName] = refAssy;
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

        internal string CompileCSharpSource()
        {
            List<ITaskItem> sources = new List<ITaskItem>();
            string codeFile = this.GeneratedCodeFile;
            sources.Add(new TaskItem(codeFile));

            // If client has added extra user code into the
            // compile request, add it in now
            string userCodeFile = this.UserCodeFile;
            if (!string.IsNullOrEmpty(userCodeFile))
            {
                sources.Add(new TaskItem(userCodeFile));
            }

            List<ITaskItem> references = new List<ITaskItem>();
            foreach (string s in this.ReferenceAssemblies)
                references.Add(new TaskItem(s));

            Csc csc = new Csc();
            MockBuildEngine buildEngine = this.MockBuildEngine;
            csc.BuildEngine = buildEngine;  // needed before task can log

            csc.NoStandardLib = true;   // don't include std lib stuff -- we're feeding it silverlight
            csc.NoConfig = true;        // don't load the csc.rsp file to get references
            csc.TargetType = "library";
            csc.Sources = sources.ToArray();
            csc.References = references.ToArray();
            csc.DefineConstants += "SILVERLIGHT";
            csc.OutputAssembly = new TaskItem(this.OutputAssemblyName);

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
            return csc.OutputAssembly.ItemSpec;
        }

        internal string CompileVisualBasicSource()
        {
            List<ITaskItem> sources = new List<ITaskItem>();
            sources.Add(new TaskItem(this.GeneratedCodeFile));

            // If client has added extra user code into the
            // compile request, add it in now
            string userCodeFile = this.UserCodeFile;
            if (!string.IsNullOrEmpty(userCodeFile))
            {
                sources.Add(new TaskItem(userCodeFile));
            }

            // Transform references into a list of ITaskItems.
            // Here, we skip over mscorlib explicitly because this is already included as a project reference.
            List<ITaskItem> references =
                this.ReferenceAssemblies
                    .Where(reference => !reference.EndsWith("mscorlib.dll", StringComparison.Ordinal))
                    .Select<string, ITaskItem>(reference => new TaskItem(reference) as ITaskItem)
                    .ToList();

            Vbc vbc = new Vbc();
            MockBuildEngine buildEngine = this.MockBuildEngine;
            vbc.BuildEngine = buildEngine;  // needed before task can log

            vbc.NoStandardLib = true;   // don't include std lib stuff -- we're feeding it silverlight
            vbc.NoConfig = true;        // don't load the vbc.rsp file to get references
            vbc.TargetType = "library";
            vbc.Sources = sources.ToArray();
            vbc.References = references.ToArray();
            vbc.SdkPath = CompilerHelper.GetSilverlightSdkReferenceAssembliesPath();
            vbc.RootNamespace = "TestRootNS";
            vbc.DefineConstants += "SILVERLIGHT";

            vbc.OutputAssembly = new TaskItem(this.OutputAssemblyName);

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
            return vbc.OutputAssembly.ItemSpec;
        }

        #region IDisposable Members

        public void Dispose()
        {
            this._generatedTypes = null;
            this._generatedAssembly = null;
            this.SafeDelete(this._generatedCodeFile);
            this.SafeDelete(this._userCodeFile);
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
