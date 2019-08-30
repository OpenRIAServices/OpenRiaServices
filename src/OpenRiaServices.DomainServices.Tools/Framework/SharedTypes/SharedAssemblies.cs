using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using OpenRiaServices.DomainServices;

namespace OpenRiaServices.DomainServices.Tools.SharedTypes
{
    /// <summary>
    /// Internal class to maintain a set of known assemblies
    /// and allow clients to ask whether types or methods are
    /// in that set.
    /// </summary>
    internal class SharedAssemblies
    {
        private readonly List<string> _assemblyFileNames;
        private List<Assembly> _assemblies;
        private readonly IEnumerable<string> _assemblySearchPaths;
        private readonly ConcurrentDictionary<string, Type> _sharedTypeByName = new ConcurrentDictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        private readonly ILogger _logger;

        /// <summary>
        /// Creates a new instance of this type.
        /// </summary>
        /// <param name="assemblyFileNames">The set of assemblies to use</param>
        /// <param name="assemblySearchPaths">Optional set of paths to search for referenced assemblies.</param>
        /// <param name="logger">Optional logger to use to report errors or warnings</param>
        internal SharedAssemblies(IEnumerable<string> assemblyFileNames, IEnumerable<string> assemblySearchPaths, ILogger logger)
        {
            if (assemblyFileNames == null)
            {
                throw new ArgumentNullException("assemblyFileNames");
            }
            this._logger = logger;
            this._assemblyFileNames = new List<string>(assemblyFileNames);
            this._assemblySearchPaths = assemblySearchPaths ?? Array.Empty<string>();
        }

        /// <summary>
        /// Gets the set of loaded assemblies
        /// </summary>
        private IEnumerable<Assembly> Assemblies
        {
            get
            {
                if (this._assemblies == null)
                {
                    this.LoadAssemblies();
                }
                return this._assemblies;
            }
        }

        /// <summary>
        /// Returns the location of the shared assembly containing the
        /// code member described by <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The description of the code element.</param>
        /// <returns>The location of the assembly that contains it or <c>null</c> if it is not in a shared assembly.</returns>
        internal string GetSharedAssemblyPath(CodeMemberKey key)
        {
            Debug.Assert(key != null, "key cannot be null");
            string location = null;

            Type type = this.GetSharedType(key.TypeName);
            if (type != null)
            {
                switch (key.KeyKind)
                {
                    case CodeMemberKey.CodeMemberKeyKind.TypeKey:
                        location = type.Assembly.Location;
                        break;

                    case CodeMemberKey.CodeMemberKeyKind.PropertyKey:
                        PropertyInfo propertyInfo = type.GetProperty(key.MemberName);
                        if (propertyInfo != null)
                        {
                            location = propertyInfo.DeclaringType.Assembly.Location;
                        }
                        break;

                    case CodeMemberKey.CodeMemberKeyKind.MethodKey:
                        Type[] parameterTypes = this.GetSharedTypes(key.ParameterTypeNames);
                        if (parameterTypes != null)
                        {
                            MethodBase methodBase = this.FindSharedMethodOrConstructor(type, key);
                            if (methodBase != null)
                            {
                                location = methodBase.DeclaringType.Assembly.Location;
                            }
                        }
                        break;

                    default:
                        Debug.Fail("unsupported key kind");
                        break;
                }
            }
            return location;
        }

        /// <summary>
        /// Locates the <see cref="MethodBase"/> in the set of shared assemblies that
        /// corresponds to the method described by <paramref name="key"/>.
        /// </summary>
        /// <param name="sharedType">The <see cref="Type"/> we have already located in our set of shared assemblies.</param>
        /// <param name="key">The key describing the method to find.</param>
        /// <returns>The matching <see cref="MethodBase"/> or <c>null</c> if no match is found.</returns>
        private MethodBase FindSharedMethodOrConstructor(Type sharedType, CodeMemberKey key)
        {
            Type[] parameterTypes = this.GetSharedTypes(key.ParameterTypeNames);
            if (parameterTypes == null)
            {
                return null;
            }
            bool isConstructor = key.IsConstructor;
            IEnumerable<MethodBase> methods = isConstructor ? sharedType.GetConstructors().Cast<MethodBase>() : sharedType.GetMethods().Cast<MethodBase>();
            foreach (MethodBase method in methods)
            {
                if (!isConstructor && !string.Equals(method.Name, key.MemberName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                ParameterInfo[] parameterInfos = method.GetParameters();
                if (parameterInfos.Length != parameterTypes.Length)
                {
                    continue;
                }
                int matchedParameters = 0;
                for (int i = 0; i < parameterInfos.Length; ++i)
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

                if (matchedParameters == parameterInfos.Length)
                {
                    return method;
                }
            }
            return null;
        }

        /// <summary>
        /// Given a collection of <see cref="Type"/> names, this method returns
        /// an array of all those types from the set of shared assemblies.
        /// </summary>
        /// <param name="typeNames">The collection of type names.  It can be null.</param>
        /// <returns>The collection of types in the shared assemblies.
        /// A <c>null</c> means one or more types in the list were not shared.</returns>
        private Type[] GetSharedTypes(IEnumerable<string> typeNames)
        {
            List<Type> types = new List<Type>();
            if (typeNames != null)
            {
                foreach (string typeName in typeNames)
                {
                    Type type = this.GetSharedType(typeName);
                    if (type == null)
                    {
                        return null;
                    }
                    types.Add(type);
                }
            }
            return types.ToArray();
        }

        /// <summary>
        /// Returns the <see cref="MethodBase"/> of the method or constructor from the
        /// set of shared assemblies, if it exists.
        /// </summary>
        /// <param name="typeName">The fully qualified type name declaring the method.</param>
        /// <param name="methodName">The name of the method</param>
        /// <param name="parameterTypeNames">The fully qualified type names of the method parameters.</param>
        /// <returns>The <see cref="MethodBase"/> if it exists in the shared assemblies, otherwise <c>null</c></returns>
        internal MethodBase GetSharedMethod(string typeName, string methodName, IEnumerable<string> parameterTypeNames)
        {
            Debug.Assert(!string.IsNullOrEmpty(typeName), "typeName cannot be null");
            Debug.Assert(!string.IsNullOrEmpty(methodName), "methodName cannot be null");

            MethodBase sharedMethod = null;
            Type sharedType = this.GetSharedType(typeName);
            if (sharedType != null)
            {
                CodeMemberKey key = CodeMemberKey.CreateMethodKey(typeName, methodName, parameterTypeNames == null ? Array.Empty<string>() : parameterTypeNames.ToArray());
                sharedMethod = this.FindSharedMethodOrConstructor(sharedType, key);
            }

            return sharedMethod;
        }

        /// <summary>
        /// Returns the <see cref="Type"/> from the set of shared assemblies of the given name.
        /// </summary>
        /// <param name="typeName">The fully qualified type name.</param>
        /// <returns>The <see cref="Type"/> from the shared assemblies if it exists, otherwise <c>null</c>.</returns>
        internal Type GetSharedType(string typeName)
        {
            Type sharedType = this._sharedTypeByName.GetOrAdd(typeName, n =>
            {
                return this.FindSharedType(n);
            });
            return sharedType;
        }

        /// <summary>
        /// If the given type is defined in a system assembly, returns the type directly, otherwise <c>null</c>.
        /// </summary>
        /// <param name="type">The type whose equivalent we need.</param>
        /// <returns>If the type lives in a system assembly, the input type, else <c>null</c>.</returns>
        private static Type EquivalentMsCorlibType(Type type)
        {
            if (AssemblyUtilities.IsAssemblyMsCorlib(type.Assembly.GetName()))
            {
                // For now, only a core set of predefined system types are assumed to be supported
                return TypeUtility.IsPredefinedType(type) ? type : null;
            }

            return null;
        }

        /// <summary>
        /// Searches the shared assemblies for a type of the given name.
        /// </summary>
        /// <param name="typeName">The fully-qualified type name.</param>
        /// <returns>The <see cref="Type"/> or <c>null</c> if it is not in one of the shared assemblies.</returns>
        private Type FindSharedType(string typeName)
        {
            Type type = Type.GetType(typeName, /*throwOnError*/ false);
            if (type != null)
            {
                Type result = FindSharedTypeInAssemblies(type.FullName);
                if (result != null)
                    return result;

                // TODO: review
                // If we could not find the type, but it lives in mscorlib,
                // we treat it specially because we cannot load mscorlib to tell.
                return EquivalentMsCorlibType(type);
            }

            return FindSharedTypeInAssemblies(typeName);
        }

        /// <summary>
        /// Locates and returns the type in the set of known assemblies
        /// that is equivalent to the given type.
        /// </summary>
        /// <param name="typeFullName">FullName of the type to search for</param>        
        private Type FindSharedTypeInAssemblies(string typeFullName)
        {
            foreach (Assembly assembly in this.Assemblies)
            {
                // Utility autorecovers and logs known common exceptions
                IEnumerable<Type> types = AssemblyUtilities.GetExportedTypes(assembly, this._logger);

                foreach (Type searchType in types)
                {
                    if (string.Equals(typeFullName, searchType.FullName, StringComparison.Ordinal))
                    {
                        return searchType;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Loads all the named assemblies into a cache of known assemblies
        /// </summary>
        private void LoadAssemblies()
        {
            this._assemblies = new List<Assembly>();
            Dictionary<string, Assembly> loadedAssemblies = new Dictionary<string, Assembly>();
            foreach (string file in this._assemblyFileNames)
            {
                // Pass 1 -- load all the assemblies we have been given.  No referenced assemblies yet.
                Assembly assembly = AssemblyUtilities.ReflectionOnlyLoadFrom(file, this._logger);
                if (assembly != null)
                {
                    this._assemblies.Add(assembly);

                    // Keep track of loaded assemblies for next step
                    loadedAssemblies[assembly.FullName] = assembly;
                }
            }

            // Pass 2 -- recursively load all reference assemblies from the assemblies we loaded.
            foreach (Assembly assembly in this._assemblies)
            {
                AssemblyUtilities.ReflectionOnlyLoadReferences(assembly, this._assemblySearchPaths, loadedAssemblies, /*recursive*/ true, this._logger);
            }
        }
    }
}
