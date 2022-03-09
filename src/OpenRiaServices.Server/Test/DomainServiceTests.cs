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
using OpenRiaServices.EntityFramework;
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
    using Description = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;

    /// <summary>
    /// Summary description for DomainServiceTests
    /// </summary>
    [TestClass]
    public class DomainServiceTests
    {
        private readonly DomainServiceDescription _domainServiceDescription;
        private readonly DomainService _domainService;
        private readonly CityData _cityData;

        public DomainServiceTests()
        {
            _cityData = new CityData();
            _domainService = new CityDomainService();
            _domainServiceDescription = DomainServiceDescription.GetDescription(_domainService.GetType());
        }

        [TestInitialize]
        public void TestInitialize()
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            System.Threading.Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
        }

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

        [TestMethod]
        [WorkItem(801321)]
        public void TestChangeSetValidation()
        {
            ErrorTestDomainService ds = new ErrorTestDomainService();
            DomainServiceContext ctxt = new DomainServiceContext(new MockDataService(), new MockUser("mathew"), DomainOperationType.Submit);
            ds.Initialize(ctxt);

            // null entity
            ChangeSetEntry entry = new ChangeSetEntry();
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                ChangeSetProcessor.ProcessAsync(ds, new ChangeSetEntry[] { entry }).GetAwaiter().GetResult();
            },
            string.Format(CultureInfo.CurrentCulture, Resource.InvalidChangeSet, Resource.InvalidChangeSet_NullEntity));

            // duplicate Ids
            List<ChangeSetEntry> entries = new List<ChangeSetEntry>();
            entries.Add(new ChangeSetEntry { Entity = new City(), Id = 1 });
            entries.Add(new ChangeSetEntry { Entity = new City(), Id = 1 });
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                ChangeSetProcessor.ProcessAsync(ds, entries).GetAwaiter().GetResult();
            },
            string.Format(CultureInfo.CurrentCulture, Resource.InvalidChangeSet, Resource.InvalidChangeSet_DuplicateId));

            // invalid member name
            entries.Clear();
            Dictionary<string, int[]> associations = new Dictionary<string, int[]>();
            associations["invalid"] = null;
            entries.Add(new ChangeSetEntry { Entity = new City(), Id = 1, Associations = associations });
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                ChangeSetProcessor.ProcessAsync(ds, entries).GetAwaiter().GetResult();
            }, string.Format(Resource.InvalidChangeSet,
                string.Format(Resource.InvalidChangeSet_InvalidAssociationMember, typeof(City), "invalid")));

            // valid member, but not an association
            entries.Clear();
            associations = new Dictionary<string, int[]>();
            associations["StateName"] = null;
            entries.Add(new ChangeSetEntry { Entity = new City(), Id = 1, Associations = associations });
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                ChangeSetProcessor.ProcessAsync(ds, entries).GetAwaiter().GetResult();
            }, string.Format(Resource.InvalidChangeSet,
                string.Format(Resource.InvalidChangeSet_InvalidAssociationMember, typeof(City), "StateName")));

            // specify null associated id collection
            entries.Clear();
            associations = new Dictionary<string, int[]>();
            associations["ZipCodes"] = null;
            entries.Add(new ChangeSetEntry { Entity = new City(), Id = 1, Associations = associations });
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                ChangeSetProcessor.ProcessAsync(ds, entries).GetAwaiter().GetResult();
            }, string.Format(Resource.InvalidChangeSet,
                string.Format(Resource.InvalidChangeSet_AssociatedIdsCannotBeNull, typeof(City).FullName, "ZipCodes")));

            // ids specified in associated id collections must be valid
            entries.Clear();
            associations = new Dictionary<string, int[]>();
            associations["ZipCodes"] = new int[] { 9 };  // not in changeset
            entries.Add(new ChangeSetEntry { Entity = new City(), Id = 1, Associations = associations });
            entries.Add(new ChangeSetEntry { Entity = new City(), Id = 2 });
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                ChangeSetProcessor.ProcessAsync(ds, entries).GetAwaiter().GetResult();
            }, string.Format(Resource.InvalidChangeSet,
                string.Format(Resource.InvalidChangeSet_AssociatedIdNotInChangeset, 9, typeof(City).FullName, "ZipCodes")));

            // entities must be of the same Type
            entries.Clear();
            entries.Add(new ChangeSetEntry { Entity = new City(), OriginalEntity = new Zip(), Id = 1 });
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                ChangeSetProcessor.ProcessAsync(ds, entries).GetAwaiter().GetResult();
            }, string.Format(Resource.InvalidChangeSet, Resource.InvalidChangeSet_MustBeSameType));

            entries.Clear();
            City entity = new City();
            entries.Add(new ChangeSetEntry { Entity = entity, Id = 1 });
            entries.Add(new ChangeSetEntry { Entity = entity, Id = 2 });
            ExceptionHelper.ExpectInvalidOperationException(delegate
            {
                ChangeSetProcessor.ProcessAsync(ds, entries).GetAwaiter().GetResult();
            },
            string.Format(CultureInfo.CurrentCulture, Resource.InvalidChangeSet, Resource.InvalidChangeSet_DuplicateEntity));
        }

        /// <summary>
        /// Verify that DomainService.Query throws a helpful exception when passing in invalid IQueryables.
        /// </summary>
        [TestMethod]
        public void Query_InvalidIQueryable()
        {
            CityDomainService ds = new CityDomainService();

            DomainServiceContext dsc = new DomainServiceContext(new MockDataService(), new MockUser("mathew"), DomainOperationType.Query);
            ds.Initialize(dsc);

            DomainServiceDescription desc = DomainServiceDescription.GetDescription(typeof(CityDomainService));

            DomainOperationEntry queryOperation = desc.GetQueryMethod("GetCities");
            QueryDescription query = new QueryDescription(queryOperation, Array.Empty<object>(), /* includeTotalCount */ true, Array.Empty<Zip>().AsQueryable().Where(z => z.Code == 98052));

            ExceptionHelper.ExpectArgumentException(() =>  
                ds.QueryAsync<City>(query, CancellationToken.None).GetAwaiter().GetResult()
            , "Expression of type 'System.Linq.EnumerableQuery`1[Cities.City]' cannot be used for parameter of type 'System.Linq.IQueryable`1[Cities.Zip]' of method 'System.Linq.IQueryable`1[Cities.Zip] Where[Zip](System.Linq.IQueryable`1[Cities.Zip], System.Linq.Expressions.Expression`1[System.Func`2[Cities.Zip,System.Boolean]])'");
        }

        /// <summary>
        /// Verify that DomainService.Query executes IQueryables eagerly.
        /// </summary>
        [TestMethod]
        public async Task Query_ExecutesEagerly()
        {
            CachedQueryResultsDomainService ds = new CachedQueryResultsDomainService();

            DomainServiceContext dsc = new DomainServiceContext(new MockDataService(), new MockUser("mathew"), DomainOperationType.Query);
            ds.Initialize(dsc);

            DomainServiceDescription desc = DomainServiceDescription.GetDescription(typeof(CachedQueryResultsDomainService));

            DomainOperationEntry queryOperation = desc.GetQueryMethod("GetCities");
            QueryDescription query = new QueryDescription(queryOperation, Array.Empty<object>());

            IEnumerable results = (await ds.QueryAsync<City>(query, CancellationToken.None)).Result;
            Assert.IsTrue(ds.ExecutedQuery, "Query wasn't executed eagerly.");
        }

        /// <summary>
        /// Verify that DomainService.Query composes Take(resultLimit) on top of query results 
        /// when a result limit is set on the query operation.
        /// </summary>
        [TestMethod]
        [Description("Verify that DomainService.Query composes Take(resultLimit) on top of query results when a result limit is set on the query operation.")]
        [WorkItem(688352)]
        public async Task Query_ResultLimit()
        {
            Func<string, int, Task<QueryResult<Cities.City>>> executeQueryAsync = async (queryName, pageSize) =>
            {
                ResultLimitDomainService ds = new ResultLimitDomainService();
                DomainServiceContext dsc = new DomainServiceContext(new MockDataService(), new MockUser("mathew"), DomainOperationType.Query);
                ds.Initialize(dsc);

                DomainServiceDescription desc = DomainServiceDescription.GetDescription(typeof(ResultLimitDomainService));
                IQueryable<City> filter = null;

                if (pageSize > -1)
                {
                    filter = Array.Empty<City>().AsQueryable().Take(pageSize);
                }

                DomainOperationEntry queryOperation = desc.GetQueryMethod(queryName);
                QueryDescription query = new QueryDescription(queryOperation, Array.Empty<object>(), true, filter);
                var queryResult = await ds.QueryAsync<City>(query, CancellationToken.None);
                return new QueryResult<City>()
                {
                    Results = (IEnumerable<Cities.City>)queryResult.Result,
                    TotalCount = queryResult.TotalCount
                };
            };

            var allResults = await executeQueryAsync("GetCities", -1);
            int totalCities = allResults.Results.Count();

            // Verify that ResultLimit=0 is the same as not having a ResultLimit at all.
            var results = await executeQueryAsync("GetCities0", -1);
            Assert.AreEqual(totalCities, results.Results.Count(), "Expected to get back all cities.");
            Assert.AreEqual(allResults.Results.Count(), results.TotalCount, "Unexpected total count.");

            // Verify that ResultLimit=-1 is the same as not having a ResultLimit at all.
            results = await executeQueryAsync("GetCitiesM1", -1);
            Assert.AreEqual(totalCities, results.Results.Count(), "Expected to get back all cities.");
            Assert.AreEqual(allResults.Results.Count(), results.TotalCount, "Unexpected total count.");

            // Verify that ResultLimit=10 gives us back only the first 10 cities.
            results = await executeQueryAsync("GetCities10", -1);
            Assert.AreEqual(10, results.Results.Count());
            Assert.IsTrue(results.Results.SequenceEqual(allResults.Results.Take(10)), "Expected the first 10 cities.");
            Assert.AreEqual(allResults.Results.Count(), results.TotalCount, "Unexpected total count.");

            // Verify that ResultLimit=10 with a page size of 2 gives us back only the first 2 cities, and it gives us back the proper total count.
            results = await executeQueryAsync("GetCities10", 2);
            Assert.AreEqual(2, results.Results.Count());
            Assert.IsTrue(results.Results.SequenceEqual(allResults.Results.Take(2)), "Expected the first 2 cities.");
            Assert.AreEqual(allResults.Results.Count(), results.TotalCount, "Unexpected total count.");
        }

        /// <summary>
        /// Verify that the OnError handler is called at the appropriate times
        /// for Query operations.
        /// </summary>
        [TestMethod]
        public async Task OnErrorHandling_Query()
        {
            OnErrorDomainService ds = new OnErrorDomainService();

            DomainServiceContext dsc = new DomainServiceContext(new MockDataService(), new MockUser("mathew"), DomainOperationType.Query);
            DomainServiceDescription desc = DomainServiceDescription.GetDescription(typeof(OnErrorDomainService));

            DomainOperationEntry queryOperation = desc.GetQueryMethod("GetCities");
            QueryDescription query = new QueryDescription(queryOperation, new object[] { 1 });

            DomainOperationEntry queryOperationDeferredException = desc.GetQueryMethod("GetCitiesDeferredException");
            QueryDescription queryDeferredException = new QueryDescription(queryOperationDeferredException, Array.Empty<object>());

            // verify that even top level exceptions go through
            // the OnError handler
            Exception expectedException = null;
            try
            {
                // cause a context not initialized exception
                await ds.QueryAsync<City>(query, CancellationToken.None);
            }
            catch (InvalidOperationException e)
            {
                expectedException = e;
            }
            Assert.AreSame(expectedException, ds.LastError.Error);

            // verify that exception thrown from within the query operation
            // is also handled
            expectedException = null;
            ds = new OnErrorDomainService();
            ds.Initialize(dsc);
            try
            {
                await ds.QueryAsync<City>(query, CancellationToken.None);
            }
            catch (TargetInvocationException tie)
            {
                expectedException = tie.InnerException;
            }
            catch (Exception e)
            {
                expectedException = e;
            }
            Assert.AreSame(expectedException, ds.LastError.Error);

            // verify that exception thrown from a query execution 
            // is also handled (query is executed inside the Query method)
            expectedException = null;
            ds = new OnErrorDomainService();
            ds.Initialize(dsc);
            try
            {
                await ds.QueryAsync<City>(queryDeferredException, CancellationToken.None);
            }
            catch (TargetInvocationException tie)
            {
                expectedException = tie.InnerException;
            }
            catch (Exception e)
            {
                expectedException = e;
            }
            Assert.AreSame(expectedException, ds.LastError.Error);

            // verify that exceptions can be replaced
            expectedException = null;
            ds = new OnErrorDomainService();
            ds.ReplaceExceptionMessage = "An exception occurred.";
            ds.Initialize(dsc);
            try
            {
                foreach (var entity in (await ds.QueryAsync<City>(query, CancellationToken.None)).Result)
                {
                }
            }
            catch (TargetInvocationException tie)
            {
                expectedException = tie.InnerException;
            }
            catch (Exception e)
            {
                expectedException = e;
            }
            Assert.AreSame(ds.ReplaceExceptionMessage, expectedException.Message);

            // verify that continuable exceptions like validation errors
            // are not passed to OnError
            expectedException = null;
            ds = new OnErrorDomainService();
            ds.Initialize(dsc);
            query = new QueryDescription(queryOperation, new object[] { 50 });
            var queryResult = await ds.QueryAsync<City>(query, CancellationToken.None);
            
            Assert.IsTrue(queryResult.HasValidationErrors, "Should have validation errors");
            Assert.AreEqual(1, queryResult.ValidationErrors.Count());
            Assert.IsNull(ds.LastError);
        }

        /// <summary>
        /// Verify that the OnError handler is called at the appropriate times
        /// for invoke operations.
        /// </summary>
        [TestMethod]
        public async Task OnErrorHandling_Invoke()
        {
            OnErrorDomainService ds = new OnErrorDomainService();

            DomainServiceContext dsc = new DomainServiceContext(new MockDataService(), new MockUser("mathew"), DomainOperationType.Invoke);
            ds.Initialize(dsc);

            DomainServiceDescription desc = DomainServiceDescription.GetDescription(typeof(OnErrorDomainService));
            DomainOperationEntry operation = desc.GetInvokeOperation("CityOperation");

            // verify that even top level exceptions go through
            // the OnError handler
            Exception expectedException = null;
            try
            {
                // cause a domain service not initialized exception
                await ds.InvokeAsync(new InvokeDescription(operation, new object[] { 1 }), CancellationToken.None);
            }
            catch (TargetInvocationException tie)
            {
                expectedException = tie.InnerException;
            }
            catch (Exception e)
            {
                expectedException = e;
            }
            Assert.AreSame(expectedException, ds.LastError.Error);

            expectedException = null;
            ds = new OnErrorDomainService();
            ds.Initialize(dsc);
            try
            {
                // pass a null parameter
                await ds.InvokeAsync(null, CancellationToken.None);
            }
            catch (TargetInvocationException tie)
            {
                expectedException = tie.InnerException;
            }
            catch (ArgumentNullException e)
            {
                expectedException = e;
            }
            Assert.AreSame(expectedException, ds.LastError.Error);

            // verify that exception thrown from within the operation
            // is also handled
            expectedException = null;
            ds = new OnErrorDomainService();
            ds.Initialize(dsc);
            try
            {
                await ds.InvokeAsync(new InvokeDescription(operation, new object[] { 1 }), CancellationToken.None);
            }
            catch (TargetInvocationException tie)
            {
                expectedException = tie.InnerException;
            }
            catch (Exception e)
            {
                expectedException = e;
            }
            Assert.AreSame(expectedException, ds.LastError.Error);

            // verify that exceptions can be replaced
            expectedException = null;
            ds = new OnErrorDomainService();
            ds.ReplaceExceptionMessage = "An exception occurred.";
            ds.Initialize(dsc);
            try
            {
                await ds.InvokeAsync(new InvokeDescription(operation, new object[] { 1 }), CancellationToken.None);
            }
            catch (TargetInvocationException tie)
            {
                expectedException = tie.InnerException;
            }
            catch (Exception e)
            {
                expectedException = e;
            }
            Assert.AreSame(ds.ReplaceExceptionMessage, expectedException.Message);

            // verify that continuable exceptions like validation errors
            // are not passed to OnError
            expectedException = null;
            ds = new OnErrorDomainService();
            ds.Initialize(dsc);
            var invokeResult = await ds.InvokeAsync(new InvokeDescription(operation, new object[] { 10 }), CancellationToken.None);
            Assert.IsNotNull(invokeResult.ValidationErrors);
            Assert.AreEqual(1, invokeResult.ValidationErrors.Count);
            Assert.IsNull(ds.LastError);
        }

        /// <summary>
        /// Verify that the OnError handler is called at the appropriate times
        /// for submit.
        /// </summary>
        [TestMethod]
        public async Task OnErrorHandling_Submit()
        {
            OnErrorDomainService ds = new OnErrorDomainService();

            // prepare a changeset for submit
            DomainServiceContext dsc = new DomainServiceContext(new MockDataService(), new MockUser("mathew"), DomainOperationType.Submit);
            DomainServiceDescription desc = DomainServiceDescription.GetDescription(typeof(OnErrorDomainService));
            DomainOperationEntry updateOperationEntry = desc.GetSubmitMethod(typeof(Cities.City), DomainOperation.Update);
            Exception expectedException = null;
            ChangeSetEntry updateOperation = new ChangeSetEntry();
            updateOperation.DomainOperationEntry = updateOperationEntry;
            updateOperation.Operation = DomainOperation.Update;
            updateOperation.Entity = new City()
            {
                Name = "Redmond",
                CountyName = "King",
                StateName = "OH"
            };
            updateOperation.OriginalEntity = new City()
            {
                Name = "Redmond",
                CountyName = "King",
                StateName = "WA"
            };

            ChangeSet cs = new ChangeSet(new ChangeSetEntry[] { updateOperation });

            // verify that even top level exceptions go through
            // the OnError handler
            try
            {
                // cause a domain service not initialize exception
                await ds.SubmitAsync(cs, CancellationToken.None);
            }
            catch (InvalidOperationException e)
            {
                expectedException = e;
            }
            Assert.AreSame(expectedException, ds.LastError.Error);

            expectedException = null;
            ds = new OnErrorDomainService();
            ds.Initialize(dsc);
            try
            {
                // pass a null parameter
                await ds.SubmitAsync(null, CancellationToken.None);
            }
            catch (ArgumentNullException e)
            {
                expectedException = e;
            }
            Assert.AreSame(expectedException, ds.LastError.Error);

            // verify that exception thrown from within the operation
            // is also handled
            expectedException = null;
            ds = new OnErrorDomainService();
            ds.Initialize(dsc);
            try
            {
                await ds.SubmitAsync(cs, CancellationToken.None);
            }
            catch (Exception e)
            {
                expectedException = e;
            }
            Assert.AreSame(expectedException, ds.LastError.Error);

            // verify that exceptions can be replaced
            expectedException = null;
            ds = new OnErrorDomainService();
            ds.ReplaceExceptionMessage = "An exception occurred.";
            ds.Initialize(dsc);
            try
            {
                await ds.SubmitAsync(cs, CancellationToken.None);
            }
            catch (Exception e)
            {
                expectedException = e;
            }
            Assert.AreSame(ds.ReplaceExceptionMessage, expectedException.Message);

            // verify that continuable exceptions like validation errors
            // are not passed to OnError
            expectedException = null;
            ds = new OnErrorDomainService();
            ds.Initialize(dsc);
            ((City)updateOperation.Entity).StateName = "ASDF"; // Too long
            await ds.SubmitAsync(cs, CancellationToken.None);
            Assert.IsTrue(cs.HasError);
            Assert.AreEqual(1, cs.ChangeSetEntries.Count(p => p.HasError));
            Assert.IsNull(ds.LastError);
        }

        // Verify that AttachAsModified works correctly when no original values are provided for non-concurrency properties.
        [TestMethod]
        public void ObjectContextExtensions_AttachAsModified()
        {
            TestDomainServices.EF.Northwind nw = new TestDomainServices.EF.Northwind();
            DomainServiceContext ctxt = new DomainServiceContext(new MockDataService(), new MockUser("mathew"), DomainOperationType.Submit);
            nw.Initialize(ctxt);

            var current = new NorthwindModel.Category()
            {
                EntityKey = new System.Data.Entity.Core.EntityKey("NorthwindEntities.Categories", "CategoryID", 1),
                CategoryID = 1,
                CategoryName = "Category",
                Description = "My category"
            };
            var original = new NorthwindModel.Category()
            {
                EntityKey = new System.Data.Entity.Core.EntityKey("NorthwindEntities.Categories", "CategoryID", 1),
                CategoryID = 1,
                CategoryName = "Category"
            };

            ObjectContextExtensions.AttachAsModified(nw.ObjectContext.Categories, current, original);

            var currentEntry = nw.ObjectContext.ObjectStateManager.GetObjectStateEntry(current);

            string[] changedProperties = currentEntry.GetModifiedProperties().ToArray();
            Assert.IsTrue(changedProperties.Contains("Description"));
        }

        [TestMethod]
        public async Task Bug594068_PersistChangesErrorHandling()
        {
            ErrorTestDomainService ds = new ErrorTestDomainService();
            DomainServiceContext ctxt = new DomainServiceContext(new MockDataService(), new MockUser("mathew"), DomainOperationType.Submit);
            ds.Initialize(ctxt);

            ChangeSetEntry updateOp = new ChangeSetEntry();
            updateOp.Entity = new City()
            {
                Name = "Oregon",
                CountyName = "Lucas",
                StateName = "OH",
                ZoneID = 1
            };
            updateOp.OriginalEntity = new City()
            {
                Name = "Oregon",
                CountyName = "Lucas",
                StateName = "OH",
                ZoneID = 2
            };
            updateOp.Operation = DomainOperation.Update;

            // Verify that the validation exception is handled and is associated
            // with the right ChangeSetEntry
            ChangeSet cs = new ChangeSet(new ChangeSetEntry[] { updateOp });
            await ds.SubmitAsync(cs, CancellationToken.None);
            ChangeSetEntry resultOp = cs.ChangeSetEntries.First();
            Assert.IsTrue(resultOp.HasError);
            var error = resultOp.ValidationErrors.Single();
            Assert.AreSame("Invalid City Update!", error.Message);
            Assert.AreEqual("AMember", error.SourceMemberNames.Single());
        }


        [TestMethod]
        public void DomainService_MultipleInitialization()
        {
            CityDomainService cities = new CityDomainService();
            DomainServiceContext operationContext = new DomainServiceContext(new MockDataService(), new MockUser("mathew"), (DomainOperationType)0);
            cities.Initialize(operationContext);

            InvalidOperationException expectedException = null;
            try
            {
                cities.Initialize(operationContext);
            }
            catch (InvalidOperationException e)
            {
                expectedException = e;
            }
            Assert.IsNotNull(expectedException);
        }

        /// <summary>
        /// Verify that in an inheritance hierarchy, all TDPs declared at various levels
        /// are registered.
        /// </summary>
        [TestMethod]
        public void TDPRegistration_TestTDPInheritance()
        {
            DomainServiceDescription description = DomainServiceDescription.GetDescription(typeof(ProviderB));

            // Verify Poco TDP is in effect
            PropertyDescriptorCollection custProps = TypeDescriptor.GetProperties(typeof(TestEntityB));
            AttributeCollection attribs = custProps["ID"].Attributes;
            Assert.IsNotNull(attribs.OfType<KeyAttribute>().SingleOrDefault());

            PropertyDescriptorCollection props = TypeDescriptor.GetProperties(typeof(TestEntityB));
            PropertyDescriptor propA = props["PropA"];
            PropertyDescriptor propB = props["PropB"];

            // Verify TDPA is in effect
            Attribute[] testAttribs = propB.Attributes.OfType<TestAttributeA>().Where(p => p.Tag == "tdpa_int").ToArray();
            Assert.AreEqual(1, testAttribs.Length);

            // Verify TDPB is in effect
            DataTypeAttribute dataTypeAttribute = props["Date"].Attributes.OfType<DataTypeAttribute>().SingleOrDefault();
            Assert.IsNotNull(dataTypeAttribute);
            dataTypeAttribute = props["PropA"].Attributes.OfType<DataTypeAttribute>().SingleOrDefault();
            Assert.IsNull(dataTypeAttribute);
        }

        [TestMethod]
        public void TDPRegistration_TestPocoTDP()
        {
            PocoTestDomainService provider = new PocoTestDomainService();
            DomainServiceDescription description = DomainServiceDescription.GetDescription(typeof(PocoTestDomainService));

            // verify [Key] attributes
            PropertyDescriptorCollection custProps = TypeDescriptor.GetProperties(typeof(PocoCustomer));
            AttributeCollection attribs = custProps["ID"].Attributes;
            Assert.IsNotNull(attribs.OfType<KeyAttribute>().SingleOrDefault());
            attribs = custProps["Name"].Attributes;
            Assert.IsNull(attribs.OfType<KeyAttribute>().SingleOrDefault());

            // verify FK association : Order->Customer
            PropertyDescriptorCollection orderProps = TypeDescriptor.GetProperties(typeof(PocoOrder));
            attribs = orderProps["Customer"].Attributes;
            AssociationAttribute assoc = attribs.OfType<AssociationAttribute>().SingleOrDefault();
            Assert.IsNotNull(assoc);
            Assert.AreEqual("PocoOrder_Customer", assoc.Name);
            Assert.AreEqual("CustomerID", assoc.ThisKey);
            Assert.AreEqual("ID", assoc.OtherKey);
            Assert.AreEqual(true, assoc.IsForeignKey);

            // verify collection association : Customer->Order*
            attribs = custProps["Orders"].Attributes;
            assoc = attribs.OfType<AssociationAttribute>().SingleOrDefault();
            Assert.IsNotNull(assoc);
            Assert.AreEqual("PocoOrder_Customer", assoc.Name);
            Assert.AreEqual("ID", assoc.ThisKey);
            Assert.AreEqual("CustomerID", assoc.OtherKey);
            Assert.AreEqual(false, assoc.IsForeignKey);

            // verify FK association : Employee->Employee (Manager)
            PropertyDescriptorCollection employeeProps = TypeDescriptor.GetProperties(typeof(PocoEmployee));
            attribs = employeeProps["Manager"].Attributes;
            assoc = attribs.OfType<AssociationAttribute>().SingleOrDefault();
            Assert.IsNotNull(assoc);
            Assert.AreEqual("PocoEmployee_Manager", assoc.Name);
            Assert.AreEqual("ManagerID", assoc.ThisKey);
            Assert.AreEqual("ID", assoc.OtherKey);
            Assert.AreEqual(true, assoc.IsForeignKey);

            // verify FK association : Employee->Employee (HiringManager)
            // This tests multiple FK associations of the same type
            attribs = employeeProps["HiringManager"].Attributes;
            assoc = attribs.OfType<AssociationAttribute>().SingleOrDefault();
            Assert.IsNotNull(assoc);
            Assert.AreEqual("PocoEmployee_HiringManager", assoc.Name);
            Assert.AreEqual("HiringManagerID", assoc.ThisKey);
            Assert.AreEqual("ID", assoc.OtherKey);
            Assert.AreEqual(true, assoc.IsForeignKey);

            // verify collection association : Employee->Employee* (Reports)
            attribs = employeeProps["Reports"].Attributes;
            assoc = attribs.OfType<AssociationAttribute>().SingleOrDefault();
            Assert.IsNotNull(assoc);
            Assert.AreEqual("PocoEmployee_Manager", assoc.Name);
            Assert.AreEqual("ID", assoc.ThisKey);
            Assert.AreEqual("ManagerID", assoc.OtherKey);
            Assert.AreEqual(false, assoc.IsForeignKey);
        }

        [TestMethod]
        public void TDPRegistration_TestCustomProviderRegistration()
        {
            ProviderA providerA = new ProviderA();

            // TODO : currently have to access Description to cause TDP registration
            // move that into DomainService base constructor?

            DomainServiceDescription description = DomainServiceDescription.GetDescription(typeof(ProviderA));

            PropertyDescriptorCollection props = TypeDescriptor.GetProperties(typeof(TestEntityA));
            PropertyDescriptor propA = props["PropA"];
            PropertyDescriptor propB = props["PropB"];

            // verify that buddy metadata is there
            TestAttributeA[] testAttribs = propA.Attributes.OfType<TestAttributeA>().Where(p => p.Tag == "mdx").ToArray();
            Assert.AreEqual(1, testAttribs.Length);
            testAttribs = propB.Attributes.OfType<TestAttributeA>().Where(p => p.Tag == "mdx").ToArray();
            Assert.AreEqual(1, testAttribs.Length);

            // verify that custom TDP metadata is there for TDPA
            testAttribs = propA.Attributes.OfType<TestAttributeA>().Where(p => p.Tag == "tdpa_int").ToArray();
            Assert.AreEqual(0, testAttribs.Length);
            testAttribs = propB.Attributes.OfType<TestAttributeA>().Where(p => p.Tag == "tdpa_int").ToArray();
            Assert.AreEqual(1, testAttribs.Length);

            // verify that custom TDP metadata is there for TDPB
            DataTypeAttribute dataTypeAttribute = props["Date"].Attributes.OfType<DataTypeAttribute>().SingleOrDefault();
            Assert.IsNotNull(dataTypeAttribute);
            dataTypeAttribute = props["PropA"].Attributes.OfType<DataTypeAttribute>().SingleOrDefault();
            Assert.IsNull(dataTypeAttribute);
        }

        [TestMethod]
        [Microsoft.VisualStudio.TestTools.UnitTesting.Ignore] // This test get different results depending on test execution order, (but always fail)
        public void TDPRegistration_TestDuplicateRegistration()
        {
            DomainServiceDescription descriptionA = DomainServiceDescription.GetDescription(typeof(ProviderB));

            PropertyDescriptorCollection props = TypeDescriptor.GetProperties(typeof(TestEntityA));
            PropertyDescriptor propB = props["PropB"];
            int count = propB.Attributes.OfType<TestAttributeA>().Count();
            Assert.AreEqual(3, count);
        }

        /// <summary>
        /// Verify that a provider author can override ValidateChangeset to control validation.
        /// If the method returns false, the changeset is not processed.
        /// </summary>
        [TestMethod]
        public async Task ProviderOverrides_ValidateChangesetAsync()
        {
            TestDomainService_OverloadTests dp = null;
            TestEntity e = new TestEntity
            {
                ID = 1,
                PropA = "Example"
            };

            List<ChangeSetEntry> ops = new List<ChangeSetEntry>();
            ChangeSetEntry op = new ChangeSetEntry();
            op.Entity = e;
            op.Operation = DomainOperation.Update;
            op.HasMemberChanges = true;  // here we're indicating an update w/o providing original
            ops.Add(op);

            ChangeSet cs = new ChangeSet(ops);

            // verify success for valid changeset
            dp = new TestDomainService_OverloadTests();
            await dp.SubmitAsync(cs, CancellationToken.None);
            Assert.AreEqual(1, dp.ValidateCount);
            Assert.AreEqual(1, dp.UpdateCount);
            Assert.AreEqual(1, dp.SubmitCount);

            // verify that if validation fails, the changeset isn't processed
            dp = new TestDomainService_OverloadTests();
            dp.IsValid = false;
            await dp.SubmitAsync(cs, CancellationToken.None);
            Assert.AreEqual(1, dp.ValidateCount);
            Assert.AreEqual(0, dp.UpdateCount);
            Assert.AreEqual(0, dp.SubmitCount);
        }

        [TestMethod]
        [Description("Calling ValidateOperations with entity updates that fails validation")]
        public void ValidateOperations_Invalid_Update()
        {
            Zip updatedZip = new Zip() { Code = -1, FourDigit = -1 };

            ChangeSetEntry operation = new ChangeSetEntry();
            operation.DomainOperationEntry = _domainServiceDescription.GetSubmitMethod(typeof(Zip), DomainOperation.Update);
            operation.Operation = DomainOperation.Update;
            operation.Entity = updatedZip;

            bool success = DomainService.ValidateOperations(new ChangeSetEntry[] { operation }, _domainServiceDescription, null);
            Assert.IsFalse(success);
            Assert.AreEqual(5, operation.ValidationErrors.Count(), "There should be 5 property-level validation errors");

            IEnumerable<string> errorMessages = operation.ValidationErrors.Select(e => e.Message);
            UnitTestHelper.AssertListContains(errorMessages, (new RangeAttribute(0, 99999)).FormatErrorMessage("Code"));
            UnitTestHelper.AssertListContains(errorMessages, (new MustStartWithAttribute(9)).FormatErrorMessage("Code"));
            UnitTestHelper.AssertListContains(errorMessages, (new RangeAttribute(0, 9999)).FormatErrorMessage("FourDigit"));
            UnitTestHelper.AssertListContains(errorMessages, (new RequiredAttribute()).FormatErrorMessage("CityName"));
            UnitTestHelper.AssertListContains(errorMessages, (new RequiredAttribute()).FormatErrorMessage("StateName"));
        }

        [TestMethod]
        [Description("Calling ValidateOperations with entity custom updates that fails validation")]
        public void ValidateOperations_Invalid_Custom_Update()
        {
            City validCity = new City() { Name = "Jupiter", CountyName = "Palm Beach", StateName = "FL" };
            City invalidCity = new City();

            DomainServiceDescription domainServiceDescription = DomainServiceDescription.GetDescription(typeof(OnErrorDomainService));

            ChangeSetEntry updateOperation = new ChangeSetEntry();
            updateOperation.DomainOperationEntry = _domainServiceDescription.GetSubmitMethod(typeof(City), DomainOperation.Update);
            updateOperation.Operation = DomainOperation.Update;
            updateOperation.Entity = validCity;

            ChangeSetEntry customUpdateOperation = new ChangeSetEntry();
            customUpdateOperation.Entity = invalidCity;
            customUpdateOperation.EntityActions = new EntityActionCollection { { "CityCustomMethod", Array.Empty<object>() } };

            // pass an update and a custom update, and make sure the custom update is processed.
            bool success = DomainService.ValidateOperations(new ChangeSetEntry[] { updateOperation, customUpdateOperation }, domainServiceDescription, null);
            Assert.IsFalse(success);
            Assert.AreEqual(null, updateOperation.ValidationErrors);
            Assert.AreNotEqual(null, customUpdateOperation.ValidationErrors);
            IEnumerable<string> errorMessages = customUpdateOperation.ValidationErrors.Select(e => e.Message);
            UnitTestHelper.AssertListContains(errorMessages, "The CityName field is required.");
        }

        [TestMethod]
        [Description("Calling ValidateOperations with custom updates whose parameters fail deep validation")]
        public void ValidateOperations_Invalid_Custom_Update_With_Update()
        {
            DomainServiceInsertCustom_Entity entity1 = new DomainServiceInsertCustom_Entity() { Key = 1 };
            DomainServiceInsertCustom_Entity entity2 = new DomainServiceInsertCustom_Entity() { Key = 2 };
            DomainServiceInsertCustom_Entity entity3 = new DomainServiceInsertCustom_Entity() { Key = 3 };
            DomainServiceDescription domainServiceDescription = DomainServiceDescription.GetDescription(typeof(DomainServiceInsertCustom_Service));
            DomainOperationEntry insert = domainServiceDescription.GetSubmitMethod(typeof(DomainServiceInsertCustom_Entity), DomainOperation.Insert);
            DomainOperationEntry update = domainServiceDescription.GetSubmitMethod(typeof(DomainServiceInsertCustom_Entity), DomainOperation.Update);

            // Custom update arrives with an inserted entity.
            // This ensures parameter validation is respected.
            ChangeSetEntry customUpdateOperation1 = new ChangeSetEntry();
            customUpdateOperation1.Entity = entity1;
            customUpdateOperation1.Operation = DomainOperation.Insert;
            customUpdateOperation1.DomainOperationEntry = insert;
            customUpdateOperation1.EntityActions = new EntityActionCollection { { "UpdateEntityWithInt", new object[] { 1 } } };

            // Custom update arrives with an updated entity.
            // This ensures complex type deep validation is respected.
            ChangeSetEntry customUpdateOperation2 = new ChangeSetEntry();
            customUpdateOperation2.Entity = entity2;
            customUpdateOperation2.Operation = DomainOperation.Update;
            customUpdateOperation2.DomainOperationEntry = update;
            customUpdateOperation2.EntityActions = new EntityActionCollection { { "UpdateEntityWithObject", new object[] { new DomainServiceInsertCustom_Validated_Object() } } };

            // Custom update arrives with an entity with no other changes than a custom update.
            // This ensures complex type collection deep validation is respected.
            ChangeSetEntry customUpdateOperation3 = new ChangeSetEntry();
            customUpdateOperation3.Entity = entity2;
            customUpdateOperation3.Operation = DomainOperation.None;
            customUpdateOperation3.DomainOperationEntry = null;

            List<DomainServiceInsertCustom_Validated_Object> param = new List<DomainServiceInsertCustom_Validated_Object>();
            param.Add(new DomainServiceInsertCustom_Validated_Object());
            customUpdateOperation3.EntityActions = new EntityActionCollection { { "UpdateEntityWithCollection", new object[] { param } } };

            bool success = DomainService.ValidateOperations(
                new ChangeSetEntry[] { customUpdateOperation1, customUpdateOperation2, customUpdateOperation3 },
                domainServiceDescription,
                null);
            Assert.IsFalse(success);
            Action<ChangeSetEntry, string> validateResult = (entry, error) =>
            {
                string customUpdateName = entry.EntityActions.First().Key;
                Assert.IsNotNull(entry.ValidationErrors, string.Format("{0} validation is null", customUpdateName));
                Assert.AreEqual(1, entry.ValidationErrors.Count(), string.Format("{0} has more than 1 validation", customUpdateName));
                Assert.AreEqual(error, entry.ValidationErrors.First().Message, string.Format("{0} has unexpected  validation", customUpdateName));
            };

            validateResult(customUpdateOperation1, AlwaysFailValidator.GetError("i"));
            validateResult(customUpdateOperation2, AlwaysFailValidator.GetError("IntProp"));
            validateResult(customUpdateOperation3, AlwaysFailValidator.GetError("IntProp"));
        }

        [TestMethod]
        [Description("Calling ValidateOperations with entity updates that fails validation because of entity-level errors")]
        public void ValidateOperations_Invalid_Update_Entity_Level_Errors()
        {
            Zip updatedZip = new Zip() { Code = 98052, FourDigit = 0000, CityName = "Name", StateName = "Name" };

            ChangeSetEntry operation = new ChangeSetEntry();
            operation.DomainOperationEntry = _domainServiceDescription.GetSubmitMethod(typeof(Zip), DomainOperation.Update);
            operation.Operation = DomainOperation.Update;
            operation.Entity = updatedZip;

            bool success = DomainService.ValidateOperations(new ChangeSetEntry[] { operation }, _domainServiceDescription, null);
            Assert.IsFalse(success);
            Assert.AreEqual(1, operation.ValidationErrors.Count(), "There should be 1 entity-level validation error");

            IEnumerable<string> errorMessages = operation.ValidationErrors.Select(e => e.Message);
            Assert.IsTrue(operation.ValidationErrors.Any(err => err.Message == "Zip codes cannot have matching city and state names" && err.SourceMemberNames.Contains("StateName") && err.SourceMemberNames.Contains("CityName")));
            UnitTestHelper.AssertListContains(errorMessages, "Zip codes cannot have matching city and state names");
        }

        [TestMethod]
        [Description("Calling ValidateOperations with entity updates that fails validation because of collection errors")]
        public void ValidateOperations_Invalid_Update_Collection_Errors()
        {
            State state = new State { Name = "ST", FullName = "State" };
            state.Counties.Add(new County { Name = "Invalid", StateName = "ST" });

            ChangeSetEntry operation = new ChangeSetEntry();
            operation.DomainOperationEntry = _domainServiceDescription.GetSubmitMethod(typeof(State), DomainOperation.Update);
            operation.Operation = DomainOperation.Update;
            operation.Entity = state;

            bool success = DomainService.ValidateOperations(new ChangeSetEntry[] { operation }, _domainServiceDescription, null);
            Assert.IsFalse(success,
                "The operation should fail.");
            Assert.AreEqual(1, operation.ValidationErrors.Count(),
                "There should be 1 validation error");

            ValidationResultInfo validationResult = operation.ValidationErrors.First();
            Assert.AreEqual("The value must not contain invalid counties", validationResult.Message,
                "The validation error should concern invalid counties");
            Assert.AreEqual(1, validationResult.SourceMemberNames.Count(),
                "The validation error should be for a single member");
            Assert.AreEqual("Counties", validationResult.SourceMemberNames.First(),
                "The validation error should be for Counties");
        }

        [TestMethod]
        [Description("Calling ValidateOperations with insert operation that fails validation")]
        public void ValidateOperations_Invalid_Insert()
        {
            Zip newZip = new Zip() { Code = -1, FourDigit = -1 };

            ChangeSetEntry operation = new ChangeSetEntry();
            operation.DomainOperationEntry = _domainServiceDescription.GetSubmitMethod(typeof(Zip), DomainOperation.Insert);
            operation.Operation = DomainOperation.Insert;
            operation.Entity = newZip;

            bool success = DomainService.ValidateOperations(new ChangeSetEntry[] { operation }, _domainServiceDescription, null);
            Assert.IsFalse(success);
            Assert.AreEqual(5, operation.ValidationErrors.Count(), "There should be 5 property-level validation errors");

            IEnumerable<string> errorMessages = operation.ValidationErrors.Select(e => e.Message);
            UnitTestHelper.AssertListContains(errorMessages, "The field Code must be between 0 and 99999.");
            UnitTestHelper.AssertListContains(errorMessages, "Code must start with the prefix 9");
            UnitTestHelper.AssertListContains(errorMessages, "The field FourDigit must be between 0 and 9999.");
            UnitTestHelper.AssertListContains(errorMessages, "The CityName field is required.");
            UnitTestHelper.AssertListContains(errorMessages, "The StateName field is required.");
        }

        [TestMethod]
        [Description("Calling ValidateOperations with insert operation that fails validation because of entity-level error")]
        public void ValidateOperations_Invalid_Insert_Entity_Level_Errors()
        {
            Zip newZip = new Zip() { Code = 98052, FourDigit = 0000, CityName = "Name", StateName = "Name" };

            ChangeSetEntry operation = new ChangeSetEntry();
            operation.DomainOperationEntry = _domainServiceDescription.GetSubmitMethod(typeof(Zip), DomainOperation.Insert);
            operation.Operation = DomainOperation.Insert;
            operation.Entity = newZip;

            bool success = DomainService.ValidateOperations(new ChangeSetEntry[] { operation }, _domainServiceDescription, null);
            Assert.IsFalse(success);
            Assert.AreEqual(1, operation.ValidationErrors.Count(), "There should be 1 entity-level validation error");

            IEnumerable<string> errorMessages = operation.ValidationErrors.Select(e => e.Message);
            Assert.IsTrue(operation.ValidationErrors.Any(err => err.Message == "Zip codes cannot have matching city and state names" && err.SourceMemberNames.Contains("StateName") && err.SourceMemberNames.Contains("CityName")));
            UnitTestHelper.AssertListContains(errorMessages, "Zip codes cannot have matching city and state names");
        }

        [TestMethod]
        [Description("Making sure that excluded members aren't validated")]
        public void ValidateOperations_Excluded_Properties_Skipped()
        {
            DomainServiceDescription dsd = DomainServiceDescription.GetDescription(typeof(ExcludeValidationEntityDomainService));
            //dsd.Initialize();
            Assert.IsNotNull(dsd);

            ExcludeValidationEntity entity = new ExcludeValidationEntity()
            {
                P1to10 = 5,
                P1to10Excluded = 11,
                P1to20Excluded = 5
            };

            ChangeSetEntry insert = new ChangeSetEntry();
            insert.DomainOperationEntry = dsd.GetSubmitMethod(typeof(ExcludeValidationEntity), DomainOperation.Insert);
            insert.Entity = entity;
            insert.Operation = DomainOperation.Insert;

            ChangeSetEntry update = new ChangeSetEntry();
            update.DomainOperationEntry = dsd.GetSubmitMethod(typeof(ExcludeValidationEntity), DomainOperation.Update);
            update.Entity = entity;
            update.Operation = DomainOperation.Update;

            ChangeSetEntry delete = new ChangeSetEntry();
            delete.DomainOperationEntry = dsd.GetSubmitMethod(typeof(ExcludeValidationEntity), DomainOperation.Delete);
            delete.Entity = entity;
            delete.Operation = DomainOperation.Delete;

            // Verify that excluded members aren't validated
            bool success = DomainService.ValidateOperations(new ChangeSetEntry[] { insert, update, delete }, dsd, null);
            Assert.IsTrue(success);

            // Verify that entity-level validation works on excluded members
            entity.P1to20Excluded = 15;
            success = DomainService.ValidateOperations(new ChangeSetEntry[] { update }, dsd, null);
            Assert.IsFalse(success);

            // Verify that non-excluded members are still validated
            entity.P1to10 = 11;
            success = DomainService.ValidateOperations(new ChangeSetEntry[] { update }, dsd, null);
            Assert.IsFalse(success);
        }

        [TestMethod]
        [Description("Calling ValidateOperations with domain method that fails validation")]
        public void ValidateOperations_Invalid_DomainMethod()
        {
            Zip invalidZip = new Zip() { Code = 91023, FourDigit = 1234, CityName = "MyCity", StateName = "MyCity" };
            IEnumerable<Attribute> attrs = invalidZip.GetType().GetCustomAttributes(true).Cast<Attribute>();
            System.Diagnostics.Debug.WriteLine(attrs.Count().ToString());

            ChangeSetEntry operation = new ChangeSetEntry();
            operation.DomainOperationEntry = _domainServiceDescription.GetCustomMethod(typeof(Zip), "ReassignZipCode");
            operation.Operation = DomainOperation.Update;
            operation.EntityActions = new EntityActionCollection { { "ReassignZipCode", new object[] { 10, false } } };
            operation.Entity = invalidZip;

            bool success = DomainService.ValidateOperations(new ChangeSetEntry[] { operation }, _domainServiceDescription, null);
            Assert.IsFalse(success);
            Assert.AreEqual(1, operation.ValidationErrors.Count());
            UnitTestHelper.AssertListContains<ValidationResultInfo>(operation.ValidationErrors, (e => e.Message == "Zip codes cannot have matching city and state names" && e.SourceMemberNames.Contains("StateName") && e.SourceMemberNames.Contains("CityName")));
        }

        [TestMethod]
        [Description("Calling ValidationOperations with multiple operations that fail validation")]
        public void ValidationOperations_Invalid_MultipleOperations()
        {
            List<ChangeSetEntry> operations = new List<ChangeSetEntry>();
            Zip invalidZip = new Zip() { Code = 91023, FourDigit = 1234, CityName = "MyCity", StateName = "MyCity" };
            Zip originalZip = new Zip() { Code = 91023, FourDigit = 1234, CityName = "MyCity", StateName = "WA" };
            Zip invalidZipForInsert = new Zip() { Code = 12345, FourDigit = 12345, CityName = "My", StateName = "My" };
            Zip invalidZipWithEntityErrors = new Zip() { Code = 98052, FourDigit = 0000, CityName = "My", StateName = "My" };
            City invalidCityForInvoke = new City() { StateName = "Texas", CountyName = "County", Name = "Houston" };
            City invalidCityForDelete = new City() { StateName = "Texas", CountyName = "County", Name = "Austin" };

            // use entity that fails object-level validation and invalid parameter for method invocation
            ChangeSetEntry zipInvokeOp = new ChangeSetEntry();
            zipInvokeOp.DomainOperationEntry = _domainServiceDescription.GetCustomMethod(typeof(Zip), "ReassignZipCode");
            zipInvokeOp.Operation = DomainOperation.Update;
            zipInvokeOp.EntityActions = new EntityActionCollection { { "ReassignZipCode", new object[] { -10000, false } } };
            zipInvokeOp.Entity = invalidZip;
            operations.Add(zipInvokeOp);

            // use the same entity for update
            ChangeSetEntry zipUpdateOp = new ChangeSetEntry();
            zipUpdateOp.DomainOperationEntry = _domainServiceDescription.GetSubmitMethod(typeof(Zip), DomainOperation.Update);
            zipUpdateOp.Operation = DomainOperation.Update;
            zipUpdateOp.OriginalEntity = originalZip;
            zipUpdateOp.Entity = invalidZip;
            operations.Add(zipUpdateOp);

            // use a different but same type entity for insert (with property-level errors)
            ChangeSetEntry zip2InsertOp = new ChangeSetEntry();
            zip2InsertOp.DomainOperationEntry = _domainServiceDescription.GetSubmitMethod(typeof(Zip), DomainOperation.Insert);
            zip2InsertOp.Operation = DomainOperation.Insert;
            zip2InsertOp.Entity = invalidZipForInsert;
            operations.Add(zip2InsertOp);

            // use a different but same type entity for insert (with entity-level errors)
            ChangeSetEntry zip3InsertOp = new ChangeSetEntry();
            zip3InsertOp.DomainOperationEntry = _domainServiceDescription.GetSubmitMethod(typeof(Zip), DomainOperation.Insert);
            zip3InsertOp.Operation = DomainOperation.Insert;
            zip3InsertOp.Entity = invalidZipWithEntityErrors;
            operations.Add(zip3InsertOp);

            // invoke domain method with an invalid entity of a different type
            ChangeSetEntry cityInvokeOp = new ChangeSetEntry();
            cityInvokeOp.DomainOperationEntry = _domainServiceDescription.GetCustomMethod(typeof(City), "AssignCityZone");
            cityInvokeOp.EntityActions = new EntityActionCollection { { "AssignCityZone", new object[] { "SomeZone" } } };
            cityInvokeOp.Operation = DomainOperation.Update;
            cityInvokeOp.Entity = invalidCityForInvoke;
            operations.Add(cityInvokeOp);

            // include delete of entity that fails validation
            ChangeSetEntry city2DeleteOp = new ChangeSetEntry();
            city2DeleteOp.DomainOperationEntry = _domainServiceDescription.GetSubmitMethod(typeof(City), DomainOperation.Delete);
            city2DeleteOp.Operation = DomainOperation.Delete;
            city2DeleteOp.Entity = invalidCityForDelete;
            operations.Add(city2DeleteOp);

            // call ValidateOperations. Verify results are expected. Note that Delete operations are not validated.
            bool success = DomainService.ValidateOperations(operations, _domainServiceDescription, null);
            Assert.IsFalse(success);
            DomainServiceTests.LogErrorListContents("zipInvokeOp.Errors", zipInvokeOp.ValidationErrors);
            DomainServiceTests.LogErrorListContents("zipUpdateOp.Errors", zipUpdateOp.ValidationErrors);
            DomainServiceTests.LogErrorListContents("zip2InsertOp.Errors", zip2InsertOp.ValidationErrors);
            DomainServiceTests.LogErrorListContents("cityInvokeOp.Errors", cityInvokeOp.ValidationErrors);
            Assert.AreEqual(2, zipInvokeOp.ValidationErrors.Count());
            UnitTestHelper.AssertListContains<ValidationResultInfo>(zipInvokeOp.ValidationErrors, (e => e.Message == "Zip codes cannot have matching city and state names" && e.SourceMemberNames.Contains("StateName") && e.SourceMemberNames.Contains("CityName")));
            UnitTestHelper.AssertListContains<ValidationResultInfo>(zipInvokeOp.ValidationErrors, (e => e.Message == "The field offset must be between -9999 and 9999."));
            Assert.AreEqual(1, zipUpdateOp.ValidationErrors.Count());
            UnitTestHelper.AssertListContains<ValidationResultInfo>(zipUpdateOp.ValidationErrors, (e => e.Message == "Zip codes cannot have matching city and state names"));
            Assert.AreEqual(2, zip2InsertOp.ValidationErrors.Count());
            UnitTestHelper.AssertListContains<ValidationResultInfo>(zip2InsertOp.ValidationErrors, (e => e.Message == "Code must start with the prefix 9"));
            UnitTestHelper.AssertListContains<ValidationResultInfo>(zip2InsertOp.ValidationErrors, (e => e.Message == "The field FourDigit must be between 0 and 9999."));
            Assert.AreEqual(1, zip3InsertOp.ValidationErrors.Count());
            UnitTestHelper.AssertListContains<ValidationResultInfo>(zip3InsertOp.ValidationErrors, (e => e.Message == "Zip codes cannot have matching city and state names"));
            Assert.AreEqual(1, cityInvokeOp.ValidationErrors.Count());
            UnitTestHelper.AssertListContains<ValidationResultInfo>(cityInvokeOp.ValidationErrors, (e => e.Message == "The field StateName must be a string with a maximum length of 2."));
            Assert.IsNull(city2DeleteOp.ValidationErrors);
        }

        [TestMethod]
        [Description("Verifies the behavior of the DomainService.Factory property.")]
        public void DomainServiceFactory_FactoryPropertyBehavior()
        {
            // After setting the DP Factory to null, verify that the property getter does 
            // not return null.  It should instead return a default factory implementation.
            IDomainServiceFactory prevFactory = DomainService.Factory;
            try
            {
                DomainService.Factory = null;
                Assert.IsNotNull(DomainService.Factory);

                // Set the DP Factory to a mock instance and verify the property getter return.
                IDomainServiceFactory mockFactory = new MockDomainServiceFactory();
                DomainService.Factory = mockFactory;
                Assert.AreEqual<IDomainServiceFactory>(mockFactory, DomainService.Factory);
            }
            finally
            {
                // Be sure to restore the factory!
                DomainService.Factory = prevFactory;
            }
        }

        [TestMethod]
        [Description("Verifies the default factory behavior of the DomainService.Factory property.")]
        public void DomainServiceFactory_DefaultFactoryBehavior()
        {
            // After setting the DP Factory to null, verify that the property getter does 
            // not return null.  It should instead return a default factory implementation.
            IDomainServiceFactory prevFactory = DomainService.Factory;
            try
            {
                DomainService.Factory = null;
                Assert.IsNotNull(DomainService.Factory);

                // Verify the default factory creates an instance as expected.
                MockDomainService provider = DomainService.Factory.CreateDomainService(typeof(MockDomainService), null) as MockDomainService;
                Assert.IsNotNull(provider);
                Assert.IsTrue(provider.Initialized);

                // Verify the default factory disposed the instance as expected.
                DomainService.Factory.ReleaseDomainService(provider);
                Assert.IsTrue(provider.Disposed);
            }
            finally
            {
                // Be sure to restore the factory!
                DomainService.Factory = prevFactory;
            }
        }

        [TestMethod]
        [Description("Verifies the default factory throws on invalid DomainService types.")]
        public void DomainServiceFactory_InvalidDomainServiceType()
        {
            // After setting the DP Factory to null, verify that the property getter does 
            // not return null.  It should instead return a default factory implementation.
            IDomainServiceFactory prevFactory = DomainService.Factory;
            try
            {
                DomainService.Factory = null;
                Assert.IsNotNull(DomainService.Factory);

                // Verify the default factory throws as expected
                ExceptionHelper.ExpectArgumentException(
                    () => DomainService.Factory.CreateDomainService(typeof(string), null),
                    string.Format(CultureInfo.CurrentCulture, Resource.DomainService_Factory_InvalidDomainServiceType, typeof(string)) + "\r\nParameter name: domainServiceType");
            }
            finally
            {
                // Be sure to restore the factory!
                DomainService.Factory = prevFactory;
            }
        }

        [TestMethod]
        [Description("Verifies the behavior of ChangeSet.Associate() in end-to-end DomainService scenarios.")]
        public async Task DomainService_AssociatedEntities_ChangePropagationAsync()
        {
            var context = new DomainServiceContext(new MockDataService(), new MockUser("user"), DomainOperationType.Query);
            var service = new DomainService_AssociatedEntities();
            DomainServiceDescription serviceDescription = DomainServiceDescription.GetDescription(typeof(DomainService_AssociatedEntities));
            var queryOp = serviceDescription.GetQueryMethod("GetCustomers");

            service.Initialize(context);

            var result = (IEnumerable<PresentationCustomer>)(await service.QueryAsync<PresentationCustomer>(new QueryDescription(queryOp), CancellationToken.None)).Result;
            var pmEntity1 = result.Single(p => p.ID == 1);

            Assert.AreEqual("First1 Last1", pmEntity1.Name);
            Assert.AreEqual("Value1", pmEntity1.Message);

            pmEntity1.Message = "Value1.1";

            context = new DomainServiceContext(new MockDataService(), new MockUser("user"), DomainOperationType.Submit);
            service = new DomainService_AssociatedEntities();
            service.Initialize(context);
            var updateOp = new ChangeSetEntry() { Entity = pmEntity1, Operation = DomainOperation.Update, HasMemberChanges = true };
            var changeSet = new ChangeSet(new[] { updateOp });
            await service.SubmitAsync(changeSet, CancellationToken.None);

            Assert.AreEqual("UpdatedFirst1 UpdatedLast1", pmEntity1.Name);
            Assert.AreEqual("Value1.1", pmEntity1.Message);
        }

        [TestMethod]
        [Description("Verifies that the ChangeSet.Replace method works even if called from a Associate transformation.")]
        public async Task DomainService_AssociatedEntities_ReplaceInTransform()
        {
            var context = new DomainServiceContext(new MockDataService(), new MockUser("user"), DomainOperationType.Query);
            var service = new DomainService_AssociatedEntities();
            DomainServiceDescription serviceDescription = DomainServiceDescription.GetDescription(typeof(DomainService_AssociatedEntities));
            var queryOp = serviceDescription.GetQueryMethod("GetCustomers");

            service.Initialize(context);

            var result = (IEnumerable<PresentationCustomer>)
                (await service.QueryAsync<PresentationCustomer>(new QueryDescription(queryOp), CancellationToken.None))
                .Result;
            var pmEntity1 = result.Single(p => p.ID == 1);

            Assert.AreEqual("First1 Last1", pmEntity1.Name);
            Assert.AreEqual("Value1", pmEntity1.Message);

            context = new DomainServiceContext(new MockDataService(), new MockUser("user"), DomainOperationType.Submit);
            service = new DomainService_AssociatedEntities();
            service.Initialize(context);
            var customOp = new ChangeSetEntry()
            {
                Entity = pmEntity1,
                Operation = DomainOperation.Update,
                EntityActions = new EntityActionCollection { { "CustomCustomerUpdate", new object[] { false } } }
            };
            var changeSet = new ChangeSet(new[] { customOp });
            pmEntity1.Message = "ReplaceInTransform";
            await service.SubmitAsync(changeSet, CancellationToken.None);

            // No changes
            var entityInChangeSet = customOp.Entity as PresentationCustomer;
            Assert.AreEqual("First1 Last1", entityInChangeSet.Name);
            Assert.AreEqual("Replaced", entityInChangeSet.Message);

            // Inspect the ChangeSet
            Assert.IsFalse(changeSet.HasError);
        }

        [TestMethod]
        [Description("Verifies that the expected exception is raised when a change conflict occurs and the entity in conflict is not contained in the changeset.")]
        public async Task DomainService_ChangeConflict_EntityNotInChangeSet()
        {
            var context = new DomainServiceContext(new MockDataService(), new MockUser("user"), DomainOperationType.Query);
            var service = new DomainService_AssociatedEntities();
            DomainServiceDescription serviceDescription = DomainServiceDescription.GetDescription(typeof(DomainService_AssociatedEntities));
            var queryOp = serviceDescription.GetQueryMethod("GetCustomers");

            service.Initialize(context);


            var result = (IEnumerable<PresentationCustomer>)(await service.QueryAsync<PresentationCustomer>(new QueryDescription(queryOp), CancellationToken.None)).Result;
            var pmEntity1 = result.Single(p => p.ID == 1);

            Assert.AreEqual("First1 Last1", pmEntity1.Name);
            Assert.AreEqual("Value1", pmEntity1.Message);

            context = new DomainServiceContext(new MockDataService(), new MockUser("user"), DomainOperationType.Submit);
            service = new DomainService_AssociatedEntities();
            service.Initialize(context);
            var customOp = new ChangeSetEntry()
            {
                Entity = pmEntity1,
                Operation = DomainOperation.Update,
                EntityActions = new EntityActionCollection { { "CustomCustomerUpdate", new object[] { true } } }
            };
            var changeSet = new ChangeSet(new[] { customOp });
            pmEntity1.Message = "UseEntityNotInChangeSet";

            ExceptionHelper.ExpectArgumentException(
                () => service.SubmitAsync(changeSet, CancellationToken.None).GetAwaiter().GetResult(),
                Resource.ChangeSet_ChangeSetEntryNotFound,
                "entity");
        }

        /// <summary>
        /// Write the list of errors to console
        /// </summary>
        /// <param name="listName">descriptive name of the list</param>
        /// <param name="errors">error list</param>
        public static void LogErrorListContents(string listName, IEnumerable<ValidationResultInfo> errors)
        {
            Console.WriteLine(string.Format("Contents of error list {0}:", listName));
            if (errors == null)
            {
                Console.WriteLine("<null>");
                return;
            }
            Console.WriteLine(string.Format("Count: {0}", errors.Count()));
            foreach (ValidationResultInfo error in errors)
            {
                Console.WriteLine(string.Format(
                    "Item: Message={0}, ErrorCode={1}, StackTrace={2}",
                    error.Message,
                    error.ErrorCode,
                    error.StackTrace));
            }
            Console.WriteLine();
        }

        private class QueryResult<T>
        {
            public IEnumerable<T> Results
            {
                get;
                set;
            }

            public int TotalCount
            {
                get;
                set;
            }
        }
    }

    #region Mock DomainService Types

    #region TDP registration test classes

    [EnableClientAccess]
    [DomainServiceDescriptionProvider(typeof(DSDPA))]
    [DomainServiceDescriptionProvider(typeof(DSDPB))]
    public abstract class ProviderBase : DomainService
    {
    }

    public class ProviderA : ProviderBase
    {
        [Query]
        public IEnumerable<TestEntityA> GetTestEntities()
        {
            return null;
        }
    }

    [DomainServiceDescriptionProvider(typeof(PocoDomainServiceDescriptionProvider))]
    [DomainServiceDescriptionProvider(typeof(DSDPA))]
    [DomainServiceDescriptionProvider(typeof(DSDPB))]
    public class ProviderB : ProviderBase
    {
        [Query]
        public IEnumerable<TestEntityB> GetTestEntities()
        {
            return null;
        }
    }

    [MetadataType(typeof(TestEntityMetadata))]
    public partial class TestEntity
    {
        [Key]
        public int ID
        {
            get;
            set;
        }

        public string PropA
        {
            get;
            set;
        }

        public int PropB
        {
            get;
            set;
        }

        public string Date
        {
            get;
            set;
        }
    }

    public partial class TestEntityMetadata
    {
        [TestAttributeA("mdx")]
        public static object PropA;

        [TestAttributeA("mdx")]
        public static object PropB;
    }

    public class TestEntityA : TestEntity
    {
    }

    public class TestEntityB : TestEntity
    {
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public partial class TestAttributeA : Attribute
    {
        public TestAttributeA(string tag)
        {
            this.Tag = tag;
        }

        public string Tag
        {
            get;
            set;
        }

        public override object TypeId
        {
            get
            {
                return this;
            }
        }
    }

    /// <summary>
    /// Provider that adds TestAttributeA to all int members
    /// </summary>
    public class DSDPA : DomainServiceDescriptionProvider
    {
        private readonly DomainServiceDescriptionProvider parentProvider;

        public DSDPA(Type domainServiceType, DomainServiceDescriptionProvider parent)
            : base(domainServiceType, parent)
        {
            this.parentProvider = parent;
        }

        public override ICustomTypeDescriptor GetTypeDescriptor(Type t, ICustomTypeDescriptor parent)
        {
            parent = base.GetTypeDescriptor(t, parent);

            return new TDA(parent);
        }

        public class TDA : CustomTypeDescriptor
        {
            public TDA(ICustomTypeDescriptor parent)
                : base(parent)
            {
            }

            public override PropertyDescriptorCollection GetProperties()
            {
                PropertyDescriptorCollection baseProperties = base.GetProperties();
                List<PropertyDescriptor> retProperties = new List<PropertyDescriptor>();

                // add custom attributes to properties as needed
                bool modificationsMade = false;
                foreach (PropertyDescriptor pd in baseProperties)
                {
                    if (pd.PropertyType == typeof(int))
                    {
                        List<Attribute> attributes = pd.Attributes.OfType<Attribute>().ToList();
                        TestAttributeA ta = new TestAttributeA("tdpa_int");
                        attributes.Add(ta);
                        attributes.Add(new TestAttributeA(Guid.NewGuid().ToString()));
                        PropertyDescriptor newPd = new MetadataPropertyDescriptorWrapper(pd, attributes.ToArray());
                        retProperties.Add(newPd);
                        modificationsMade = true;
                    }
                    else
                    {
                        retProperties.Add(pd);
                    }
                }

                return modificationsMade ? new PropertyDescriptorCollection(retProperties.ToArray(), true) : baseProperties;
            }
        }
    }

    /// <summary>
    /// Provider that adds [DataType(DataType=Date)] to all members ending in 'Date'.
    /// </summary>
    public class DSDPB : DomainServiceDescriptionProvider
    {
        private readonly DomainServiceDescriptionProvider parentProvider;

        public DSDPB(Type domainServiceType, DomainServiceDescriptionProvider parent)
            : base(domainServiceType, parent)
        {
            this.parentProvider = parent;
        }

        public override ICustomTypeDescriptor GetTypeDescriptor(Type t, ICustomTypeDescriptor parent)
        {
            parent = base.GetTypeDescriptor(t, parent);

            return new TDB(parent);
        }

        public class TDB : CustomTypeDescriptor
        {
            public TDB(ICustomTypeDescriptor parent)
                : base(parent)
            {
            }

            public override PropertyDescriptorCollection GetProperties()
            {
                PropertyDescriptorCollection baseProperties = base.GetProperties();
                List<PropertyDescriptor> retProperties = new List<PropertyDescriptor>();

                // add custom attributes to properties as needed
                bool modificationsMade = false;
                foreach (PropertyDescriptor pd in baseProperties)
                {
                    if (pd.Name.EndsWith("Date", StringComparison.OrdinalIgnoreCase))
                    {
                        List<Attribute> attributes = pd.Attributes.OfType<Attribute>().ToList();
                        attributes.Add(new DataTypeAttribute(DataType.Date));
                        retProperties.Add(new MetadataPropertyDescriptorWrapper(pd, attributes.ToArray()));
                        modificationsMade = true;
                    }
                    else
                    {
                        retProperties.Add(pd);
                    }
                }

                return modificationsMade ? new PropertyDescriptorCollection(retProperties.ToArray(), true) : baseProperties;
            }
        }
    }

    public class MetadataPropertyDescriptorWrapper : PropertyDescriptor
    {
        private readonly PropertyDescriptor _descriptor;
        public MetadataPropertyDescriptorWrapper(PropertyDescriptor descriptor, Attribute[] attrs)
            : base(descriptor, attrs)
        {
            _descriptor = descriptor;
        }

        public override void AddValueChanged(object component, EventHandler handler)
        {
            _descriptor.AddValueChanged(component, handler);
        }

        public override bool CanResetValue(object component)
        {
            return _descriptor.CanResetValue(component);
        }

        public override Type ComponentType
        {
            get
            {
                return _descriptor.ComponentType;
            }
        }

        public override object GetValue(object component)
        {
            return _descriptor.GetValue(component);
        }

        public override bool IsReadOnly
        {
            get
            {
                return _descriptor.IsReadOnly;
            }
        }

        public override Type PropertyType
        {
            get
            {
                return _descriptor.PropertyType;
            }
        }

        public override void RemoveValueChanged(object component, EventHandler handler)
        {
            _descriptor.RemoveValueChanged(component, handler);
        }

        public override void ResetValue(object component)
        {
            _descriptor.ResetValue(component);
        }

        public override void SetValue(object component, object value)
        {
            _descriptor.SetValue(component, value);
        }

        public override bool ShouldSerializeValue(object component)
        {
            return _descriptor.ShouldSerializeValue(component);
        }

        public override bool SupportsChangeEvents
        {
            get
            {
                return _descriptor.SupportsChangeEvents;
            }
        }
    }

    /// <summary>
    /// Provider that adds [Key] and [Association] attributes to entities based on a simple naming
    /// convention:
    ///   - Key members must be named 'ID'
    ///   - A singleton Association member of type 'T' must have a corresponding FK member named 'TID'
    ///   - A collection Association of element type 'V' on a Type 'T' requires a back reference named 'TID' on Type 'V'
    /// Caveats:
    ///   - multipart keys are not supported
    ///   - multipart FK references are not supported
    ///   - only a single collection association of a particular Type is supported on a Type
    /// </summary>
    public class PocoDomainServiceDescriptionProvider : DomainServiceDescriptionProvider
    {
        private readonly DomainServiceDescriptionProvider parentProvider;

        public PocoDomainServiceDescriptionProvider(Type domainServiceType, DomainServiceDescriptionProvider parent)
            : base(domainServiceType, parent)
        {
            this.parentProvider = parent;
        }

        public override ICustomTypeDescriptor GetTypeDescriptor(Type objectType, ICustomTypeDescriptor parent)
        {
            parent = base.GetTypeDescriptor(objectType, parent);

            return new PocoTypeDescriptor(parent);
        }

        public class PocoTypeDescriptor : CustomTypeDescriptor
        {
            public PocoTypeDescriptor(ICustomTypeDescriptor parent)
                : base(parent)
            {
            }

            public override PropertyDescriptorCollection GetProperties()
            {
                PropertyDescriptorCollection baseProperties = base.GetProperties();
                List<PropertyDescriptor> retProperties = new List<PropertyDescriptor>();

                // add custom attributes to properties as needed
                bool modificationsMade = false;
                PropertyInfo referenceProp = null;
                Type ienumType = null;
                foreach (PropertyDescriptor pd in baseProperties)
                {
                    if (string.Compare("ID", pd.Name) == 0)
                    {
                        // Primary key member
                        List<Attribute> attributes = pd.Attributes.OfType<Attribute>().ToList();
                        attributes.Add(new KeyAttribute());
                        retProperties.Add(new MetadataPropertyDescriptorWrapper(pd, attributes.ToArray()));
                        modificationsMade = true;
                    }
                    else if ((referenceProp = pd.ComponentType.GetProperty(pd.Name + "ID", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) != null)
                    {
                        // FK association member
                        string assocName = string.Format("{0}_{1}", pd.ComponentType.Name, pd.Name);
                        AssociationAttribute assoc = new AssociationAttribute(assocName, referenceProp.Name, "ID");
                        assoc.IsForeignKey = true;

                        List<Attribute> attributes = pd.Attributes.OfType<Attribute>().ToList();
                        attributes.Add(assoc);
                        retProperties.Add(new MetadataPropertyDescriptorWrapper(pd, attributes.ToArray()));
                        modificationsMade = true;
                    }
                    else if ((ienumType = FindIEnumerable(pd.PropertyType)) != null)
                    {
                        // see if there is a FK reference from the element type to this type
                        Type elementType = GetElementType(pd.PropertyType);
                        PropertyInfo backReference = GetBackReference(elementType, pd.ComponentType);
                        if (backReference != null)
                        {
                            // collection association member
                            string assocName = string.Format("{0}_{1}", elementType.Name, backReference.Name.Substring(0, backReference.Name.Length - 2));
                            AssociationAttribute assoc = new AssociationAttribute(assocName, "ID", backReference.Name);

                            List<Attribute> attributes = pd.Attributes.OfType<Attribute>().ToList();
                            attributes.Add(assoc);
                            retProperties.Add(new MetadataPropertyDescriptorWrapper(pd, attributes.ToArray()));
                            modificationsMade = true;
                        }
                    }
                    else
                    {
                        // no changes
                        retProperties.Add(pd);
                    }
                }

                return modificationsMade ? new PropertyDescriptorCollection(retProperties.ToArray(), true) : baseProperties;
            }

            /// <summary>
            /// Searches the 
            /// </summary>
            /// <param name="parentType"></param>
            /// <param name="targetType"></param>
            /// <returns></returns>
            private PropertyInfo GetBackReference(Type parentType, Type targetType)
            {
                // for each property name 'P', search for a corresponding property 'PID' - if
                // that pattern is found, the pair comprise a FK reference
                IEnumerable<PropertyInfo> parentProperties = parentType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (PropertyInfo propInfo in parentProperties.Where(p => p.PropertyType == targetType))
                {
                    PropertyInfo referenceProperty = parentProperties.SingleOrDefault(p => p.Name == (propInfo.Name + "ID"));
                    if (referenceProperty != null)
                    {
                        return referenceProperty;
                    }
                }

                return null;
            }

            private static Type GetElementType(Type seqType)
            {
                Type ienum = FindIEnumerable(seqType);
                if (ienum == null)
                    return seqType;
                return ienum.GetGenericArguments()[0];
            }

            private static Type FindIEnumerable(Type seqType)
            {
                if (seqType == null || seqType == typeof(string))
                    return null;
                if (seqType.IsArray)
                    return typeof(IEnumerable<>).MakeGenericType(seqType.GetElementType());
                if (seqType.IsGenericType)
                {
                    foreach (Type arg in seqType.GetGenericArguments())
                    {
                        Type ienum = typeof(IEnumerable<>).MakeGenericType(arg);
                        if (ienum.IsAssignableFrom(seqType))
                        {
                            return ienum;
                        }
                    }
                }
                Type[] ifaces = seqType.GetInterfaces();
                if (ifaces != null && ifaces.Length > 0)
                {
                    foreach (Type iface in ifaces)
                    {
                        Type ienum = FindIEnumerable(iface);
                        if (ienum != null)
                            return ienum;
                    }
                }
                if (seqType.BaseType != null && seqType.BaseType != typeof(object))
                {
                    return FindIEnumerable(seqType.BaseType);
                }
                return null;
            }
        }
    }

    public class PocoOrder
    {
        // expect a Key attribute to be generated here
        public int ID
        {
            get;
            set;
        }

        public string CustomerID
        {
            get;
            set;
        }

        // expect an Association attribute to be generated here
        public PocoCustomer Customer
        {
            get;
            set;
        }

        public decimal OrderTotal
        {
            get;
            set;
        }
    }

    public class PocoCustomer
    {
        public string ID
        {
            get;
            set;
        }

        public string Name
        {
            get;
            set;
        }

        // expect an Association attribute to be generated here
        public IEnumerable<PocoOrder> Orders
        {
            get;
            set;
        }
    }

    public class PocoEmployee
    {
        // expect an Key attribute to be generated here
        public int ID
        {
            get;
            set;
        }

        public int ManagerID
        {
            get;
            set;
        }

        // expect an Association attribute to be generated here
        public PocoEmployee Manager
        {
            get;
            set;
        }

        public int HiringManagerID
        {
            get;
            set;
        }

        // expect an Association attribute to be generated here
        public PocoEmployee HiringManager
        {
            get;
            set;
        }

        // expect an Association attribute to be generated here
        public IEnumerable<PocoEmployee> Reports
        {
            get;
            set;
        }
    }

    [EnableClientAccess]
    [DomainServiceDescriptionProvider(typeof(PocoDomainServiceDescriptionProvider))]
    public class PocoTestDomainService : DomainService
    {
        [Query]
        public IEnumerable<PocoCustomer> GetCustomers()
        {
            return null;
        }

        [Query]
        public IEnumerable<PocoOrder> GetOrders()
        {
            return null;
        }

        [Query]
        public IEnumerable<PocoEmployee> GetEmployees()
        {
            return null;
        }
    }

    #endregion

    [EnableClientAccess]
    public class LTSService_ConstructorInit : LinqToSqlDomainService<DataTests.Northwind.LTS.NorthwindDataContext>
    {
        public LTSService_ConstructorInit()
        {
            DataLoadOptions loadOpts = new DataLoadOptions();
            loadOpts.LoadWith<DataTests.Northwind.LTS.Order>(p => p.Order_Details);

            this.DataContext.LoadOptions = loadOpts;
        }
    }

    [EnableClientAccess]
    public class EFService_ConstructorInit : LinqToEntitiesDomainService<NorthwindModel.NorthwindEntities>
    {
        public EFService_ConstructorInit()
        {
            string conn = this.ObjectContext.Connection.ConnectionString;
        }
    }

    [EnableClientAccess]
    public class ErrorTestDomainService : DomainService
    {
        private readonly CityData cities = new CityData();

        public IEnumerable<City> GetCities()
        {
            return cities.Cities;
        }

        public void UpdateCity(City current)
        {

        }

        [Invoke]
        public void CityOp(City city)
        {
            throw new ValidationException("Invalid City Update!", null, city);
        }

        protected override ValueTask<bool> PersistChangeSetAsync(CancellationToken cancellationToken)
        {
            // verify that validation exceptions thrown from PersistChangeSet
            // are handled by the framework properly
            City city = this.ChangeSet.ChangeSetEntries.First().Entity as City;
            ValidationResult vr = new ValidationResult("Invalid City Update!", new string[] { "AMember" });
            throw new ValidationException(vr, null, city);
        }
    }

    /// <summary>
    /// Test provider that overrides several of the sumbit pipeline
    /// methods.
    /// </summary>
    [EnableClientAccess]
    public class TestDomainService_OverloadTests : DomainService
    {
        public int UpdateCount;
        public int SubmitCount;
        public int ValidateCount;
        public int ConflictCount;
        public bool IsValid = true;

        public TestDomainService_OverloadTests()
        {
            // initialize with a test user
            Initialize(new DomainServiceContext(new MockDataService(), new MockUser("mathew") {
                IsAuthenticated = true
            }, DomainOperationType.Submit));
        }

        // verify that we can override base initialization
        public override void Initialize(DomainServiceContext operationContext)
        {
            base.Initialize(operationContext);

            // additional initialization can be performed here
        }

        [Query]
        public IEnumerable<TestEntity> Get()
        {
            throw new NotImplementedException();
        }

        [Update]
        public void UpdateTestEntity(TestEntity curr)
        {
            UpdateCount++;
        }

        protected override ValueTask<bool> PersistChangeSetAsync(CancellationToken cancellationToken)
        {
            SubmitCount++;

            ChangeSetEntry updateOperation = this.ChangeSet.ChangeSetEntries.First();
            if (ConflictCount-- > 0)
            {
                updateOperation.ConflictMembers = new string[] { "PropA" };
            }
            else
            {
                updateOperation.ConflictMembers = null;
            }

            return new ValueTask<bool>(!this.ChangeSet.HasError);
        }

        protected override async ValueTask<bool> ValidateChangeSetAsync(CancellationToken cancellationToken)
        {
            ValidateCount++;

            await base.ValidateChangeSetAsync(cancellationToken);

            return IsValid;
        }

        protected override ValueTask<bool> ExecuteChangeSetAsync(CancellationToken cancellationToken)
        {
            return base.ExecuteChangeSetAsync(cancellationToken);
        }
    }

    public class ReadonlyEntity
    {
        [Editable(false)]
        public Nullable<char> NullableCharReadOnlyProperty
        {
            get;
            set;
        }

        [Editable(false)]
        public int IntReadOnlyProperty
        {
            get;
            set;
        }

        [Editable(false)]
        public string StringReadOnlyProperty
        {
            get;
            set;
        }

        [Editable(false)]
        public Uri UriReadOnlyProperty
        {
            get;
            set;
        }

        [Editable(false)]
        public XElement XElementReadOnlyProperty
        {
            get;
            set;
        }

        [Editable(false)]
        public IEnumerable<decimal> EnumerableDecimalReadOnlyProperty
        {
            get;
            set;
        }

        [Editable(false)]
        public byte[] ByteArrayReadOnlyProperty
        {
            get;
            set;
        }

        [Editable(false)]
        public List<Uri> ListUriReadOnlyProperty
        {
            get;
            set;
        }

        [Editable(false)]
        public List<XElement> ListXElementReadOnlyProperty
        {
            get;
            set;
        }

        [Editable(false)]
        public List<String> ListStringReadOnlyProperty
        {
            get;
            set;
        }

        [Editable(false)]
        public List<Binary> ListBinaryReadOnlyProperty
        {
            get;
            set;
        }

        [Exclude]
        [Editable(false)]
        public string ReadOnlyExcludedProperty
        {
            get;
            set;
        }

        [Editable(false)]
        public Dictionary<string, XElement> ReadonlyDictionaryProperty
        {
            get;
            set;
        }
    }

    [EnableClientAccess]
    public class MockDomainService_SelectThrows : DomainService
    {
        [Query]
        public IQueryable<TestEntity> GetEntities()
        {
            throw new Exception("Test");
        }
    }

    public class MockDomainService : DomainService
    {
        public bool Disposed { get; private set; }
        public bool Initialized { get; private set; }

        public override void Initialize(DomainServiceContext domainServiceContext)
        {
            this.Initialized = true;
        }

        protected override void Dispose(bool disposing)
        {
            this.Disposed = true;
        }
    }

    public class MockDomainServiceFactory : IDomainServiceFactory
    {
        public DomainService CreateDomainService(Type domainServiceType, DomainServiceContext domainServiceContext)
        {
            throw new NotImplementedException();
        }

        public void ReleaseDomainService(DomainService domainService)
        {
            throw new NotImplementedException();
        }
    }

    public class DomainService_AssociatedEntities : DomainService
    {
        private readonly IEnumerable<DataStoreCustomer> customers =
            new[]
            {
                new DataStoreCustomer() { ID = 1, FirstName = "First1", LastName = "Last1", Message = "Value1" },
                new DataStoreCustomer() { ID = 2, FirstName = "First2", LastName = "Last2", Message = "Value2" },
                new DataStoreCustomer() { ID = 3, FirstName = "First3", LastName = "Last3", Message = "Value3" }
            };

        [Query]
        public IQueryable<PresentationCustomer> GetCustomers()
        {
            return (from c in customers
                    select new PresentationCustomer()
                    {
                        ID = c.ID,
                        Name = c.FirstName + " " + c.LastName,
                        Message = c.Message
                    }).AsQueryable();
        }

        [Update]
        public void UpdateCustomer(PresentationCustomer customer)
        {
            // Retrieve the DAL customer
            DataStoreCustomer dataCustomer = customers.Single(c => c.ID == customer.ID);

            // Update the DAL customer
            dataCustomer.Message = customer.Message;

            // Let's perform additional updates that our transform should progagate
            dataCustomer.FirstName = "UpdatedFirst" + customer.ID.ToString();
            dataCustomer.LastName = "UpdatedLast" + customer.ID.ToString();

            // Associate the entities so any DAL changes flow back
            this.ChangeSet.Associate(customer, dataCustomer, (c, s) => c.Name = s.FirstName + " " + s.LastName);
        }

        [EntityAction]
        public void CustomCustomerUpdate(PresentationCustomer customer, bool triggerError)
        {
            // Retrieve the DAL customer
            DataStoreCustomer dataCustomer = customers.Single(c => c.ID == customer.ID);

            // Update the DAL customer
            dataCustomer.Message = customer.Message;
            dataCustomer.TriggerError = triggerError;

            // Let's use ChangeSet.Associate with an entity not in the changeset
            if (customer.Message == "UseEntityNotInChangeSet")
            {
                customer = new PresentationCustomer()
                {
                    ID = customer.ID,
                    Message = customer.Message,
                    Name = customer.Name
                };
            }

            // Associate the entities so any DAL changes flow back
            this.ChangeSet.Associate(
                customer,
                dataCustomer,
                (c, s) =>
                {
                    c.Name = s.FirstName + " " + s.LastName;

                    if (customer.Message == "ReplaceInTransform")
                    {
                        this.ChangeSet.Replace(
                            c,
                            new PresentationCustomer()
                            {
                                ID = c.ID,
                                Message = "Replaced",
                                Name = c.Name
                            });
                    }
                });
        }

        protected override ValueTask<bool> PersistChangeSetAsync(CancellationToken cancellationToken)
        {
            // Check if we should fake an optimistic concurrency exception and 
            // return conflict members
            var dalEntitiesInError = customers.Where(d => d.TriggerError);
            foreach (var dalEntity in dalEntitiesInError)
            {
                var associatedEntities = this.ChangeSet.GetAssociatedEntities<PresentationCustomer, DataStoreCustomer>(dalEntity);
                var operations = this.ChangeSet.ChangeSetEntries.Where(e => associatedEntities.Contains((PresentationCustomer)e.Entity));

                foreach (var op in operations)
                {
                    op.ConflictMembers = new[] { "Message" };
                    op.StoreEntity = this.GetCustomers().Single(c => c.ID == ((PresentationCustomer)op.Entity).ID);
                }
            }

            return new ValueTask<bool>(!this.ChangeSet.HasError);
        }
    }

    public class DataStoreCustomer
    {
        [Key]
        public int ID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Message { get; set; }
        [Exclude]
        public bool TriggerError { get; set; }
    }
    public class PresentationCustomer
    {
        [Key]
        public int ID { get; set; }
        public string Name { get; set; }
        public string Message { get; set; }
    }

    public static class AlwaysFailValidator
    {
        public static ValidationResult Validate(object o, ValidationContext context)
        {
            string displayName = context.DisplayName ?? "TypeLevel";
            string result = GetError(displayName);
            return new ValidationResult(result);
        }

        public static string GetError(string displayName)
        {
            return string.Format("{0} fails validation", displayName);
        }
    }

    public class DomainServiceInsertCustom_Service : DomainService
    {
        public IEnumerable<DomainServiceInsertCustom_Entity> GetEntities() { return null; }
        public void InsertEntity(DomainServiceInsertCustom_Entity entity) { }
        public void UpdateEntity(DomainServiceInsertCustom_Entity entity) { }
        [EntityAction]
        public void UpdateEntityWithInt(DomainServiceInsertCustom_Entity entity,
            [CustomValidation(typeof(AlwaysFailValidator), "Validate")] int i) { }
        [EntityAction]
        public void UpdateEntityWithObject(DomainServiceInsertCustom_Entity entity, DomainServiceInsertCustom_Validated_Object obj) { }
        [EntityAction]
        public void UpdateEntityWithCollection(DomainServiceInsertCustom_Entity entity, IEnumerable<DomainServiceInsertCustom_Validated_Object> objs) { }
    }

    public class DomainServiceInsertCustom_Entity
    {
        [Key]
        public int Key { get; set; }
    }

    public class DomainServiceInsertCustom_Validated_Object
    {
        [CustomValidation(typeof(AlwaysFailValidator), "Validate")]
        public int IntProp { get; set; }
    }
    #endregion // Mock DomainService Types
}
