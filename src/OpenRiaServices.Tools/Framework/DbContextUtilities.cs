using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/* Unmerged change from project 'OpenRiaServices.VisualStudio.DomainServices.Tools'
Before:
using System.Reflection;
using OpenRiaServices;
using OpenRiaServices.Server;
After:
using System.Reflection;
*/
using System.Reflection;
using OpenRiaServices.Server;

namespace OpenRiaServices.Tools
{
    /// <summary>
    /// Provides DbContext Utilities using late binding so as not to take a direct dependency on EntityFramework 4.1.
    /// </summary>
    internal static class DbContextUtilities
    {
        private const string DatabaseTypeName = @"System.Data.Entity.Database";
        private const string IDatabaseInitializerTypeName = "System.Data.Entity.IDatabaseInitializer`1";
        internal const string DbContextTypeName = @"System.Data.Entity.DbContext";

        private static Type _dbContextTypeReference = null;

        /// <summary>
        /// Resets any cached DB Context type reference
        /// </summary>
        internal static void ResetDbContextTypeReference()
        {
            DbContextUtilities._dbContextTypeReference = null;
        }
        
        /// <summary>
        /// Sets the Database initializer associated to the DbContext with the given value.
        /// </summary>
        /// <param name="contextType">The type of the DbContext for which we want to set the value of the Database initializer.</param>
        /// <param name="dbContextTypeReference">The reference to DbContext type (or typeof(DbContext)).</param>
        /// <param name="initializer">The initializer object.</param>
        public static void SetDbInitializer(Type contextType, Type dbContextTypeReference, object initializer)
        {
            
            // We need the context type and the reference to DbContext type to be not null to set the database initializer.
            if (contextType == null || dbContextTypeReference == null)
            {
                return;
            }

            // Here, we basically need to do this: Database.SetInitializer<typeof(ContextType)>(initializer);
            // Load typeof(Database) from the EntityFramework assembly.
            Type databaseType = DbContextUtilities.LoadTypeFromAssembly(dbContextTypeReference.Assembly, DbContextUtilities.DatabaseTypeName);
            if (databaseType != null)
            {
                // Get the method Database.SetInitializer<DbContext>(IDatabaseInitializer<DbContext>);
                Type databaseInitializerType = dbContextTypeReference.Assembly.GetType(DbContextUtilities.IDatabaseInitializerTypeName);
                if (databaseInitializerType != null && databaseInitializerType.IsGenericType)
                {
                    databaseInitializerType = databaseInitializerType.MakeGenericType(new Type[] { dbContextTypeReference });

                    MethodInfo setInitializerMethod = databaseType.GetMethod("SetInitializer", new Type[] { databaseInitializerType });
                    if (setInitializerMethod != null && setInitializerMethod.IsGenericMethod)
                    {
                        // Add the DbContext generic parameter to the method.
                        MethodInfo genericSetInitializerMethod = setInitializerMethod.MakeGenericMethod(new Type[] { contextType });
                        if (genericSetInitializerMethod != null)
                        {
                            // We found the right method. Now invoke it with the initializer parameter.
                            genericSetInitializerMethod.Invoke(null, new object[] { initializer });
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Compares 2 types by their full names. This also checks if the type to be compared with is a system type. If either the type 
        /// or system type name are null / empty, then it returns false.
        /// </summary>
        /// <param name="type">Type to be compared with.</param>
        /// <param name="systemTypeFullName">Full name of a system type.</param>
        /// <returns><c>true</c> if the type is a system type and its name is same as the given system type name.</returns>
        public static bool CompareWithSystemType(Type type, string systemTypeFullName)
        {
            if (type == null || string.IsNullOrEmpty(systemTypeFullName))
            {
                return false;
            }

            string typeFullName = type.FullName;
            if (type.IsGenericType)
            {
                typeFullName = type.GetGenericTypeDefinition().FullName;
            }

            if (!typeFullName.Equals(systemTypeFullName))
            {
                return false;
            }

            if (!type.Assembly.IsSystemAssembly())
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Loads the given type from the given assembly.
        /// </summary>
        /// <param name="assembly">Assembly from which to load the type.</param>
        /// <param name="typeName">Name of the type to be loaded.</param>
        /// <returns>The type loaded from the given assembly.</returns>
        public static Type LoadTypeFromAssembly(Assembly assembly, string typeName)
        {
            Type type = null;
            if (assembly != null)
            {
                type = assembly.GetType(typeName, /* throw on error */ false, /* ignore case */ false);
            }
            return type;
        }

        /// <summary>
        /// Finds the reference to System.Data.Entity.DbContext type given the contextType, if it exists.
        /// </summary>
        /// <param name="contextType">The context type from which the DbContext type reference is to be found</param>
        /// <returns>The reference to DbContext type.</returns>
        public static Type GetDbContextTypeReference(Type contextType)
        {
            
            if (DbContextUtilities._dbContextTypeReference == null)
            {
                // if contextType is an interface or a value type, then we know it is not a DbContext type.
                if (!contextType.IsInterface && !contextType.IsValueType)
                {
                    Type t = contextType;
                    // If the type if null or object, then it is not the DbContext type. 
                    // We need to check for null as well, since Walking an interface hierarchy does not lead to Object.
                    while (t != null && t != typeof(object))
                    {
                        if (DbContextUtilities.CompareWithSystemType(t, DbContextUtilities.DbContextTypeName))
                        {
                            DbContextUtilities._dbContextTypeReference = t;
                            break;
                        }
                        t = t.BaseType;
                    }
                }
            }
            return DbContextUtilities._dbContextTypeReference;
        }

#if WIZARD
        
        /// <summary>
        /// Returns the reference to the DbContext type. This is non-null only if there is a type deriving from DbContext in the project.
        /// </summary>
        public static Type DbContextTypeReference
        {
            get
            {
                return DbContextUtilities._dbContextTypeReference;
            }
        }

        /// <summary>
        /// Checks if this type is assignable from typeof(DbContext).
        /// </summary>
        /// <param name="type">The type to check if it is a DbContext.</param>
        /// <returns><c>true</c> is the type is a DbContext, <c>false</c> otherwise.</returns>
        public static bool IsDbContext(this Type type)
        {
            // If we have a reference to typeof(DbContext), then check if type is assignable from it.
            if (DbContextUtilities._dbContextTypeReference != null)
            {
                return DbContextUtilities._dbContextTypeReference.IsAssignableFrom(type);
            }
            else
            {
                // If we don't have reference to typeof(DbContext), then compare the base types to see if one of them is the EntityFramework DbContext type.
                // If we find a match, we also find the DbContext type. So populate the _dbContextTypeReference with that value.
                Type t = DbContextUtilities.GetDbContextTypeReference(type);
                if (t != null)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
#else
        private const string DbDomainServiceTypeName = @"OpenRiaServices.EntityFramework.DbDomainService`1";       

        /// <summary>
        /// Returns the DbContext type given the <see cref="DomainService"/> type. Uses late binding so as to avoid adding a reference to EF 4.1.
        /// </summary>
        /// <param name="domainServiceType">The type of the domain service.</param>
        /// <returns>The type of the DbContext.</returns>
        public static Type GetDbContextType(Type domainServiceType)
        {
            Type dbContextType = null;
            Type currType = domainServiceType;
            while (currType != null && currType != typeof(object) && currType != typeof(DomainService))
            {
                if (currType.IsGenericType && DbContextUtilities.CompareWithSystemType(currType, DbContextUtilities.DbDomainServiceTypeName))
                {
                    Type[] typeArgs = currType.GetGenericArguments();
                    if (typeArgs != null && typeArgs.Any())
                    {
                        dbContextType = typeArgs[0];
                    }
                    break;
                }
                currType = currType.BaseType;
            }
            return dbContextType;
        }
#endif
    }
}
