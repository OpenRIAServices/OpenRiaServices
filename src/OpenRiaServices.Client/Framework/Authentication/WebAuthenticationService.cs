using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace OpenRiaServices.Client.Authentication
{
    /// <summary>
    /// Abstract extension of the <see cref="AuthenticationService"/> that
    /// interacts with a <see cref="DomainContext"/> generated from a domain
    /// service implementing
    /// <c>OpenRiaServices.Server.Authentication.IAuthentication&lt;T&gt;</c>.
    /// </summary>
    public abstract class WebAuthenticationService : AuthenticationService
    {
        private const string LoginQueryName = "LoginQuery";
        private const string LogoutQueryName = "LogoutQuery";
        private const string LoadUserQueryName = "GetUserQuery";

        private readonly object _syncLock = new object();

        private bool _initialized;
        private string _domainContextType;
        private AuthenticationDomainContextBase _domainContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebAuthenticationService"/> class.
        /// </summary>
        private protected WebAuthenticationService()
        {
        }

        /// <summary>
        /// Gets or sets the type of the domain context.
        /// </summary>
        /// <remarks>
        /// If the <see cref="DomainContext"/> is not set when this service is
        /// started, it will instantiate a context specified by the
        /// <see cref="DomainContextType"/>. In determining the type, this
        /// string is treated as the full name of a type in the application 
        /// assembly. If the initial search does not return a valid type, this 
        /// string is treated as the assembly qualified name of a type.
        /// </remarks>
        /// <exception cref="InvalidOperationException"> is thrown if this
        /// property is set after the service is started.
        /// </exception>
        public string DomainContextType
        {
            get
            {
                return this._domainContextType;
            }

            set
            {
                this.AssertIsNotActive();
                this._domainContextType = value;
            }
        }

        /// <summary>
        /// Gets the domain context this service delegates authenticating, loading, 
        /// and saving to.
        /// </summary>
        /// <exception cref="InvalidOperationException"> is thrown if this
        /// property is set after the service is started.
        /// </exception>
        public AuthenticationDomainContextBase DomainContext
        {
            get
            {
                return this._domainContext;
            }

            set
            {
                this.AssertIsNotActive();
                this._domainContext = value;
            }
        }

        /// <summary>
        /// Gets a value that indicates whether this service supports cancellation.
        /// </summary>
        /// <remarks>
        /// This implementation always returns <c>true</c>.
        /// </remarks>
        protected internal override bool SupportsCancellation
        {
            get { return true; }
        }

        /// <summary>
        /// Creates a default user.
        /// </summary>
        /// <remarks>
        /// Creates a user using the default constructor.
        /// </remarks>
        /// <returns>A default user</returns>
        /// <exception cref="InvalidOperationException"> is thrown if the
        /// <see cref="WebAuthenticationService.DomainContext"/> is <c>null</c> and a new instance
        /// cannot be created.
        /// </exception>
        protected override IPrincipal CreateDefaultUser()
        {
            this.Initialize();

            IPrincipal user = null;
            ConstructorInfo userConstructor = this.DomainContext.UserType.GetConstructor(TypeUtility.EmptyTypes);

            if (userConstructor != null)
            {
                try
                {
                    user = (IPrincipal)userConstructor.Invoke(TypeUtility.EmptyTypes);
                }
                catch (TargetInvocationException tie)
                {
                    if (tie.InnerException != null)
                    {
                        throw tie.InnerException;
                    }
                    throw;
                }
            }

            if (user == null)
            {
                throw new InvalidOperationException(Resources.ApplicationServices_CannotInitializeUser);
            }

            return user;
        }

        /// <summary>
        /// Begins an asynchronous <c>Login</c> operation.
        /// </summary>
        /// <param name="parameters">Login parameters that specify the user to authenticate</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <exception cref="InvalidOperationException"> is thrown if the
        /// <see cref="WebAuthenticationService.DomainContext"/> is <c>null</c> and a new instance
        /// cannot be created.
        /// </exception>
        /// <returns>The result of the login operation in case request was completed without exceptions</returns>
        protected internal override Task<LoginResult> LoginAsync(LoginParameters parameters, CancellationToken cancellationToken)
        {
            this.Initialize();

            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            EntityQuery query;

            try
            {
                query = (EntityQuery)this.DomainContext.GetType().GetMethod(
                    WebAuthenticationService.LoginQueryName,
                    new Type[] { typeof(string), typeof(string), typeof(bool), typeof(string) }).Invoke(
                    this.DomainContext,
                    new object[] { parameters.UserName, parameters.Password, parameters.IsPersistent, parameters.CustomData });
            }
            catch (TargetInvocationException tie)
            {
                if (tie.InnerException != null)
                {
                    throw tie.InnerException;
                }
                throw;
            }

            Task<ILoadResult> loadTask = LoadAsync(query, cancellationToken);

            return LoadUserImplementation(loadTask);
            async Task<LoginResult> LoadUserImplementation(Task<ILoadResult> loadTask)
            {
                var result = await loadTask.ConfigureAwait(false);

                IPrincipal user = (IPrincipal)result.Entities.SingleOrDefault();
                this.PrepareUser(user);
                return new LoginResult(user, (user != null));
            }
        }

        /// <summary>
        /// Begins an asynchronous <c>Logout</c> operation.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The result of the login operation in case request was completed without exceptions</returns>
        /// <exception cref="InvalidOperationException"> is thrown if the
        /// <see cref="WebAuthenticationService.DomainContext"/> is <c>null</c> and a new instance
        /// cannot be created.
        /// </exception>
        protected internal override Task<LogoutResult> LogoutAsync(CancellationToken cancellationToken)
        {
            this.Initialize();
            EntityQuery query;

            try
            {
                query = (EntityQuery)this.DomainContext.GetType().GetMethod(
                    WebAuthenticationService.LogoutQueryName,
                    TypeUtility.EmptyTypes).Invoke(
                    this.DomainContext,
                    TypeUtility.EmptyTypes);
            }
            catch (TargetInvocationException tie)
            {
                if (tie.InnerException != null)
                {
                    throw tie.InnerException;
                }
                throw;
            }

            Task<ILoadResult> loadTask = LoadAsync(query, cancellationToken);
            return LogoutAsyncContinuation(loadTask);

            async Task<LogoutResult> LogoutAsyncContinuation(Task<ILoadResult> loadTask)
            {
                var result = await loadTask.ConfigureAwait(false);

                IPrincipal user = (IPrincipal)result.Entities.SingleOrDefault();
                if (user == null)
                {
                    throw new InvalidOperationException(Resources.ApplicationServices_LogoutNoUser);
                }
                this.PrepareUser(user);
                return new LogoutResult(user);
            }
        }

        /// <summary>
        /// Begins an asynchronous <c>LoadUser</c> operation.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The result of the login operation in case request was completed without exceptions</returns>
        /// <exception cref="InvalidOperationException"> is thrown if the
        /// <see cref="WebAuthenticationService.DomainContext"/> is <c>null</c> and a new instance
        /// cannot be created.
        /// </exception>
        protected internal override Task<LoadUserResult> LoadUserAsync(CancellationToken cancellationToken)
        {
            this.Initialize();

            EntityQuery query;

            try
            {
                query = (EntityQuery)this.DomainContext.GetType().GetMethod(
                    WebAuthenticationService.LoadUserQueryName,
                    TypeUtility.EmptyTypes).Invoke(
                    this.DomainContext,
                    TypeUtility.EmptyTypes);
            }
            catch (TargetInvocationException tie)
            {
                if (tie.InnerException != null)
                {
                    throw tie.InnerException;
                }
                throw;
            }

            Task<ILoadResult> loadTask = LoadAsync(query, cancellationToken);

            return LoadUserImplementation(loadTask);
            async Task<LoadUserResult> LoadUserImplementation(Task<ILoadResult> loadTask)
            {
                var result = await loadTask.ConfigureAwait(false);

                IPrincipal user = (IPrincipal)result.Entities.SingleOrDefault();
                if (user == null)
                {
                    throw new InvalidOperationException(Resources.ApplicationServices_LoadNoUser);
                }
                this.PrepareUser(user);
                return new LoadUserResult(user);
            }
        }

        /// <summary>
        ///  Helepr method to invoke generic LoadAsync when the type of the result is not known at compile time
        /// </summary>
        private Task<ILoadResult> LoadAsync(EntityQuery query, CancellationToken cancellationToken)
        {
            // Get MethodInfo for Load<TEntity>(EntityQuery<TEntity>, LoadBehavior, Action<LoadOperation<TEntity>>, object, LoadOperation<TEntity>)
            var method = new Func<EntityQuery<Entity>, LoadBehavior, CancellationToken, Task<ILoadResult>>(this.LoadAsyncHelper<Entity>);
            var loadMethod = method.Method.GetGenericMethodDefinition();
            Task<ILoadResult> loadTask;
            try
            {
                loadTask = (Task<ILoadResult>)loadMethod
                    .MakeGenericMethod(query.EntityType)
                    .Invoke(this, new object[] { query, LoadBehavior.MergeIntoCurrent, cancellationToken });
            }
            catch (TargetInvocationException tie)
            {
                if (tie.InnerException != null)
                {
                    throw tie.InnerException;
                }
                throw;
            }

            return loadTask;
        }

        /// <summary>
        ///  Helepr method to invoke generic LoadAsync when the type of the result is not known at compile time
        /// </summary>
        private async Task<ILoadResult> LoadAsyncHelper<T>(EntityQuery<T> query, LoadBehavior loadBehavior, CancellationToken cancellationToken)
            where T : Entity
        {
            return await this.DomainContext.LoadAsync(query, loadBehavior, cancellationToken);
        }

        /// <summary>
        /// Begins an asynchronous <c>SaveUser</c> operation.
        /// </summary>
        /// <param name="user">The authenticated user to save</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The result of the login operation in case request was completed without exceptions</returns>
        /// <exception cref="InvalidOperationException"> is thrown if the user is anonymous.</exception>
        /// <exception cref="InvalidOperationException"> is thrown if the
        /// <see cref="WebAuthenticationService.DomainContext"/> is <c>null</c> and a new instance
        /// cannot be created.
        /// </exception>
        protected internal override Task<SaveUserResult> SaveUserAsync(IPrincipal user, CancellationToken cancellationToken)
        {
            this.Initialize();

            if (!user.Identity.IsAuthenticated)
            {
                throw new InvalidOperationException(Resources.ApplicationServices_CannotSaveAnonymous);
            }

            var task = this.DomainContext.SubmitChangesAsync(cancellationToken);
            return SaveUserContinuation(task);

            async Task<SaveUserResult> SaveUserContinuation(Task<SubmitResult> submitResult)
            {
                try
                {
                    var res = await submitResult.ConfigureAwait(false);
                    IPrincipal user = (IPrincipal)res.ChangeSet.OfType<IPrincipal>().SingleOrDefault();
                    this.PrepareUser(user);
                    return new SaveUserResult(user);

                }
                catch (SubmitOperationException ex)
                {
                    if (ex.EntitiesInError.Count != 0)
                    {
                        throw new InvalidOperationException(Resources.ApplicationServices_SaveErrors);
                    }

                    throw;
                }
            }
        }

        /// <summary>
        /// Initializes this authentication service.
        /// </summary>
        /// <remarks>
        /// This method is invoked before the service is used for the first time from
        /// <see cref="CreateDefaultUser"/> and the <c>BeginXx</c> methods. It can also
        /// be called earlier to ensure the service is ready for use. Subsequent
        /// invocations will not reinitialize the service.
        /// </remarks>
        /// <exception cref="InvalidOperationException"> is thrown if the
        /// <see cref="WebAuthenticationService.DomainContext"/> is <c>null</c> and a new instance
        /// cannot be created.
        /// </exception>
        protected void Initialize()
        {
            lock (this._syncLock)
            {
                if (!this._initialized)
                {
                    this._initialized = true;
                    this.InitializeDomainContext();
                }
            }
        }

        /// <summary>
        /// Initializes the domain context.
        /// </summary>
        /// <remarks>
        /// If the domain context has not already been set, this method trys to instantiate
        /// one specified by the <see cref="DomainContextType"/> string.
        /// </remarks>
        /// <exception cref="InvalidOperationException"> is thrown if the
        /// <see cref="WebAuthenticationService.DomainContext"/> is <c>null</c> and a new instance
        /// cannot be created.
        /// </exception>
        private void InitializeDomainContext()
        {
            if (this._domainContext == null)
            {
                // Get application assembly so we can start searching for web context type there
                Assembly applicationAssembly =
#if SILVERLIGHT
                    Application.Current.GetType().Assembly;
#else
                    Assembly.GetEntryAssembly();
#endif

                Type type = FindDomainContextType(applicationAssembly);
                if ((type != null) && typeof(AuthenticationDomainContextBase).IsAssignableFrom(type))
                {
                    ConstructorInfo constructor = type.GetConstructor(TypeUtility.EmptyTypes);
                    if (constructor != null)
                    {
                        try
                        {
                            this._domainContext = constructor.Invoke(TypeUtility.EmptyTypes) as AuthenticationDomainContextBase;
                        }
                        catch (TargetInvocationException tie)
                        {
                            if (tie.InnerException != null)
                            {
                                throw tie.InnerException;
                            }
                            throw;
                        }
                    }
                }
            }

            if (this._domainContext == null)
            {
                throw new InvalidOperationException(Resources.ApplicationServices_CannotInitializeDomainContext);
            }
        }

        /// <summary>
        /// Search for at AuthenticationDomainContextBase implementation in the specified assembly.
        /// First it tries based on name using <see cref="DomainContextType"/>, then find the first 
        /// implementation deriving from AuthenticationDomainContextBase.
        /// </summary>
        /// <param name="applicationAssembly"></param>
        /// <returns>
        ///  A type which inherits <see cref="AuthenticationDomainContextBase"/> or <c>null</c>
        ///  if no type could be found.
        /// </returns>
        private Type FindDomainContextType(Assembly applicationAssembly)
        {
            Type type = null;
            if (!string.IsNullOrEmpty(this.DomainContextType))
            {
                // First, try to load the type by full name from the application assembly
                type = applicationAssembly?.GetType(this.DomainContextType);
                // If that doesn't work, allow for assembly qualified names
                if (type == null)
                {
                    type = Type.GetType(this.DomainContextType);
                }
            }

            if (type == null && applicationAssembly != null)
            {
                // Finally, we'll look for a domain context that has been generated from a domain 
                // service extending AuthenticationBase<T>. Our CodeProcessor generates these 
                // providers as extending AuthenticationDomainContextBase.
                foreach (Type tempType in applicationAssembly.GetTypes())
                {
                    if (typeof(AuthenticationDomainContextBase).IsAssignableFrom(tempType))
                    {
                        type = tempType;
                        break;
                    }
                }
            }

            return type;
        }

        /// <summary>
        /// Throws an exception if the service is active.
        /// </summary>
        /// <exception cref="InvalidOperationException"> is thrown if the service is active.
        /// </exception>
        private void AssertIsNotActive()
        {
            lock (this._syncLock)
            {
                if (this._initialized)
                {
                    throw new InvalidOperationException(Resources.ApplicationServices_ServiceMustNotBeActive);
                }
            }
        }

        /// <summary>
        /// Prepares the deserialized user to return from an End method.
        /// </summary>
        /// <remarks>
        /// This methods ensures only a single user is present in the entity set of the
        /// <see cref="WebAuthenticationService.DomainContext"/>.
        /// </remarks>
        /// <param name="user">The user to prepare</param>
        private void PrepareUser(IPrincipal user)
        {
            if (user != null)
            {
                this.ClearOldUsers(user);
            }
        }

        /// <summary>
        /// Clears all users but the one specified from the user entity set in the <see cref="DomainContext"/>.
        /// </summary>
        /// <param name="user">The single user to keep in the user entity set</param>
        private void ClearOldUsers(IPrincipal user)
        {
            IEnumerable<IPrincipal> usersToDetach = this.DomainContext.UserSet.OfType<IPrincipal>().Where(u => u != user).ToList();
            foreach (IPrincipal userToDetach in usersToDetach)
            {
                this.DomainContext.UserSet.Detach((Entity)userToDetach);
            }
        }
    }
}
