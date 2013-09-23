using OpenRiaServices.DomainServices.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Xml.Linq;
using System.ServiceModel.Syndication;
using System.ServiceModel.Channels;
using System.Text;

namespace OpenRiaServices.DomainServices.Hosting.Local.Test
{
    
    
    /// <summary>
    ///This is a test class for WcfTraceServiceTest and is intended
    ///to contain all WcfTraceServiceTest Unit Tests
    ///</summary>
    [TestClass()]
    public class WcfTraceServiceTest
    {


        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion

        [TestMethod()]
        [DeploymentItem("OpenRiaServices.DomainServices.Hosting.dll")]
        public void CreateTraceSyndicationFeedTest()
        {
            SyndicationFeed actual = WcfTraceService_Accessor.CreateTraceSyndicationFeed();
            Assert.IsNotNull(actual);
            Assert.IsTrue(actual.Title.Text == "WCF RIA Service Traces");
        }

        [TestMethod()]
        [DeploymentItem("OpenRiaServices.DomainServices.Hosting.dll")]
        public void CreateTraceXmlTest()
        {
            Stream actual = WcfTraceService_Accessor.CreateTraceXml();
            Assert.IsNotNull(actual);
            XElement root = XElement.Load(actual);
            Assert.IsTrue(root.Name.LocalName == "Traces");
        }

        [TestMethod()]
        [DeploymentItem("OpenRiaServices.DomainServices.Hosting.dll")]
        public void CreateTraceHtmlTest()
        {
            Stream actual = WcfTraceService_Accessor.CreateTraceHtml();
            Assert.IsNotNull(actual);
            MemoryStream ms = new MemoryStream();
            actual.CopyTo(ms);
            string html = Encoding.UTF8.GetString(ms.ToArray());
            Assert.IsTrue(html.StartsWith("<html>", StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(html.EndsWith("</html>", StringComparison.OrdinalIgnoreCase));
        }

        [TestMethod()]
        [DeploymentItem("OpenRiaServices.DomainServices.Hosting.dll")]
        public void CreateTraceSyndicationItemTest()
        {
            SyndicationItem actual = WcfTraceService_Accessor.CreateTraceSyndicationItem(this.CreateSampleTraceEntry());
            Assert.IsNotNull(actual);
            Assert.IsTrue(actual.Title.Text == "Information: Security Impersonation succeeded at the server.");
            Assert.IsTrue(actual.PublishDate == new DateTimeOffset(634052379151696992, TimeSpan.Zero));
            Assert.IsTrue(actual.LastUpdatedTime == actual.PublishDate);
        }

        XElement CreateSampleTraceEntry()
        {
            return XElement.Parse("<Trace><Timestamp>634052379151696992</Timestamp><Code>458758</Code><TraceRecord xmlns=\"http://schemas.microsoft.com/2004/10/E2ETraceEvent/TraceRecord\" Severity=\"Information\"><TraceIdentifier>http://msdn.microsoft.com/en-US/library/System.ServiceModel.Security.SecurityImpersonationSuccess.aspx</TraceIdentifier><Description>Security Impersonation succeeded at the server.</Description><AppDomain>/LM/W3SVC/1/ROOT/riatracing-1-129141143812693125</AppDomain><ExtendedData xmlns=\"http://schemas.microsoft.com/2006/08/ServiceModel/SecurityImpersonationTraceRecord\"><OperationAction>http://tempuri.org/WcfTraceService/GetTrace</OperationAction><OperationName>GetTrace</OperationName></ExtendedData></TraceRecord></Trace>");
        }

        [TestMethod()]
        [DeploymentItem("OpenRiaServices.DomainServices.Hosting.dll")]
        public void InstanceTest()
        {
            WcfTraceService actual;
            actual = WcfTraceService_Accessor.Instance;
            Assert.IsNotNull(actual);
        }
    }
}
