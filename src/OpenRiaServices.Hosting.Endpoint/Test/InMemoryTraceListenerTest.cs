using OpenRiaServices.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Xml.Linq;
using System.Linq;

namespace OpenRiaServices.Hosting.Local.Test
{
    
    
    /// <summary>
    ///This is a test class for InMemoryTraceListenerTest and is intended
    ///to contain all InMemoryTraceListenerTest Unit Tests
    ///</summary>
    [TestClass()]
    public class InMemoryTraceListenerTest
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

        /// <summary>
        ///A test for InMemoryTraceListener Constructor
        ///</summary>
        [TestMethod()]
        public void InMemoryTraceListenerConstructorTest()
        {
            string name = "foo";
            InMemoryTraceListener target = new InMemoryTraceListener(name);
            Assert.IsTrue(target.Name == name);
        }

        /// <summary>
        ///A test for GetEntries
        ///</summary>
        [TestMethod()]
        [DeploymentItem("OpenRiaServices.Hosting.Endpoint.dll")]
        public void GetEntriesTest()
        {
            XElement[] actual;
            InMemoryTraceListener.Clear();
            InMemoryTraceListener target = new InMemoryTraceListener();
            target.Write("System.ServiceModel Information: 1 : ");
            target.WriteLine("<TraceRecord xmlns=\"http://schemas.microsoft.com/2004/10/E2ETraceEvent/TraceRecord\" Severity=\"Information\"><TraceIdentifier>http://msdn.microsoft.com/en-US/library/System.ServiceModel.Security.SecurityImpersonationSuccess.aspx</TraceIdentifier><Description>Security Impersonation succeeded at the server.</Description><AppDomain>/LM/W3SVC/1/ROOT/riatracing-1-129141143812693125</AppDomain><ExtendedData xmlns=\"http://schemas.microsoft.com/2006/08/ServiceModel/SecurityImpersonationTraceRecord\"><OperationAction>http://tempuri.org/WcfTraceService/GetTrace</OperationAction><OperationName>GetTrace</OperationName></ExtendedData></TraceRecord>");
            target.Write("System.ServiceModel Information: 2 : ");
            target.WriteLine("");
            target.WriteLine("<TraceRecord xmlns=\"http://schemas.microsoft.com/2004/10/E2ETraceEvent/TraceRecord\" Severity=\"Information\"><TraceIdentifier>http://msdn.microsoft.com/en-US/library/System.ServiceModel.Security.SecurityImpersonationSuccess.aspx</TraceIdentifier><Description>Security Impersonation succeeded at the server.</Description><AppDomain>/LM/W3SVC/1/ROOT/riatracing-1-129141143812693125</AppDomain><ExtendedData xmlns=\"http://schemas.microsoft.com/2006/08/ServiceModel/SecurityImpersonationTraceRecord\"><OperationAction>http://tempuri.org/WcfTraceService/GetTrace</OperationAction><OperationName>GetTrace</OperationName></ExtendedData></TraceRecord>");
            target.Write("boo");
            target.WriteLine("foobarbaz");
            target.Write("System.ServiceModel Information: 3 : ");
            target.WriteLine("<TraceRecord xmlns=\"http://schemas.microsoft.com/2004/10/E2ETraceEvent/TraceRecord\" Severity=\"Information\"><TraceIdentifier>http://msdn.microsoft.com/en-US/library/System.ServiceModel.Security.SecurityImpersonationSuccess.aspx</TraceIdentifier><Description>Security Impersonation succeeded at the server.</Description><AppDomain>/LM/W3SVC/1/ROOT/riatracing-1-129141143812693125</AppDomain><ExtendedData xmlns=\"http://schemas.microsoft.com/2006/08/ServiceModel/SecurityImpersonationTraceRecord\"><OperationAction>http://tempuri.org/WcfTraceService/GetTrace</OperationAction><OperationName>GetTrace</OperationName></ExtendedData></TraceRecord>");
            actual = InMemoryTraceListener.GetEntries();
            Assert.IsNotNull(actual);
            Assert.IsTrue(actual.Length == 3);
            int lastEntryCode = 4;
            foreach (XElement entry in actual)
            {
                int currentEntryCode = Int32.Parse(entry.Descendants().First(x => x.Name.LocalName == "Code").Value);
                Assert.IsTrue(currentEntryCode < lastEntryCode);
                lastEntryCode = currentEntryCode;
            }
        }


        /// <summary>
        ///A test for MaxEntries
        ///</summary>
        [TestMethod()]
        [DeploymentItem("OpenRiaServices.Hosting.Endpoint.dll")]
        public void MaxEntriesTest()
        {
            InMemoryTraceListener.MaxEntries = 500;
            Assert.IsTrue(InMemoryTraceListener.MaxEntries == 500);
            try
            {
                InMemoryTraceListener.MaxEntries = -1;
                Assert.Fail("InMemoryTraceListener.MaxEntries accepted -1 as a value");
            }
            catch (Exception e)
            {
                Assert.IsInstanceOfType(e, typeof(ArgumentOutOfRangeException));
            }
        }
    }
}
