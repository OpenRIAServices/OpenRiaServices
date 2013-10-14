extern alias SSmDsClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestDomainServices;

using DescriptionAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;

namespace OpenRiaServices.DomainServices.Client.Test
{
    using Resource = SSmDsClient::OpenRiaServices.DomainServices.Client.Resource;

    /// <summary>
    /// Tests for compositional hierarchy features.
    /// </summary>
    [TestClass]
    public class AssociationInheritanceTests : UnitTestBase
    {
        private static Uri AssociationInheritanceScenarios_Uri = new Uri(TestURIs.RootURI, "TestDomainServices-AssociationInheritanceScenarios.svc");

        [TestMethod]
        [Asynchronous]
        [Description("Loads a hierarchy containing associations to derived types")]
        public void Association_Inheritance_HierarchyQuery()
        {
            AssociationInheritanceScenarios ctxt = new AssociationInheritanceScenarios(AssociationInheritanceScenarios_Uri);

            LoadOperation lo = ctxt.Load(ctxt.GetMastersQuery(), false);

            EnqueueConditional(() => lo.IsComplete);
            EnqueueCallback(delegate
            {
                TestHelperMethods.AssertOperationSuccess(lo);

                // Master, 2 D1's, 2 D2's, 1 D3 and 1 D4
                Assert.AreEqual(7, lo.AllEntities.Count(), "Unexpected number of entities in hierarchy");

                AI_MasterDerived master = ctxt.AI_Masters.OfType<AI_MasterDerived>().FirstOrDefault();
                Assert.IsNotNull(master, "expected 1 master");

                // Confirm the single-value relations
                AI_DetailDerived3 d3 = master.DetailDerived3;
                Assert.IsNotNull(d3, "no master.D3");
                Assert.AreSame(master, d3.Master, "wrong master.D3");

                AI_DetailDerived4 d4 = master.DetailDerived4;
                Assert.IsNotNull(d4, "no master.D4");
                Assert.AreSame(master, d4.Master, "wrong master.D4");

                // Confirm the multi-value relations
                Assert.AreEqual(2, master.DetailDerived1s.Count, "wrong number of D1's");
                Assert.AreEqual(2, master.DetailDerived2s.Count, "wrong number of D2's");

                foreach (AI_DetailDerived1 d1 in master.DetailDerived1s)
                {
                    Assert.AreSame(master, d1.Master, "D1.Master not root master");
                }

                foreach (AI_DetailDerived2 d2 in master.DetailDerived2s)
                {
                    Assert.AreSame(master, d2.Master, "D1.Master not root master");
                }
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Modifies a singleton association to a derived type")]
        public void Association_Inheritance_Modify_Singleton_Derived_Association()
        {
            AssociationInheritanceScenarios ctxt = new AssociationInheritanceScenarios(AssociationInheritanceScenarios_Uri);

            LoadOperation lo = ctxt.Load(ctxt.GetMastersQuery(), false);
            SubmitOperation so = null;

            EnqueueConditional(() => lo.IsComplete);
            EnqueueCallback(delegate
            {
                TestHelperMethods.AssertOperationSuccess(lo);

                AI_MasterDerived master = ctxt.AI_Masters.OfType<AI_MasterDerived>().FirstOrDefault();
                Assert.IsNotNull(master, "expected one master");

                AI_DetailDerived3 d3 = master.DetailDerived3;
                Assert.IsNotNull(d3, "no master.D3");
                Assert.AreSame(master, d3.Master, "wrong master.D3");

                // Verify we can unset it
                master.DetailDerived3 = null;
                Assert.IsNull(master.DetailDerived3, "could not reset master.D4");

                AI_DetailDerived3 newD3 = new AI_DetailDerived3();
                newD3.ID = 99;
                master.DetailDerived3 = newD3;

                AI_DetailDerived4 newD4 = new AI_DetailDerived4();
                newD4.ID = 999;
                master.DetailDerived4 = newD4;

                so = ctxt.SubmitChanges();
            });

            EnqueueConditional(() => so.IsComplete);
            EnqueueCallback(delegate
            {
                TestHelperMethods.AssertOperationSuccess(so);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Modifies a multiple value association to a derived type")]
        public void Association_Inheritance_Modify_MultipleValue_Derived_Association()
        {
            AssociationInheritanceScenarios ctxt = new AssociationInheritanceScenarios(AssociationInheritanceScenarios_Uri);

            LoadOperation lo = ctxt.Load(ctxt.GetMastersQuery(), false);
            SubmitOperation so = null;

            EnqueueConditional(() => lo.IsComplete);
            EnqueueCallback(delegate
            {
                TestHelperMethods.AssertOperationSuccess(lo);

                AI_MasterDerived master = ctxt.AI_Masters.OfType<AI_MasterDerived>().FirstOrDefault();
                Assert.IsNotNull(master, "expected 1 master");

                // Verify we can remove one derived from the list
                AI_DetailDerived1 d1 = master.DetailDerived1s.First();
                master.DetailDerived1s.Remove(d1);
                Assert.AreEqual(1, master.DetailDerived1s.Count, "master.D1s.Remove didn't work");

                AI_DetailDerived1 newD1 = new AI_DetailDerived1();
                newD1.ID = 9999;

                master.DetailDerived1s.Add(newD1);
                Assert.AreEqual(2, master.DetailDerived1s.Count, "master.D1s.Add didn't work");

                AI_DetailDerived2 newD2 = new AI_DetailDerived2();
                newD2.ID = 99999;

                master.DetailDerived2s.Add(newD2);
                Assert.AreEqual(3, master.DetailDerived2s.Count, "master.D2s.Add didn't work");

                so = ctxt.SubmitChanges();
            });

            EnqueueConditional(() => so.IsComplete);
            EnqueueCallback(delegate
            {
                TestHelperMethods.AssertOperationSuccess(so);
            });

            EnqueueTestComplete();
        }
    }
}
