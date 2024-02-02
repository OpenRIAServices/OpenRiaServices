extern alias SSmDsClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using Cities;
using DataTests.AdventureWorks.LTS;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.Silverlight.Testing;

namespace OpenRiaServices.Client.Test
{
    using Resource = SSmDsClient::OpenRiaServices.Client.Resource;
    using TestDescription = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;

    [TestClass]
    public class EntityTestsE2E : UnitTestBase
    {
        [TestMethod]
        [Asynchronous]
        [TestDescription("Verifies that entity child and parent relationships are restored after RejectChanges is called.")]
        [WorkItem(720495)]
        public void Entity_RejectChanges_ParentAssociationRestored()
        {
            List<Employee> employeeList = new List<Employee>();
            ConfigurableEntityContainer container = new ConfigurableEntityContainer();
            container.CreateSet<Employee>(EntitySetOperations.All);
            ConfigurableDomainContext catalog = new ConfigurableDomainContext(new WebDomainClient<TestDomainServices.LTS.Catalog.ICatalogContract>(TestURIs.EFCore_Catalog), container);

            var load = catalog.Load(catalog.GetEntityQuery<Employee>("GetEmployees"), throwOnError:false);
            this.EnqueueCompletion(() => load);
            this.EnqueueCallback(() =>
            {
                Assert.AreEqual(null, load.Error);

                Employee parent, child;
                parent = container.GetEntitySet<Employee>().OrderByDescending(e => e.Reports.Count).First();

                while (parent != null)
                {
                    // Track parent, get a report from it
                    employeeList.Add(parent);
                    child = parent.Reports.OrderByDescending(e => e.Reports.Count).FirstOrDefault();

                    // Track child
                    if (child == null)
                    {
                        break;
                    }

                    // Remove child and continue
                    parent.Reports.Remove(child);
                    parent = child;
                }

                // By rejecting changes, our parent<=>child relationships should be restored.
                catalog.RejectChanges();

                // Unwind, walking up management chain
                foreach (Employee employee in employeeList.Reverse<Employee>())
                {
                    Assert.AreSame(parent, employee, "Expected parent relationship to be restored.");
                    parent = employee.Manager;
                    Assert.IsTrue(parent.Reports.Contains(employee), "Expected child relationship to be restored.");
                }
            });
            this.EnqueueTestComplete();
        }

        private class TestCityContainer : EntityContainer
        {
            public TestCityContainer()
            {
                CreateEntitySet<City>(EntitySetOperations.Add | EntitySetOperations.Edit | EntitySetOperations.Remove);
                CreateEntitySet<County>(EntitySetOperations.Add | EntitySetOperations.Edit | EntitySetOperations.Remove);
            }
        }
    }

    public class ConfigurableDomainContext : DomainContext
    {
        private readonly EntityContainer _entityContainer;

        public ConfigurableDomainContext(DomainClient client, EntityContainer entityContainer)
            : base(client)
        {
            this._entityContainer = entityContainer;
        }

        public EntityQuery<TEntity> GetEntityQuery<TEntity>(string queryName) where TEntity : Entity
        {
            return this.CreateQuery<TEntity>(queryName, null, false, false);
        }

        protected override EntityContainer CreateEntityContainer()
        {
            return this._entityContainer;
        }
    }

    public class ConfigurableEntityContainer : EntityContainer
    {
        public void CreateSet<TEntity>(EntitySetOperations operations) where TEntity : Entity, new()
        {
            base.CreateEntitySet<TEntity>(operations);
        }
    }

}
