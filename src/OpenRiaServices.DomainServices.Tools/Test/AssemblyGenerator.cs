using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using OpenRiaServices.DomainServices.Server.Test.Utilities;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.CodeAnalysis.Text;

namespace OpenRiaServices.DomainServices.Tools.Test
{
    internal class AssemblyGenerator : IDisposable
    {
        private readonly string _relativeTestDir;
        private readonly bool _isCSharp;
        private readonly IEnumerable<Type> _domainServiceTypes;
        private DomainServiceCatalog _domainServiceCatalog;
        private MockBuildEngine _mockBuildEngine;
        private MockSharedCodeService _mockSharedCodeService;
        private string _generatedCode;
        private Assembly _generatedAssembly;
        private IList<string> _referenceAssemblies;
        private Type[] _generatedTypes;
        private string _userCode;
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
            this.OutputAssemblyName = Guid.NewGuid().ToString();
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

        internal string OutputAssemblyName { get; }

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
                    this._referenceAssemblies = CompilerHelper.GetClientAssemblies(this._relativeTestDir);
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
                                                        : (CodeDomClientCodeGenerator)new VisualBasicCodeDomClientCodeGenerator();
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

                assy = Assembly.ReflectionOnlyLoad(generatedAssembly.ToArray());
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
        }

        #endregion
    }
}
