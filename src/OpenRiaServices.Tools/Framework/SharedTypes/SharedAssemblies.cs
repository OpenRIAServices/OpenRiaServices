using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Mono.Cecil;

namespace OpenRiaServices.Tools.SharedTypes
{
    /// <summary>
    /// Internal class to maintain a set of known assemblies
    /// and allow clients to ask whether types or methods are
    /// in that set.
    /// </summary>
    internal sealed class SharedAssemblies : IDisposable
    {
        private readonly Dictionary<string, TypeInfo> _sharedTypeByName;
        private readonly CustomAssemblyResolver _resolver;
        private readonly ILogger _logger;

        /// <summary>
        /// Creates a new instance of this type.
        /// </summary>
        /// <param name="assemblyFileNames">The set of assemblies to use</param>
        /// <param name="assemblySearchPaths">Optional set of paths to search for referenced assemblies.</param>
        /// <param name="logger">Optional logger to use to report errors or warnings</param>
        public SharedAssemblies(IEnumerable<string> assemblyFileNames, IEnumerable<string> assemblySearchPaths, ILogger logger)
        {
            if (assemblyFileNames == null)
            {
                throw new ArgumentNullException(nameof(assemblyFileNames));
            }
            _logger = logger;
            _sharedTypeByName = new Dictionary<string, TypeInfo>(StringComparer.Ordinal);
            _resolver = new CustomAssemblyResolver(assemblySearchPaths ?? Enumerable.Empty<string>());
            _resolver.ResolveFailure += _resolver_ResolveFailure;
            LoadAssemblies(assemblyFileNames, logger);
        }

        private AssemblyDefinition _resolver_ResolveFailure(object sender, AssemblyNameReference reference)
        {
            _logger.LogMessage(string.Format(CultureInfo.CurrentCulture, Resource.ClientCodeGen_Assembly_Load_Error, reference.FullName, string.Empty));
            return null;
        }

        private void LoadAssemblies(IEnumerable<string> assemblyFileNames, ILogger logger)
        {
            var loaded = new HashSet<AssemblyNameReference>(new AssemblyNameReferenceComparer());
            var references = new HashSet<AssemblyNameReference>(new AssemblyNameReferenceComparer());
            foreach (var assembly in assemblyFileNames)
            {
                try
                {
                    AssemblyDefinition definition = _resolver.LoadAssembly(assembly);
                    loaded.Add(definition.Name);
                    foreach (ModuleDefinition module in definition.Modules)
                    {
                        AddPublicTypesToDictionary(module);

                        foreach (var reference in module.AssemblyReferences)
                            references.Add(reference);
                    }
                }
                catch (Exception ex)
                {
                    // Some common exceptions log a warning and keep running
                 if (ex is System.IO.FileNotFoundException ||
                        ex is System.IO.FileLoadException ||
                        ex is System.IO.PathTooLongException ||
                        ex is System.BadImageFormatException ||
                        ex is System.Security.SecurityException)
                    {
                        if (logger != null)
                        {
                            logger.LogMessage(string.Format(CultureInfo.CurrentCulture, Resource.ClientCodeGen_Assembly_Load_Error, assembly, ex.Message));
                        }
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            // PERF: Consider replacing this with some smarter code using TypeReferences
            // from loaded assemblies and creating "incomplete" types with them
            // to populate the dictionary
            references.ExceptWith(loaded);
            foreach (var assembly in references)
            {
                try
                {
                    AssemblyDefinition definition = _resolver.Resolve(assembly);
                    foreach (ModuleDefinition m in definition.Modules)
                    {
                        AddPublicTypesToDictionary(m);
                    }
                }
                catch (Exception ex)
                {
                    // Some common exceptions log a warning and keep running
                    if (ex is System.IO.FileNotFoundException ||
                        ex is System.IO.FileLoadException ||
                        ex is System.IO.PathTooLongException ||
                        ex is System.BadImageFormatException ||
                        ex is System.Security.SecurityException)
                    {
                        if (logger != null)
                        {
                            logger.LogMessage(string.Format(CultureInfo.CurrentCulture, Resource.ClientCodeGen_Assembly_Load_Error, assembly, ex.Message));
                        }
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        private void AddPublicTypesToDictionary(ModuleDefinition module)
        {
            foreach (var type in module.Types)
            {
                if (type.IsPublic)
                {
                    _sharedTypeByName[type.FullName] = new TypeInfo(type);
                }
            }
        }

        private static TypeInfo GetBaseType(TypeInfo typeInfo)
        {
            if (typeInfo.BaseTypeCache == null
                && typeInfo.TypeDefinition.BaseType != null)
            {
                TypeDefinition type = typeInfo.TypeDefinition.BaseType.Resolve();
                typeInfo.BaseTypeCache = new TypeInfo(type);
            }

            return typeInfo.BaseTypeCache;
        }

        /// <summary>
        /// Search for a property in a class hierarcy and get the type implementing the property or <c>null</c>
        /// </summary>
        private static TypeInfo HasProperty(TypeInfo type, string name)
        {
            while (type != null)
            {
                // Get or Initialize Property collection
                if (type.PropertiesCache == null)
                {
                    type.PropertiesCache = new HashSet<string>();
                    foreach (var property in type.TypeDefinition.Properties)
                    {
                        var method = property.GetMethod ?? property.SetMethod;
                        if (method.IsPublic)
                            type.PropertiesCache.Add(property.Name);
                    }
                }

                if (type.PropertiesCache.Contains(name))
                    return type;

                type = GetBaseType(type);
            }

            return null;
        }

        /// <summary>
        /// Returns the location of the shared assembly containing the
        /// code member described by <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The description of the code element.</param>
        /// <returns>The location of the assembly that contains it or <c>null</c> if it is not in a shared assembly.</returns>
        public string GetSharedAssemblyPath(CodeMemberKey key)
        {
            Debug.Assert(key != null, "key cannot be null");
            ModuleDefinition location = null;

            TypeInfo typeInfo;
            if (TryGetSharedType(key.TypeName, out typeInfo))
            {
                switch (key.KeyKind)
                {
                    case CodeMemberKey.CodeMemberKeyKind.TypeKey:
                        location = typeInfo.ModuleDefinition;
                        break;

                    case CodeMemberKey.CodeMemberKeyKind.PropertyKey:
                        location = HasProperty(typeInfo, key.MemberName)?.ModuleDefinition;
                        break;

                    case CodeMemberKey.CodeMemberKeyKind.MethodKey:
                        var method = FindSharedMethodOrConstructor(typeInfo, key);
                        if (method != null)
                        {
                            location = method.DeclaringType.Module;
                        }
                        break;

                    default:
                        throw new NotImplementedException();
                }
            }

            return location?.Assembly.Name.Name;
        }

        /// <summary>
        /// Locates the <see cref="MethodDefinition"/> in the set of shared assemblies that
        /// corresponds to the method described by <paramref name="key"/>.
        /// </summary>
        /// <param name="sharedType">The <see cref="TypeInfo"/> we have already located in our set of shared assemblies.</param>
        /// <param name="key">The key describing the method to find.</param>
        /// <returns>The matching <see cref="MethodDefinition"/> or <c>null</c> if no match is found.</returns>
        private MethodDefinition FindSharedMethodOrConstructor(TypeInfo sharedType, CodeMemberKey key)
        {
            TypeInfo[] parameterTypes = GetSharedTypes(key.ParameterTypeNames);
            if (parameterTypes == null)
            {
                return null;
            }
            bool isConstructor = key.IsConstructor;

            do
            {
                foreach (var method in sharedType.TypeDefinition.Methods)
                {
                    // Name matches or it is a constructur we are 
                    if (isConstructor ? !method.IsConstructor : !string.Equals(method.Name, key.MemberName, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var parameterInfos = method.Parameters;
                    if (parameterInfos.Count != parameterTypes.Length)
                    {
                        continue;
                    }
                    int matchedParameters = 0;
                    for (int i = 0; i < parameterInfos.Count; ++i)
                    {
                        if (string.Equals(parameterInfos[i].ParameterType.FullName, parameterTypes[i].FullName, StringComparison.OrdinalIgnoreCase))
                        {
                            ++matchedParameters;
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (matchedParameters == parameterInfos.Count)
                    {
                        return method;
                    }
                }

                // Continue searching in base types
            } while (!isConstructor &&
                (sharedType = GetBaseType(sharedType)) != null);

            return null;
        }

        /// <summary>
        /// Given a collection of <see cref="Type"/> names, this method returns
        /// an array of all those types from the set of shared assemblies.
        /// </summary>
        /// <param name="typeNames">The collection of type names.  It can be null.</param>
        /// <returns>The collection of types in the shared assemblies.
        /// A <c>null</c> means one or more types in the list were not shared.</returns>
        TypeInfo[] GetSharedTypes(string[] typeNames)
        {
            if (typeNames == null)
                return Array.Empty<TypeInfo>();

            var types = new TypeInfo[typeNames.Length];
            for (int i = 0; i < typeNames.Length; ++i)
            {
                if (!TryGetSharedType(typeNames[i], out types[i]))
                {
                    return null;
                }
            }
            return types;
        }

        /// <summary>
        /// Returns the <see cref="Type"/> from the set of shared assemblies of the given name.
        /// </summary>
        /// <param name="typeName">The fully qualified type name.</param>
        /// <param name="typeInfo">the matching type descriptor if any was found</param>
        /// <returns>The <see cref="Type"/> from the shared assemblies if it exists, otherwise <c>null</c>.</returns>
        private bool TryGetSharedType(string typeName, out TypeInfo typeInfo)
        {
            if (!_sharedTypeByName.TryGetValue(typeName, out typeInfo))
            {
                // Check if array ( with "[]" after type name) or generic with "[[...](,[...])*]" patter
                int openBracet = typeName.IndexOf('[');
                if (openBracet != -1 && openBracet + 1 < typeName.Length)
                {
                    // Is array if '[]'
                    if (typeName[openBracet + 1] == ']')
                    {
                        string underlyingTypeName = typeName.Substring(0, openBracet);
                        if (TryGetSharedType(underlyingTypeName, out typeInfo))
                        {
                            // It is a array, we "cheat" and return the underlying type
                            // since we assume array is always reachable the type in the 
                            // array is what is important
                            // But strip any part of the name which corresponds to assembly qualified part
                            typeInfo = new TypeInfo(typeInfo, typeName.Substring(0, (openBracet + 1) + 1));
                        }
                    }
                    // Or generic if '[['
                    else if (typeName[openBracet + 1] == '[')
                    {
                        string genericTypeName = typeName.Substring(0, openBracet);
                        TypeInfo genericType;
                        if (TryGetSharedType(genericTypeName, out genericType))
                        {
                            if (TryGetSharedGenericParameters(typeName, openBracet + 1, out int endBracket))
                            {
                                // Skip any part after typename which would be assembly qualified part
                                typeInfo = new TypeInfo(genericType, typeName.Substring(0, endBracket + 1));
                            }
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"Unexpected single '[' in type name cannot determine type '{typeName}'");
                    }
                }

                // Check if typename is assembly qualified, and try resolving without assembly qualified name
                if (typeInfo == null)
                {
                    int assemblyNameStart = typeName.IndexOf(',', 0);
                    if (assemblyNameStart != -1)
                    {
                        var shortTypeName = typeName.Substring(0, assemblyNameStart);
                        TryGetSharedType(shortTypeName, out typeInfo);
                    }
                }

                if (typeInfo == null)
                {
                    // TODO: look in "mscorlib" (lookup a known type and use module type system?)
                }

                _sharedTypeByName.Add(typeName, typeInfo);
            }

            return typeInfo != null;
        }

        private bool TryGetSharedGenericParameters(string typeName, int startBracket, out int endBracket)
        {
            while (true)
            {
                endBracket = typeName.IndexOf(']', startBracket + 1);
                if (endBracket == -1)
                {
                    _logger.LogError($"Expected ']' in typename '{typeName}' after position {startBracket}");
                    return false;
                }

                string parameterName = typeName.Substring(startBracket + 1, endBracket - (startBracket + 1));
                if (TryGetSharedType(parameterName, out _))
                {
                    // Check for closing bracet of generic type (double ']')
                    if (typeName[endBracket + 1] == ']')
                    {
                        endBracket = endBracket + 1;
                        return true;
                    }
                    else if (typeName[endBracket + 1] == ',' && typeName[endBracket + 2] == '[') // ",[" or "]"
                    {
                        startBracket = endBracket + 2;
                    }
                    else
                    {
                        throw new NotSupportedException($"Unrecognized typename '{typeName}' eexpected ']' or ',[' at position {endBracket + 1}");
                    }
                }
                else // Bail out if a single argument cannot be found
                {
                    return false;
                }
            }
        }

        public void Dispose()
        {
            _resolver.Dispose();
        }

        [DebuggerDisplay("{FullName,nq}")]
        private class TypeInfo
        {
            private readonly string _typeName;

            public HashSet<string> PropertiesCache { get; set; }
            public TypeInfo BaseTypeCache { get; set; }
            public TypeDefinition TypeDefinition { get; }
            public string FullName => _typeName ?? TypeDefinition.FullName;
            public ModuleDefinition ModuleDefinition => TypeDefinition.Module;

            public TypeInfo(TypeDefinition typeDefinition)
            {
                TypeDefinition = typeDefinition;
            }

            public TypeInfo(TypeInfo typeInfo, string typeName)
            {
                TypeDefinition = typeInfo.TypeDefinition;
                _typeName = typeName;
            }
        };

        /// <summary>
        /// Load dependencies from a specified list of directories
        /// </summary>
        private class CustomAssemblyResolver : DefaultAssemblyResolver
        {
            public CustomAssemblyResolver(IEnumerable<string> assemblySearchPaths)
            {
                foreach (var dir in base.GetSearchDirectories())
                    base.RemoveSearchDirectory(dir);

                foreach (var dir in assemblySearchPaths)
                    base.AddSearchDirectory(dir);
            }

            public AssemblyDefinition LoadAssembly(string path)
            {
                AssemblyDefinition a = AssemblyDefinition.ReadAssembly(
                    File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete),
                    new ReaderParameters()
                    {
                        AssemblyResolver = this,
                        ReadSymbols = false,
                    });
                base.RegisterAssembly(a);
                return a;
            }
        }

        /// <summary>
        /// Compares <see cref="AssemblyNameReference"/> based on Name and PublicKeyToken
        /// </summary>
        private class AssemblyNameReferenceComparer : IEqualityComparer<AssemblyNameReference>
        {
            public bool Equals(AssemblyNameReference x, AssemblyNameReference y)
            {
                if (x.Name == y.Name
                    && x.PublicKeyToken.Length == y.PublicKeyToken.Length)
                {
                    for (int i = 0; i < x.PublicKeyToken.Length; ++i)
                    {
                        if (x.PublicKeyToken[i] != y.PublicKeyToken[i])
                        {
                            return false;
                        }
                    }

                    return true;
                }
                return false;
            }

            public int GetHashCode(AssemblyNameReference obj)
            {
                return obj.Name.GetHashCode();
            }
        }
    }
}
