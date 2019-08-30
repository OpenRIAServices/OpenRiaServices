using System;
using System.Configuration;
using System.ServiceModel.Activation;
using System.ServiceModel.Configuration;
using System.Diagnostics.Eventing;
using System.Globalization;
using System.Security;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Web.Configuration;
using System.Web.Hosting;

namespace OpenRiaServices.DomainServices.Hosting
{
    internal static class DiagnosticUtility
    {
        private static readonly EventProvider provider = CreateEventProvider();

        // See MSDN for documentation on these events.
        private static EventDescriptor operationInvokedEvent = new EventDescriptor(205, 0x0, 0x12, 0x4, 0x0, 0x0, (long)0x2000000000080004); // 205
        private static EventDescriptor operationCompletedEvent = new EventDescriptor(214, 0x0, 0x12, 0x4, 0x0, 0x0, (long)0x20000000000E0004); // 214
        private static EventDescriptor operationFailedEvent = new EventDescriptor(222, 0x0, 0x12, 0x3, 0x0, 0x0, (long)0x20000000000E0004); // 222
        private static EventDescriptor operationFaultedEvent = new EventDescriptor(223, 0x0, 0x12, 0x3, 0x0, 0x0, (long)0x20000000000E0004); // 223
        private static EventDescriptor serviceExceptionEvent = new EventDescriptor(219, 0x0, 0x12, 0x2, 0x0, 0x0, (long)0x20000000000E0004); // 219

        private static string appDomain = AppDomain.CurrentDomain.FriendlyName;
        private static string hostReference = String.Empty;

        private static EventProvider CreateEventProvider()
        {
            bool isTracingSupported = AppDomain.CurrentDomain.IsFullyTrusted && Environment.OSVersion.Version.Major >= 6;
            if (isTracingSupported)
            {
                DiagnosticSection section = null;

                try
                {
                    section = (DiagnosticSection)WebConfigurationManager.GetSection("system.serviceModel/diagnostics");
                }
                catch (ConfigurationErrorsException)
                {
                    // If we're unable to retrieve the section, fall-back on the default ETW provider ID.
                }
                catch (SecurityException)
                {
                    // If we're unable to retrieve the section, fall-back on the default ETW provider ID.
                }

                if (section != null && !String.IsNullOrEmpty(section.EtwProviderId))
                {
                    // Our assembly is SecurityTransparent, but EventProvider's constructor is SecurityCritical. We 
                    // use reflection to invoke the constructor such that we can remain SecurityTransparent. However, 
                    // because there's also a CAS demand at runtime that demands a certain permission, we can only 
                    // do all of this when the application is running in full-trust.
                    // We also only support this on Vista or greater, because ETW is not supported on older platforms.
                    return (EventProvider)Activator.CreateInstance(typeof(EventProvider), new Guid(section.EtwProviderId));
                }
            }

            return null;
        }

        public static void OperationInvoked(string methodName)
        {
            if (provider != null && provider.IsEnabled(operationInvokedEvent.Level, operationInvokedEvent.Keywords))
            {
                provider.WriteEvent(ref operationInvokedEvent, new object[] { methodName, GetCallerInfo(), GetHostReference(), appDomain });
            }
        }

        public static void OperationCompleted(string methodName, long duration)
        {
            if (provider != null && provider.IsEnabled(operationCompletedEvent.Level, operationCompletedEvent.Keywords))
            {
                provider.WriteEvent(ref operationCompletedEvent, new object[] { methodName, duration, GetHostReference(), appDomain });
            }
        }

        public static void OperationFailed(string methodName, long duration)
        {
            if (provider != null && provider.IsEnabled(operationFailedEvent.Level, operationFailedEvent.Keywords))
            {
                provider.WriteEvent(ref operationFailedEvent, new object[] { methodName, duration, GetHostReference(), appDomain });
            }
        }

        public static void OperationFaulted(string methodName, long duration)
        {
            if (provider != null && provider.IsEnabled(operationFaultedEvent.Level, operationFaultedEvent.Keywords))
            {
                provider.WriteEvent(ref operationFaultedEvent, new object[] { methodName, duration, GetHostReference(), appDomain });
            }
        }

        public static void ServiceException(Exception ex)
        {
            if (provider != null && provider.IsEnabled(serviceExceptionEvent.Level, serviceExceptionEvent.Keywords))
            {
                provider.WriteEvent(ref serviceExceptionEvent, new object[] { ex.ToString(), ex.GetType().ToString(), GetHostReference(), appDomain });
            }
        }

        public static long GetTicks()
        {
            return DateTime.UtcNow.Ticks;
        }

        public static long GetDuration(long startTicks)
        {
            return (long)new TimeSpan(GetTicks() - startTicks).TotalMilliseconds;
        }

        private static string GetCallerInfo()
        {
            OperationContext context = OperationContext.Current;

            object obj;
            if (((context != null) && (context.IncomingMessageProperties != null)) && context.IncomingMessageProperties.TryGetValue(RemoteEndpointMessageProperty.Name, out obj))
            {
                RemoteEndpointMessageProperty property = obj as RemoteEndpointMessageProperty;
                if (property != null)
                {
                    return string.Format(CultureInfo.InvariantCulture, "{0}:{1}", new object[] { property.Address, property.Port });
                }
            }
            return "null";
        }

        private static string GetHostReference()
        {
            if (OperationContext.Current != null)
            {
                ServiceHostBase host = OperationContext.Current.Host;
                if (host != null && host.Extensions != null)
                {
                    VirtualPathExtension extension = host.Extensions.Find<VirtualPathExtension>();
                    if (extension != null && extension.VirtualPath != null)
                    {
                        //     HostReference Format
                        //     <SiteName><ApplicationVirtualPath>|<ServiceVirtualPath>|<ServiceName> 
                        string serviceName = (host.Description != null) ? host.Description.Name : string.Empty;
                        string application = HostingEnvironment.ApplicationVirtualPath;

                        string servicePath = extension.VirtualPath;
                        if (!String.IsNullOrEmpty(servicePath) && servicePath[0] == '~')
                        {
                            servicePath = application + "|" + servicePath.Substring(1);
                        }
                        return string.Format(CultureInfo.InvariantCulture, "{0}{1}|{2}", HostingEnvironment.SiteName, servicePath, serviceName);
                    }
                }
            }

            // If the entire host reference is not available, fall back to site name and app virtual path.  This will happen
            // if you try to emit a trace from outside an operation (e.g. startup) before an in operation trace has been emitted.
            if (String.IsNullOrEmpty(DiagnosticUtility.hostReference))
            {
                DiagnosticUtility.hostReference = string.Format(CultureInfo.InvariantCulture, "{0}{1}", HostingEnvironment.SiteName, HostingEnvironment.ApplicationVirtualPath);
            }

            return DiagnosticUtility.hostReference;
        }
    }
}
