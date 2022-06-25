using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity.Core.Objects;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Security.Principal;
using OpenRiaServices.Client.Test;
#if NET472
using OpenRiaServices.EntityFramework;
#else
using OpenRiaServices.Server.EntityFrameworkCore;
#endif
using OpenRiaServices.Hosting;
using OpenRiaServices.Hosting.Wcf;
using OpenRiaServices.Hosting.Wcf.Behaviors;
using System.Text;
using System.Threading;
using System.Web;
using Cities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpenRiaServices.Server.Test
{
    public partial class DirectServiceTests
    {
        // TODO: Remove the [Ignore] on the following two tests once we've updated our test runner such that it 
        //       starts a webserver before running these tests. Or consider moving these tests, or consider 
        //       writing true direct tests that don't require a webserver.

        /// <summary>
        /// Verify that when a member is marked Exclude, it doesn't show up in the serialized response
        /// </summary>
        [TestMethod]
        [Microsoft.VisualStudio.TestTools.UnitTesting.Ignore]
        public void TestDomainOperationEntry_VerifyDataMemberExclusion()
        {
            string soap = RequestDirect(typeof(TestDomainServices.LTS.Catalog), "GetProducts", null);

            // verify that the server entity type has the SafetyStockLevel property and that
            // it is marked [Exclude]
            DomainServiceDescription.GetDescription(typeof(TestDomainServices.LTS.Catalog));
            PropertyDescriptor pd = TypeDescriptor.GetProperties(typeof(DataTests.AdventureWorks.LTS.Product)).Cast<PropertyDescriptor>().Single(p => p.Name == "SafetyStockLevel");
            Assert.IsTrue(pd.Attributes.OfType<ExcludeAttribute>().Count() == 1);

            // verify that the serialized response doesn't contain excluded data
            Assert.IsTrue(soap.Contains("ProductID"));  // make sure we got at least one product
            Assert.IsFalse(soap.Contains("SafetyStockLevel"));
        }

        [TestMethod]
        [Microsoft.VisualStudio.TestTools.UnitTesting.Ignore]
        public void TestDataService_LTS_Query_MultipleThreads()
        {
            const int numberOfThreads = 10;
            Semaphore s = new Semaphore(0, numberOfThreads);
            Exception lastError = null;

            string soap = RequestDirect(typeof(TestDomainServices.LTS.Catalog), "GetProducts", null);
            Assert.IsTrue(soap.Contains("ProductID"));  // make sure we got at least one product

            for (int i = 0; i < numberOfThreads; i++)
            {
                ThreadPool.QueueUserWorkItem(delegate
                {
                    try
                    {
                        string soap2 = RequestDirect(typeof(TestDomainServices.LTS.Catalog), "GetProducts", null);
                        Assert.IsTrue(soap2.Contains("ProductID"));  // make sure we got at least one product
                    }
                    catch (Exception ex)
                    {
                        lastError = ex;
                    }
                    finally
                    {
                        s.Release();
                    }
                });
            }

            for (int i = 0; i < numberOfThreads; i++)
            {
                s.WaitOne(TimeSpan.FromSeconds(5));
            }

            if (lastError != null)
            {
                Assert.Fail(lastError.ToString());
            }
        }
    }
}
