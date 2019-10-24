using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Mono.Cecil;

namespace OpenRiaServices.DomainServices.Tools.SharedTypes
{

    /// <summary>
    /// Internal class to maintain a set of known assemblies
    /// and allow clients to ask whether types or methods are
    /// in that set.
    /// </summary>
    internal sealed class SharedAssemblies : ISharedAssemblies, IDisposable
    {
        private readonly Dictionary<string, TypeInfo> _sharedTypeByName;
        private readonly CustomAssemblyResolver _resolver;

        [DebuggerDisplay("{FullName,nq}")]
        class TypeInfo
        {
            public HashSet<string> Properties;
            public TypeInfo BaseType;
            private readonly string _typeName;

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

        private readonly ILogger _logger;

        class CustomAssemblyResolver : DefaultAssemblyResolver
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
        /// Creates a new instance of this type.
        /// </summary>
        /// <param name="assemblyFileNames">The set of assemblies to use</param>
        /// <param name="assemblySearchPaths">Optional set of paths to search for referenced assemblies.</param>
        /// <param name="logger">Optional logger to use to report errors or warnings</param>
        public SharedAssemblies(IEnumerable<string> assemblyFileNames, IEnumerable<string> assemblySearchPaths, ILogger logger)
        {
            if (assemblyFileNames == null)
            {
                throw new ArgumentNullException("assemblyFileNames");
            }
            this._logger = logger;
            this._sharedTypeByName = new Dictionary<string, TypeInfo>(StringComparer.Ordinal);

            this._resolver = new CustomAssemblyResolver(assemblySearchPaths ?? Enumerable.Empty<string>());
            this._resolver.ResolveFailure += _resolver_ResolveFailure;
            LoadAssemblies(assemblyFileNames, logger);
        }

        private AssemblyDefinition _resolver_ResolveFailure(object sender, AssemblyNameReference reference)
        {
            _logger.LogMessage(string.Format(CultureInfo.CurrentCulture, Resource.ClientCodeGen_Assembly_Load_Error, reference.FullName, string.Empty));
            return null;
        }

        class AssemblyNameReferenceComparer : IEqualityComparer<AssemblyNameReference>
        {
            public bool Equals(AssemblyNameReference x, AssemblyNameReference y)
            {
                return x.FullName == y.FullName;
            }

            public int GetHashCode(AssemblyNameReference obj)
            {
                return obj.FullName.GetHashCode();
            }
        }

        private void LoadAssemblies(IEnumerable<string> assemblyFileNames, ILogger logger)
        {
            var loaded = new HashSet<AssemblyNameReference>(new AssemblyNameReferenceComparer());
            var references = new HashSet<AssemblyNameReference>(new AssemblyNameReferenceComparer());
            foreach (var assembly in assemblyFileNames)
            {
                try
                {
                    AssemblyDefinition a = _resolver.LoadAssembly(assembly);
                    loaded.Add(a.Name);
                    IEnumerable<ModuleDefinition> ms = a.Modules;
                    foreach (ModuleDefinition m in ms)
                    {
                        AddPublicTypesToDictionary(m);

                        foreach (var reference in m.AssemblyReferences)
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

            // TODO: Consider replacing this with some smarter code using TypeReferences
            // from loaded assemblies and creating "incomplete" types with them
            // to populate the dictionary
            references.ExceptWith(loaded);
            foreach (var assembly in references)
            {
                try
                {
                    AssemblyDefinition a = _resolver.Resolve(assembly);
                    IEnumerable<ModuleDefinition> ms = a.Modules;
                    foreach (ModuleDefinition m in ms)
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

        private void AddPublicTypesToDictionary(ModuleDefinition m)
        {
            foreach (var t in m.Types)
            {
                if (t.IsPublic)
                {
                    _sharedTypeByName[t.FullName] = new TypeInfo(t);
                }
            }
        }

        TypeInfo GetBaseType(TypeInfo typeInfo)
        {
            if (typeInfo.BaseType == null
                && typeInfo.TypeDefinition.BaseType != null)
            {
                var type = typeInfo.TypeDefinition.BaseType.Resolve();
                typeInfo.BaseType = new TypeInfo(type);
            }

            return typeInfo.BaseType;
        }

        TypeInfo HasProperty(TypeInfo type, string name)
        {
            while (type != null)
            {
                // Get or Initialize Property collection
                if (type.Properties == null)
                {
                    type.Properties = new HashSet<string>();
                    foreach (var property in type.TypeDefinition.Properties)
                    {
                        var method = property.GetMethod ?? property.SetMethod;
                        if (method.IsPublic)
                            type.Properties.Add(property.Name);
                    }
                }

                if (type.Properties.Contains(name))
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
            if (this.TryGetSharedType(key.TypeName, out typeInfo))
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
                        var method = this.FindSharedMethodOrConstructor(typeInfo, key);
                        if (method != null)
                        {
                            location = method.DeclaringType.Module;
                        }
                        break;

                    default:
                        Debug.Fail("unsupported key kind");
                        break;
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
            var parameterTypes = this.GetSharedTypes(key.ParameterTypeNames);
            if (parameterTypes == null)
            {
                return null;
            }
            bool isConstructor = key.IsConstructor;

            do
            {
                var methods = sharedType.TypeDefinition.Methods;
                foreach (var method in methods)
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
        TypeInfo[] GetSharedTypes(IEnumerable<string> typeNames)
        {
            if (typeNames == null)
                return Array.Empty<TypeInfo>();

            List<TypeInfo> types = new List<TypeInfo>();
            foreach (string typeName in typeNames)
            {
                TypeInfo typeInfo = default;
                if (!this.TryGetSharedType(typeName, out typeInfo))
                {
                    return null;
                }
                types.Add(typeInfo);
            }
            return types.ToArray();
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
                    // Is array
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
                    // Or generic
                    else if (typeName[openBracet + 1] == '[')
                    {
                        string genericTypeName = typeName.Substring(0, openBracet);
                        TypeInfo genericType;
                        if (TryGetSharedType(genericTypeName, out genericType))
                        {
                            int endBracket;
                            if (TryGetSharedGenericParameters(typeName, openBracet + 1, out endBracket))
                            {
                                // Skip any part after typename which would be assembly qualified part
                                typeInfo = new TypeInfo(genericType, typeName.Substring(0, endBracket +1));
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
    }
}
