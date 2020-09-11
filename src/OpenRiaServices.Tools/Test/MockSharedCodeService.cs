extern alias SystemWebDomainServices;

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using OpenRiaServices;
using OpenRiaServices.Tools.SourceLocation;
using OpenRiaServices.Tools.SharedTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TypeUtility = SystemWebDomainServices::OpenRiaServices.TypeUtility;

namespace OpenRiaServices.Tools.Test
{
    /// <summary>
    /// This mock class support the ISharedCodeService used for detecting shared
    /// types between client and server.  It is fed a list of types and methods to
    /// consider shared.  They are typically in the test assembly.
    /// </summary>
    class MockSharedCodeService : ISharedCodeService, IDisposable
    {
        private static Type[] _knownSharedAttributeTypes = new Type[] {
            typeof(DataMemberAttribute), 
            typeof(System.ComponentModel.DescriptionAttribute),
            typeof(DomainIdentifierAttribute),
            typeof(ExternalReferenceAttribute),                    
            typeof(ReadOnlyAttribute),
            typeof(CompositionAttribute)
        };

        private readonly HashSet<Type> _sharedTypes;
        private readonly HashSet<CodeMemberKey> _sharedMethods = new HashSet<CodeMemberKey>();
        private readonly HashSet<string> _sharedFiles;
        private readonly HashSet<Type> _unknowableTypes = new HashSet<Type>();
        private readonly HashSet<Type> _unsharedTypes = new HashSet<Type>();

        private readonly SourceFileLocationService _pdbSourceFileLocationService;

        public MockSharedCodeService(IEnumerable<Type> sharedTypes, IEnumerable<MethodBase> sharedMethods, IEnumerable<string> sharedFiles)
        {
            if (sharedTypes == null)
            {
                sharedTypes = Enumerable.Empty<Type>();
            }

            if (sharedMethods == null)
            {
                sharedMethods = Enumerable.Empty<MethodBase>();
            }

            if (sharedFiles == null)
            {
                sharedFiles = Enumerable.Empty<string>();
            }

            foreach (Type t in sharedTypes)
                Assert.IsNotNull(t, "Test error -- null passed in for a shared type");

            foreach (MethodInfo m in sharedMethods)
                Assert.IsNotNull(m, "Test error -- null passed in for a shared method");

            this._sharedTypes = new HashSet<Type>(sharedTypes);

            foreach (MethodBase m in sharedMethods)
            {
                this._sharedMethods.Add(CodeMemberKey.CreateMethodKey((m)));
            }
            this._sharedFiles = new HashSet<string>(sharedFiles);

            // Open up a real PDB based location service if have shared files
            if (this._sharedFiles.Any())
            {
                this._pdbSourceFileLocationService = new SourceFileLocationService(new[] { new PdbSourceFileProviderFactory(/*symbolSearchPath*/ null, /*logger*/ null) }, new FilenameMap());
            }
        }

        /// <summary>
        /// Adds the given type to the list of types we consider "unknowable"
        /// to simulate missing PDB or codeless class
        /// </summary>
        /// <param name="t"></param>
        public void AddUnknowableType(Type t)
        {
            this._unknowableTypes.Add(t);
        }

        /// <summary>
        /// Adds the given type to the list of types we consider "unshared"
        /// to override any other discovery mechanisms
        /// </summary>
        /// <param name="t"></param>
        public void AddUnsharedType(Type t)
        {
            this._unsharedTypes.Add(t);
        }

        /// <summary>
        /// Adds the given type to the list of types we consider "shared"
        /// to override any other discovery mechanisms
        /// </summary>
        /// <param name="t"></param>
        public void AddSharedType(Type t)
        {
            this._sharedTypes.Add(t);
        }


        public CodeMemberShareKind GetTypeShareKind(string typeName)
        {
            string fullName = MockSharedCodeService.AssemblyQualifiedTypeNameToFullTypeName(typeName);

            if (this._unknowableTypes.Any(t => string.Equals(t.FullName, fullName)))
            {
                return CodeMemberShareKind.Unknown;
            }

            if (this._unsharedTypes.Any(t => string.Equals(t.FullName, fullName)))
            {
                return CodeMemberShareKind.NotShared;
            }

            if (this._sharedTypes.Any(t => string.Equals(t.FullName, fullName)))
            {
                return CodeMemberShareKind.SharedBySource;
            }

            // Check for file sharing needs fully qualified name
            if (this.IsTypeSharedInFile(typeName))
            {
                return CodeMemberShareKind.SharedBySource;
            }

            if (IsSharedFrameworkType(typeName))
            {
                return CodeMemberShareKind.SharedByReference;
            }

            return CodeMemberShareKind.NotShared;
        }

        public CodeMemberShareKind GetMethodShareKind(MethodBase methodBase)
        {
            CodeMemberKey key = CodeMemberKey.CreateMethodKey(methodBase);
            if (this._sharedMethods.Contains(key) || IsSharedFrameworkType(methodBase.DeclaringType))
            {
                return CodeMemberShareKind.SharedByReference;
            }
            if (this.IsMethodSharedInFile(methodBase))
            {
                return CodeMemberShareKind.SharedBySource;
            }
            return CodeMemberShareKind.NotShared;
        }
 
        private bool IsTypeSharedInFile(string typeName)
        {
            if (this._pdbSourceFileLocationService == null)
            {
                return false;
            }
            Type t = CodeMemberKey.CreateTypeKey(typeName).Type;
            if (t != null)
            {
                IEnumerable<string> files = this._pdbSourceFileLocationService.GetFilesForType(t);
                foreach (string file in files)
                {
                    if (this.IsFileShared(file))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private bool IsTypeSharedInFile(Type t)
        {
            if (this._pdbSourceFileLocationService == null)
            {
                return false;
            }
            IEnumerable<string> files = this._pdbSourceFileLocationService.GetFilesForType(t);
            foreach (string file in files)
            {
                if (this.IsFileShared(file))
                {
                    return true;
                }
            }
            return false;
        }


        private bool IsMethodSharedInFile(MethodBase method)
        {
            if (this._pdbSourceFileLocationService == null || method == null)
            {
                return false;
            }
            return this.IsFileShared(this._pdbSourceFileLocationService.GetFileForMember(method));
        }

        /// <summary>
        /// Returns true if the given file is in our shared list.
        /// Due to VSTT's Deployment item rules, we often have shared files *copied* to
        /// deployment areas that are not physically the same as the ones in the PDB.
        /// This method tries to match based on identical short name and identical content.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private bool IsFileShared(string file)
        {
            if (string.IsNullOrEmpty(file))
            {
                return false;
            }
            foreach (string sharedFile in this._sharedFiles)
            {
                if (string.Equals(sharedFile, file, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                // Okay, tolerate a different location if the contents and shortnames are the same
                if (string.Equals(Path.GetFileName(sharedFile), Path.GetFileName(file), StringComparison.OrdinalIgnoreCase))
                {
                    string s1, s2;
                    using (StreamReader t1 = new StreamReader(file))
                    {
                        s1 = t1.ReadToEnd();
                    }

                    using (StreamReader t2 = new StreamReader(sharedFile))
                    {
                        s2 = t2.ReadToEnd();
                    }
                    if (s1.Equals(s2))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Returns true iff the given type is a DataAnnotations attribute shared across tiers
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        private static bool IsSharedFrameworkType(Type t)
        {
            // If this isn't a system assembly, we know immediately that
            // it's not a shared framework type
            if (!SystemWebDomainServices::OpenRiaServices.TypeUtility.IsSystemAssembly(t.Assembly))
            {
                return false;
            }

            // Otherwise, we check for specific namespaces and types
            return
                !string.IsNullOrEmpty(t.Namespace) &&
                (
                    // Is acceptable core type?
                    t.Namespace.Equals("System") ||

                    // Is acceptable DataAnnotations Type?
                    t.Namespace.Equals("System.ComponentModel.DataAnnotations") ||

                    // Is known framework type?
                    MockSharedCodeService._knownSharedAttributeTypes.Contains(t)
                );
        }

        private static string AssemblyQualifiedTypeNameToFullTypeName(string typeName)
        {
            int comma = typeName.IndexOf(',');
            return comma < 0 ? typeName : typeName.Substring(0, comma);
        }

        private static bool IsSharedFrameworkType(string typeName)
        {
            // If this isn't a system assembly, we know immediately that
            // it's not a shared framework type
            Type systemType = Type.GetType(typeName, /*throwOnError*/ false);
            if (systemType != null)
            {
                if (!SystemWebDomainServices::OpenRiaServices.TypeUtility.IsSystemAssembly(systemType.Assembly))
                {
                    return false;
                }
                // Mock matches the real shared assemblies in allowing all mscorlib to match
                if (AssemblyUtilities.IsAssemblyMsCorlib(systemType.Assembly.GetName()))
                {
                    return true;
                }
                // The mock declares that anything in System is also shared
                // Anything in EntityFramework.dll is not shared.
                string assemblyName = systemType.Assembly.FullName;
                int comma = assemblyName.IndexOf(',');
                if (comma >= 0)
                {
                    assemblyName = assemblyName.Substring(0, comma);
                    if (string.Equals("System", assemblyName, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                    if (string.Equals("EntityFramework", assemblyName, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }
            }
            typeName = MockSharedCodeService.AssemblyQualifiedTypeNameToFullTypeName(typeName);

            int dot = typeName.LastIndexOf('.');
            string namespaceName = dot < 0 ? string.Empty : typeName.Substring(0, dot);
            string shortTypeName = typeName.Substring(dot + 1);

            if (string.Equals("System.ComponentModel.DataAnnotations", namespaceName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            foreach (Type t in MockSharedCodeService._knownSharedAttributeTypes)
            {
                if (string.Equals(t.FullName, typeName))
                {
                    return true;
                }
            }

            return false;
        }

        #region IDisposable Members

        public void Dispose()
        {
            this._pdbSourceFileLocationService.Dispose();
        }

        #endregion

        #region ISharedCodeService Members

        public CodeMemberShareKind GetTypeShareKind(Type type)
        {
            if (this._unknowableTypes.Contains(type))
            {
                return CodeMemberShareKind.Unknown;
            }

            if (this._unsharedTypes.Contains(type))
            {
                return CodeMemberShareKind.NotShared;
            }

            if (this._sharedTypes.Contains(type))
            {
                return CodeMemberShareKind.SharedByReference;
            }

            if (this.IsTypeSharedInFile(type))
            {
                return CodeMemberShareKind.SharedBySource;
            }

            if (IsSharedFrameworkType(type))
            {
                return CodeMemberShareKind.SharedByReference;
            }

            return CodeMemberShareKind.NotShared;
        }


        public CodeMemberShareKind GetPropertyShareKind(string typeName, string propertyName)
        {
            CodeMemberKey key = CodeMemberKey.CreatePropertyKey(typeName, propertyName);
            PropertyInfo propertyInfo = key.PropertyInfo;
            if (propertyInfo == null)
            {
                return CodeMemberShareKind.NotShared;
            }
            MethodBase method = propertyInfo.GetGetMethod();

            return GetMethodShareKind(typeName, method.Name, Array.Empty<string>());
        }

        public CodeMemberShareKind GetMethodShareKind(string typeName, string methodName, IEnumerable<string> parameterTypeNames)
        {
            CodeMemberKey key = CodeMemberKey.CreateMethodKey(typeName, methodName, parameterTypeNames == null ? Array.Empty<string>() : parameterTypeNames.ToArray());
            if (this._sharedMethods.Contains(key))
            {
                return CodeMemberShareKind.SharedByReference;
            }
            if (this.IsMethodSharedInFile(key.MethodBase))
            {
                return CodeMemberShareKind.SharedBySource;
            }
            return CodeMemberShareKind.NotShared;

        }
        #endregion ISharedCodeService Members
    }
}
