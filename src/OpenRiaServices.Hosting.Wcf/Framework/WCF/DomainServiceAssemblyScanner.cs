﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using OpenRiaServices.Server;
using System.Web.Compilation;

namespace OpenRiaServices.Hosting.Wcf
{
    static class DomainServiceAssemblyScanner
    {
        public static IEnumerable<Type> DiscoverDomainServices(IEnumerable<Assembly> assemblies = null)
        {
            assemblies ??= BuildManager.GetReferencedAssemblies().Cast<Assembly>();

            Type domainServiceBaseType = typeof(DomainService);
            List<Type> types = new List<Type>();

            foreach (Assembly assembly in assemblies)
            {
                if (!TypeUtility.CanContainDomainServiceImplementations(assembly))
                    continue;

                Type[] exportedTypes = null;
                try
                {
                    exportedTypes = assembly.GetExportedTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    exportedTypes = ex.Types;
                }
                catch (Exception ex)
                {
                    if (ex.IsFatal())
                    {
                        throw;
                    }
                    // If we're unable to load the assembly, ignore it for now.
                }

                if (exportedTypes != null)
                {
                    foreach (Type exportedType in exportedTypes)
                    {
                        if (exportedType.IsAbstract || exportedType.IsInterface || exportedType.IsValueType || !domainServiceBaseType.IsAssignableFrom(exportedType))
                        {
                            continue;
                        }

                        if (TypeDescriptor.GetAttributes(exportedType)[typeof(EnableClientAccessAttribute)] == null)
                        {
                            continue;
                        }

                        types.Add(exportedType);
                    }
                }
            }

            return types;
        }
    }
}
