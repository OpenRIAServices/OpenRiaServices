//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace RootNamespace
{
    
    
    /// <summary>
    /// Context for the RIA application.
    /// </summary>
    /// <remarks>
    /// This context extends the base to make application services and types available
    /// for consumption from code and xaml.
    /// </remarks>
    public sealed partial class WebContext : global::OpenRiaServices.Client.ApplicationServices.WebContextBase
    {
        
        #region Extensibility Method Definitions

        /// <summary>
        /// This method is invoked from the constructor once initialization is complete and
        /// can be used for further object setup.
        /// </summary>
        partial void OnCreated();

        #endregion
        
        
        /// <summary>
        /// Initializes a new instance of the WebContext class.
        /// </summary>
        public WebContext()
        {
            this.OnCreated();
        }
        
        /// <summary>
        /// Gets the context that is registered as a lifetime object with the current application.
        /// </summary>
        /// <exception cref="System.InvalidOperationException"> is thrown if there is no current application,
        /// no contexts have been added, or more than one context has been added.
        /// </exception>
        /// <seealso cref="System.Windows.Application.ApplicationLifetimeObjects"/>
        public new static global::RootNamespace.WebContext Current
        {
            get
            {
                return ((global::RootNamespace.WebContext)(global::OpenRiaServices.Client.ApplicationServices.WebContextBase.Current));
            }
        }
    }
}
namespace RootNamespace.TestNamespace
{
    
    
    /// <summary>
    /// The DomainContext corresponding to the 'AuthenticationService1' DomainService.
    /// </summary>
    public sealed partial class AuthenticationService1 : global::OpenRiaServices.Client.ApplicationServices.AuthenticationDomainContextBase
    {
        
        #region Extensibility Method Definitions

        /// <summary>
        /// This method is invoked from the constructor once initialization is complete and
        /// can be used for further object setup.
        /// </summary>
        partial void OnCreated();

        #endregion
        
        
        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationService1"/> class.
        /// </summary>
        public AuthenticationService1() : 
                this(new global::System.Uri("RootNamespace-TestNamespace-AuthenticationService1.svc", global::System.UriKind.Relative))
        {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationService1"/> class with the specified service URI.
        /// </summary>
        /// <param name="serviceUri">The AuthenticationService1 service URI.</param>
        public AuthenticationService1(global::System.Uri serviceUri) : 
                this(global::OpenRiaServices.Client.DomainContext.CreateDomainClient(typeof(global::RootNamespace.TestNamespace.AuthenticationService1.IAuthenticationService1Contract), serviceUri, false))
        {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationService1"/> class with the specified <paramref name="domainClient"/>.
        /// </summary>
        /// <param name="domainClient">The DomainClient instance to use for this DomainContext.</param>
        public AuthenticationService1(global::OpenRiaServices.Client.DomainClient domainClient) : 
                base(domainClient)
        {
            this.OnCreated();
        }
        
        /// <summary>
        /// Gets the set of <see cref="User1"/> entity instances that have been loaded into this <see cref="AuthenticationService1"/> instance.
        /// </summary>
        public global::OpenRiaServices.Client.EntitySet<global::RootNamespace.TestNamespace.User1> User1s
        {
            get
            {
                return base.EntityContainer.GetEntitySet<global::RootNamespace.TestNamespace.User1>();
            }
        }
        
        /// <summary>
        /// Gets an EntityQuery instance that can be used to load <see cref="User1"/> entity instances using the 'GetUser' query.
        /// </summary>
        /// <returns>An EntityQuery that can be loaded to retrieve <see cref="User1"/> entity instances.</returns>
        public global::OpenRiaServices.Client.EntityQuery<global::RootNamespace.TestNamespace.User1> GetUserQuery()
        {
            this.ValidateMethod("GetUserQuery", null);
            return base.CreateQuery<global::RootNamespace.TestNamespace.User1>("GetUser", null, false, false);
        }
        
        /// <summary>
        /// Gets an EntityQuery instance that can be used to load <see cref="User1"/> entity instances using the 'Login' query.
        /// </summary>
        /// <param name="userName">The value for the 'userName' parameter of the query.</param>
        /// <param name="password">The value for the 'password' parameter of the query.</param>
        /// <param name="isPersistent">The value for the 'isPersistent' parameter of the query.</param>
        /// <param name="customData">The value for the 'customData' parameter of the query.</param>
        /// <returns>An EntityQuery that can be loaded to retrieve <see cref="User1"/> entity instances.</returns>
        public global::OpenRiaServices.Client.EntityQuery<global::RootNamespace.TestNamespace.User1> LoginQuery(string userName, string password, bool isPersistent, string customData)
        {
            global::System.Collections.Generic.Dictionary<string, object> parameters = new global::System.Collections.Generic.Dictionary<string, object>();
            parameters.Add("userName", userName);
            parameters.Add("password", password);
            parameters.Add("isPersistent", isPersistent);
            parameters.Add("customData", customData);
            this.ValidateMethod("LoginQuery", parameters);
            return base.CreateQuery<global::RootNamespace.TestNamespace.User1>("Login", parameters, true, false);
        }
        
        /// <summary>
        /// Gets an EntityQuery instance that can be used to load <see cref="User1"/> entity instances using the 'Logout' query.
        /// </summary>
        /// <returns>An EntityQuery that can be loaded to retrieve <see cref="User1"/> entity instances.</returns>
        public global::OpenRiaServices.Client.EntityQuery<global::RootNamespace.TestNamespace.User1> LogoutQuery()
        {
            this.ValidateMethod("LogoutQuery", null);
            return base.CreateQuery<global::RootNamespace.TestNamespace.User1>("Logout", null, true, false);
        }
        
        /// <summary>
        /// Creates a new EntityContainer for this DomainContext's EntitySets.
        /// </summary>
        /// <returns>A new container instance.</returns>
        protected override global::OpenRiaServices.Client.EntityContainer CreateEntityContainer()
        {
            return new global::RootNamespace.TestNamespace.AuthenticationService1.AuthenticationService1EntityContainer();
        }
        
        /// <summary>
        /// Service contract for the 'AuthenticationService1' DomainService.
        /// </summary>
        [global::System.ServiceModel.ServiceContractAttribute()]
        public interface IAuthenticationService1Contract
        {
            
            /// <summary>
            /// Asynchronously invokes the 'GetUser' operation.
            /// </summary>
            /// <param name="callback">Callback to invoke on completion.</param>
            /// <param name="asyncState">Optional state object.</param>
            /// <returns>An IAsyncResult that can be used to monitor the request.</returns>
            [global::OpenRiaServices.Client.HasSideEffects(false)]
            [global::System.ServiceModel.OperationContractAttribute(AsyncPattern=true, Action="http://tempuri.org/AuthenticationService1/GetUser", ReplyAction="http://tempuri.org/AuthenticationService1/GetUserResponse")]
            global::System.IAsyncResult BeginGetUser(global::System.AsyncCallback callback, object asyncState);
            
            /// <summary>
            /// Completes the asynchronous operation begun by 'BeginGetUser'.
            /// </summary>
            /// <param name="result">The IAsyncResult returned from 'BeginGetUser'.</param>
            /// <returns>The 'QueryResult' returned from the 'GetUser' operation.</returns>
            global::OpenRiaServices.Client.QueryResult<global::RootNamespace.TestNamespace.User1> EndGetUser(global::System.IAsyncResult result);
            
            /// <summary>
            /// Asynchronously invokes the 'Login' operation.
            /// </summary>
            /// <param name="userName">The value for the 'userName' parameter of this action.</param>
            /// <param name="password">The value for the 'password' parameter of this action.</param>
            /// <param name="isPersistent">The value for the 'isPersistent' parameter of this action.</param>
            /// <param name="customData">The value for the 'customData' parameter of this action.</param>
            /// <param name="callback">Callback to invoke on completion.</param>
            /// <param name="asyncState">Optional state object.</param>
            /// <returns>An IAsyncResult that can be used to monitor the request.</returns>
            [global::OpenRiaServices.Client.HasSideEffects(true)]
            [global::System.ServiceModel.OperationContractAttribute(AsyncPattern=true, Action="http://tempuri.org/AuthenticationService1/Login", ReplyAction="http://tempuri.org/AuthenticationService1/LoginResponse")]
            global::System.IAsyncResult BeginLogin(string userName, string password, bool isPersistent, string customData, global::System.AsyncCallback callback, object asyncState);
            
            /// <summary>
            /// Completes the asynchronous operation begun by 'BeginLogin'.
            /// </summary>
            /// <param name="result">The IAsyncResult returned from 'BeginLogin'.</param>
            /// <returns>The 'QueryResult' returned from the 'Login' operation.</returns>
            global::OpenRiaServices.Client.QueryResult<global::RootNamespace.TestNamespace.User1> EndLogin(global::System.IAsyncResult result);
            
            /// <summary>
            /// Asynchronously invokes the 'Logout' operation.
            /// </summary>
            /// <param name="callback">Callback to invoke on completion.</param>
            /// <param name="asyncState">Optional state object.</param>
            /// <returns>An IAsyncResult that can be used to monitor the request.</returns>
            [global::OpenRiaServices.Client.HasSideEffects(true)]
            [global::System.ServiceModel.OperationContractAttribute(AsyncPattern=true, Action="http://tempuri.org/AuthenticationService1/Logout", ReplyAction="http://tempuri.org/AuthenticationService1/LogoutResponse")]
            global::System.IAsyncResult BeginLogout(global::System.AsyncCallback callback, object asyncState);
            
            /// <summary>
            /// Completes the asynchronous operation begun by 'BeginLogout'.
            /// </summary>
            /// <param name="result">The IAsyncResult returned from 'BeginLogout'.</param>
            /// <returns>The 'QueryResult' returned from the 'Logout' operation.</returns>
            global::OpenRiaServices.Client.QueryResult<global::RootNamespace.TestNamespace.User1> EndLogout(global::System.IAsyncResult result);
            
            /// <summary>
            /// Asynchronously invokes the 'SubmitChanges' operation.
            /// </summary>
            /// <param name="changeSet">The change-set to submit.</param>
            /// <param name="callback">Callback to invoke on completion.</param>
            /// <param name="asyncState">Optional state object.</param>
            /// <returns>An IAsyncResult that can be used to monitor the request.</returns>
            [global::System.ServiceModel.OperationContractAttribute(AsyncPattern=true, Action="http://tempuri.org/AuthenticationService1/SubmitChanges", ReplyAction="http://tempuri.org/AuthenticationService1/SubmitChangesResponse")]
            global::System.IAsyncResult BeginSubmitChanges(global::System.Collections.Generic.IEnumerable<global::OpenRiaServices.Client.ChangeSetEntry> changeSet, global::System.AsyncCallback callback, object asyncState);
            
            /// <summary>
            /// Completes the asynchronous operation begun by 'BeginSubmitChanges'.
            /// </summary>
            /// <param name="result">The IAsyncResult returned from 'BeginSubmitChanges'.</param>
            /// <returns>The collection of change-set entry elements returned from 'SubmitChanges'.</returns>
            global::System.Collections.Generic.IEnumerable<global::OpenRiaServices.Client.ChangeSetEntry> EndSubmitChanges(global::System.IAsyncResult result);
        }
        
        internal sealed class AuthenticationService1EntityContainer : global::OpenRiaServices.Client.EntityContainer
        {
            
            public AuthenticationService1EntityContainer()
            {
                this.CreateEntitySet<global::RootNamespace.TestNamespace.User1>(global::OpenRiaServices.Client.EntitySetOperations.Edit);
            }
        }
    }
    
    /// <summary>
    /// The DomainContext corresponding to the 'AuthenticationService2' DomainService.
    /// </summary>
    public sealed partial class AuthenticationService2 : global::OpenRiaServices.Client.ApplicationServices.AuthenticationDomainContextBase
    {
        
        #region Extensibility Method Definitions

        /// <summary>
        /// This method is invoked from the constructor once initialization is complete and
        /// can be used for further object setup.
        /// </summary>
        partial void OnCreated();

        #endregion
        
        
        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationService2"/> class.
        /// </summary>
        public AuthenticationService2() : 
                this(new global::System.Uri("RootNamespace-TestNamespace-AuthenticationService2.svc", global::System.UriKind.Relative))
        {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationService2"/> class with the specified service URI.
        /// </summary>
        /// <param name="serviceUri">The AuthenticationService2 service URI.</param>
        public AuthenticationService2(global::System.Uri serviceUri) : 
                this(global::OpenRiaServices.Client.DomainContext.CreateDomainClient(typeof(global::RootNamespace.TestNamespace.AuthenticationService2.IAuthenticationService2Contract), serviceUri, false))
        {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationService2"/> class with the specified <paramref name="domainClient"/>.
        /// </summary>
        /// <param name="domainClient">The DomainClient instance to use for this DomainContext.</param>
        public AuthenticationService2(global::OpenRiaServices.Client.DomainClient domainClient) : 
                base(domainClient)
        {
            this.OnCreated();
        }
        
        /// <summary>
        /// Gets the set of <see cref="User2"/> entity instances that have been loaded into this <see cref="AuthenticationService2"/> instance.
        /// </summary>
        public global::OpenRiaServices.Client.EntitySet<global::RootNamespace.TestNamespace.User2> User2s
        {
            get
            {
                return base.EntityContainer.GetEntitySet<global::RootNamespace.TestNamespace.User2>();
            }
        }
        
        /// <summary>
        /// Gets an EntityQuery instance that can be used to load <see cref="User2"/> entity instances using the 'GetUser' query.
        /// </summary>
        /// <returns>An EntityQuery that can be loaded to retrieve <see cref="User2"/> entity instances.</returns>
        public global::OpenRiaServices.Client.EntityQuery<global::RootNamespace.TestNamespace.User2> GetUserQuery()
        {
            this.ValidateMethod("GetUserQuery", null);
            return base.CreateQuery<global::RootNamespace.TestNamespace.User2>("GetUser", null, false, false);
        }
        
        /// <summary>
        /// Gets an EntityQuery instance that can be used to load <see cref="User2"/> entity instances using the 'Login' query.
        /// </summary>
        /// <param name="userName">The value for the 'userName' parameter of the query.</param>
        /// <param name="password">The value for the 'password' parameter of the query.</param>
        /// <param name="isPersistent">The value for the 'isPersistent' parameter of the query.</param>
        /// <param name="customData">The value for the 'customData' parameter of the query.</param>
        /// <returns>An EntityQuery that can be loaded to retrieve <see cref="User2"/> entity instances.</returns>
        public global::OpenRiaServices.Client.EntityQuery<global::RootNamespace.TestNamespace.User2> LoginQuery(string userName, string password, bool isPersistent, string customData)
        {
            global::System.Collections.Generic.Dictionary<string, object> parameters = new global::System.Collections.Generic.Dictionary<string, object>();
            parameters.Add("userName", userName);
            parameters.Add("password", password);
            parameters.Add("isPersistent", isPersistent);
            parameters.Add("customData", customData);
            this.ValidateMethod("LoginQuery", parameters);
            return base.CreateQuery<global::RootNamespace.TestNamespace.User2>("Login", parameters, true, false);
        }
        
        /// <summary>
        /// Gets an EntityQuery instance that can be used to load <see cref="User2"/> entity instances using the 'Logout' query.
        /// </summary>
        /// <returns>An EntityQuery that can be loaded to retrieve <see cref="User2"/> entity instances.</returns>
        public global::OpenRiaServices.Client.EntityQuery<global::RootNamespace.TestNamespace.User2> LogoutQuery()
        {
            this.ValidateMethod("LogoutQuery", null);
            return base.CreateQuery<global::RootNamespace.TestNamespace.User2>("Logout", null, true, false);
        }
        
        /// <summary>
        /// Creates a new EntityContainer for this DomainContext's EntitySets.
        /// </summary>
        /// <returns>A new container instance.</returns>
        protected override global::OpenRiaServices.Client.EntityContainer CreateEntityContainer()
        {
            return new global::RootNamespace.TestNamespace.AuthenticationService2.AuthenticationService2EntityContainer();
        }
        
        /// <summary>
        /// Service contract for the 'AuthenticationService2' DomainService.
        /// </summary>
        [global::System.ServiceModel.ServiceContractAttribute()]
        public interface IAuthenticationService2Contract
        {
            
            /// <summary>
            /// Asynchronously invokes the 'GetUser' operation.
            /// </summary>
            /// <param name="callback">Callback to invoke on completion.</param>
            /// <param name="asyncState">Optional state object.</param>
            /// <returns>An IAsyncResult that can be used to monitor the request.</returns>
            [global::OpenRiaServices.Client.HasSideEffects(false)]
            [global::System.ServiceModel.OperationContractAttribute(AsyncPattern=true, Action="http://tempuri.org/AuthenticationService2/GetUser", ReplyAction="http://tempuri.org/AuthenticationService2/GetUserResponse")]
            global::System.IAsyncResult BeginGetUser(global::System.AsyncCallback callback, object asyncState);
            
            /// <summary>
            /// Completes the asynchronous operation begun by 'BeginGetUser'.
            /// </summary>
            /// <param name="result">The IAsyncResult returned from 'BeginGetUser'.</param>
            /// <returns>The 'QueryResult' returned from the 'GetUser' operation.</returns>
            global::OpenRiaServices.Client.QueryResult<global::RootNamespace.TestNamespace.User2> EndGetUser(global::System.IAsyncResult result);
            
            /// <summary>
            /// Asynchronously invokes the 'Login' operation.
            /// </summary>
            /// <param name="userName">The value for the 'userName' parameter of this action.</param>
            /// <param name="password">The value for the 'password' parameter of this action.</param>
            /// <param name="isPersistent">The value for the 'isPersistent' parameter of this action.</param>
            /// <param name="customData">The value for the 'customData' parameter of this action.</param>
            /// <param name="callback">Callback to invoke on completion.</param>
            /// <param name="asyncState">Optional state object.</param>
            /// <returns>An IAsyncResult that can be used to monitor the request.</returns>
            [global::OpenRiaServices.Client.HasSideEffects(true)]
            [global::System.ServiceModel.OperationContractAttribute(AsyncPattern=true, Action="http://tempuri.org/AuthenticationService2/Login", ReplyAction="http://tempuri.org/AuthenticationService2/LoginResponse")]
            global::System.IAsyncResult BeginLogin(string userName, string password, bool isPersistent, string customData, global::System.AsyncCallback callback, object asyncState);
            
            /// <summary>
            /// Completes the asynchronous operation begun by 'BeginLogin'.
            /// </summary>
            /// <param name="result">The IAsyncResult returned from 'BeginLogin'.</param>
            /// <returns>The 'QueryResult' returned from the 'Login' operation.</returns>
            global::OpenRiaServices.Client.QueryResult<global::RootNamespace.TestNamespace.User2> EndLogin(global::System.IAsyncResult result);
            
            /// <summary>
            /// Asynchronously invokes the 'Logout' operation.
            /// </summary>
            /// <param name="callback">Callback to invoke on completion.</param>
            /// <param name="asyncState">Optional state object.</param>
            /// <returns>An IAsyncResult that can be used to monitor the request.</returns>
            [global::OpenRiaServices.Client.HasSideEffects(true)]
            [global::System.ServiceModel.OperationContractAttribute(AsyncPattern=true, Action="http://tempuri.org/AuthenticationService2/Logout", ReplyAction="http://tempuri.org/AuthenticationService2/LogoutResponse")]
            global::System.IAsyncResult BeginLogout(global::System.AsyncCallback callback, object asyncState);
            
            /// <summary>
            /// Completes the asynchronous operation begun by 'BeginLogout'.
            /// </summary>
            /// <param name="result">The IAsyncResult returned from 'BeginLogout'.</param>
            /// <returns>The 'QueryResult' returned from the 'Logout' operation.</returns>
            global::OpenRiaServices.Client.QueryResult<global::RootNamespace.TestNamespace.User2> EndLogout(global::System.IAsyncResult result);
            
            /// <summary>
            /// Asynchronously invokes the 'SubmitChanges' operation.
            /// </summary>
            /// <param name="changeSet">The change-set to submit.</param>
            /// <param name="callback">Callback to invoke on completion.</param>
            /// <param name="asyncState">Optional state object.</param>
            /// <returns>An IAsyncResult that can be used to monitor the request.</returns>
            [global::System.ServiceModel.OperationContractAttribute(AsyncPattern=true, Action="http://tempuri.org/AuthenticationService2/SubmitChanges", ReplyAction="http://tempuri.org/AuthenticationService2/SubmitChangesResponse")]
            global::System.IAsyncResult BeginSubmitChanges(global::System.Collections.Generic.IEnumerable<global::OpenRiaServices.Client.ChangeSetEntry> changeSet, global::System.AsyncCallback callback, object asyncState);
            
            /// <summary>
            /// Completes the asynchronous operation begun by 'BeginSubmitChanges'.
            /// </summary>
            /// <param name="result">The IAsyncResult returned from 'BeginSubmitChanges'.</param>
            /// <returns>The collection of change-set entry elements returned from 'SubmitChanges'.</returns>
            global::System.Collections.Generic.IEnumerable<global::OpenRiaServices.Client.ChangeSetEntry> EndSubmitChanges(global::System.IAsyncResult result);
        }
        
        internal sealed class AuthenticationService2EntityContainer : global::OpenRiaServices.Client.EntityContainer
        {
            
            public AuthenticationService2EntityContainer()
            {
                this.CreateEntitySet<global::RootNamespace.TestNamespace.User2>(global::OpenRiaServices.Client.EntitySetOperations.Edit);
            }
        }
    }
    
    /// <summary>
    /// The 'User1' entity class.
    /// </summary>
    [global::System.Runtime.Serialization.DataContractAttribute(Namespace="http://schemas.datacontract.org/2004/07/RootNamespace.TestNamespace")]
    public sealed partial class User1 : global::OpenRiaServices.Client.Entity, global::System.Security.Principal.IIdentity, global::System.Security.Principal.IPrincipal
    {
        
        private string _name = string.Empty;
        
        private global::System.Collections.Generic.IEnumerable<string> _roles;
        
        #region Extensibility Method Definitions

        /// <summary>
        /// This method is invoked from the constructor once initialization is complete and
        /// can be used for further object setup.
        /// </summary>
        partial void OnCreated();
        partial void OnNameChanging(string value);
        partial void OnNameChanged();
        partial void OnRolesChanging(global::System.Collections.Generic.IEnumerable<string> value);
        partial void OnRolesChanged();

        #endregion
        
        
        /// <summary>
        /// Initializes a new instance of the <see cref="User1"/> class.
        /// </summary>
        public User1()
        {
            this.OnCreated();
        }
        
        /// <summary>
        /// Gets or sets the 'Name' value.
        /// </summary>
        [global::System.ComponentModel.DataAnnotations.EditableAttribute(false, AllowInitialValue=true)]
        [global::System.ComponentModel.DataAnnotations.KeyAttribute()]
        [global::System.ComponentModel.DataAnnotations.RoundtripOriginalAttribute()]
        [global::System.Runtime.Serialization.DataMemberAttribute()]
        public string Name
        {
            get
            {
                return this._name;
            }
            set
            {
                if ((this._name != value))
                {
                    this.OnNameChanging(value);
                    this.ValidateProperty("Name", value);
                    this._name = value;
                    this.RaisePropertyChanged("Name");
                    this.OnNameChanged();
                    this.RaisePropertyChanged("IsAuthenticated");
                }
            }
        }
        
        /// <summary>
        /// Gets or sets the 'Roles' value.
        /// </summary>
        [global::System.ComponentModel.DataAnnotations.EditableAttribute(false)]
        [global::System.Runtime.Serialization.DataMemberAttribute()]
        public global::System.Collections.Generic.IEnumerable<string> Roles
        {
            get
            {
                return this._roles;
            }
            set
            {
                if ((this._roles != value))
                {
                    this.OnRolesChanging(value);
                    this.ValidateProperty("Roles", value);
                    this._roles = value;
                    this.RaisePropertyChanged("Roles");
                    this.OnRolesChanged();
                }
            }
        }
        
        string global::System.Security.Principal.IIdentity.AuthenticationType
        {
            get
            {
                return string.Empty;
            }
        }
        
        /// <summary>
        /// Gets a value indicating whether the identity is authenticated.
        /// </summary>
        /// <remarks>
        /// This value is <c>true</c> if <see cref="Name"/> is not <c>null</c> or empty.
        /// </remarks>
        public bool IsAuthenticated
        {
            get
            {
                return (true != string.IsNullOrEmpty(this.Name));
            }
        }
        
        string global::System.Security.Principal.IIdentity.Name
        {
            get
            {
                return this.Name;
            }
        }
        
        global::System.Security.Principal.IIdentity global::System.Security.Principal.IPrincipal.Identity
        {
            get
            {
                return this;
            }
        }
        
        /// <summary>
        /// Computes a value from the key fields that uniquely identifies this entity instance.
        /// </summary>
        /// <returns>An object instance that uniquely identifies this entity instance.</returns>
        public override object GetIdentity()
        {
            return this._name;
        }
        
        /// <summary>
        /// Return whether the principal is in the role.
        /// </summary>
        /// <remarks>
        /// Returns whether the specified role is contained in the roles.
        /// This implementation is case sensitive.
        /// </remarks>
        /// <param name="role">The name of the role for which to check membership.</param>
        /// <returns>Whether the principal is in the role.</returns>
        public bool IsInRole(string role)
        {
            if ((this.Roles == null))
            {
                return false;
            }
            return global::System.Linq.Enumerable.Contains(this.Roles, role);
        }
    }
    
    /// <summary>
    /// The 'User2' entity class.
    /// </summary>
    [global::System.Runtime.Serialization.DataContractAttribute(Namespace="http://schemas.datacontract.org/2004/07/RootNamespace.TestNamespace")]
    public sealed partial class User2 : global::OpenRiaServices.Client.Entity, global::System.Security.Principal.IIdentity, global::System.Security.Principal.IPrincipal
    {
        
        private string _name = string.Empty;
        
        private global::System.Collections.Generic.IEnumerable<string> _roles;
        
        #region Extensibility Method Definitions

        /// <summary>
        /// This method is invoked from the constructor once initialization is complete and
        /// can be used for further object setup.
        /// </summary>
        partial void OnCreated();
        partial void OnNameChanging(string value);
        partial void OnNameChanged();
        partial void OnRolesChanging(global::System.Collections.Generic.IEnumerable<string> value);
        partial void OnRolesChanged();

        #endregion
        
        
        /// <summary>
        /// Initializes a new instance of the <see cref="User2"/> class.
        /// </summary>
        public User2()
        {
            this.OnCreated();
        }
        
        /// <summary>
        /// Gets or sets the 'Name' value.
        /// </summary>
        [global::System.ComponentModel.DataAnnotations.EditableAttribute(false, AllowInitialValue=true)]
        [global::System.ComponentModel.DataAnnotations.KeyAttribute()]
        [global::System.ComponentModel.DataAnnotations.RoundtripOriginalAttribute()]
        [global::System.Runtime.Serialization.DataMemberAttribute()]
        public string Name
        {
            get
            {
                return this._name;
            }
            set
            {
                if ((this._name != value))
                {
                    this.OnNameChanging(value);
                    this.ValidateProperty("Name", value);
                    this._name = value;
                    this.RaisePropertyChanged("Name");
                    this.OnNameChanged();
                    this.RaisePropertyChanged("IsAuthenticated");
                }
            }
        }
        
        /// <summary>
        /// Gets or sets the 'Roles' value.
        /// </summary>
        [global::System.ComponentModel.DataAnnotations.EditableAttribute(false)]
        [global::System.Runtime.Serialization.DataMemberAttribute()]
        public global::System.Collections.Generic.IEnumerable<string> Roles
        {
            get
            {
                return this._roles;
            }
            set
            {
                if ((this._roles != value))
                {
                    this.OnRolesChanging(value);
                    this.ValidateProperty("Roles", value);
                    this._roles = value;
                    this.RaisePropertyChanged("Roles");
                    this.OnRolesChanged();
                }
            }
        }
        
        string global::System.Security.Principal.IIdentity.AuthenticationType
        {
            get
            {
                return string.Empty;
            }
        }
        
        /// <summary>
        /// Gets a value indicating whether the identity is authenticated.
        /// </summary>
        /// <remarks>
        /// This value is <c>true</c> if <see cref="Name"/> is not <c>null</c> or empty.
        /// </remarks>
        public bool IsAuthenticated
        {
            get
            {
                return (true != string.IsNullOrEmpty(this.Name));
            }
        }
        
        string global::System.Security.Principal.IIdentity.Name
        {
            get
            {
                return this.Name;
            }
        }
        
        global::System.Security.Principal.IIdentity global::System.Security.Principal.IPrincipal.Identity
        {
            get
            {
                return this;
            }
        }
        
        /// <summary>
        /// Computes a value from the key fields that uniquely identifies this entity instance.
        /// </summary>
        /// <returns>An object instance that uniquely identifies this entity instance.</returns>
        public override object GetIdentity()
        {
            return this._name;
        }
        
        /// <summary>
        /// Return whether the principal is in the role.
        /// </summary>
        /// <remarks>
        /// Returns whether the specified role is contained in the roles.
        /// This implementation is case sensitive.
        /// </remarks>
        /// <param name="role">The name of the role for which to check membership.</param>
        /// <returns>Whether the principal is in the role.</returns>
        public bool IsInRole(string role)
        {
            if ((this.Roles == null))
            {
                return false;
            }
            return global::System.Linq.Enumerable.Contains(this.Roles, role);
        }
    }
}
