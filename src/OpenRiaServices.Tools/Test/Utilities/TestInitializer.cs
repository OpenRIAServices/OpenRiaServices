using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.Build.Locator;
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
#if NETFRAMEWORK
            var vsInstance =
            Microsoft.Build.Locator.MSBuildLocator.QueryVisualStudioInstances(new Microsoft.Build.Locator.VisualStudioInstanceQueryOptions()
            {
                DiscoveryTypes = Microsoft.Build.Locator.DiscoveryType.DotNetSdk | Microsoft.Build.Locator.DiscoveryType.VisualStudioSetup
            })
            .First();

            s_msbuildPath = vsInstance.MSBuildPath;

            s_loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => IsMsBuildAssembly(a.FullName))
                .ToDictionary(x => x.GetName().Name);

            // Load msbuild assemblt
            var msbuildAssemblies = Directory.GetFiles(s_msbuildPath, "MSBuild*.dll");
            s_loadedAssemblies.Clear();
            foreach (var msbuildAssembly in msbuildAssemblies)
            {
                var assembly = Assembly.LoadFrom(msbuildAssembly);
                s_loadedAssemblies.Add(assembly.GetName().Name, assembly);
            }

            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            Microsoft.Build.Locator.MSBuildLocator.RegisterInstance(vsInstance);
#else
            // Register the most recent version of MSBuild
            Microsoft.Build.Locator.MSBuildLocator.RegisterInstance(MSBuildLocator.QueryVisualStudioInstances().OrderByDescending(
               instance => instance.Version).First());

            //s_msbuildPath = Path.GetDirectoryName(typeof(TestInitializer).Assembly.Location);
            //var msbuildAssemblies = Directory.GetFiles(s_msbuildPath, "Microsoft.Build*.dll");
            //s_loadedAssemblies = new();
            //foreach (var msbuildAssembly in msbuildAssemblies)
            //{
            //    var assembly = Assembly.LoadFrom(msbuildAssembly);
            //    s_loadedAssemblies.Add(assembly.GetName().Name, assembly);
            //}

            //AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
#endif
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
