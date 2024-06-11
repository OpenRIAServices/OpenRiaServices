//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace TestDomainServices
{
    
    
    /// <summary>
    /// The 'InheritanceB' entity class.
    /// </summary>
    [global::System.Runtime.Serialization.DataContractAttribute(Namespace="http://schemas.datacontract.org/2004/07/TestDomainServices")]
    public sealed partial class InheritanceB : global::OpenRiaServices.Client.Entity
    {
        
        private int _id;
        
        private string _inheritanceAProp;
        
        private string _inheritanceBProp;
        
        private int _inheritanceD_ID;
        
        private global::OpenRiaServices.Client.EntityRef<global::TestDomainServices.InheritanceT1> _t1;
        
        private int _t1_id;
        
        private global::OpenRiaServices.Client.EntityCollection<global::TestDomainServices.InheritanceT1> _t1s;
        
        #region Extensibility Method Definitions

        /// <summary>
        /// This method is invoked from the constructor once initialization is complete and
        /// can be used for further object setup.
        /// </summary>
        partial void OnCreated();
        partial void OnIDChanging(int value);
        partial void OnIDChanged();
        partial void OnInheritanceAPropChanging(string value);
        partial void OnInheritanceAPropChanged();
        partial void OnInheritanceBPropChanging(string value);
        partial void OnInheritanceBPropChanged();
        partial void OnInheritanceD_IDChanging(int value);
        partial void OnInheritanceD_IDChanged();
        partial void OnT1_IDChanging(int value);
        partial void OnT1_IDChanged();

        #endregion
        
        
        /// <summary>
        /// Initializes a new instance of the <see cref="InheritanceB"/> class.
        /// </summary>
        public InheritanceB()
        {
            this.OnCreated();
        }
        
        /// <summary>
        /// Gets or sets the 'ID' value.
        /// </summary>
        [global::System.ComponentModel.DataAnnotations.EditableAttribute(false, AllowInitialValue=true)]
        [global::System.ComponentModel.DataAnnotations.KeyAttribute()]
        [global::System.ComponentModel.DataAnnotations.RoundtripOriginalAttribute()]
        [global::System.Runtime.Serialization.DataMemberAttribute()]
        public int ID
        {
            get
            {
                return this._id;
            }
            set
            {
                if ((this._id != value))
                {
                    this.OnIDChanging(value);
                    this.ValidateProperty("ID", value);
                    this._id = value;
                    this.RaisePropertyChanged("ID");
                    this.OnIDChanged();
                }
            }
        }
        
        /// <summary>
        /// Gets or sets the 'InheritanceAProp' value.
        /// </summary>
        [global::System.Runtime.Serialization.DataMemberAttribute()]
        public string InheritanceAProp
        {
            get
            {
                return this._inheritanceAProp;
            }
            set
            {
                if ((this._inheritanceAProp != value))
                {
                    this.OnInheritanceAPropChanging(value);
                    this.RaiseDataMemberChanging("InheritanceAProp");
                    this.ValidateProperty("InheritanceAProp", value);
                    this._inheritanceAProp = value;
                    this.RaiseDataMemberChanged("InheritanceAProp");
                    this.OnInheritanceAPropChanged();
                }
            }
        }
        
        /// <summary>
        /// Gets or sets the 'InheritanceBProp' value.
        /// </summary>
        [global::System.Runtime.Serialization.DataMemberAttribute()]
        public string InheritanceBProp
        {
            get
            {
                return this._inheritanceBProp;
            }
            set
            {
                if ((this._inheritanceBProp != value))
                {
                    this.OnInheritanceBPropChanging(value);
                    this.RaiseDataMemberChanging("InheritanceBProp");
                    this.ValidateProperty("InheritanceBProp", value);
                    this._inheritanceBProp = value;
                    this.RaiseDataMemberChanged("InheritanceBProp");
                    this.OnInheritanceBPropChanged();
                }
            }
        }
        
        /// <summary>
        /// Gets or sets the 'InheritanceD_ID' value.
        /// </summary>
        [global::System.ComponentModel.DataAnnotations.RoundtripOriginalAttribute()]
        [global::System.Runtime.Serialization.DataMemberAttribute()]
        public int InheritanceD_ID
        {
            get
            {
                return this._inheritanceD_ID;
            }
            set
            {
                if ((this._inheritanceD_ID != value))
                {
                    this.OnInheritanceD_IDChanging(value);
                    this.RaiseDataMemberChanging("InheritanceD_ID");
                    this.ValidateProperty("InheritanceD_ID", value);
                    this._inheritanceD_ID = value;
                    this.RaiseDataMemberChanged("InheritanceD_ID");
                    this.OnInheritanceD_IDChanged();
                }
            }
        }
        
        /// <summary>
        /// Gets or sets the associated <see cref="InheritanceT1"/> entity.
        /// </summary>
        [global::System.ComponentModel.DataAnnotations.EntityAssociationAttribute("InheritanceBase_InheritanceT1", "T1_ID", "ID", IsForeignKey=true)]
        public global::TestDomainServices.InheritanceT1 T1
        {
            get
            {
                if ((this._t1 == null))
                {
                    this._t1 = new global::OpenRiaServices.Client.EntityRef<global::TestDomainServices.InheritanceT1>(this, "T1", this.FilterT1);
                }
                return this._t1.Entity;
            }
            set
            {
                global::TestDomainServices.InheritanceT1 previous = this.T1;
                if ((previous != value))
                {
                    this.ValidateProperty("T1", value);
                    if ((value != null))
                    {
                        this.T1_ID = value.ID;
                    }
                    else
                    {
                        this.T1_ID = default(int);
                    }
                    this._t1.Entity = value;
                    this.RaisePropertyChanged("T1");
                }
            }
        }
        
        /// <summary>
        /// Gets or sets the 'T1_ID' value.
        /// </summary>
        [global::System.ComponentModel.DataAnnotations.RoundtripOriginalAttribute()]
        [global::System.Runtime.Serialization.DataMemberAttribute()]
        public int T1_ID
        {
            get
            {
                return this._t1_id;
            }
            set
            {
                if ((this._t1_id != value))
                {
                    this.OnT1_IDChanging(value);
                    this.RaiseDataMemberChanging("T1_ID");
                    this.ValidateProperty("T1_ID", value);
                    this._t1_id = value;
                    this.RaiseDataMemberChanged("T1_ID");
                    this.OnT1_IDChanged();
                }
            }
        }
        
        /// <summary>
        /// Gets the collection of associated <see cref="InheritanceT1"/> entity instances.
        /// </summary>
        [global::System.ComponentModel.DataAnnotations.EntityAssociationAttribute("InheritanceT1_InheritanceBase", "ID", "InheritanceBase_ID")]
        public global::OpenRiaServices.Client.EntityCollection<global::TestDomainServices.InheritanceT1> T1s
        {
            get
            {
                if ((this._t1s == null))
                {
                    this._t1s = new global::OpenRiaServices.Client.EntityCollection<global::TestDomainServices.InheritanceT1>(this, "T1s", this.FilterT1s);
                }
                return this._t1s;
            }
        }
        
        private bool FilterT1(global::TestDomainServices.InheritanceT1 entity)
        {
            return (entity.ID == this.T1_ID);
        }
        
        private bool FilterT1s(global::TestDomainServices.InheritanceT1 entity)
        {
            return (entity.InheritanceBase_ID == this.ID);
        }
        
        /// <summary>
        /// Computes a value from the key fields that uniquely identifies this entity instance.
        /// </summary>
        /// <returns>An object instance that uniquely identifies this entity instance.</returns>
        public override object GetIdentity()
        {
            return this._id;
        }
    }
    
    /// <summary>
    /// The 'InheritanceC' entity class.
    /// </summary>
    [global::System.Runtime.Serialization.DataContractAttribute(Namespace="http://schemas.datacontract.org/2004/07/TestDomainServices")]
    public sealed partial class InheritanceC : global::OpenRiaServices.Client.Entity
    {
        
        private int _id;
        
        private string _inheritanceAProp;
        
        private string _inheritanceCProp;
        
        private int _inheritanceD_ID;
        
        private global::OpenRiaServices.Client.EntityRef<global::TestDomainServices.InheritanceT1> _t1;
        
        private int _t1_id;
        
        private global::OpenRiaServices.Client.EntityCollection<global::TestDomainServices.InheritanceT1> _t1s;
        
        #region Extensibility Method Definitions

        /// <summary>
        /// This method is invoked from the constructor once initialization is complete and
        /// can be used for further object setup.
        /// </summary>
        partial void OnCreated();
        partial void OnIDChanging(int value);
        partial void OnIDChanged();
        partial void OnInheritanceAPropChanging(string value);
        partial void OnInheritanceAPropChanged();
        partial void OnInheritanceCPropChanging(string value);
        partial void OnInheritanceCPropChanged();
        partial void OnInheritanceD_IDChanging(int value);
        partial void OnInheritanceD_IDChanged();
        partial void OnT1_IDChanging(int value);
        partial void OnT1_IDChanged();

        #endregion
        
        
        /// <summary>
        /// Initializes a new instance of the <see cref="InheritanceC"/> class.
        /// </summary>
        public InheritanceC()
        {
            this.OnCreated();
        }
        
        /// <summary>
        /// Gets or sets the 'ID' value.
        /// </summary>
        [global::System.ComponentModel.DataAnnotations.EditableAttribute(false, AllowInitialValue=true)]
        [global::System.ComponentModel.DataAnnotations.KeyAttribute()]
        [global::System.ComponentModel.DataAnnotations.RoundtripOriginalAttribute()]
        [global::System.Runtime.Serialization.DataMemberAttribute()]
        public int ID
        {
            get
            {
                return this._id;
            }
            set
            {
                if ((this._id != value))
                {
                    this.OnIDChanging(value);
                    this.ValidateProperty("ID", value);
                    this._id = value;
                    this.RaisePropertyChanged("ID");
                    this.OnIDChanged();
                }
            }
        }
        
        /// <summary>
        /// Gets or sets the 'InheritanceAProp' value.
        /// </summary>
        [global::System.Runtime.Serialization.DataMemberAttribute()]
        public string InheritanceAProp
        {
            get
            {
                return this._inheritanceAProp;
            }
            set
            {
                if ((this._inheritanceAProp != value))
                {
                    this.OnInheritanceAPropChanging(value);
                    this.RaiseDataMemberChanging("InheritanceAProp");
                    this.ValidateProperty("InheritanceAProp", value);
                    this._inheritanceAProp = value;
                    this.RaiseDataMemberChanged("InheritanceAProp");
                    this.OnInheritanceAPropChanged();
                }
            }
        }
        
        /// <summary>
        /// Gets or sets the 'InheritanceCProp' value.
        /// </summary>
        [global::System.Runtime.Serialization.DataMemberAttribute()]
        public string InheritanceCProp
        {
            get
            {
                return this._inheritanceCProp;
            }
            set
            {
                if ((this._inheritanceCProp != value))
                {
                    this.OnInheritanceCPropChanging(value);
                    this.RaiseDataMemberChanging("InheritanceCProp");
                    this.ValidateProperty("InheritanceCProp", value);
                    this._inheritanceCProp = value;
                    this.RaiseDataMemberChanged("InheritanceCProp");
                    this.OnInheritanceCPropChanged();
                }
            }
        }
        
        /// <summary>
        /// Gets or sets the 'InheritanceD_ID' value.
        /// </summary>
        [global::System.ComponentModel.DataAnnotations.RoundtripOriginalAttribute()]
        [global::System.Runtime.Serialization.DataMemberAttribute()]
        public int InheritanceD_ID
        {
            get
            {
                return this._inheritanceD_ID;
            }
            set
            {
                if ((this._inheritanceD_ID != value))
                {
                    this.OnInheritanceD_IDChanging(value);
                    this.RaiseDataMemberChanging("InheritanceD_ID");
                    this.ValidateProperty("InheritanceD_ID", value);
                    this._inheritanceD_ID = value;
                    this.RaiseDataMemberChanged("InheritanceD_ID");
                    this.OnInheritanceD_IDChanged();
                }
            }
        }
        
        /// <summary>
        /// Gets or sets the associated <see cref="InheritanceT1"/> entity.
        /// </summary>
        [global::System.ComponentModel.DataAnnotations.EntityAssociationAttribute("InheritanceBase_InheritanceT1", "T1_ID", "ID", IsForeignKey=true)]
        public global::TestDomainServices.InheritanceT1 T1
        {
            get
            {
                if ((this._t1 == null))
                {
                    this._t1 = new global::OpenRiaServices.Client.EntityRef<global::TestDomainServices.InheritanceT1>(this, "T1", this.FilterT1);
                }
                return this._t1.Entity;
            }
            set
            {
                global::TestDomainServices.InheritanceT1 previous = this.T1;
                if ((previous != value))
                {
                    this.ValidateProperty("T1", value);
                    if ((value != null))
                    {
                        this.T1_ID = value.ID;
                    }
                    else
                    {
                        this.T1_ID = default(int);
                    }
                    this._t1.Entity = value;
                    this.RaisePropertyChanged("T1");
                }
            }
        }
        
        /// <summary>
        /// Gets or sets the 'T1_ID' value.
        /// </summary>
        [global::System.ComponentModel.DataAnnotations.RoundtripOriginalAttribute()]
        [global::System.Runtime.Serialization.DataMemberAttribute()]
        public int T1_ID
        {
            get
            {
                return this._t1_id;
            }
            set
            {
                if ((this._t1_id != value))
                {
                    this.OnT1_IDChanging(value);
                    this.RaiseDataMemberChanging("T1_ID");
                    this.ValidateProperty("T1_ID", value);
                    this._t1_id = value;
                    this.RaiseDataMemberChanged("T1_ID");
                    this.OnT1_IDChanged();
                }
            }
        }
        
        /// <summary>
        /// Gets the collection of associated <see cref="InheritanceT1"/> entity instances.
        /// </summary>
        [global::System.ComponentModel.DataAnnotations.EntityAssociationAttribute("InheritanceT1_InheritanceBase", "ID", "InheritanceBase_ID")]
        public global::OpenRiaServices.Client.EntityCollection<global::TestDomainServices.InheritanceT1> T1s
        {
            get
            {
                if ((this._t1s == null))
                {
                    this._t1s = new global::OpenRiaServices.Client.EntityCollection<global::TestDomainServices.InheritanceT1>(this, "T1s", this.FilterT1s);
                }
                return this._t1s;
            }
        }
        
        private bool FilterT1(global::TestDomainServices.InheritanceT1 entity)
        {
            return (entity.ID == this.T1_ID);
        }
        
        private bool FilterT1s(global::TestDomainServices.InheritanceT1 entity)
        {
            return (entity.InheritanceBase_ID == this.ID);
        }
        
        /// <summary>
        /// Computes a value from the key fields that uniquely identifies this entity instance.
        /// </summary>
        /// <returns>An object instance that uniquely identifies this entity instance.</returns>
        public override object GetIdentity()
        {
            return this._id;
        }
    }
    
    /// <summary>
    /// The 'InheritanceT1' entity class.
    /// </summary>
    [global::System.Runtime.Serialization.DataContractAttribute(Namespace="http://schemas.datacontract.org/2004/07/TestDomainServices")]
    public sealed partial class InheritanceT1 : global::OpenRiaServices.Client.Entity
    {
        
        private string _description;
        
        private int _id;
        
        private int _inheritanceBase_ID;
        
        #region Extensibility Method Definitions

        /// <summary>
        /// This method is invoked from the constructor once initialization is complete and
        /// can be used for further object setup.
        /// </summary>
        partial void OnCreated();
        partial void OnDescriptionChanging(string value);
        partial void OnDescriptionChanged();
        partial void OnIDChanging(int value);
        partial void OnIDChanged();
        partial void OnInheritanceBase_IDChanging(int value);
        partial void OnInheritanceBase_IDChanged();

        #endregion
        
        
        /// <summary>
        /// Initializes a new instance of the <see cref="InheritanceT1"/> class.
        /// </summary>
        public InheritanceT1()
        {
            this.OnCreated();
        }
        
        /// <summary>
        /// Gets or sets the 'Description' value.
        /// </summary>
        [global::System.Runtime.Serialization.DataMemberAttribute()]
        public string Description
        {
            get
            {
                return this._description;
            }
            set
            {
                if ((this._description != value))
                {
                    this.OnDescriptionChanging(value);
                    this.RaiseDataMemberChanging("Description");
                    this.ValidateProperty("Description", value);
                    this._description = value;
                    this.RaiseDataMemberChanged("Description");
                    this.OnDescriptionChanged();
                }
            }
        }
        
        /// <summary>
        /// Gets or sets the 'ID' value.
        /// </summary>
        [global::System.ComponentModel.DataAnnotations.EditableAttribute(false, AllowInitialValue=true)]
        [global::System.ComponentModel.DataAnnotations.KeyAttribute()]
        [global::System.ComponentModel.DataAnnotations.RoundtripOriginalAttribute()]
        [global::System.Runtime.Serialization.DataMemberAttribute()]
        public int ID
        {
            get
            {
                return this._id;
            }
            set
            {
                if ((this._id != value))
                {
                    this.OnIDChanging(value);
                    this.ValidateProperty("ID", value);
                    this._id = value;
                    this.RaisePropertyChanged("ID");
                    this.OnIDChanged();
                }
            }
        }
        
        /// <summary>
        /// Gets or sets the 'InheritanceBase_ID' value.
        /// </summary>
        [global::System.ComponentModel.DataAnnotations.RoundtripOriginalAttribute()]
        [global::System.Runtime.Serialization.DataMemberAttribute()]
        public int InheritanceBase_ID
        {
            get
            {
                return this._inheritanceBase_ID;
            }
            set
            {
                if ((this._inheritanceBase_ID != value))
                {
                    this.OnInheritanceBase_IDChanging(value);
                    this.RaiseDataMemberChanging("InheritanceBase_ID");
                    this.ValidateProperty("InheritanceBase_ID", value);
                    this._inheritanceBase_ID = value;
                    this.RaiseDataMemberChanged("InheritanceBase_ID");
                    this.OnInheritanceBase_IDChanged();
                }
            }
        }
        
        /// <summary>
        /// Computes a value from the key fields that uniquely identifies this entity instance.
        /// </summary>
        /// <returns>An object instance that uniquely identifies this entity instance.</returns>
        public override object GetIdentity()
        {
            return this._id;
        }
    }
    
    /// <summary>
    /// The DomainContext corresponding to the 'TestProvider_Inheritance1' DomainService.
    /// </summary>
    public sealed partial class TestProvider_Inheritance1 : global::OpenRiaServices.Client.DomainContext
    {
        
        #region Extensibility Method Definitions

        /// <summary>
        /// This method is invoked from the constructor once initialization is complete and
        /// can be used for further object setup.
        /// </summary>
        partial void OnCreated();

        #endregion
        
        
        /// <summary>
        /// Initializes a new instance of the <see cref="TestProvider_Inheritance1"/> class.
        /// </summary>
        public TestProvider_Inheritance1() : 
                this(new global::System.Uri("TestDomainServices-TestProvider_Inheritance1.svc", global::System.UriKind.Relative))
        {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="TestProvider_Inheritance1"/> class with the specified service URI.
        /// </summary>
        /// <param name="serviceUri">The TestProvider_Inheritance1 service URI.</param>
        public TestProvider_Inheritance1(global::System.Uri serviceUri) : 
                this(global::OpenRiaServices.Client.DomainContext.CreateDomainClient(typeof(global::TestDomainServices.TestProvider_Inheritance1.ITestProvider_Inheritance1Contract), serviceUri, false))
        {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="TestProvider_Inheritance1"/> class with the specified <paramref name="domainClient"/>.
        /// </summary>
        /// <param name="domainClient">The DomainClient instance to use for this DomainContext.</param>
        public TestProvider_Inheritance1(global::OpenRiaServices.Client.DomainClient domainClient) : 
                base(domainClient)
        {
            this.OnCreated();
        }
        
        /// <summary>
        /// Gets the set of <see cref="InheritanceB"/> entity instances that have been loaded into this <see cref="TestProvider_Inheritance1"/> instance.
        /// </summary>
        public global::OpenRiaServices.Client.EntitySet<global::TestDomainServices.InheritanceB> InheritanceBs
        {
            get
            {
                return base.EntityContainer.GetEntitySet<global::TestDomainServices.InheritanceB>();
            }
        }
        
        /// <summary>
        /// Gets the set of <see cref="InheritanceC"/> entity instances that have been loaded into this <see cref="TestProvider_Inheritance1"/> instance.
        /// </summary>
        public global::OpenRiaServices.Client.EntitySet<global::TestDomainServices.InheritanceC> InheritanceCs
        {
            get
            {
                return base.EntityContainer.GetEntitySet<global::TestDomainServices.InheritanceC>();
            }
        }
        
        /// <summary>
        /// Gets the set of <see cref="InheritanceT1"/> entity instances that have been loaded into this <see cref="TestProvider_Inheritance1"/> instance.
        /// </summary>
        public global::OpenRiaServices.Client.EntitySet<global::TestDomainServices.InheritanceT1> InheritanceT1s
        {
            get
            {
                return base.EntityContainer.GetEntitySet<global::TestDomainServices.InheritanceT1>();
            }
        }
        
        /// <summary>
        /// Gets an EntityQuery instance that can be used to load <see cref="InheritanceB"/> entity instances using the 'GetBs' query.
        /// </summary>
        /// <returns>An EntityQuery that can be loaded to retrieve <see cref="InheritanceB"/> entity instances.</returns>
        public global::OpenRiaServices.Client.EntityQuery<global::TestDomainServices.InheritanceB> GetBsQuery()
        {
            this.ValidateMethod("GetBsQuery", null);
            return base.CreateQuery<global::TestDomainServices.InheritanceB>("GetBs", null, false, true);
        }
        
        /// <summary>
        /// Gets an EntityQuery instance that can be used to load <see cref="InheritanceC"/> entity instances using the 'GetCs' query.
        /// </summary>
        /// <returns>An EntityQuery that can be loaded to retrieve <see cref="InheritanceC"/> entity instances.</returns>
        public global::OpenRiaServices.Client.EntityQuery<global::TestDomainServices.InheritanceC> GetCsQuery()
        {
            this.ValidateMethod("GetCsQuery", null);
            return base.CreateQuery<global::TestDomainServices.InheritanceC>("GetCs", null, false, true);
        }
        
        /// <summary>
        /// Gets an EntityQuery instance that can be used to load <see cref="InheritanceT1"/> entity instances using the 'GetInheritanceT1s' query.
        /// </summary>
        /// <returns>An EntityQuery that can be loaded to retrieve <see cref="InheritanceT1"/> entity instances.</returns>
        public global::OpenRiaServices.Client.EntityQuery<global::TestDomainServices.InheritanceT1> GetInheritanceT1sQuery()
        {
            this.ValidateMethod("GetInheritanceT1sQuery", null);
            return base.CreateQuery<global::TestDomainServices.InheritanceT1>("GetInheritanceT1s", null, false, true);
        }
        
        /// <summary>
        /// Creates a new EntityContainer for this DomainContext's EntitySets.
        /// </summary>
        /// <returns>A new container instance.</returns>
        protected override global::OpenRiaServices.Client.EntityContainer CreateEntityContainer()
        {
            return new global::TestDomainServices.TestProvider_Inheritance1.TestProvider_Inheritance1EntityContainer();
        }
        
        /// <summary>
        /// Service contract for the 'TestProvider_Inheritance1' DomainService.
        /// </summary>
        [global::System.ServiceModel.ServiceContractAttribute()]
        public interface ITestProvider_Inheritance1Contract
        {
            
            /// <summary>
            /// Asynchronously invokes the 'GetBs' operation.
            /// </summary>
            /// <param name="callback">Callback to invoke on completion.</param>
            /// <param name="asyncState">Optional state object.</param>
            /// <returns>An IAsyncResult that can be used to monitor the request.</returns>
            [global::OpenRiaServices.Client.HasSideEffects(false)]
            [global::System.ServiceModel.OperationContractAttribute(AsyncPattern=true, Action="http://tempuri.org/TestProvider_Inheritance1/GetBs", ReplyAction="http://tempuri.org/TestProvider_Inheritance1/GetBsResponse")]
            global::System.IAsyncResult BeginGetBs(global::System.AsyncCallback callback, object asyncState);
            
            /// <summary>
            /// Completes the asynchronous operation begun by 'BeginGetBs'.
            /// </summary>
            /// <param name="result">The IAsyncResult returned from 'BeginGetBs'.</param>
            /// <returns>The 'QueryResult' returned from the 'GetBs' operation.</returns>
            global::OpenRiaServices.Client.QueryResult<global::TestDomainServices.InheritanceB> EndGetBs(global::System.IAsyncResult result);
            
            /// <summary>
            /// Asynchronously invokes the 'GetCs' operation.
            /// </summary>
            /// <param name="callback">Callback to invoke on completion.</param>
            /// <param name="asyncState">Optional state object.</param>
            /// <returns>An IAsyncResult that can be used to monitor the request.</returns>
            [global::OpenRiaServices.Client.HasSideEffects(false)]
            [global::System.ServiceModel.OperationContractAttribute(AsyncPattern=true, Action="http://tempuri.org/TestProvider_Inheritance1/GetCs", ReplyAction="http://tempuri.org/TestProvider_Inheritance1/GetCsResponse")]
            global::System.IAsyncResult BeginGetCs(global::System.AsyncCallback callback, object asyncState);
            
            /// <summary>
            /// Completes the asynchronous operation begun by 'BeginGetCs'.
            /// </summary>
            /// <param name="result">The IAsyncResult returned from 'BeginGetCs'.</param>
            /// <returns>The 'QueryResult' returned from the 'GetCs' operation.</returns>
            global::OpenRiaServices.Client.QueryResult<global::TestDomainServices.InheritanceC> EndGetCs(global::System.IAsyncResult result);
            
            /// <summary>
            /// Asynchronously invokes the 'GetInheritanceT1s' operation.
            /// </summary>
            /// <param name="callback">Callback to invoke on completion.</param>
            /// <param name="asyncState">Optional state object.</param>
            /// <returns>An IAsyncResult that can be used to monitor the request.</returns>
            [global::OpenRiaServices.Client.HasSideEffects(false)]
            [global::System.ServiceModel.OperationContractAttribute(AsyncPattern=true, Action="http://tempuri.org/TestProvider_Inheritance1/GetInheritanceT1s", ReplyAction="http://tempuri.org/TestProvider_Inheritance1/GetInheritanceT1sResponse")]
            global::System.IAsyncResult BeginGetInheritanceT1s(global::System.AsyncCallback callback, object asyncState);
            
            /// <summary>
            /// Completes the asynchronous operation begun by 'BeginGetInheritanceT1s'.
            /// </summary>
            /// <param name="result">The IAsyncResult returned from 'BeginGetInheritanceT1s'.</param>
            /// <returns>The 'QueryResult' returned from the 'GetInheritanceT1s' operation.</returns>
            global::OpenRiaServices.Client.QueryResult<global::TestDomainServices.InheritanceT1> EndGetInheritanceT1s(global::System.IAsyncResult result);
        }
        
        internal sealed class TestProvider_Inheritance1EntityContainer : global::OpenRiaServices.Client.EntityContainer
        {
            
            public TestProvider_Inheritance1EntityContainer()
            {
                this.CreateEntitySet<global::TestDomainServices.InheritanceB>(global::OpenRiaServices.Client.EntitySetOperations.None);
                this.CreateEntitySet<global::TestDomainServices.InheritanceC>(global::OpenRiaServices.Client.EntitySetOperations.None);
                this.CreateEntitySet<global::TestDomainServices.InheritanceT1>(global::OpenRiaServices.Client.EntitySetOperations.None);
            }
        }
    }
}
