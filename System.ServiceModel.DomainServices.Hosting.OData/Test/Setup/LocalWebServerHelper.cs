namespace OpenRiaServices.DomainServices.Hosting.OData.UnitTests
{
    #region Namespaces
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Data.Test.Astoria;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.VisualStudio.WebHost;
    #endregion

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
        private static Process process;

        /// <summary>Path to which files will be written to.</summary>
        private static string targetPhysicalPath;

        /// <summary>Performs cleanup and ensures that there are no active web servers.</summary>
        public static void Cleanup()
        {
            if (process != null)
            {
                // The local web server does not respond to CloseMainWindow.
                Trace.WriteLine("Closing web server process...");
                if (!process.HasExited)
                {
                    try
                    {
                        process.Kill();
                        process.WaitForExit(60 * 1000); // wait 60 secs.
                    }
                    catch (InvalidOperationException)
                    {
                        Trace.WriteLine("Unable to kill local web server process.");
                    }
                }

                process.Dispose();
                process = null;

                localPortNumber = -1;
            }
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
                       "       <add name=\"" + TestUtil.ODataEndPointName + "\" type=\"OpenRiaServices.DomainServices.Hosting.ODataEndpointFactory, OpenRiaServices.DomainServices.Hosting.OData, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\" />\r\n" +
                       "     </endpoints>\r\n" +
                       "   </domainServices>\r\n" +
                       "</system.serviceModel>\r\n";

            }
        }

        /// <summary>httpruntime config section (goes to: /configuration/system.web/httpRuntime)</summary>
        public static string WebConfigHttpRuntimeFragment
        {
            get;
            set;
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
                "<%@ ServiceHost Language=\"C#\" Debug=\"true\" Factory=\"OpenRiaServices.DomainServices.Hosting.DomainServiceHostFactory, OpenRiaServices.DomainServices.Hosting, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\" Service=\"" + serviceType.FullName.Replace('+', '.') + "\" %>\r\n";

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
                "      <section name=\"domainServices\" type=\"OpenRiaServices.DomainServices.Hosting.DomainServicesSection, OpenRiaServices.DomainServices.Hosting, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\" allowDefinition=\"MachineToApplication\" requirePermission=\"false\" />\r\n" +
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
                (WebConfigTrustLevelFragment ?? string.Empty) +
                WebConfigCompilationFragment +
                (WebConfigHttpRuntimeFragment ?? string.Empty) +
                (AuthenticationModeFragment ?? string.Empty) +
                " </system.web>\r\n" +
                WebConfigCodeDomFragment +
                WebConfigServiceModelFragment +
                "</configuration>\r\n";

            // Clear httpRuntime section to prevent subsequently created web apps from injecting httpRuntime settings unknowingly
            WebConfigHttpRuntimeFragment = null;

            File.WriteAllText(Path.Combine(physicalPath, "web.config"), configContents);

            string physicalBinPath = Path.Combine(physicalPath, "bin");
            System.Data.Test.Astoria.IOUtil.EnsureEmptyDirectoryExists(physicalBinPath);

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
            if (process == null)
            {
                string serverPath = FindWebServerPath();
                string portNumberText = LocalPortNumber.ToString(CultureInfo.InvariantCulture);
                string arguments = "/port:" + portNumberText +
                    " /path:\"" + LocalWebServerHelper.TargetPhysicalPath + "\"";

                Trace.WriteLine("Starting web server:  \"" + serverPath + "\" " + arguments);

                process = Process.Start(serverPath, arguments);
                process.WaitForInputIdle();
            }
        }

        /// <summary>
        /// Helper method to find where the local web server binary is available from.
        /// </summary>
        /// <returns>The path to a local WebDev.WebServer.exe file.</returns>
        private static string FindWebServerPath()
        {
            string path = System.Environment.ExpandEnvironmentVariables(@"%programfiles%\Common Files\microsoft shared\DevServer\10.0\WebDev.WebServer40.exe");
            if (!File.Exists(path))
            {
                throw new InvalidOperationException("Unable to find web server at " + path + ".");
            }

            return path;
        }
    }
}
