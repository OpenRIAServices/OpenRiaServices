﻿extern alias httpDomainClient; 

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using httpDomainClient::OpenRiaServices.Client.DomainClients;
using OpenRiaServices.Common.Test;

namespace OpenRiaServices.Client.Test
{
    [TestClass()]
    public sealed class Main
    {
        private static IISExpressWebserver s_webServer = null;

        [AssemblyInitialize()]
        public static void AssemblyInit(TestContext context)
        {
            Thread.CurrentThread.CurrentUICulture
                = Thread.CurrentThread.CurrentCulture
                    = new System.Globalization.CultureInfo("en-US");

            StartWebServer();

#if NETFRAMEWORK
#pragma warning disable CS0618 // Type or member is obsolete
            DomainContext.DomainClientFactory = new Web.WebDomainClientFactory()
            {
                ServerBaseUri = TestURIs.RootURI,
            };
#pragma warning restore CS0618 // Type or member is obsolete

            //// Uncomment below to run tests using BinaryHttpDomainClientFactory instead:
            ///
#else
            DomainContext.DomainClientFactory = new BinaryHttpDomainClientFactory(TestURIs.RootURI, new HttpClientHandler()
            {
                CookieContainer = new CookieContainer(),
                UseCookies = true,
                AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip,
            });
#endif

            HttpWebRequest.DefaultCachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.Default);
        }

        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {
#if !VBTests
            // make sure our test database is removed on the server after all unit tests
            // have been run
            ((IDisposable)UpdateTests.TestDatabase).Dispose();
#endif
            s_webServer?.Stop();
        }

        private static void StartWebServer()
        {
            string projectPath = File.ReadAllLines("ClientTestProjectPath.txt")[0];
#if VBTests
            string webSitePath = Path.GetFullPath(Path.Combine(projectPath, @"..\..\..\Test\WebsiteFullTrust"));
#else
            string webSitePath = Path.GetFullPath(Path.Combine(projectPath, @"..\WebsiteFullTrust"));
#endif

            if (!Directory.Exists(webSitePath))
                throw new FileNotFoundException($"Website not found at {webSitePath}");

            s_webServer = new IISExpressWebserver();
#if VBTests
            s_webServer.Start(webSitePath, 60000);
#else
            s_webServer.Start(webSitePath, 60002);
#endif
        }
    }
}
