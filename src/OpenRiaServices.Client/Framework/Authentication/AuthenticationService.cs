using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

namespace OpenRiaServices.Client.Authentication
{
    /// <summary>
    /// Service that is responsible for authenticating, loading, and saving the current user. 
    /// </summary>
    /// <remarks>
    /// This abstract base exposes <c>Login</c>, <c>Logout</c>, <c>LoadUser</c>, and
    /// <c>SaveUser</c> as asynchronous operations. It also provides a number of properties
    /// that can be bound to including <see cref="IsBusy"/> and <see cref="User"/>.
    /// <para>
    /// Concrete implementations will have a much different view of this class through a
    /// number of abstract template methods. These methods follow the async result pattern
    /// and are presented in Begin/End pairs for each operation. Optionally, cancel methods
    /// for each operation can also be implemented.
    /// </para>
    /// </remarks>
    public abstract class AuthenticationService : INotifyPropertyChanged
    {
        private readonly object _syncLock = new object();
        private IPrincipal _user;
        private PropertyChangedEventHandler _propertyChangedEventHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationService"/> class.
        /// </summary>
        protected AuthenticationService()
        {
        }

        /// <summary>
        /// Raised when a new user is successfully logged in.
        /// </summary>
        /// <remarks>
        /// This event is raised either when <see cref="User"/> changes from anonymous to
        /// authenticated or when it changes from one authenticated identity to another.
        /// </remarks>
        public event EventHandler<AuthenticationEventArgs> LoggedIn;

        /// <summary>
        /// Raised when a user is successfully logged out.
        /// </summary>
        /// <remarks>
        /// This event is raised when <see cref="User"/> changes from authenticated to
        /// anonymous.
        /// </remarks>
        public event EventHandler<AuthenticationEventArgs> LoggedOut;

        /// <summary>
        /// Gets a value indicating whether an asynchronous operation is in progress
        /// </summary>
        public bool IsBusy => this.Operation != null;

        /// <summary>
        /// Gets a value indicating whether an asynchronous <c>Login</c> operation is in progress
        /// </summary>
        public bool IsLoggingIn => this.Operation is LoginOperation;

        /// <summary>
        /// Gets a value indicating whether an asynchronous <c>Logout</c> operation is in progress
        /// </summary>
        public bool IsLoggingOut => this.Operation is LogoutOperation;

        /// <summary>
        /// Gets a value indicating whether an asynchronous <c>LoadUser</c> operation is in progress
        /// </summary>
        public bool IsLoadingUser => this.Operation is LoadUserOperation;

        /// <summary>
        /// Gets a value indicating whether an asynchronous <c>SaveUser</c> operation is in progress
        /// </summary>
        public bool IsSavingUser => this.Operation is SaveUserOperation;

        /// <summary>
        /// Gets the current user.
        /// </summary>
        /// <remarks>
        /// This value may be updated by the <c>Login</c>, <c>Logout</c>, and <c>LoadUser</c>
        /// operations. Prior to one of those methods completing successfully, this property
        /// will contain a default user.
        /// </remarks>
        public IPrincipal User
        {
            get
            {
                if (this._user == null)
                {
                    this._user = this.CreateDefaultUser();
                    if (this._user == null)
                    {
                        throw new InvalidOperationException(Resources.ApplicationServices_UserIsNull);
                    }
                }
                return this._user;
            }
        }

        /// <summary>
        /// Gets or sets the current operation.
        /// </summary>
        /// <remarks>
        /// Only one operation can be active at a time. This property should not be set directly
        /// but instead can be modified via the <see cref="StartOperation"/> method.
        /// </remarks>
        private protected AuthenticationOperation Operation { get; private set; }

        /// <summary>
        /// Asynchronously authenticates and logs in to the server with the specified parameters.
        /// </summary>
        /// <remarks>
        /// This method starts an operation with no complete action or user state. If this method
        /// returns normally, a <see cref="LoggedIn"/> event may be raised. Also, successful
        /// completion of this operation will update the <see cref="User"/>.
        /// </remarks>
        /// <param name="userName">The username associated with the user to authenticate</param>
        /// <param name="password">the password associated with the user to authenticate</param>
        /// <returns>Returns the login operation.</returns>
        /// <exception cref="InvalidOperationException"> is thrown if this method is called while
        /// another asynchronous operation is still being processed.
        /// </exception>
        /// <seealso cref="LoggedIn"/>
        public LoginOperation Login(string userName, string password)
        {
            return this.Login(new LoginParameters(userName, password));
        }

        /// <summary>
        /// Asynchronously authenticates and logs in to the server with the specified parameters.
        /// </summary>
        /// <remarks>
        /// This method starts an operation with no complete action or user state. If this method
        /// returns normally, a <see cref="LoggedIn"/> event may be raised. Also, successful
        /// completion of this operation will update the <see cref="User"/>.
        /// </remarks>
        /// <param name="parameters">Login parameters that specify the user to authenticate</param>
        /// <returns>Returns the login operation.</returns>
        /// <exception cref="InvalidOperationException"> is thrown if this method is called while
        /// another asynchronous operation is still being processed.
        /// </exception>
        /// <seealso cref="LoggedIn"/>
        public LoginOperation Login(LoginParameters parameters)
        {
            return this.Login(parameters, null, null);
        }

        /// <summary>
        /// Asynchronously authenticates and logs in to the server with the specified parameters.
        /// </summary>
        /// <remarks>
        /// If this method returns normally, a <see cref="LoggedIn"/> event may be raised. Also,
        /// successful completion of this operation will update the <see cref="User"/>.
        /// </remarks>
        /// <param name="parameters">Login parameters that specify the user to authenticate</param>
        /// <param name="completeAction">This action will be invoked immediately after the operation
        /// completes and is called in all cases including success, cancellation, and error. This
        /// parameter is optional.
        /// </param>
        /// <param name="userState">This state will be set into
        /// <see cref="OperationBase.UserState"/>. This parameter is optional.
        /// </param>
        /// <returns>Returns the login operation.</returns>
        /// <exception cref="InvalidOperationException"> is thrown if this method is called while
        /// another asynchronous operation is still being processed.
        /// </exception>
        /// <seealso cref="LoggedIn"/>
        public LoginOperation Login(LoginParameters parameters, Action<LoginOperation> completeAction, object userState)
        {
            this.StartOperation(new LoginOperation(this, parameters, completeAction, userState));

            return (LoginOperation)this.Operation;
        }

        /// <summary>
        /// Asynchronously logs an authenticated user out from the server.
        /// </summary>
        /// <remarks>
        /// This method starts an operation with no complete action or user state. If this method
        /// returns normally, a <see cref="LoggedOut"/> event may be raised. Also, successful
        /// completion of this operation will update the <see cref="User"/>.
        /// </remarks>
        /// <param name="throwOnError">True if an unhandled error should result in an exception, false otherwise.
        /// To handle an operation error, <see cref="OperationBase.MarkErrorAsHandled"/> can be called from the
        /// operation completion callback or from a <see cref="OperationBase.Completed"/> event handler.
        /// </param>
        /// <returns>Returns the logout operation.</returns>
        /// <exception cref="InvalidOperationException"> is thrown if this method is called while
        /// another asynchronous operation is still being processed.
        /// </exception>
        /// <seealso cref="LoggedOut"/>
        public LogoutOperation Logout(bool throwOnError)
        {
            var callback = !throwOnError ? AuthenticationService.HandleOperationError : (Action<LogoutOperation>)null;
            return this.Logout(callback, null);
        }

        /// <summary>
        /// Asynchronously logs an authenticated user out from the server.
        /// </summary>
        /// <remarks>
        /// If this method returns normally, a <see cref="LoggedOut"/> event may be raised. Also,
        /// successful completion of this operation will update the <see cref="User"/>.
        /// </remarks>
        /// <param name="completeAction">This action will be invoked immediately after the operation
        /// completes and is called in all cases including success, cancellation, and error. This
        /// parameter is optional.
        /// </param>
        /// <param name="userState">This state will be set into
        /// <see cref="OperationBase.UserState"/>. This parameter is optional.
        /// </param>
        /// <returns>Returns the logout operation.</returns>
        /// <exception cref="InvalidOperationException"> is thrown if this method is called while
        /// another asynchronous operation is still being processed.
        /// </exception>
        /// <seealso cref="LoggedOut"/>
        public LogoutOperation Logout(Action<LogoutOperation> completeAction, object userState)
        {
            this.StartOperation(new LogoutOperation(this, completeAction, userState));

            return (LogoutOperation)this.Operation;
        }

        /// <summary>
        /// Asynchronously loads the authenticated user from the server.
        /// </summary>
        /// <remarks>
        /// This method starts an operation with no complete action or user state. Successful
        /// completion of this operation will update the <see cref="User"/>.
        /// </remarks>
        /// <returns>Returns the load user operation.</returns>
        /// <exception cref="InvalidOperationException"> is thrown if this method is called while
        /// another asynchronous operation is still being processed.
        /// </exception>
        public LoadUserOperation LoadUser()
        {
            return this.LoadUser(null, null);
        }

        /// <summary>
        /// Asynchronously loads the authenticated user from the server.
        /// </summary>
        /// <remarks>
        /// Successful completion of this operation will update the <see cref="User"/>.
        /// </remarks>
        /// <param name="completeAction">This action will be invoked immediately after the operation
        /// completes and is called in all cases including success, cancellation, and error. This
        /// parameter is optional.
        /// </param>
        /// <param name="userState">This state will be set into
        /// <see cref="OperationBase.UserState"/>. This parameter is optional.
        /// </param>
        /// <returns>Returns the load user operation.</returns>
        /// <exception cref="InvalidOperationException"> is thrown if this method is called while
        /// another asynchronous operation is still being processed.
        /// </exception>
        public LoadUserOperation LoadUser(Action<LoadUserOperation> completeAction, object userState)
        {
            this.StartOperation(new LoadUserOperation(this, completeAction, userState));

            return (LoadUserOperation)this.Operation;
        }

        /// <summary>
        /// Asynchronously saves the authenticated user to the server.
        /// </summary>
        /// <remarks>
        /// This method starts an operation with no complete action or user state.
        /// </remarks>
        /// <param name="throwOnError">True if an unhandled error should result in an exception, false otherwise.
        /// To handle an operation error, <see cref="OperationBase.MarkErrorAsHandled"/> can be called from the
        /// operation completion callback or from a <see cref="OperationBase.Completed"/> event handler.
        /// </param>
        /// <returns>Returns the save user operation.</returns>
        /// <exception cref="InvalidOperationException"> is thrown if this method is called while
        /// another asynchronous operation is still being processed.
        /// </exception>
        public SaveUserOperation SaveUser(bool throwOnError)
        {
            var callback = !throwOnError ? AuthenticationService.HandleOperationError : (Action<SaveUserOperation>)null;
            return this.SaveUser(callback, null);
        }

        /// <summary>
        /// Asynchronously saves the authenticated user to the server.
        /// </summary>
        /// <param name="completeAction">This action will be invoked immediately after the operation
        /// completes and is called in all cases including success, cancellation, and error. This
        /// parameter is optional.
        /// </param>
        /// <param name="userState">This state will be set into
        /// <see cref="OperationBase.UserState"/>. This parameter is optional.
        /// </param>
        /// <returns>Returns the save user operation.</returns>
        /// <exception cref="InvalidOperationException"> is thrown if this method is called while
        /// another asynchronous operation is still being processed.
        /// </exception>
        public SaveUserOperation SaveUser(Action<SaveUserOperation> completeAction, object userState)
        {
            this.StartOperation(new SaveUserOperation(this, completeAction, userState));

            return (SaveUserOperation)this.Operation;
        }

        /// <summary>
        /// Starts an asynchronous operation if one is not already in progress
        /// </summary>
        /// <param name="operation">The operation to start</param>
        /// <exception cref="InvalidOperationException"> is returned if this method is called while
        /// another asynchronous operation is still being processed.
        /// </exception>
        private void StartOperation(AuthenticationOperation operation)
        {
            Debug.Assert(operation != null, "The operation cannot be null.");
            lock (this._syncLock)
            {
                if (this.IsBusy)
                {
                    throw new InvalidOperationException(Resources.ApplicationServices_UserServiceIsBusy);
                }
                this.Operation = operation;
            }

            try
            {
                var task = operation.InvokeAsync(operation.CancellationToken);
                // Continue on same SynchronizationContext
                var scheduler = SynchronizationContext.Current != null ? TaskScheduler.FromCurrentSynchronizationContext() : TaskScheduler.Default;
                task.ContinueWith(StartOperationComplete, operation, CancellationToken.None, TaskContinuationOptions.HideScheduler, scheduler);
            }
            catch (Exception)
            {
                this.Operation = null;
                throw;
            }

            this.RaisePropertyChanged(nameof(IsBusy));
            this.RaisePropertyChanged(AuthenticationService.GetBusyPropertyName(operation));
        }

        /// <summary>
        /// This is run on the calling SynchronizationContext when an operation started
        /// by <see cref="StartOperation"/> completes
        /// </summary>
        private void StartOperationComplete(Task<AuthenticationResult> res, object state)
        {
            var operation = (AuthenticationOperation)state;
            AuthenticationResult endResult = null;

            bool raiseUserChanged = false;
            bool raiseLoggedIn = false;
            bool raiseLoggedOut = false;

            // Setting the operation to null indicates the service is no longer busy and
            // can process another operation
            this.Operation = null;
            try
            {
                if (res.IsCanceled)
                {
                    operation.SetCancelled();
                    return;
                }

                try
                {
                    endResult = res.GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    operation.SetError(ex);

                    if (ex.IsFatal())
                    {
                        throw;
                    }

                    return;
                }

                // If the operation completed successfully, update the user and 
                // determine which events should be raised
                IPrincipal currentUser = this._user;
                if (endResult?.User != null && currentUser != endResult.User)
                {
                    raiseLoggedIn =
                        // anonymous -> authenticated
                        currentUser == null
                        || (!currentUser.Identity.IsAuthenticated && endResult.User.Identity.IsAuthenticated)
                        // authenticated -> authenticated
                        || (endResult.User.Identity.IsAuthenticated && currentUser.Identity.Name != endResult.User.Identity.Name);
                    raiseLoggedOut =
                        // authenticated -> anonymous
                        currentUser != null
                        && currentUser.Identity.IsAuthenticated && !endResult.User.Identity.IsAuthenticated;

                    this._user = endResult.User;
                    raiseUserChanged = true;
                }
                operation.Complete(endResult);
            }
            finally
            {
                // Raise notification events as appropriate
                if (raiseUserChanged)
                {
                    this.RaisePropertyChanged(nameof(User));
                }
                this.RaisePropertyChanged(nameof(IsBusy));
                this.RaisePropertyChanged(AuthenticationService.GetBusyPropertyName(operation));

                if (raiseLoggedIn)
                {
                    this.LoggedIn?.Invoke(this, new AuthenticationEventArgs(endResult.User));
                }
                if (raiseLoggedOut)
                {
                    this.LoggedOut?.Invoke(this, new AuthenticationEventArgs(endResult.User));
                }
            }
        }

        /// <summary>
        /// Returns the name of the "busy" property for the specified operation
        /// </summary>
        /// <param name="operation">The operation to get the property name for</param>
        /// <returns>The name of the "busy" property for the operation</returns>
        /// <seealso cref="IsLoggingIn"/>
        /// <seealso cref="IsLoggingOut"/>
        /// <seealso cref="IsLoadingUser"/>
        /// <seealso cref="IsSavingUser"/>
        private static string GetBusyPropertyName(AuthenticationOperation operation)
        {
            Debug.Assert(operation != null, "The operation cannot be null.");

            return operation switch
            {
                LoginOperation _ => nameof(IsLoggingIn),
                LogoutOperation _ => nameof(IsLoggingOut),
                LoadUserOperation _ => nameof(IsLoadingUser),
                SaveUserOperation _ => nameof(IsSavingUser),
                _ => throw new NotImplementedException("unknown operation type"),
            };
        }

        /// <summary>
        /// Raises a <see cref="INotifyPropertyChanged.PropertyChanged"/> event for the specified property.
        /// </summary>
        /// <param name="propertyName">The property to raise an event for</param>
        /// <exception cref="ArgumentNullException"> is thrown if the <paramref name="propertyName"/>
        /// is null.
        /// </exception>
        protected void RaisePropertyChanged(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            this._propertyChangedEventHandler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Marks an operation in error as handled.
        /// </summary>
        /// <param name="ao">The operation in error.</param>
        private static void HandleOperationError(AuthenticationOperation ao)
        {
            if (ao.HasError)
            {
                ao.MarkErrorAsHandled();
            }
        }

        /// <summary>
        /// Gets a value indicating whether this authentication implementation supports
        /// cancellation.
        /// </summary>
        /// <remarks>
        /// This value is <c>false</c> by default. When a derived class sets it to <c>true</c>,
        /// it is assumed that all cancellation methods have also been implemented.
        /// </remarks>
        protected internal virtual bool SupportsCancellation
        {
            get { return false; }
        }

        /// <summary>
        /// Creates a default user.
        /// </summary>
        /// <remarks>
        /// This method will be invoked by <see cref="User"/> when it is accessed before the value
        /// is set. The returned value is then stored and returned on all subsequent gets until a
        /// <c>Login</c>, <c>Logout</c>, or <c>LoadUser</c> operation completes successfully.
        /// </remarks>
        /// <returns>A default user. This value may not be <c>null</c>.</returns>
        /// <exception cref="InvalidOperationException"> is thrown from <see cref="User"/> if
        /// this operation returns <c>null</c>.
        /// </exception>
        protected abstract IPrincipal CreateDefaultUser();

        /// <summary>
        /// Begins an asynchronous <c>Login</c> operation.
        /// </summary>
        /// <remarks>
        /// This method is invoked from <c>Login</c>. Exceptions thrown from this method will
        /// prevent the operation from starting and then be thrown from <c>Login</c>.
        /// </remarks>
        /// <param name="parameters">Login parameters that specify the user to authenticate. This
        /// parameter is optional.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The result of the login operation in case request was completed without exceptions</returns>
        protected internal abstract Task<LoginResult> LoginAsync(LoginParameters parameters, CancellationToken cancellationToken);

        /// <summary>
        /// Begins an asynchronous <c>Logout</c> operation.
        /// </summary>
        /// <remarks>
        /// This method is invoked from <c>Logout</c>. Exceptions thrown from this method will
        /// prevent the operation from starting and then be thrown from <c>Logout</c>.
        /// </remarks>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The result of the logout operation in case request was completed without exceptions</returns>
        protected internal abstract Task<LogoutResult> LogoutAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Begins an asynchronous <c>LoadUser</c> operation.
        /// </summary>
        /// <remarks>
        /// This method is invoked from <c>LoadUser</c>. Exceptions thrown from this method will
        /// prevent the operation from starting and then be thrown from <c>LoadUser</c>.
        /// </remarks>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The result of the load operation in case request was completed without exceptions</returns>
        protected internal abstract Task<LoadUserResult> LoadUserAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Begins an asynchronous <c>SaveUser</c> operation.
        /// </summary>
        /// <remarks>
        /// This method is invoked from <c>SaveUser</c>. Exceptions thrown from this method will
        /// prevent the operation from starting and then be thrown from <c>SaveUser</c>.
        /// </remarks>
        /// <param name="user">The user to save. This parameter will not be null.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The result of the save operation in case request was completed without exceptions</returns>
        protected internal abstract Task<SaveUserResult> SaveUserAsync(IPrincipal user, CancellationToken cancellationToken);

        /// <summary>
        /// Raised every time a property value changes. See <see cref="INotifyPropertyChanged.PropertyChanged"/>.
        /// </summary>
        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add
            {
                this._propertyChangedEventHandler = (PropertyChangedEventHandler)Delegate.Combine(this._propertyChangedEventHandler, value);
            }
            remove
            {
                this._propertyChangedEventHandler = (PropertyChangedEventHandler)Delegate.Remove(this._propertyChangedEventHandler, value);
            }
        }
    }
}
