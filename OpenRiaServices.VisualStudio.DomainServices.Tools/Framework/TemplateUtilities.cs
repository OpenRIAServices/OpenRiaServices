using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;

using IServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace OpenRiaServices.VisualStudio.DomainServices.Tools
{
    /// <summary>
    /// Utility methods shared by the template wizards.
    /// </summary>
    public static class TemplateUtilities
    {
#if VS10
        private const string SilverlightDesignerKeyName = @"Software\Microsoft\VisualStudio\10.0\DesignerPlatforms\Silverlight";
        public const decimal DefaultSilverlightVersion = 4.0m;
#elif VS11
        private const string SilverlightDesignerKeyName = @"Software\Microsoft\VisualStudio\11.0\DesignerPlatforms\Silverlight";
        public const decimal DefaultSilverlightVersion = 4.0m;
#elif VS12
        private const string SilverlightDesignerKeyName = @"Software\Microsoft\VisualStudio\12.0\DesignerPlatforms\Silverlight";
        public const decimal DefaultSilverlightVersion = 5.0m;
#else
#error OpenRiaServices.VisualStudio.DomainServices.Tools must target either Visual Studio Version 10.0 or 11.0 or 12.0
#endif
        private const string SilverlightHostValueName = "SilverlightHost";
        

        // Copied from Microsoft.VisualStudio.Shell.ServiceProvider in Microsoft.VisualStudio.Shell.10.0.dll
        public static InterfaceType GetService<InterfaceType, ServiceType>(IServiceProvider serviceProvider)
            where InterfaceType : class
            where ServiceType : class
        {
            InterfaceType service = default(InterfaceType);
            try
            {
                IntPtr zero = IntPtr.Zero;
                Guid guidService = typeof(ServiceType).GUID;
                Guid riid = typeof(InterfaceType).GUID;
                if ((0 == serviceProvider.QueryService(ref guidService, ref riid, out zero)) && (IntPtr.Zero != zero))
                {
                    try
                    {
                        return Marshal.GetObjectForIUnknown(zero) as InterfaceType;
                    }
                    finally
                    {
                        Marshal.Release(zero);
                    }
                }
            }
            catch (System.Security.SecurityException)
            {
                // Swallow security exceptions and fall back to our default Silverlight version
            }
            return service;
        }

        public static string GetSilverlightVersion(object serviceProvider)
        {
            string version = null;
            string toolVersion = GetSilverlightToolsVersion();
            Debug.Assert(serviceProvider is IServiceProvider, "serviceProvider must implement IServiceProvider!");
            foreach (string slVersion in GetInstalledSLFrameworks((IServiceProvider)serviceProvider))
            {
                int compare = String.Compare(slVersion, toolVersion, StringComparison.OrdinalIgnoreCase);
                // only consider sl supported by tool (slVersion < toolVersion)
                // exact match is preferred.
                if (compare <= 0)
                {
                    version = slVersion;
                    if (compare == 0)
                    {
                        break;
                    }
                }
            }

            return version ?? String.Format(CultureInfo.InvariantCulture, "v{0:0.0}", TemplateUtilities.DefaultSilverlightVersion);
        }

        /// <summary>
        /// Get the highest version of Silverlight supported by the Silverlight Tools on the machine.
        /// </summary>
        /// <returns>A version string that includes the prefix of 'v'.</returns>
        private static string GetSilverlightToolsVersion()
        {
            string silverlightHost = string.Format(System.Globalization.CultureInfo.InvariantCulture, "v{0:0.0}", TemplateUtilities.DefaultSilverlightVersion);

            try
            {
                using (RegistryKey designerKey = Registry.LocalMachine.OpenSubKey(TemplateUtilities.SilverlightDesignerKeyName))
                {
                    if (designerKey != null)
                    {
                        silverlightHost = (string)designerKey.GetValue(SilverlightHostValueName);
                    }
                }
            }
            catch (System.Security.SecurityException)
            {
                // Swallow security exceptions and fall back to our default Silverlight version
            }

            return silverlightHost;
        }

        // Copied from SLPackage.Package.GetInstalledSLFrameworks in Microsoft.VisualStudio.Silverlight.dll
        private static List<string> GetInstalledSLFrameworks(IServiceProvider serviceProvider)
        {
            List<string> list = new List<string>();
            IVsFrameworkMultiTargeting service = GetService<IVsFrameworkMultiTargeting, SVsFrameworkMultiTargeting>(serviceProvider);
            if (service != null)
            {
                Array array;
                service.GetSupportedFrameworks(out array);
                // array content samples:
                // ".NETFramework,Version=v2.0"
                // ".NETFramework,Version=v3.0"
                // ".NETFramework,Version=v3.5"
                // ".NETFramework,Version=v3.5,Profile=Client"
                // ".NETFramework,Version=v4.0"
                // ".NETFramework,Version=v4.0,Profile=Client"
                // "Silverlight,Version=v4.0"
                // "Silverlight,Version=v4.0,Profile=WindowsPhone71"
                // "Silverlight,Version=v5.0"
                foreach (string str in array)
                {
                    FrameworkName name = new FrameworkName(str);
                    if ((String.Compare(name.Identifier, "Silverlight", StringComparison.OrdinalIgnoreCase) == 0 && name.Version.Major > 2)
                        && String.IsNullOrEmpty(name.Profile))
                    {
                        string version = String.Format(CultureInfo.InvariantCulture, "v{0}.{1}", name.Version.Major, name.Version.Minor);
                        if (!String.IsNullOrEmpty(GetSilverlightRuntimeSDKInstallLocation(version)))
                        {
                            list.Add(version);
                        }
                    }
                }
            }
            list.Sort();
            return list;
        }

        // Copied from Microsoft.VisualStudio.Silverlight.SLPackage in Microsoft.VisualStudio.Silverlight.dll
        private static string GetSilverlightRuntimeSDKInstallLocation(string version)
        {
            string str = null;
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(String.Format(CultureInfo.InvariantCulture, @"SOFTWARE\Microsoft\Microsoft SDKs\Silverlight\{0}\ReferenceAssemblies", version)))
                {
                    if (key != null)
                    {
                        str = (string)key.GetValue("SLRuntimeInstallPath");
                    }
                }
            }
            catch (System.Security.SecurityException)
            {
                // Swallow security exceptions and fall back to our default Silverlight version
            }
            return str;
        }
    }
}
