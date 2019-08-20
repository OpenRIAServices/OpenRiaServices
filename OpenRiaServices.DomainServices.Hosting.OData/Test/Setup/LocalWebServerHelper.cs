using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Test.Astoria;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.Common.Test;

namespace OpenRiaServices.DomainServices.Hosting.OData.Test
{
    /// <summary>
    /// Provides a helper class for tests that rely on a local web
    /// server.
    /// </summary>
    [TestClass]
    public class LocalWebServerHelper
    {
        /// <summary>Port number for the local web server.</summary>
        private static int localPortNumber = -1;

        /// <summary>Local web server process.</summary>
        private static IISExpressWebserver s_webserver;

        /// <summary>Path to which files will be written to.</summary>
        private static string targetPhysicalPath;

        /// <summary>Performs cleanup and ensures that there are no active web servers.</summary>
        public static void Cleanup()
        {
            if (s_webserver != null)
            {
                // The local web server does not respond to CloseMainWindow.
                Trace.WriteLine("Closing web server process...");

                try
                {
                    s_webserver.Stop();
                }
                catch (InvalidOperationException)
                {
                    Trace.WriteLine("Unable to kill local web server process.");
                }
            }

            s_webserver = null;
            localPortNumber = -1;
        }

        /// <summary>
        /// Performs cleanup after all tests in the current assembly have
        /// been executed.
        /// </summary>
        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {
            Cleanup();
        }

        /// <summary>Port number for the local web server.</summary>
        public static int LocalPortNumber
        {
            get
            {
                if (localPortNumber == -1)
                {
                    //localPortNumber = NetworkUtil.GetUnusedLocalServerPort();
                    localPortNumber = 60005;
                }

                return localPortNumber;
            }

            set
            {
                localPortNumber = value;
            }
        }

        /// <summary>
        /// Sets up the required files locally to test the web data service
        /// through the local web server, including a string with arguments.
        /// </summary>
        public static string SetupServiceFiles(string serviceFileName, Type serviceType)
        {
            Dictionary<string, string> connections = new Dictionary<string, string>();

            connections.Add("NorthwindEntities", ConfigurationManager.ConnectionStrings["NorthwindEntities"].ConnectionString);

            return SetupServiceFiles(serviceFileName, serviceType, connections);
        }

        /// <summary>Path to which files will be written to.</summary>
        public static string TargetPhysicalPath
        {
            get
            {
                if (targetPhysicalPath == null)
                {
                    targetPhysicalPath = System.Environment.CurrentDirectory;
                }

                return targetPhysicalPath;
            }
            set
            {
                targetPhysicalPath = value;
            }
        }

        /// <summary>Text fragment to set up the system.codedom section in a standard web.config file.</summary>
        public static string WebConfigCodeDomFragment
        {
            get
            {
                return " <system.codedom>\r\n" +
                    "  <compilers>\r\n" +
                    "   <compiler language='c#;cs;csharp'\r\n" +
                    "    extension='.cs'\r\n" +
                    "    type='Microsoft.CSharp.CSharpCodeProvider,System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'\r\n" +
                    "    >\r\n" +
                    "    <providerOption name='CompilerVersion' value='v4.0' />\r\n" +
                    "   </compiler>\r\n" +
                    "  </compilers>\r\n" +
                    " </system.codedom>\r\n";
            }
        }

        /// <summary>Servicemodel config section (goes to: /configuration/system.serviceModel)</summary>
        public static string WebConfigServiceModelFragment
        {
            get
            {
                return " <system.serviceModel>\r\n" +
                       "   <serviceHostingEnvironment aspNetCompatibilityEnabled=\"true\"/>" +
                       "   <domainServices>\r\n" +
                       "     <endpoints>\r\n" +
                       "       <add name=\"" + TestUtil.ODataEndPointName + "\" type=\"" + typeof(ODataEndpointFactory).AssemblyQualifiedName + "\" />\r\n" +
                       "     </endpoints>\r\n" +
                       "   </domainServices>\r\n" +
                       "</system.serviceModel>\r\n";

            }
        }

        /// <summary>httpruntime config section (goes to: /configuration/system.web/httpRuntime)</summary>
        public static string WebConfigHttpRuntimeFragment
        {
            get
            {
                //return string.Empty;
                return "<httpRuntime targetFramework=\"4.5\" />";
            }
        }

        /// <summary>Text fragment to set up the compilation section in a standard web.config file.</summary>
        public static string WebConfigCompilationFragment
        {
            get
            {
                return
                    "  <compilation debug='true'>\r\n" +
                    "   <assemblies>\r\n" +
                    "    <add assembly='System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=B77A5C561934E089'/>\r\n" +
                    "    <add assembly='System.Data.DataSetExtensions, Version=4.0.0.0, Culture=neutral, PublicKeyToken=B77A5C561934E089'/>\r\n" +
                    "    <add assembly='System.Web.Extensions, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31BF3856AD364E35'/>\r\n" +
                    "    <add assembly='System.Xml.Linq, Version=4.0.0.0, Culture=neutral, PublicKeyToken=B77A5C561934E089'/>\r\n" +
                    "    <add assembly='System.Data.Entity, Version=4.0.0.0, Culture=neutral, PublicKeyToken=B77A5C561934E089'/>\r\n" +
                    "    <add assembly='System.ServiceModel, Version=4.0.0.0, Culture=neutral, PublicKeyToken=B77A5C561934E089'/>\r\n" +
                    "    <add assembly='System.ServiceModel.Web, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31BF3856AD364E35'/>\r\n" +
                    "   </assemblies>\r\n" +
                    "  </compilation>\r\n";
            }
        }

        /// <summary>
        /// authentication mode fragment, ex:
        ///     &lt;authentication mode="Forms"&gt;&lt;/authentication&gt;
        /// If set, this will be added to the system.web element in web.config.
        /// </summary>
        public static string AuthenticationModeFragment
        {
            get;
            set;
        }

        /// <summary>
        /// trust level fragment, ex:
        ///   &lt;trust level=\"Medium\"/&gt;
        /// </summary>
        public static string WebConfigTrustLevelFragment
        {
            get;
            set;
        }

        /// <summary>
        /// Sets up the required files locally to test the web data service
        /// through the local web server.
        /// </summary>
        public static string SetupServiceFiles(string serviceFileName, Type serviceType, Dictionary<string, string> entityConnectionStrings)
        {
            TestUtil.CheckArgumentNotNull(serviceFileName, "serviceFileName");

            string result = "http://localhost:" + LocalPortNumber.ToString(CultureInfo.InvariantCulture) + "/" + serviceFileName;
            SetupRemoteServiceFiles(serviceFileName, serviceType, entityConnectionStrings, LocalWebServerHelper.TargetPhysicalPath);
            return result;
        }

        /// <summary>
        /// Sets up the required files to test the web data service through a web server.
        /// </summary>
        public static void SetupRemoteServiceFiles(string serviceFileName, Type serviceType, Dictionary<string, string> entityConnectionStrings, string physicalPath)
        {
            TestUtil.CheckArgumentNotNull(serviceFileName, "serviceFileName");
            TestUtil.CheckArgumentNotNull(serviceType, "serviceType");
            TestUtil.CheckArgumentNotNull(physicalPath, "physicalPath");
            TestUtil.CheckArgumentNotNull(entityConnectionStrings, "entityConnectionStrings");
            IOUtil.EnsureDirectoryExists(physicalPath);

            //
            // To set up a DomainDataService service, the following files are required:
            //   service.svc    - holds the entry point (we'll need to customize this to test extensions)
            //   web.config     - provides configuration information to setup service and reference assemblies
            //
            // Some notes:
            //   Setting Debug to 'true' includes symbols in the @ServiceHost directive.
            //
            string serviceContents =
                $"<%@ ServiceHost Language=\"C#\" Debug=\"true\" Factory=\"{typeof(DomainServiceHostFactory).AssemblyQualifiedName}\" Service=\"{(serviceType.FullName.Replace('+', '.'))}\" %>\r\n";

            //DomainDataServiceTest.TheDataService\" %>\r\n" +
            //"namespace DomainDataServiceTest\r\n" +
            //"{\r\n";
            //serviceContents +="    public class TheDataService : " + serviceType.FullName.Replace('+', '.') + "\r\n{}\r\n}\r\n";

            File.WriteAllText(Path.Combine(physicalPath, serviceFileName), serviceContents);

            string configContents =
                "<?xml version='1.0'?>\r\n" +
                "<configuration>\r\n" +
                "  <configSections>\r\n" +
                "    <sectionGroup name=\"system.serviceModel\">\r\n" +
                "      <section name=\"domainServices\" type=\"" + typeof(DomainServicesSection).AssemblyQualifiedName + "\" allowDefinition=\"MachineToApplication\" requirePermission=\"false\" />\r\n" +
                "    </sectionGroup>\r\n" +
                "  </configSections>\r\n" +
                " <connectionStrings>\r\n";
            foreach (KeyValuePair<string, string> entityConnection in entityConnectionStrings)
            {
                configContents +=
                    "  <add name='" + entityConnection.Key + "' providerName='System.Data.EntityClient' " +
                    "connectionString='" + entityConnection.Value + "'/>\r\n";
            }

            configContents +=
                " </connectionStrings>\r\n" +
                " <system.web>\r\n" +
                " <globalization culture=\"en-US\" uiCulture=\"en-US\" />\r\n" +
                (WebConfigTrustLevelFragment ?? string.Empty) +
                WebConfigCompilationFragment +
                (WebConfigHttpRuntimeFragment ?? string.Empty) +
                (AuthenticationModeFragment ?? string.Empty) +
                " </system.web>\r\n" +
                WebConfigCodeDomFragment +
                WebConfigServiceModelFragment +
                "</configuration>\r\n";

            File.WriteAllText(Path.Combine(physicalPath, "web.config"), configContents);

            string physicalBinPath = Path.Combine(physicalPath, "bin");
            IOUtil.EnsureEmptyDirectoryExists(physicalBinPath);

            // Copy all dlls to the bin folder since the are not gac'ed.
            foreach (string filter in new[] { "*.dll", "*.pdb" })
            {
                foreach (string sourceFile in Directory.EnumerateFiles(physicalPath, filter))
                {
                    string targetFile = Path.Combine(physicalBinPath, Path.GetFileName(sourceFile));
                    File.Copy(sourceFile, targetFile, true /* overwrite */);
                }
            }
        }

        /// <summary>Starts an instance of the local web server.</summary>
        public static void StartWebServer()
        {
            if (s_webserver == null)
            {
                s_webserver = new IISExpressWebserver();
                s_webserver.Start(LocalWebServerHelper.TargetPhysicalPath, LocalWebServerHelper.LocalPortNumber);
            }
        }
    }
}
