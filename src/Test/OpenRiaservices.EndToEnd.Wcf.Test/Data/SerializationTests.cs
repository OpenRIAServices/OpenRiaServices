extern alias SSmDsClient;
using System;
using System.Linq;
using System.Text;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.Silverlight.Testing;

namespace OpenRiaServices.Client.Test
{
    [TestClass]
    public class SerializationTests : UnitTestBase
    {
        [TestMethod]
        [Description("Ensure code gen and surrogates agree on both DataContract and DataMember attributes. If they don't, the client fails to deserialize the object or its properties.")]
        [Asynchronous]
        public void SerializeEntityWithCustomDataContract()
        {
            TestDomainServices.MockCustomerDomainContext context = new TestDomainServices.MockCustomerDomainContext(TestURIs.MockCustomers);
            SubmitOperation submit = null;
            LoadOperation<TestDomainServices.MockReport> load = context.Load(context.GetReportsQuery(), false);

            this.EnqueueCompletion(() => load);

            this.EnqueueCallback(() =>
            {
                TestHelperMethods.AssertOperationSuccess(load);
                // Ensure DataContract(Name, Namespace) on entity matches.
                Assert.AreEqual(3, load.Entities.Count, "GetReportsQuery must return 3 reports");
                int numReportBodies = 0;

                string reportsResult = load.Entities.Aggregate<TestDomainServices.MockReport, string>(string.Empty,
                    (result, mockReport) =>
                    {
                        StringBuilder sb = new StringBuilder();
                        Action<bool, string, object> appendIf = (emit, property, value) =>
                            {
                                if (emit)
                                {
                                    sb.Append(string.Format("{0} is {1}", property, value));
                                }
                            };

                        // Ensure entity CLR properties code gen correlates with surrogate layer DataMember(Name, ...)
                        sb.Append(string.Format("CustomerId is {0}", mockReport.CustomerId));
                        appendIf(mockReport.Customer != null, "Customer", mockReport.Customer);
                        appendIf(mockReport.ReportElementFieldId == 0, "ReportElementFieldId", mockReport.ReportElementFieldId);
                        appendIf(mockReport.ReportTitle == null, "ReportTitle", mockReport.ReportTitle);

                        // Ensure complex type CLR properties code gen correlates with surrogate layer DataMember(Name, ...)
                        var reportBody = mockReport.ReportBody;
                        if (reportBody != null)
                        {
                            numReportBodies++;
                            appendIf(reportBody.TimeEntered.Year != 1970, "TimeEntered", reportBody.TimeEntered);
                            appendIf(reportBody.Report == null, "Report", reportBody.Report);
                        }

                        // Ensure entity projection properties code gen correlates with surrogate layer DataMember(Name, ...)
                        appendIf(mockReport.State == null, "State", mockReport.State);
                        appendIf(mockReport.Start == null, "Start", mockReport.Start);

                        if (!string.IsNullOrEmpty(result))
                        {
                            result += " ";
                        }

                        return result + sb.ToString();
                    });

                Assert.AreEqual("CustomerId is 1 CustomerId is 2 CustomerId is 3", reportsResult);

                // Ensure DataContract(Name, Namespace) on complex types matches.
                Assert.AreEqual(2, numReportBodies, "GetReportsQuery must return 2 report bodies");

                // Update a report (entity properties and complex type properties) to ensure it is serialized to the server correctly
                TestDomainServices.MockReport reportToChange = load.Entities.First();
                reportToChange.ReportTitle = reportToChange.ReportTitle + "!";
                reportToChange.ReportBody.Report = reportToChange.ReportBody.Report + "!";
                reportToChange.ReportBody.TimeEntered = DateTime.Now;
                reportToChange.MockReportCustomMethod();
                submit = context.SubmitChanges();
            });
            
            this.EnqueueCompletion(() => submit);
            
            this.EnqueueCallback(() =>
            {
                // No errors means the server found all the data was correct.
                TestHelperMethods.AssertOperationSuccess(submit);
            });
            
            this.EnqueueTestComplete();
        }
    }
}
