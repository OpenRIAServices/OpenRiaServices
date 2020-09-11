using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpenRiaServices.Tools.Test.Utilities
{
    [TestClass]
    public sealed class TestInitializer
    {
        // List for resolved assemblies
        // contains entries by both fullname and just by "short" name
        private static Dictionary<string, Assembly> s_loadedAssemblies;
        private static string s_msbuildPath;

        [AssemblyInitialize]
        public static void AssemblyInit(TestContext context)
        {
            RegisterMSBuildAssemblyResolve();

            //Set currenct culture to en-US by default since there are hard coded
            //strings in some tests
            Thread.CurrentThread.CurrentCulture =
            Thread.CurrentThread.CurrentUICulture =
            CultureInfo.DefaultThreadCurrentCulture =
            CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");

            // Ensure we reference sql client so that it is copied to test folder
            // se https://stackoverflow.com/questions/18455747/no-entity-framework-provider-found-for-the-ado-net-provider-with-invariant-name#18642452
            var type = typeof(System.Data.Entity.SqlServer.SqlProviderServices);
            if (type == null)
                throw new Exception("Do not remove, ensures static reference to System.Data.Entity.SqlServer");
        }


        private static void RegisterMSBuildAssemblyResolve()
        {
            var vsInstance =
            Microsoft.Build.Locator.MSBuildLocator.QueryVisualStudioInstances()
            .First();

            s_msbuildPath = vsInstance.MSBuildPath;

            s_loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => IsMsBuildAssembly(a.FullName))
                .ToDictionary(x => x.GetName().Name);

            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        private static void UnregisterMSBuildAssemblies()
        {
            AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
        }

        /// <summary>
        /// Determines if an assembly name refers to an msbuild assembly
        /// </summary>
        /// <param name="fullName"></param>
        /// <returns></returns>
        private static bool IsMsBuildAssembly(string fullName)
        {
            return fullName.StartsWith("Microsoft.Build", StringComparison.OrdinalIgnoreCase);
        }


        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (!IsMsBuildAssembly(args.Name))
                return null;

            // Try to match by full name, then by name and last search in msbuild directory
            lock (s_loadedAssemblies)
            {
                Assembly assembly;
                if (s_loadedAssemblies.TryGetValue(args.Name, out assembly))
                {
                    return assembly;
                }

                var assemblyName = new AssemblyName(args.Name);
                if (s_loadedAssemblies.TryGetValue(assemblyName.Name, out assembly))
                {
                    s_loadedAssemblies.Add(assemblyName.FullName, assembly);
                    return assembly;
                }

                var filePath = Path.Combine(s_msbuildPath, assemblyName.Name + ".dll");
                if (File.Exists(filePath))
                {
                    assembly = Assembly.LoadFrom(filePath);
                }
                else
                {
                    assembly = null;
                }

                s_loadedAssemblies.Add(assemblyName.Name, assembly);
                s_loadedAssemblies.Add(args.Name, assembly);
                return assembly;
            }
        }

    }
}
