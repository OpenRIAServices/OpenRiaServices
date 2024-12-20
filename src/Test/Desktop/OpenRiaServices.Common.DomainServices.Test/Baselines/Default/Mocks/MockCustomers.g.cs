//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace TestDomainServices
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Threading.Tasks;
    using OpenRiaServices;
    using OpenRiaServices.Client;
    using OpenRiaServices.Client.Authentication;
    
    
    /// <summary>
    /// The 'MockCustomer' entity class.
    /// </summary>
    [DataContract(Namespace="http://schemas.datacontract.org/2004/07/TestDomainServices")]
    public sealed partial class MockCustomer : Entity
    {
        
        private EntityRef<global::Cities.City> _city;
        
        private string _cityName;
        
        private int _customerId;
        
        private EntityCollection<global::Cities.City> _previousResidences;
        
        private string _stateName;
        
        #region Extensibility Method Definitions

        /// <summary>
        /// This method is invoked from the constructor once initialization is complete and
        /// can be used for further object setup.
        /// </summary>
        partial void OnCreated();
        partial void OnCityNameChanging(string value);
        partial void OnCityNameChanged();
        partial void OnCustomerIdChanging(int value);
        partial void OnCustomerIdChanged();
        partial void OnStateNameChanging(string value);
        partial void OnStateNameChanged();
        partial void OnMockCustomerCustomMethodInvoking(string expectedStateName, string expectedOriginalStateName);
        partial void OnMockCustomerCustomMethodInvoked();

        #endregion
        
        
        /// <summary>
        /// Initializes a new instance of the <see cref="MockCustomer"/> class.
        /// </summary>
        public MockCustomer()
        {
            this.OnCreated();
        }
        
        /// <summary>
        /// Gets or sets the associated <see cref="City"/> entity.
        /// </summary>
        [EntityAssociation("Customer_City", new string[] {
                "CityName",
                "StateName"}, new string[] {
                "Name",
                "StateName"}, IsForeignKey=true)]
        [ExternalReference()]
        public global::Cities.City City
        {
            get
            {
                if ((this._city == null))
                {
                    this._city = new EntityRef<global::Cities.City>(this, "City", this.FilterCity);
                }
                return this._city.Entity;
            }
            set
            {
                global::Cities.City previous = this.City;
                if ((previous != value))
                {
                    this.ValidateProperty("City", value);
                    if ((value != null))
                    {
                        this.CityName = value.Name;
                        this.StateName = value.StateName;
                    }
                    else
                    {
                        this.CityName = default(string);
                        this.StateName = default(string);
                    }
                }
            }
        }
        
        /// <summary>
        /// Gets or sets the 'CityName' value.
        /// </summary>
        [DataMember()]
        [RoundtripOriginal()]
        public string CityName
        {
            get
            {
                return this._cityName;
            }
            set
            {
                if ((this._cityName != value))
                {
                    this.OnCityNameChanging(value);
                    this.RaiseDataMemberChanging("CityName");
                    this.ValidateProperty("CityName", value);
                    this._cityName = value;
                    this.RaiseDataMemberChanged("CityName");
                    this.OnCityNameChanged();
                }
            }
        }
        
        /// <summary>
        /// Gets or sets the 'CustomerId' value.
        /// </summary>
        [DataMember()]
        [Editable(false, AllowInitialValue=true)]
        [Key()]
        [RoundtripOriginal()]
        public int CustomerId
        {
            get
            {
                return this._customerId;
            }
            set
            {
                if ((this._customerId != value))
                {
                    this.OnCustomerIdChanging(value);
                    this.ValidateProperty("CustomerId", value);
                    this._customerId = value;
                    this.RaisePropertyChanged("CustomerId");
                    this.OnCustomerIdChanged();
                }
            }
        }
        
        /// <summary>
        /// Gets the collection of associated <see cref="City"/> entity instances.
        /// </summary>
        [EntityAssociation("Customer_PreviousResidences", "StateName", "StateName")]
        [ExternalReference()]
        public EntityCollection<global::Cities.City> PreviousResidences
        {
            get
            {
                if ((this._previousResidences == null))
                {
                    this._previousResidences = new EntityCollection<global::Cities.City>(this, "PreviousResidences", this.FilterPreviousResidences);
                }
                return this._previousResidences;
            }
        }
        
        /// <summary>
        /// Gets or sets the 'StateName' value.
        /// </summary>
        [DataMember()]
        [RoundtripOriginal()]
        public string StateName
        {
            get
            {
                return this._stateName;
            }
            set
            {
                if ((this._stateName != value))
                {
                    this.OnStateNameChanging(value);
                    this.RaiseDataMemberChanging("StateName");
                    this.ValidateProperty("StateName", value);
                    this._stateName = value;
                    this.RaiseDataMemberChanged("StateName");
                    this.OnStateNameChanged();
                }
            }
        }
        
        /// <summary>
        /// Gets a value indicating whether the 'MockCustomerCustomMethod' action has been invoked on this entity.
        /// </summary>
        [Display(AutoGenerateField=false)]
        public bool IsMockCustomerCustomMethodInvoked
        {
            get
            {
                return base.IsActionInvoked("MockCustomerCustomMethod");
            }
        }
        
        /// <summary>
        /// Gets a value indicating whether the 'MockCustomerCustomMethod' method can be invoked on this entity.
        /// </summary>
        [Display(AutoGenerateField=false)]
        public bool CanMockCustomerCustomMethod
        {
            get
            {
                return base.CanInvokeAction("MockCustomerCustomMethod");
            }
        }
        
        private bool FilterCity(global::Cities.City entity)
        {
            return ((entity.Name == this.CityName) 
                        && (entity.StateName == this.StateName));
        }
        
        private bool FilterPreviousResidences(global::Cities.City entity)
        {
            return (entity.StateName == this.StateName);
        }
        
        /// <summary>
        /// Computes a value from the key fields that uniquely identifies this entity instance.
        /// </summary>
        /// <returns>An object instance that uniquely identifies this entity instance.</returns>
        public override object GetIdentity()
        {
            return this._customerId;
        }
        
        /// <summary>
        /// Invokes the 'MockCustomerCustomMethod' action on this entity.
        /// </summary>
        /// <param name="expectedStateName">The value to pass to the server method's 'expectedStateName' parameter.</param>
        /// <param name="expectedOriginalStateName">The value to pass to the server method's 'expectedOriginalStateName' parameter.</param>
        [EntityAction("MockCustomerCustomMethod", AllowMultipleInvocations=false)]
        public void MockCustomerCustomMethod(string expectedStateName, string expectedOriginalStateName)
        {
            this.OnMockCustomerCustomMethodInvoking(expectedStateName, expectedOriginalStateName);
            base.InvokeAction("MockCustomerCustomMethod", expectedStateName, expectedOriginalStateName);
            this.OnMockCustomerCustomMethodInvoked();
        }
    }
    
    /// <summary>
    /// The DomainContext corresponding to the 'MockCustomerDomainService' DomainService.
    /// </summary>
    public sealed partial class MockCustomerDomainContext : DomainContext
    {
        
        #region Extensibility Method Definitions

        /// <summary>
        /// This method is invoked from the constructor once initialization is complete and
        /// can be used for further object setup.
        /// </summary>
        partial void OnCreated();

        #endregion
        
        
        /// <summary>
        /// Initializes a new instance of the <see cref="MockCustomerDomainContext"/> class.
        /// </summary>
        public MockCustomerDomainContext() : 
                this(new Uri("TestDomainServices-MockCustomerDomainService.svc", UriKind.Relative))
        {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="MockCustomerDomainContext"/> class with the specified service URI.
        /// </summary>
        /// <param name="serviceUri">The MockCustomerDomainService service URI.</param>
        public MockCustomerDomainContext(Uri serviceUri) : 
                this(DomainContext.CreateDomainClient(typeof(IMockCustomerDomainServiceContract), serviceUri, false))
        {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="MockCustomerDomainContext"/> class with the specified <paramref name="domainClient"/>.
        /// </summary>
        /// <param name="domainClient">The DomainClient instance to use for this DomainContext.</param>
        public MockCustomerDomainContext(DomainClient domainClient) : 
                base(domainClient)
        {
            this.OnCreated();
        }
        
        /// <summary>
        /// Gets the set of <see cref="MockCustomer"/> entity instances that have been loaded into this <see cref="MockCustomerDomainContext"/> instance.
        /// </summary>
        public EntitySet<MockCustomer> MockCustomers
        {
            get
            {
                return base.EntityContainer.GetEntitySet<MockCustomer>();
            }
        }
        
        /// <summary>
        /// Gets the set of <see cref="MockReport"/> entity instances that have been loaded into this <see cref="MockCustomerDomainContext"/> instance.
        /// </summary>
        public EntitySet<MockReport> MockReports
        {
            get
            {
                return base.EntityContainer.GetEntitySet<MockReport>();
            }
        }
        
        /// <summary>
        /// Gets an EntityQuery instance that can be used to load <see cref="MockCustomer"/> entity instances using the 'GetCustomers' query.
        /// </summary>
        /// <returns>An EntityQuery that can be loaded to retrieve <see cref="MockCustomer"/> entity instances.</returns>
        public EntityQuery<MockCustomer> GetCustomersQuery()
        {
            this.ValidateMethod("GetCustomersQuery", null);
            return base.CreateQuery<MockCustomer>("GetCustomers", null, false, true);
        }
        
        /// <summary>
        /// Gets an EntityQuery instance that can be used to load <see cref="MockReport"/> entity instances using the 'GetReports' query.
        /// </summary>
        /// <returns>An EntityQuery that can be loaded to retrieve <see cref="MockReport"/> entity instances.</returns>
        public EntityQuery<MockReport> GetReportsQuery()
        {
            this.ValidateMethod("GetReportsQuery", null);
            return base.CreateQuery<MockReport>("GetReports", null, false, true);
        }
        
        /// <summary>
        /// Invokes the 'MockCustomerCustomMethod' method of the specified <see cref="MockCustomer"/> entity.
        /// </summary>
        /// <param name="current">The <see cref="MockCustomer"/> entity instance.</param>
        /// <param name="expectedStateName">The value for the 'expectedStateName' parameter for this action.</param>
        /// <param name="expectedOriginalStateName">The value for the 'expectedOriginalStateName' parameter for this action.</param>
        public void MockCustomerCustomMethod(MockCustomer current, string expectedStateName, string expectedOriginalStateName)
        {
            current.MockCustomerCustomMethod(expectedStateName, expectedOriginalStateName);
        }
        
        /// <summary>
        /// Invokes the 'MockReportCustomMethod' method of the specified <see cref="MockReport"/> entity.
        /// </summary>
        /// <param name="current">The <see cref="MockReport"/> entity instance.</param>
        public void MockReportCustomMethod(MockReport current)
        {
            current.MockReportCustomMethod();
        }
        
        /// <summary>
        /// Creates a new EntityContainer for this DomainContext's EntitySets.
        /// </summary>
        /// <returns>A new container instance.</returns>
        protected override EntityContainer CreateEntityContainer()
        {
            return new MockCustomerDomainContextEntityContainer();
        }
        
        /// <summary>
        /// Service contract for the 'MockCustomerDomainService' DomainService.
        /// </summary>
        public interface IMockCustomerDomainServiceContract
        {
            
            /// <summary>
            /// Asynchronously invokes the 'GetCustomers' operation.
            /// </summary>
            /// <param name="callback">Callback to invoke on completion.</param>
            /// <param name="asyncState">Optional state object.</param>
            /// <returns>An IAsyncResult that can be used to monitor the request.</returns>
            [HasSideEffects(false)]
            IAsyncResult BeginGetCustomers(AsyncCallback callback, object asyncState);
            
            /// <summary>
            /// Completes the asynchronous operation begun by 'BeginGetCustomers'.
            /// </summary>
            /// <param name="result">The IAsyncResult returned from 'BeginGetCustomers'.</param>
            /// <returns>The 'QueryResult' returned from the 'GetCustomers' operation.</returns>
            QueryResult<MockCustomer> EndGetCustomers(IAsyncResult result);
            
            /// <summary>
            /// Asynchronously invokes the 'GetReports' operation.
            /// </summary>
            /// <param name="callback">Callback to invoke on completion.</param>
            /// <param name="asyncState">Optional state object.</param>
            /// <returns>An IAsyncResult that can be used to monitor the request.</returns>
            [HasSideEffects(false)]
            IAsyncResult BeginGetReports(AsyncCallback callback, object asyncState);
            
            /// <summary>
            /// Completes the asynchronous operation begun by 'BeginGetReports'.
            /// </summary>
            /// <param name="result">The IAsyncResult returned from 'BeginGetReports'.</param>
            /// <returns>The 'QueryResult' returned from the 'GetReports' operation.</returns>
            QueryResult<MockReport> EndGetReports(IAsyncResult result);
            
            /// <summary>
            /// Asynchronously invokes the 'SubmitChanges' operation.
            /// </summary>
            /// <param name="changeSet">The change-set to submit.</param>
            /// <param name="callback">Callback to invoke on completion.</param>
            /// <param name="asyncState">Optional state object.</param>
            /// <returns>An IAsyncResult that can be used to monitor the request.</returns>
            IAsyncResult BeginSubmitChanges(IEnumerable<ChangeSetEntry> changeSet, AsyncCallback callback, object asyncState);
            
            /// <summary>
            /// Completes the asynchronous operation begun by 'BeginSubmitChanges'.
            /// </summary>
            /// <param name="result">The IAsyncResult returned from 'BeginSubmitChanges'.</param>
            /// <returns>The collection of change-set entry elements returned from 'SubmitChanges'.</returns>
            IEnumerable<ChangeSetEntry> EndSubmitChanges(IAsyncResult result);
        }
        
        internal sealed class MockCustomerDomainContextEntityContainer : EntityContainer
        {
            
            public MockCustomerDomainContextEntityContainer()
            {
                this.CreateEntitySet<MockCustomer>(EntitySetOperations.Edit);
                this.CreateEntitySet<MockReport>(EntitySetOperations.Edit);
            }
        }
    }
    
    /// <summary>
    /// The 'MockReport' entity class.
    /// </summary>
    [DataContract(Namespace="Mock.Models", Name="MR")]
    [RoundtripOriginal()]
    public sealed partial class MockReport : Entity
    {
        
        private EntityRef<MockCustomer> _customer;
        
        private int _customerId;
        
        private MockReportBody _reportBody;
        
        private int _reportElementFieldId;
        
        private string _reportTitle;
        
        private DateTime _start;
        
        private string _state;
        
        #region Extensibility Method Definitions

        /// <summary>
        /// This method is invoked from the constructor once initialization is complete and
        /// can be used for further object setup.
        /// </summary>
        partial void OnCreated();
        partial void OnCustomerIdChanging(int value);
        partial void OnCustomerIdChanged();
        partial void OnReportBodyChanging(MockReportBody value);
        partial void OnReportBodyChanged();
        partial void OnReportElementFieldIdChanging(int value);
        partial void OnReportElementFieldIdChanged();
        partial void OnReportTitleChanging(string value);
        partial void OnReportTitleChanged();
        partial void OnStartChanging(DateTime value);
        partial void OnStartChanged();
        partial void OnStateChanging(string value);
        partial void OnStateChanged();
        partial void OnMockReportCustomMethodInvoking();
        partial void OnMockReportCustomMethodInvoked();

        #endregion
        
        
        /// <summary>
        /// Initializes a new instance of the <see cref="MockReport"/> class.
        /// </summary>
        public MockReport()
        {
            this.OnCreated();
        }
        
        /// <summary>
        /// Gets or sets the associated <see cref="MockCustomer"/> entity.
        /// </summary>
        [EntityAssociation("R_C", "CustomerId", "CustomerId")]
        public MockCustomer Customer
        {
            get
            {
                if ((this._customer == null))
                {
                    this._customer = new EntityRef<MockCustomer>(this, "Customer", this.FilterCustomer);
                }
                return this._customer.Entity;
            }
            set
            {
                MockCustomer previous = this.Customer;
                if ((previous != value))
                {
                    this.ValidateProperty("Customer", value);
                    this._customer.Entity = value;
                    this.RaisePropertyChanged("Customer");
                }
            }
        }
        
        /// <summary>
        /// Gets or sets the 'CustomerId' value.
        /// </summary>
        [DataMember(Name="CId")]
        public int CustomerId
        {
            get
            {
                return this._customerId;
            }
            set
            {
                if ((this._customerId != value))
                {
                    this.OnCustomerIdChanging(value);
                    this.RaiseDataMemberChanging("CustomerId");
                    this.ValidateProperty("CustomerId", value);
                    this._customerId = value;
                    this.RaiseDataMemberChanged("CustomerId");
                    this.OnCustomerIdChanged();
                }
            }
        }
        
        /// <summary>
        /// Gets or sets the 'ReportBody' value.
        /// </summary>
        [DataMember(Name="Data")]
        [Display(AutoGenerateField=false)]
        public MockReportBody ReportBody
        {
            get
            {
                return this._reportBody;
            }
            set
            {
                if ((this._reportBody != value))
                {
                    this.OnReportBodyChanging(value);
                    this.RaiseDataMemberChanging("ReportBody");
                    this.ValidateProperty("ReportBody", value);
                    this._reportBody = value;
                    this.RaiseDataMemberChanged("ReportBody");
                    this.OnReportBodyChanged();
                }
            }
        }
        
        /// <summary>
        /// Gets or sets the 'ReportElementFieldId' value.
        /// </summary>
        [DataMember(Name="REFId")]
        [Editable(false, AllowInitialValue=true)]
        [Key()]
        public int ReportElementFieldId
        {
            get
            {
                return this._reportElementFieldId;
            }
            set
            {
                if ((this._reportElementFieldId != value))
                {
                    this.OnReportElementFieldIdChanging(value);
                    this.ValidateProperty("ReportElementFieldId", value);
                    this._reportElementFieldId = value;
                    this.RaisePropertyChanged("ReportElementFieldId");
                    this.OnReportElementFieldIdChanged();
                }
            }
        }
        
        /// <summary>
        /// Gets or sets the 'ReportTitle' value.
        /// </summary>
        [DataMember(IsRequired=true, Name="Title")]
        public string ReportTitle
        {
            get
            {
                return this._reportTitle;
            }
            set
            {
                if ((this._reportTitle != value))
                {
                    this.OnReportTitleChanging(value);
                    this.RaiseDataMemberChanging("ReportTitle");
                    this.ValidateProperty("ReportTitle", value);
                    this._reportTitle = value;
                    this.RaiseDataMemberChanged("ReportTitle");
                    this.OnReportTitleChanged();
                }
            }
        }
        
        /// <summary>
        /// Gets or sets the 'Start' value.
        /// </summary>
        [DataMember(Name="Str")]
        [Editable(false)]
        public DateTime Start
        {
            get
            {
                return this._start;
            }
            set
            {
                if ((this._start != value))
                {
                    this.OnStartChanging(value);
                    this.ValidateProperty("Start", value);
                    this._start = value;
                    this.RaisePropertyChanged("Start");
                    this.OnStartChanged();
                }
            }
        }
        
        /// <summary>
        /// Gets or sets the 'State' value.
        /// </summary>
        [DataMember(Name="SN")]
        [Editable(false)]
        public string State
        {
            get
            {
                return this._state;
            }
            set
            {
                if ((this._state != value))
                {
                    this.OnStateChanging(value);
                    this.ValidateProperty("State", value);
                    this._state = value;
                    this.RaisePropertyChanged("State");
                    this.OnStateChanged();
                }
            }
        }
        
        /// <summary>
        /// Gets a value indicating whether the 'MockReportCustomMethod' action has been invoked on this entity.
        /// </summary>
        [Display(AutoGenerateField=false)]
        public bool IsMockReportCustomMethodInvoked
        {
            get
            {
                return base.IsActionInvoked("MockReportCustomMethod");
            }
        }
        
        /// <summary>
        /// Gets a value indicating whether the 'MockReportCustomMethod' method can be invoked on this entity.
        /// </summary>
        [Display(AutoGenerateField=false)]
        public bool CanMockReportCustomMethod
        {
            get
            {
                return base.CanInvokeAction("MockReportCustomMethod");
            }
        }
        
        private bool FilterCustomer(MockCustomer entity)
        {
            return (entity.CustomerId == this.CustomerId);
        }
        
        /// <summary>
        /// Computes a value from the key fields that uniquely identifies this entity instance.
        /// </summary>
        /// <returns>An object instance that uniquely identifies this entity instance.</returns>
        public override object GetIdentity()
        {
            return this._reportElementFieldId;
        }
        
        /// <summary>
        /// Invokes the 'MockReportCustomMethod' action on this entity.
        /// </summary>
        [EntityAction("MockReportCustomMethod", AllowMultipleInvocations=false)]
        public void MockReportCustomMethod()
        {
            this.OnMockReportCustomMethodInvoking();
            base.InvokeAction("MockReportCustomMethod");
            this.OnMockReportCustomMethodInvoked();
        }
    }
    
    /// <summary>
    /// The 'MockReportBody' class.
    /// </summary>
    [DataContract(Namespace="Mock.Models", Name="MRB")]
    [RoundtripOriginal()]
    public sealed partial class MockReportBody : ComplexObject
    {
        
        private string _report;
        
        private DateTime _timeEntered;
        
        #region Extensibility Method Definitions

        /// <summary>
        /// This method is invoked from the constructor once initialization is complete and
        /// can be used for further object setup.
        /// </summary>
        partial void OnCreated();
        partial void OnReportChanging(string value);
        partial void OnReportChanged();
        partial void OnTimeEnteredChanging(DateTime value);
        partial void OnTimeEnteredChanged();

        #endregion
        
        
        /// <summary>
        /// Initializes a new instance of the <see cref="MockReportBody"/> class.
        /// </summary>
        public MockReportBody()
        {
            this.OnCreated();
        }
        
        /// <summary>
        /// Gets or sets the 'Report' value.
        /// </summary>
        [DataMember(EmitDefaultValue=false, Name="Body")]
        public string Report
        {
            get
            {
                return this._report;
            }
            set
            {
                if ((this._report != value))
                {
                    this.OnReportChanging(value);
                    this.RaiseDataMemberChanging("Report");
                    this.ValidateProperty("Report", value);
                    this._report = value;
                    this.RaiseDataMemberChanged("Report");
                    this.OnReportChanged();
                }
            }
        }
        
        /// <summary>
        /// Gets or sets the 'TimeEntered' value.
        /// </summary>
        [DataMember(Name="EntryDate")]
        public DateTime TimeEntered
        {
            get
            {
                return this._timeEntered;
            }
            set
            {
                if ((this._timeEntered != value))
                {
                    this.OnTimeEnteredChanging(value);
                    this.RaiseDataMemberChanging("TimeEntered");
                    this.ValidateProperty("TimeEntered", value);
                    this._timeEntered = value;
                    this.RaiseDataMemberChanged("TimeEntered");
                    this.OnTimeEnteredChanged();
                }
            }
        }
    }
}
