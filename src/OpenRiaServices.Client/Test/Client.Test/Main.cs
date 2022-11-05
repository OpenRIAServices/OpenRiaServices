extern alias httpDomainClient; 

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
        [AssemblyInitialize()]
        public static void AssemblyInit(TestContext context)
        {
            Thread.CurrentThread.CurrentUICulture
                = Thread.CurrentThread.CurrentCulture
                    = new System.Globalization.CultureInfo("en-US");


            DomainContext.DomainClientFactory = new BinaryHttpDomainClientFactory(new Uri("https://localhost:21312/DOES_NOT_EXISTS"), new HttpClientHandler() {});

        }
    }
}
