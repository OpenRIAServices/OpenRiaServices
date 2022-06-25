using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data.Linq;
using System.Globalization;
using System.Linq;
using System.Reflection;
using OpenRiaServices.Client.Test;
#if NET472
using OpenRiaServices.EntityFramework;
#else
using OpenRiaServices.Server.EntityFrameworkCore;
#endif
using OpenRiaServices.Hosting.Wcf;
using System.Xml.Linq;
using Cities;
using OpenRiaServices.LinqToSql;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestDomainServices;
using System.Threading.Tasks;
using System.Threading;
using OpenRiaServices.Server.UnitTesting;

namespace OpenRiaServices.Server.Test
{
    public partial class DomainServiceTests
    {

        /// <summary>
        /// Verify that both DAL providers support accessing their respective
        /// contexts in the constructor.
        /// </summary>
        [TestMethod]
        [WorkItem(827125)]
        public void DomainServiceConstructor_ContextAccess()
        {
            LTSService_ConstructorInit lts = new LTSService_ConstructorInit();
            Assert.IsNotNull(lts.DataContext.LoadOptions);

            EFService_ConstructorInit ef = new EFService_ConstructorInit();
        }
    }
}
