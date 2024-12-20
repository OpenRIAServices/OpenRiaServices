//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace DataTests.Inheritance.LTS
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Threading;
    using System.Threading.Tasks;
    using OpenRiaServices;
    using OpenRiaServices.Client;
    using OpenRiaServices.Client.Authentication;
    
    
    /// <summary>
    /// The 'A' entity class.
    /// </summary>
    [DataContract(Namespace="http://schemas.datacontract.org/2004/07/DataTests.Inheritance.LTS")]
    [KnownType(typeof(B))]
    [KnownType(typeof(C))]
    public abstract partial class A : Entity
    {
        
        private string _address;
        
        private string _city;
        
        private string _companyName;
        
        private string _contactName;
        
        private string _contactTitle;
        
        private string _country;
        
        private string _customerID;
        
        private string _postalCode;
        
        private string _region;
        
        #region Extensibility Method Definitions

        /// <summary>
        /// This method is invoked from the constructor once initialization is complete and
        /// can be used for further object setup.
        /// </summary>
        partial void OnCreated();
        partial void OnAddressChanging(string value);
        partial void OnAddressChanged();
        partial void OnCityChanging(string value);
        partial void OnCityChanged();
        partial void OnCompanyNameChanging(string value);
        partial void OnCompanyNameChanged();
        partial void OnContactNameChanging(string value);
        partial void OnContactNameChanged();
        partial void OnContactTitleChanging(string value);
        partial void OnContactTitleChanged();
        partial void OnCountryChanging(string value);
        partial void OnCountryChanged();
        partial void OnCustomerIDChanging(string value);
        partial void OnCustomerIDChanged();
        partial void OnPostalCodeChanging(string value);
        partial void OnPostalCodeChanged();
        partial void OnRegionChanging(string value);
        partial void OnRegionChanged();

        #endregion
        
        
        /// <summary>
        /// Initializes a new instance of the <see cref="A"/> class.
        /// </summary>
        protected A()
        {
            this.OnCreated();
        }
        
        /// <summary>
        /// Gets or sets the 'Address' value.
        /// </summary>
        [ConcurrencyCheck()]
        [DataMember()]
        [RoundtripOriginal()]
        [StringLength(60)]
        public string Address
        {
            get
            {
                return this._address;
            }
            set
            {
                if ((this._address != value))
                {
                    this.OnAddressChanging(value);
                    this.RaiseDataMemberChanging("Address");
                    this.ValidateProperty("Address", value);
                    this._address = value;
                    this.RaiseDataMemberChanged("Address");
                    this.OnAddressChanged();
                }
            }
        }
        
        /// <summary>
        /// Gets or sets the 'City' value.
        /// </summary>
        [ConcurrencyCheck()]
        [DataMember()]
        [RoundtripOriginal()]
        [StringLength(15)]
        public string City
        {
            get
            {
                return this._city;
            }
            set
            {
                if ((this._city != value))
                {
                    this.OnCityChanging(value);
                    this.RaiseDataMemberChanging("City");
                    this.ValidateProperty("City", value);
                    this._city = value;
                    this.RaiseDataMemberChanged("City");
                    this.OnCityChanged();
                }
            }
        }
        
        /// <summary>
        /// Gets or sets the 'CompanyName' value.
        /// </summary>
        [ConcurrencyCheck()]
        [DataMember()]
        [Required()]
        [RoundtripOriginal()]
        [StringLength(40)]
        public string CompanyName
        {
            get
            {
                return this._companyName;
            }
            set
            {
                if ((this._companyName != value))
                {
                    this.OnCompanyNameChanging(value);
                    this.RaiseDataMemberChanging("CompanyName");
                    this.ValidateProperty("CompanyName", value);
                    this._companyName = value;
                    this.RaiseDataMemberChanged("CompanyName");
                    this.OnCompanyNameChanged();
                }
            }
        }
        
        /// <summary>
        /// Gets or sets the 'ContactName' value.
        /// </summary>
        [ConcurrencyCheck()]
        [DataMember()]
        [RoundtripOriginal()]
        [StringLength(30)]
        public string ContactName
        {
            get
            {
                return this._contactName;
            }
            set
            {
                if ((this._contactName != value))
                {
                    this.OnContactNameChanging(value);
                    this.RaiseDataMemberChanging("ContactName");
                    this.ValidateProperty("ContactName", value);
                    this._contactName = value;
                    this.RaiseDataMemberChanged("ContactName");
                    this.OnContactNameChanged();
                }
            }
        }
        
        /// <summary>
        /// Gets or sets the 'ContactTitle' value.
        /// </summary>
        [ConcurrencyCheck()]
        [DataMember()]
        [RoundtripOriginal()]
        [StringLength(30)]
        public string ContactTitle
        {
            get
            {
                return this._contactTitle;
            }
            set
            {
                if ((this._contactTitle != value))
                {
                    this.OnContactTitleChanging(value);
                    this.RaiseDataMemberChanging("ContactTitle");
                    this.ValidateProperty("ContactTitle", value);
                    this._contactTitle = value;
                    this.RaiseDataMemberChanged("ContactTitle");
                    this.OnContactTitleChanged();
                }
            }
        }
        
        /// <summary>
        /// Gets or sets the 'Country' value.
        /// </summary>
        [ConcurrencyCheck()]
        [DataMember()]
        [RoundtripOriginal()]
        [StringLength(15)]
        public string Country
        {
            get
            {
                return this._country;
            }
            set
            {
                if ((this._country != value))
                {
                    this.OnCountryChanging(value);
                    this.RaiseDataMemberChanging("Country");
                    this.ValidateProperty("Country", value);
                    this._country = value;
                    this.RaiseDataMemberChanged("Country");
                    this.OnCountryChanged();
                }
            }
        }
        
        /// <summary>
        /// Gets or sets the 'CustomerID' value.
        /// </summary>
        [ConcurrencyCheck()]
        [DataMember()]
        [Editable(false, AllowInitialValue=true)]
        [Key()]
        [Required()]
        [RoundtripOriginal()]
        [StringLength(5)]
        public string CustomerID
        {
            get
            {
                return this._customerID;
            }
            set
            {
                if ((this._customerID != value))
                {
                    this.OnCustomerIDChanging(value);
                    this.ValidateProperty("CustomerID", value);
                    this._customerID = value;
                    this.RaisePropertyChanged("CustomerID");
                    this.OnCustomerIDChanged();
                }
            }
        }
        
        /// <summary>
        /// Gets or sets the 'PostalCode' value.
        /// </summary>
        [ConcurrencyCheck()]
        [DataMember()]
        [RoundtripOriginal()]
        [StringLength(10)]
        public string PostalCode
        {
            get
            {
                return this._postalCode;
            }
            set
            {
                if ((this._postalCode != value))
                {
                    this.OnPostalCodeChanging(value);
                    this.RaiseDataMemberChanging("PostalCode");
                    this.ValidateProperty("PostalCode", value);
                    this._postalCode = value;
                    this.RaiseDataMemberChanged("PostalCode");
                    this.OnPostalCodeChanged();
                }
            }
        }
        
        /// <summary>
        /// Gets or sets the 'Region' value.
        /// </summary>
        [ConcurrencyCheck()]
        [DataMember()]
        [RoundtripOriginal()]
        [StringLength(15)]
        public string Region
        {
            get
            {
                return this._region;
            }
            set
            {
                if ((this._region != value))
                {
                    this.OnRegionChanging(value);
                    this.RaiseDataMemberChanging("Region");
                    this.ValidateProperty("Region", value);
                    this._region = value;
                    this.RaiseDataMemberChanged("Region");
                    this.OnRegionChanged();
                }
            }
        }
        
        /// <summary>
        /// Computes a value from the key fields that uniquely identifies this entity instance.
        /// </summary>
        /// <returns>An object instance that uniquely identifies this entity instance.</returns>
        public override object GetIdentity()
        {
            return this._customerID;
        }
    }
    
    /// <summary>
    /// The 'B' entity class.
    /// </summary>
    [DataContract(Namespace="http://schemas.datacontract.org/2004/07/DataTests.Inheritance.LTS")]
    public sealed partial class B : A
    {
        
        private string _phone;
        
        #region Extensibility Method Definitions

        /// <summary>
        /// This method is invoked from the constructor once initialization is complete and
        /// can be used for further object setup.
        /// </summary>
        partial void OnCreated();
        partial void OnPhoneChanging(string value);
        partial void OnPhoneChanged();

        #endregion
        
        
        /// <summary>
        /// Initializes a new instance of the <see cref="B"/> class.
        /// </summary>
        public B()
        {
            this.OnCreated();
        }
        
        /// <summary>
        /// Gets or sets the 'Phone' value.
        /// </summary>
        [ConcurrencyCheck()]
        [DataMember()]
        [RoundtripOriginal()]
        [StringLength(24)]
        public string Phone
        {
            get
            {
                return this._phone;
            }
            set
            {
                if ((this._phone != value))
                {
                    this.OnPhoneChanging(value);
                    this.RaiseDataMemberChanging("Phone");
                    this.ValidateProperty("Phone", value);
                    this._phone = value;
                    this.RaiseDataMemberChanged("Phone");
                    this.OnPhoneChanged();
                }
            }
        }
    }
    
    /// <summary>
    /// The 'C' entity class.
    /// </summary>
    [DataContract(Namespace="http://schemas.datacontract.org/2004/07/DataTests.Inheritance.LTS")]
    public sealed partial class C : A
    {
        
        private string _fax;
        
        #region Extensibility Method Definitions

        /// <summary>
        /// This method is invoked from the constructor once initialization is complete and
        /// can be used for further object setup.
        /// </summary>
        partial void OnCreated();
        partial void OnFaxChanging(string value);
        partial void OnFaxChanged();

        #endregion
        
        
        /// <summary>
        /// Initializes a new instance of the <see cref="C"/> class.
        /// </summary>
        public C()
        {
            this.OnCreated();
        }
        
        /// <summary>
        /// Gets or sets the 'Fax' value.
        /// </summary>
        [ConcurrencyCheck()]
        [DataMember()]
        [RoundtripOriginal()]
        [StringLength(24)]
        public string Fax
        {
            get
            {
                return this._fax;
            }
            set
            {
                if ((this._fax != value))
                {
                    this.OnFaxChanging(value);
                    this.RaiseDataMemberChanging("Fax");
                    this.ValidateProperty("Fax", value);
                    this._fax = value;
                    this.RaiseDataMemberChanged("Fax");
                    this.OnFaxChanged();
                }
            }
        }
    }
    
    /// <summary>
    /// The DomainContext corresponding to the 'LTS_Inheritance_DomainService' DomainService.
    /// </summary>
    public sealed partial class LTS_Inheritance_DomainContext : DomainContext
    {
        
        #region Extensibility Method Definitions

        /// <summary>
        /// This method is invoked from the constructor once initialization is complete and
        /// can be used for further object setup.
        /// </summary>
        partial void OnCreated();

        #endregion
        
        
        /// <summary>
        /// Initializes a new instance of the <see cref="LTS_Inheritance_DomainContext"/> class.
        /// </summary>
        public LTS_Inheritance_DomainContext() : 
                this(new Uri("DataTests-Inheritance-LTS-LTS_Inheritance_DomainService.svc", UriKind.Relative))
        {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="LTS_Inheritance_DomainContext"/> class with the specified service URI.
        /// </summary>
        /// <param name="serviceUri">The LTS_Inheritance_DomainService service URI.</param>
        public LTS_Inheritance_DomainContext(Uri serviceUri) : 
                this(DomainContext.CreateDomainClient(typeof(ILTS_Inheritance_DomainServiceContract), serviceUri, false))
        {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="LTS_Inheritance_DomainContext"/> class with the specified <paramref name="domainClient"/>.
        /// </summary>
        /// <param name="domainClient">The DomainClient instance to use for this DomainContext.</param>
        public LTS_Inheritance_DomainContext(DomainClient domainClient) : 
                base(domainClient)
        {
            this.OnCreated();
        }
        
        /// <summary>
        /// Gets the set of <see cref="A"/> entity instances that have been loaded into this <see cref="LTS_Inheritance_DomainContext"/> instance.
        /// </summary>
        public EntitySet<A> As
        {
            get
            {
                return base.EntityContainer.GetEntitySet<A>();
            }
        }
        
        /// <summary>
        /// Gets an EntityQuery instance that can be used to load <see cref="A"/> entity instances using the 'GetA' query.
        /// </summary>
        /// <returns>An EntityQuery that can be loaded to retrieve <see cref="A"/> entity instances.</returns>
        public EntityQuery<A> GetAQuery()
        {
            this.ValidateMethod("GetAQuery", null);
            return base.CreateQuery<A>("GetA", null, false, true);
        }
        
        /// <summary>
        /// Gets an EntityQuery instance that can be used to load <see cref="B"/> entity instances using the 'GetB' query.
        /// </summary>
        /// <returns>An EntityQuery that can be loaded to retrieve <see cref="B"/> entity instances.</returns>
        public EntityQuery<B> GetBQuery()
        {
            this.ValidateMethod("GetBQuery", null);
            return base.CreateQuery<B>("GetB", null, false, true);
        }
        
        /// <summary>
        /// Gets an EntityQuery instance that can be used to load <see cref="C"/> entity instances using the 'GetC' query.
        /// </summary>
        /// <returns>An EntityQuery that can be loaded to retrieve <see cref="C"/> entity instances.</returns>
        public EntityQuery<C> GetCQuery()
        {
            this.ValidateMethod("GetCQuery", null);
            return base.CreateQuery<C>("GetC", null, false, true);
        }
        
        /// <summary>
        /// Gets an EntityQuery instance that can be used to load <see cref="B"/> entity instances using the 'GetOneB' query.
        /// </summary>
        /// <returns>An EntityQuery that can be loaded to retrieve <see cref="B"/> entity instances.</returns>
        public EntityQuery<B> GetOneBQuery()
        {
            this.ValidateMethod("GetOneBQuery", null);
            return base.CreateQuery<B>("GetOneB", null, false, false);
        }
        
        /// <summary>
        /// Gets an EntityQuery instance that can be used to load <see cref="C"/> entity instances using the 'GetOneC' query.
        /// </summary>
        /// <returns>An EntityQuery that can be loaded to retrieve <see cref="C"/> entity instances.</returns>
        public EntityQuery<C> GetOneCQuery()
        {
            this.ValidateMethod("GetOneCQuery", null);
            return base.CreateQuery<C>("GetOneC", null, false, false);
        }
        
        /// <summary>
        /// Asynchronously invokes the 'InvokeOnB' method of the DomainService.
        /// </summary>
        /// <param name="b">The value for the 'b' parameter of this action.</param>
        /// <param name="callback">Callback to invoke when the operation completes.</param>
        /// <param name="userState">Value to pass to the callback.  It can be <c>null</c>.</param>
        /// <returns>An operation instance that can be used to manage the asynchronous request.</returns>
        public InvokeOperation<int> InvokeOnB(B b, Action<InvokeOperation<int>> callback, object userState)
        {
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            parameters.Add("b", b);
            this.ValidateMethod("InvokeOnB", parameters);
            return this.InvokeOperation<int>("InvokeOnB", typeof(int), parameters, true, callback, userState);
        }
        
        /// <summary>
        /// Asynchronously invokes the 'InvokeOnB' method of the DomainService.
        /// </summary>
        /// <param name="b">The value for the 'b' parameter of this action.</param>
        /// <returns>An operation instance that can be used to manage the asynchronous request.</returns>
        public InvokeOperation<int> InvokeOnB(B b)
        {
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            parameters.Add("b", b);
            this.ValidateMethod("InvokeOnB", parameters);
            return this.InvokeOperation<int>("InvokeOnB", typeof(int), parameters, true, null, null);
        }
        
        /// <summary>
        /// Asynchronously invokes the 'InvokeOnB' method of the DomainService.
        /// </summary>
        /// <param name="b">The value for the 'b' parameter of this action.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the work</param>
        /// <returns>An operation instance that can be used to manage the asynchronous request.</returns>
        public System.Threading.Tasks.Task<InvokeResult<int>> InvokeOnBAsync(B b, CancellationToken cancellationToken = default(CancellationToken))
        {
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            parameters.Add("b", b);
            this.ValidateMethod("InvokeOnB", parameters);
            return this.InvokeOperationAsync<int>("InvokeOnB", parameters, true, cancellationToken);
        }
        
        /// <summary>
        /// Creates a new EntityContainer for this DomainContext's EntitySets.
        /// </summary>
        /// <returns>A new container instance.</returns>
        protected override EntityContainer CreateEntityContainer()
        {
            return new LTS_Inheritance_DomainContextEntityContainer();
        }
        
        /// <summary>
        /// Service contract for the 'LTS_Inheritance_DomainService' DomainService.
        /// </summary>
        public interface ILTS_Inheritance_DomainServiceContract
        {
            
            /// <summary>
            /// Asynchronously invokes the 'GetA' operation.
            /// </summary>
            /// <param name="callback">Callback to invoke on completion.</param>
            /// <param name="asyncState">Optional state object.</param>
            /// <returns>An IAsyncResult that can be used to monitor the request.</returns>
            [HasSideEffects(false)]
            IAsyncResult BeginGetA(AsyncCallback callback, object asyncState);
            
            /// <summary>
            /// Completes the asynchronous operation begun by 'BeginGetA'.
            /// </summary>
            /// <param name="result">The IAsyncResult returned from 'BeginGetA'.</param>
            /// <returns>The 'QueryResult' returned from the 'GetA' operation.</returns>
            QueryResult<A> EndGetA(IAsyncResult result);
            
            /// <summary>
            /// Asynchronously invokes the 'GetB' operation.
            /// </summary>
            /// <param name="callback">Callback to invoke on completion.</param>
            /// <param name="asyncState">Optional state object.</param>
            /// <returns>An IAsyncResult that can be used to monitor the request.</returns>
            [HasSideEffects(false)]
            IAsyncResult BeginGetB(AsyncCallback callback, object asyncState);
            
            /// <summary>
            /// Completes the asynchronous operation begun by 'BeginGetB'.
            /// </summary>
            /// <param name="result">The IAsyncResult returned from 'BeginGetB'.</param>
            /// <returns>The 'QueryResult' returned from the 'GetB' operation.</returns>
            QueryResult<B> EndGetB(IAsyncResult result);
            
            /// <summary>
            /// Asynchronously invokes the 'GetC' operation.
            /// </summary>
            /// <param name="callback">Callback to invoke on completion.</param>
            /// <param name="asyncState">Optional state object.</param>
            /// <returns>An IAsyncResult that can be used to monitor the request.</returns>
            [HasSideEffects(false)]
            IAsyncResult BeginGetC(AsyncCallback callback, object asyncState);
            
            /// <summary>
            /// Completes the asynchronous operation begun by 'BeginGetC'.
            /// </summary>
            /// <param name="result">The IAsyncResult returned from 'BeginGetC'.</param>
            /// <returns>The 'QueryResult' returned from the 'GetC' operation.</returns>
            QueryResult<C> EndGetC(IAsyncResult result);
            
            /// <summary>
            /// Asynchronously invokes the 'GetOneB' operation.
            /// </summary>
            /// <param name="callback">Callback to invoke on completion.</param>
            /// <param name="asyncState">Optional state object.</param>
            /// <returns>An IAsyncResult that can be used to monitor the request.</returns>
            [HasSideEffects(false)]
            IAsyncResult BeginGetOneB(AsyncCallback callback, object asyncState);
            
            /// <summary>
            /// Completes the asynchronous operation begun by 'BeginGetOneB'.
            /// </summary>
            /// <param name="result">The IAsyncResult returned from 'BeginGetOneB'.</param>
            /// <returns>The 'QueryResult' returned from the 'GetOneB' operation.</returns>
            QueryResult<B> EndGetOneB(IAsyncResult result);
            
            /// <summary>
            /// Asynchronously invokes the 'GetOneC' operation.
            /// </summary>
            /// <param name="callback">Callback to invoke on completion.</param>
            /// <param name="asyncState">Optional state object.</param>
            /// <returns>An IAsyncResult that can be used to monitor the request.</returns>
            [HasSideEffects(false)]
            IAsyncResult BeginGetOneC(AsyncCallback callback, object asyncState);
            
            /// <summary>
            /// Completes the asynchronous operation begun by 'BeginGetOneC'.
            /// </summary>
            /// <param name="result">The IAsyncResult returned from 'BeginGetOneC'.</param>
            /// <returns>The 'QueryResult' returned from the 'GetOneC' operation.</returns>
            QueryResult<C> EndGetOneC(IAsyncResult result);
            
            /// <summary>
            /// Asynchronously invokes the 'InvokeOnB' operation.
            /// </summary>
            /// <param name="b">The value for the 'b' parameter of this action.</param>
            /// <param name="callback">Callback to invoke on completion.</param>
            /// <param name="asyncState">Optional state object.</param>
            /// <returns>An IAsyncResult that can be used to monitor the request.</returns>
            [HasSideEffects(true)]
            IAsyncResult BeginInvokeOnB(B b, AsyncCallback callback, object asyncState);
            
            /// <summary>
            /// Completes the asynchronous operation begun by 'BeginInvokeOnB'.
            /// </summary>
            /// <param name="result">The IAsyncResult returned from 'BeginInvokeOnB'.</param>
            /// <returns>The 'Int32' returned from the 'InvokeOnB' operation.</returns>
            int EndInvokeOnB(IAsyncResult result);
            
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
        
        internal sealed class LTS_Inheritance_DomainContextEntityContainer : EntityContainer
        {
            
            public LTS_Inheritance_DomainContextEntityContainer()
            {
                this.CreateEntitySet<A>(EntitySetOperations.Edit);
            }
        }
    }
}
