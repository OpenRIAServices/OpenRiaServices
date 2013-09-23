using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using OpenRiaServices.DomainServices;
using System.Xml.XPath;

namespace OpenRiaServices.DomainServices.Tools
{
    /// <summary>
    /// Utilities related to configuring an <see cref="AppDomain"/>
    /// that will have Silverlight assemblies loaded into it.
    /// </summary>
    /// <remarks>
    /// This class implements logic specific to handling Silverlight assembly
    /// versioning to allow referenced Silverlight assemblies to be loaded
    /// and examined properly.
    /// </remarks>
    internal static class SilverlightAppDomainUtilities
    {
        /// <summary>
        /// The key to use for storing the framework manifest as data on
        /// the <see cref="AppDomain"/> through <see cref="AppDomain.SetData(string,object)"/>
        /// and retrieving it through <see cref="AppDomain.GetData"/>.
        /// </summary>
        private const string FrameworkManifestKey = "FrameworkManifest";

        /// <summary>
        /// Creates an <see cref="AppDomain"/> configured for Silverlight code generation.
        /// </summary>
        /// <param name="silverlightFrameworkDirectory">The directory containing the Silverlight framework manifest.</param>
        internal static void ConfigureAppDomain(string silverlightFrameworkDirectory)
        {
            FrameworkManifest frameworkManifest = SilverlightAppDomainUtilities.GetFrameworkManifest(silverlightFrameworkDirectory);

            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += SilverlightAppDomainUtilities.ResolveFrameworkAssemblyVersioning;
            AppDomain.CurrentDomain.SetData(FrameworkManifestKey, frameworkManifest);
        }

        /// <summary>
        /// Gets the list of Silverlight assemblies found in the manifest within the specified
        /// Silverlight framework directory.
        /// </summary>
        /// <param name="silverlightFrameworkDirectory">The directory containing the Silverlight framework manifest.</param>
        /// <returns>The list of assemblies that are part of the Silverlight runtime.</returns>
        private static FrameworkManifest GetFrameworkManifest(string silverlightFrameworkDirectory)
        {
            FrameworkManifest manifest = new FrameworkManifest();
            List<FrameworkManifestEntry> assemblies = new List<FrameworkManifestEntry>();

            XPathDocument manifestDocument = new XPathDocument(silverlightFrameworkDirectory + "slr.dll.managed_manifest");
            XPathNavigator navigator = manifestDocument.CreateNavigator();
            XPathNodeIterator iterator = navigator.Select("/manifest/*[name and publickeytoken and version]");

            while (iterator.MoveNext())
            {
                // guaranteed to have name, publickeytoken and version by the XPath query
                iterator.Current.MoveToChild("name", string.Empty);
                string name = iterator.Current.Value;
                iterator.Current.MoveToParent();

                iterator.Current.MoveToChild("publickeytoken", string.Empty);
                string publickeytoken = iterator.Current.Value;
                iterator.Current.MoveToParent();

                iterator.Current.MoveToChild("version", string.Empty);
                string version = iterator.Current.Value;
                iterator.Current.MoveToParent();

                // The public key token that was read as a hex string; convert it to a byte array
                // Every 2 characters represent a single byte in hex.
                byte[] publicKeyTokenBytes =
                    Enumerable.Range(0, publickeytoken.Length)
                    .Where(x => 0 == x % 2)
                    .Select(x => Convert.ToByte(publickeytoken.Substring(x, 2), 16))
                    .ToArray();

                FrameworkManifestEntry assembly = new FrameworkManifestEntry();
                assembly.Name = name;
                assembly.Version = new Version(version);
                assembly.PublicKeyTokenBytes = publicKeyTokenBytes;

                assemblies.Add(assembly);
            }

            manifest.Assemblies = assemblies.ToArray();
            return manifest;
        }

        /// <summary>
        /// An event handler for resolving Silverlight framework assembly versioning.
        /// </summary>
        /// <remarks>
        /// When a previous version of a Silverlight assembly is sought, the targeted version
        /// of that Silverlight assembly will be returned.
        /// </remarks>
        /// <param name="sender">The event sender.</param>
        /// <param name="args">The assembly resolution event arguments.</param>
        /// <returns>The <see cref="Assembly"/> from the targeted version of Silverlight, or <c>null</c>.</returns>
        private static Assembly ResolveFrameworkAssemblyVersioning(object sender, ResolveEventArgs args)
        {
            FrameworkManifest frameworkManifest = (FrameworkManifest)AppDomain.CurrentDomain.GetData(SilverlightAppDomainUtilities.FrameworkManifestKey);
            System.Diagnostics.Debug.Assert(frameworkManifest != null, "The FrameworkManifest must have been set on the AppDomain");

            AssemblyName requestedAssembly = new AssemblyName(args.Name);

            // If the requested assembly is a System assembly and it's an older version
            // than the framework manifest has, then we'll need to resolve to its newer version
            bool isOldVersion = requestedAssembly.Version.CompareTo(frameworkManifest.SystemVersion) < 0;

            if (isOldVersion && requestedAssembly.IsSystemAssembly())
            {
                // Now we need to see if the requested assembly is part of the framework manifest (as opposed to an SDK assembly)
                var silverlightAssembly = (from assembly in frameworkManifest.Assemblies
                                           where assembly.Name == requestedAssembly.Name
                                           select assembly).SingleOrDefault();

                // If the assembly is part of the framework manifest, then we need to "redirect" its resolution
                // to the current framework version.
                if (silverlightAssembly != null)
                {
                    // Find the Silverlight framework assembly from the already-loaded assemblies
                    var matches = from assembly in AppDomain.CurrentDomain.ReflectionOnlyGetAssemblies()
                                  let assemblyName = assembly.GetName()
                                  where assemblyName.Name == silverlightAssembly.Name
                                     && assemblyName.GetPublicKeyToken().SequenceEqual(silverlightAssembly.PublicKeyTokenBytes)
                                     && assemblyName.Version.CompareTo(silverlightAssembly.Version) == 0
                                  select assembly;

                    return matches.SingleOrDefault();
                }
            }

            return null;
        }

        /// <summary>
        /// A framework manifest entry with assembly name parts.
        /// </summary>
        internal class FrameworkManifestEntry : MarshalByRefObject
        {
            internal string Name { get; set; }
            internal Version Version { get; set; }
            internal byte[] PublicKeyTokenBytes { get; set; }
        }

        /// <summary>
        /// Represents the framework manifest with its System version and
        /// the list of framework assemblies.
        /// </summary>
        internal class FrameworkManifest : MarshalByRefObject
        {
            private FrameworkManifestEntry[] _assemblies;

            internal Version SystemVersion { get; private set; }
            internal FrameworkManifestEntry[] Assemblies
            {
                get
                {
                    return this._assemblies;
                }
                set
                {
                    this._assemblies = value;
                    this.SystemVersion = (from assembly in this._assemblies
                                          where assembly.Name == "System"
                                          select assembly.Version).SingleOrDefault();
                }
            }
        }
    }
}
