﻿using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace OpenRiaServices.VisualStudio.Installer.Helpers
{
    /// <summary>
    /// This class unifies all the different ways of getting services within visual studio.
    /// </summary>
    /// <from>NuGet</from>   
    public static class ServiceLocator
    {
        public static void InitializePackageServiceProvider(IServiceProvider provider)
        {
            if (provider == null)
            {
                throw new ArgumentNullException("provider");
            }

            PackageServiceProvider = provider;
        }

        public static IServiceProvider PackageServiceProvider
        {
            get;
            private set;
        }

        public static TService GetInstance<TService>() where TService : class
        {
            // Special case IServiceProvider
            if (typeof(TService) == typeof(IServiceProvider))
            {
                return (TService)GetServiceProvider();
            }

            // then try to find the service as a component model, then try dte then lastly try global service
            // Per bug #2072, avoid calling GetGlobalService() from within the Initialize() method of NuGetPackage class. 
            // Doing so is illegal and may cause VS to hang. As a result of that, we defer calling GetGlobalService to the last option.
            return GetDTEService<TService>() ??
                   GetComponentModelService<TService>() ??
                   GetGlobalService<TService, TService>();
        }

        public static TInterface GetGlobalService<TService, TInterface>() where TInterface : class
        {
            if (PackageServiceProvider != null)
            {
                TInterface service = PackageServiceProvider.GetService(typeof(TService)) as TInterface;
                if (service != null)
                {
                    return service;
                }
            }

            return (TInterface)Package.GetGlobalService(typeof(TService));
        }

        private static TService GetDTEService<TService>() where TService : class
        {
            var dte = GetGlobalService<SDTE, DTE>();
            return (TService)QueryService(dte, typeof(TService));
        }

        private static TService GetComponentModelService<TService>() where TService : class
        {
            IComponentModel componentModel = GetGlobalService<SComponentModel, IComponentModel>();
            return componentModel.GetService<TService>();
        }

        private static IServiceProvider GetServiceProvider()
        {
            var dte = GetGlobalService<SDTE, DTE>();
            return GetServiceProvider(dte);
        }

        private static object QueryService(_DTE dte, Type serviceType)
        {
            Guid guidService = serviceType.GUID;
            Guid riid = guidService;
            var serviceProvider = dte as Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

            IntPtr servicePtr;
            int hr = serviceProvider.QueryService(ref guidService, ref riid, out servicePtr);

            if (hr != VsConstants.S_OK)
            {
                // We didn't find the service so return null
                return null;
            }

            object service = null;

            if (servicePtr != IntPtr.Zero)
            {
                service = Marshal.GetObjectForIUnknown(servicePtr);
                Marshal.Release(servicePtr);
            }

            return service;

        }

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "The caller is responsible for disposing this")]
        private static IServiceProvider GetServiceProvider(_DTE dte)
        {
            IServiceProvider serviceProvider = new ServiceProvider(dte as Microsoft.VisualStudio.OLE.Interop.IServiceProvider);
            Debug.Assert(serviceProvider != null, "Service provider is null");
            return serviceProvider;
        }
    }
}