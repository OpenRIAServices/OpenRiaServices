extern alias SSmDsClient;

using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using OpenRiaServices.DomainServices.Client.Test;
using Cities;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestDomainServices;
using TestDescription = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;

namespace OpenRiaServices.DomainServices.Client.UnitTest
{
    using Resource = SSmDsClient::OpenRiaServices.DomainServices.Client.Resource;

    /// <summary>
    /// Verifies the behavior of cross-domain context entity and entity collection references.
    /// </summary>
    [TestClass]
    public class CrossDomainContextTests : UnitTestBase
    {
        #region Properties

        /// <summary>
        /// Gets or sets a <see cref="MockCustomerDomainService"/> DomainContext reference.
        /// </summary>
        private MockCustomerDomainContext CustomerDomainService { get; set; }

        /// <summary>
        /// Gets or sets a <see cref="CityDataContext"/> DomainContext reference.
        /// </summary>
        private CityDomainContext CityDomainContext { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not the city data has been loaded.
        /// </summary>
        private bool? CitiesLoaded { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not the customer data has been loaded.
        /// </summary>
        private bool? CustomersLoaded { get; set; }

        #endregion Properties

        #region Test Initialization and Clean up

        /// <summary>
        /// Initialize our providers before each test.
        /// </summary>
        [TestInitialize]
        [TestDescription("Initialize our providers before each test.")]
        public void Initialize()
        {
            // Reset loading flags
            this.CitiesLoaded = null;
            this.CustomersLoaded = null;

            // Initialize our providers
            this.CityDomainContext = new CityDomainContext(TestURIs.Cities);
            this.CustomerDomainService = new MockCustomerDomainContext(TestURIs.MockCustomers);
        }

        #endregion Test Initialization and Clean up

        #region TestMethods
        /// <summary>
        /// Verifies adding a null reference throws an ArgumentNullException.
        /// </summary>
        [TestMethod]
        [TestDescription("Verifies adding a null reference throws an ArgumentNullException.")]
        public void AddNullReferenceThrows()
        {
            // Add null reference, exception should be thrown
            ExceptionHelper.ExpectArgumentNullException(
                () => this.CustomerDomainService.AddReference(typeof(MockCustomer), null),
                "domainContext");

            // Add null reference, exception should be thrown
            ExceptionHelper.ExpectArgumentNullException(
                () => this.CustomerDomainService.AddReference(null, null),
                "entityType");
        }

        /// <summary>
        /// Verifies that adding a reference with mismatching Entity type and DomainContext throws an InvalidOperationException.
        /// </summary>
        [TestMethod]
        [TestDescription("Verifies that adding a reference with mismatching Entity type and DomainContext throws an InvalidOperationException.")]
        public void AddReferenceWithIncorrectType()
        {
            // Add a reference for an Entity type not contained in the provided context.
            ExceptionHelper.ExpectInvalidOperationException(
                () => this.CustomerDomainService.AddReference(typeof(A), this.CityDomainContext),
                string.Format(CultureInfo.InvariantCulture, Resource.EntityContainerDoesntContainEntityType, typeof(A)));
        }

        /// <summary>
        /// Verifies adding redundant references throws an ArgumentException.
        /// </summary>
        [TestMethod]
        [TestDescription("Verifies adding redundant references throws an ArgumentException.")]
        public void AddRedundantReference()
        {
            // Add a reference to an entity type already in the codegen'd definition
            ExceptionHelper.ExpectArgumentException(
                () => this.CustomerDomainService.AddReference(typeof(MockCustomer), this.CustomerDomainService),
                string.Format(CultureInfo.InvariantCulture, Resource.EntityContainer_EntitySetAlreadyExists, typeof(MockCustomer)));

            // First time adding an external entity should succeed
            this.CustomerDomainService.AddReference(typeof(City), this.CityDomainContext);

            // Second time should fail
            ExceptionHelper.ExpectArgumentException(
                () => this.CustomerDomainService.AddReference(typeof(City), this.CityDomainContext),
                string.Format(CultureInfo.InvariantCulture, Resource.EntityContainer_EntitySetAlreadyExists, typeof(City)));
        }

        /// <summary>
        /// Verifies EntityCollections can be accessed across DomainContext boundaries.
        /// </summary>
        [TestMethod, Asynchronous]
        [TestDescription("Verifies EntityCollections can be accessed across DomainContext boundaries.")]
        public void AccessEntityCollections()
        {
            // Add cross-domain context reference
            this.CustomerDomainService.AddReference(typeof(City), this.CityDomainContext);

            // Load both data sources
            this.EnqueueLoadReferencedTypes();
            this.EnqueueLoadCustomers();

            // Verify EntityCollection access.
            this.EnqueueCallback(
                () =>
                {
                    // The customer data can change over time, but it should always be >0 for testing.
                    Assert.IsTrue(this.CustomerDomainService.MockCustomers.Count > 0);

                    foreach (var customer in this.CustomerDomainService.MockCustomers)
                    {
                        foreach (City city in customer.PreviousResidences)
                        {
                            // Verify the cities exist in our referenced data
                            Assert.IsNotNull(city);
                            Assert.IsTrue(this.CityDomainContext.Cities.Contains<City>(city));
                        }
                    }
                });

            this.EnqueueTestComplete();
        }

        /// <summary>
        /// Verifies cross-referenced EntityCollections are empty when the necessary DomainContext reference has not been registered.
        /// </summary>
        [TestMethod, Asynchronous]
        [TestDescription("Verifies cross-referenced EntityCollections are empty when the necessary DomainContext reference has not been registered.")]
        public void AccessEntityCollectionsWithoutReference()
        {
            // Load both data sources
            this.EnqueueLoadReferencedTypes();
            this.EnqueueLoadCustomers();

            // Examine the city collections on both providers (should be identical)
            this.EnqueueCallback(
                () =>
                {
                    // The customer data can change over time, but it should always be >0 for testing.
                    Assert.IsTrue(this.CustomerDomainService.MockCustomers.Count > 0);

                    foreach (var customer in this.CustomerDomainService.MockCustomers)
                    {
                        ExceptionHelper.ExpectInvalidOperationException(
                            () =>
                            {
                                // Attempting to access an EntityCollection property should throw an InvalidOperationException.
                                int i = customer.PreviousResidences.Count;
                            },
                            string.Format(CultureInfo.CurrentCulture, Resource.EntityContainerDoesntContainEntityType, typeof(City)));
                    }
                });

            this.EnqueueTestComplete();
        }

        /// <summary>
        /// Verifies cross-referenced EntityCollections are empty when the referenced source has not been loaded.
        /// </summary>
        [TestMethod, Asynchronous]
        [TestDescription("Verifies cross-referenced EntityCollections are empty when the referenced source has not been loaded.")]
        public void AccessEmptyEntityCollections()
        {
            // Add cross-domain context reference
            this.CustomerDomainService.AddReference(typeof(City), this.CityDomainContext);

            // Load only customer data source
            this.EnqueueLoadCustomers();

            // Examine the cross-referenced entity collection
            this.EnqueueCallback(
                () =>
                {
                    // The customer data can change over time, but it should always be >0 for testing.
                    Assert.IsTrue(this.CustomerDomainService.MockCustomers.Count > 0);

                    foreach (var customer in this.CustomerDomainService.MockCustomers)
                    {
                        // Because we didn't load our referenced source, this should be 0.
                        Assert.AreEqual<int>(0, customer.PreviousResidences.Count);
                    }
                });

            this.EnqueueTestComplete();
        }

        /// <summary>
        /// Verifies EntityRefs can be accessed across DomainContext boundaries.
        /// </summary>
        [TestMethod, Asynchronous]
        [TestDescription("Verifies EntityRefs can be accessed across DomainContext boundaries.")]
        public void AccessEntityRef()
        {
            // Add cross-domain context reference
            this.CustomerDomainService.AddReference(typeof(City), this.CityDomainContext);

            // Load both data sources
            this.EnqueueLoadReferencedTypes();
            this.EnqueueLoadCustomers();

            // Examine the customer entities
            this.EnqueueCallback(
                () =>
                {
                    // The customer data can change over time, but it should always be >0 for testing.
                    Assert.IsTrue(this.CustomerDomainService.MockCustomers.Count > 0);

                    foreach (var customer in this.CustomerDomainService.MockCustomers)
                    {
                        Assert.IsNotNull(customer.City);
                    }
                });

            this.EnqueueTestComplete();
        }

        ///// <summary>
        ///// Verifies the generated setter for cross context EntityRefs behaves as expected.
        ///// </summary>
        [TestMethod, Asynchronous]
        [WorkItem(871311)]
        [TestDescription("Verifies the generated setter for cross context EntityRefs behaves as expected.")]
        public void AccessEntityRef_Setter()
        {
            // Add cross-domain context reference
            this.CustomerDomainService.AddReference(typeof(City), this.CityDomainContext);

            // Load both data sources
            this.EnqueueLoadReferencedTypes();
            this.EnqueueLoadCustomers();

            this.EnqueueCallback(
                () =>
                {
                    MockCustomer cust = this.CustomerDomainService.MockCustomers.First();
                    Assert.IsTrue(cust.City != null);

                    int count = 0;
                    cust.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == "City")
                        {
                            count++;
                        }
                    };

                    // set to a new valid city and validate
                    City newCity = this.CityDomainContext.Cities.First(p => p.Name != cust.City.Name);
                    cust.City = newCity;
                    Assert.AreEqual(newCity.Name, cust.CityName);
                    Assert.AreEqual(newCity.StateName, cust.StateName);
                    Assert.AreEqual(1, count);

                    // set to null and validate results
                    count = 0;
                    cust.City = null;
                    Assert.AreEqual(null, cust.CityName);
                    Assert.AreEqual(null, cust.StateName);
                    Assert.AreEqual(1, count);
                });

            this.EnqueueTestComplete();
        }

        /// <summary>
        /// Verifies EntityRefs are null when the referenced source has not been loaded.
        /// </summary>
        [TestMethod, Asynchronous]
        [TestDescription("Verifies EntityRefs can be accessed across DomainContext boundaries.")]
        public void AccessNullEntityRef()
        {
            // Add cross-domain context reference
            this.CustomerDomainService.AddReference(typeof(City), this.CityDomainContext);

            // Load only our customer data source
            this.EnqueueLoadCustomers();

            // Examine the customer entities
            this.EnqueueCallback(
                () =>
                {
                    // The customer data can change over time, but it should always be >0 for testing.
                    Assert.IsTrue(this.CustomerDomainService.MockCustomers.Count > 0);

                    foreach (var customer in this.CustomerDomainService.MockCustomers)
                    {
                        Assert.IsNull(customer.City);
                    }
                });

            this.EnqueueTestComplete();
        }

        /// <summary>
        /// Verifies EntityRefs are not loaded if the proper DomainContext is not added as a reference.
        /// </summary>
        [TestMethod, Asynchronous]
        [TestDescription("Verifies EntityRefs are not loaded if the proper DomainContext is not added as a reference.")]
        public void AccessEntityRefWithoutReference()
        {
            // Load only our customer data source
            this.EnqueueLoadCustomers();

            // Examine the customer entities
            this.EnqueueCallback(
                () =>
                {
                    // The customer data can change over time, but it should always be >0 for testing.
                    Assert.IsTrue(this.CustomerDomainService.MockCustomers.Count > 0);

                    foreach (var customer in this.CustomerDomainService.MockCustomers)
                    {
                        ExceptionHelper.ExpectInvalidOperationException(
                            () =>
                            {
                                // Simply attempt to access the State property,
                                // it should throw an InvalidOperationException.
                                object o = customer.City;
                            },
                            string.Format(CultureInfo.CurrentCulture, Resource.EntityContainerDoesntContainEntityType, typeof(City)));
                    }
                });

            this.EnqueueTestComplete();
        }

        /// <summary>
        /// Verifies EntityCollection references are treated as read-only.
        /// </summary>
        [TestMethod, Asynchronous]
        [TestDescription("Verifies EntityCollection references are treated as read-only.")]
        public void ReadOnlyEntityCollection()
        {
            // Add cross-domain context reference
            this.CustomerDomainService.AddReference(typeof(City), this.CityDomainContext);

            // Load data
            this.EnqueueLoadCustomers();
            this.EnqueueLoadReferencedTypes();
            this.EnqueueCallback(
                () =>
                {
                    // Grab a reference to the first customer and city entity
                    var customer = this.CustomerDomainService.MockCustomers.First();
                    var city = customer.PreviousResidences.First();

                    // Attempt to add to an EntityCollection
                    ExceptionHelper.ExpectInvalidOperationException(
                        () => customer.PreviousResidences.Add(new City()),
                        Resource.EntityCollection_ModificationNotAllowedForExternalReference);

                    // Attempt to remove from an EntityCollection
                    ExceptionHelper.ExpectInvalidOperationException(
                        () => customer.PreviousResidences.Remove(city),
                        Resource.EntityCollection_ModificationNotAllowedForExternalReference);
                });

            this.EnqueueTestComplete();
        }

        /// <summary>
        /// Verifies EntityRef PropertyChanged events are raised correctly.
        /// </summary>
        [TestMethod, Asynchronous]
        [TestDescription("Verifies EntityRef PropertyChanged events are raised correctly.")]
        public void PropertyChangedNotifications()
        {
            MockCustomer customer = null;
            int collectionChanged = 0;
            int propertyChanged = 0;

            // Add cross-domain context reference
            this.CustomerDomainService.AddReference(typeof(City), this.CityDomainContext);

            // Load data
            this.EnqueueLoadCustomers();
            this.EnqueueCallback(
                () =>
                {
                    customer = this.CustomerDomainService.MockCustomers.First();

                    // Should be null/empty before we load reference data
                    Assert.IsNull(customer.City);
                    Assert.AreEqual<int>(0, customer.PreviousResidences.Count);

                    // Sign up for collection changed events, increment counter each time.
                    ((INotifyCollectionChanged)customer.PreviousResidences).CollectionChanged +=
                        (s, a) => collectionChanged += (a.Action == NotifyCollectionChangedAction.Add ? 1 : 0);

                    // Sign up for property changed events, increment counter each time.
                    customer.PropertyChanged +=
                        (s, a) => propertyChanged += (a.PropertyName == "City" ? 1 : 0);
                });

            // Load referenced data (after event handlers attached)
            this.EnqueueLoadReferencedTypes();

            // Verify we received the proper number of notifications
            this.EnqueueConditional(() =>
                1 == propertyChanged &&
                customer.PreviousResidences.Count == collectionChanged);
            this.EnqueueTestComplete();
        }
        #endregion

        #region Test Helper Methods

        /// <summary>
        /// Enqueues loading of externally referenced entities.
        /// </summary>
        private void EnqueueLoadReferencedTypes()
        {
            this.EnqueueCallback(() => this.CityDomainContext.Load(this.CityDomainContext.GetCitiesQuery(), this.CitiesLoadedCallback, null));
            this.EnqueueConditional(() => this.CitiesLoaded.HasValue);
            this.EnqueueCallback(() =>
                {
                    if (!this.CitiesLoaded.Value)
                    {
                        Assert.Fail("CitiesDomainService failed to load.");
                    }
                });
        }

        /// <summary>
        /// Enqueues loading of customer entities.
        /// </summary>
        private void EnqueueLoadCustomers()
        {
            this.EnqueueCallback(() => this.CustomerDomainService.Load(this.CustomerDomainService.GetCustomersQuery(), this.CustomersLoadedCallback, null));
            this.EnqueueConditional(() => this.CustomersLoaded.HasValue); 
            this.EnqueueCallback(() =>
                {
                    if (!this.CustomersLoaded.Value)
                    {
                        Assert.Fail("CustomerDomainService failed to load.");
                    }
                });
        }

        /// <summary>
        /// Callback invoked when <see cref="MockCustomer"/> entities are loaded.
        /// </summary>
        private void CustomersLoadedCallback(LoadOperation<MockCustomer> op)
        {
            this.CustomersLoaded = (!op.IsCanceled && op.Error == null);
        }

        /// <summary>
        /// Callback invoked when <see cref="City"/> entities are loaded.
        /// </summary>
        private void CitiesLoadedCallback(LoadOperation<City> op)
        {
            this.CitiesLoaded = (!op.IsCanceled && op.Error == null);
        }
        #endregion
    }
}
