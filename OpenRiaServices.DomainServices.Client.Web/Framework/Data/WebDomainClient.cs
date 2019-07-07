using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using OpenRiaServices.DomainServices.Client.Web;
using System.Threading;
using System.Threading.Tasks;

#if SILVERLIGHT
using System.Windows;
#endif

namespace OpenRiaServices.DomainServices.Client
{
    /// <summary>
    /// Default <see cref="DomainClient"/> implementation using WCF
    /// </summary>
    /// <typeparam name="TContract">The contract type.</typeparam>
    public sealed class WebDomainClient<TContract> : DomainClient where TContract : class
    {
        internal const string QueryPropertyName = "DomainServiceQuery";
        internal const string IncludeTotalCountPropertyName = "DomainServiceIncludeTotalCount";

        private ChannelFactory<TContract> _channelFactory;
        private WcfDomainClientFactory _webDomainClientFactory;
        private IEnumerable<Type> _knownTypes;
        private Uri _serviceUri;
        private bool _initializedFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebDomainClient&lt;TContract&gt;"/> class.
        /// </summary>
        /// <param name="serviceUri">The domain service Uri</param>
        /// <exception cref="ArgumentNullException"> is thrown if <paramref name="serviceUri"/>
        /// is null.
        /// </exception>
        public WebDomainClient(Uri serviceUri)
            : this(serviceUri, /* usesHttps */ false, (WcfDomainClientFactory)null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebDomainClient&lt;TContract&gt;"/> class.
        /// </summary>
        /// <param name="serviceUri">The domain service Uri</param>
        /// <param name="usesHttps">A value indicating whether the client should contact
        /// the service using an HTTP or HTTPS scheme.
        /// </param>
        /// <exception cref="ArgumentNullException"> is thrown if <paramref name="serviceUri"/>
        /// is null.
        /// </exception>
        /// <exception cref="ArgumentException"> is thrown if <paramref name="serviceUri"/>
        /// is absolute and <paramref name="usesHttps"/> is true.
        /// </exception>
        public WebDomainClient(Uri serviceUri, bool usesHttps)
            : this(serviceUri, usesHttps, (WcfDomainClientFactory)null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebDomainClient&lt;TContract&gt;"/> class.
        /// </summary>
        /// <param name="serviceUri">The domain service Uri</param>
        /// <param name="usesHttps">A value indicating whether the client should contact
        /// the service using an HTTP or HTTPS scheme.
        /// </param>
        /// <param name="channelFactory">The channel factory that creates channels to communicate with the server.</param>
        /// <exception cref="ArgumentNullException"> is thrown if <paramref name="serviceUri"/>
        /// is null.
        /// </exception>
        /// <exception cref="ArgumentException"> is thrown if <paramref name="serviceUri"/>
        /// is absolute and <paramref name="usesHttps"/> is true.
        /// </exception>
        [Obsolete("Use constructor taking a WcfDomainClientFactory instead")]
        public WebDomainClient(Uri serviceUri, bool usesHttps, ChannelFactory<TContract> channelFactory)
         : this(serviceUri, usesHttps, (WcfDomainClientFactory)null)
        {
            this._channelFactory = channelFactory;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebDomainClient&lt;TContract&gt;"/> class.
        /// </summary>
        /// <param name="serviceUri">The domain service Uri</param>
        /// <param name="usesHttps">A value indicating whether the client should contact
        /// the service using an HTTP or HTTPS scheme.
        /// </param>
        /// <param name="domainClientFactory">The domain client factory that creates channels to communicate with the server.</param>
        /// <exception cref="ArgumentNullException"> is thrown if <paramref name="serviceUri"/>
        /// is null.
        /// </exception>
        /// <exception cref="ArgumentException"> is thrown if <paramref name="serviceUri"/>
        /// is absolute and <paramref name="usesHttps"/> is true.
        /// </exception>
        public WebDomainClient(Uri serviceUri, bool usesHttps, WcfDomainClientFactory domainClientFactory)
        {
            if (serviceUri == null)
            {
                throw new ArgumentNullException("serviceUri");
            }

#if !SILVERLIGHT
            if (!serviceUri.IsAbsoluteUri)
            {
                // Relative URIs currently only supported on Silverlight
                throw new ArgumentException(OpenRiaServices.DomainServices.Client.Resource.DomainContext_InvalidServiceUri, "serviceUri");
            }
#endif

            this._serviceUri = serviceUri;
            this.UsesHttps = usesHttps;
            _webDomainClientFactory = domainClientFactory;

#if SILVERLIGHT
            // The domain client should not be initialized at design time
            if (!System.ComponentModel.DesignerProperties.IsInDesignTool)
            {
                this.Initialize();
            }
#endif
        }

        /// <summary>
        /// Gets the absolute path to the domain service.
        /// </summary>
        /// <remarks>
        /// The value returned is either the absolute Uri passed into the constructor, or
        /// an absolute Uri constructed from the relative Uri passed into the constructor.
        /// Relative Uris will be made absolute using the Application Host source.
        /// </remarks>
        public Uri ServiceUri
        {
            get
            {
                // Should this bug be preserved?
                return this._channelFactory?.Endpoint.Address.Uri ?? this._serviceUri;
            }
        }

        /// <summary>
        /// Gets a value that indicates whether the <see cref="DomainClient"/> supports cancellation.
        /// </summary>
        public override bool SupportsCancellation => true;

        /// <summary>
        /// Gets whether a secure connection should be used.
        /// </summary>
        public bool UsesHttps { get; }

        /// <summary>
        /// Gets the <see cref="WcfDomainClientFactory"/> used to create this instance, with fallback to
        /// a new <see cref="WebDomainClientFactory"/> in case it was created manually without using a DomainClientFactory.
        /// </summary>
        private WcfDomainClientFactory WebDomainClientFactory
        {
            get
            {
                return _webDomainClientFactory
#if NETSTANDARD
                    ?? (_webDomainClientFactory = new SoapDomainClientFactory());
#else
                    ?? (_webDomainClientFactory = new WebDomainClientFactory());
#endif
            }
        }

        /// <summary>
        /// Gets the list of known types.
        /// </summary>
        private IEnumerable<Type> KnownTypes
        {
            get
            {
                if (this._knownTypes == null)
                {
                    // KnownTypes is the set of all types we'll need to serialize,
                    // which is the union of the entity types and the framework
                    // message types
                    List<Type> types = this.EntityTypes.ToList();
                    types.Add(typeof(QueryResult));
                    types.Add(typeof(DomainServiceFault));
                    types.Add(typeof(ChangeSetEntry));
                    types.Add(typeof(EntityOperationType));
                    types.Add(typeof(ValidationResultInfo));

                    this._knownTypes = types;
                }
                return this._knownTypes;
            }
        }

        /// <summary>
        /// Gets the channel factory that is used to create channels for communication 
        /// with the server.
        /// </summary>
        public ChannelFactory<TContract> ChannelFactory
        {
            get
            {
#if SILVERLIGHT
                // Initialization prepares the client for use and will fail at design time
                if (System.ComponentModel.DesignerProperties.IsInDesignTool)
                {
                    throw new InvalidOperationException("Domain operations cannot be started at design time.");
                }
                this.Initialize();
#endif
                if (this._channelFactory == null)
                {
                    // TODO: Add overload where KnownTypes are passed in
                    this._channelFactory = WebDomainClientFactory.CreateChannelFactory<TContract>(_serviceUri, UsesHttps);
                }

                if (!this._initializedFactory)
                {
                    foreach (OperationDescription op in this._channelFactory.Endpoint.Contract.Operations)
                    {
                        foreach (Type knownType in this.KnownTypes)
                        {
                            op.KnownTypes.Add(knownType);
                        }
                    }
                    this._initializedFactory = true;
                }

                return this._channelFactory;
            }
        }

#if SILVERLIGHT
        /// <summary>
        /// Initializes this domain client
        /// </summary>
        /// <exception cref="InvalidOperationException"> is thrown if the current application
        /// or its host are <c>null</c>.
        /// </exception>
        private void Initialize()
        {
            this.ComposeAbsoluteServiceUri();
        }
#endif

        /// <summary>
        /// Method called by the framework to begin an asynchronous query operation
        /// </summary>
        /// <param name="query">The query to invoke.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/> to be used for requesting cancellation</param>
        /// <returns>The results returned by the query.</returns>
        /// <exception cref="InvalidOperationException">The specified query does not exist.</exception>
        protected override Task<QueryCompletedResult> QueryAsyncCore(EntityQuery query, CancellationToken cancellationToken)
        {
            TContract channel = this.ChannelFactory.CreateChannel();
            // Pass the query as a message property.
            using (OperationContextScope scope = new OperationContextScope((IContextChannel)channel))
            {
                if (query.Query != null)
                {
                    OperationContext.Current.OutgoingMessageProperties.Add(WebDomainClient<object>.QueryPropertyName, query.Query);
                }
                if (query.IncludeTotalCount)
                {
                    OperationContext.Current.OutgoingMessageProperties.Add(WebDomainClient<object>.IncludeTotalCountPropertyName, true);
                }

                return CallServiceOperation<QueryCompletedResult>(channel,
                    query.QueryName,
                    query.Parameters,
                    (state, asyncResult) =>
                    {
                        IEnumerable<ValidationResult> validationErrors = null;
                        QueryResult returnValue = null;
                        try
                        {
                            returnValue = (QueryResult)EndServiceOperationCall(state, asyncResult);
                        }
                        catch (FaultException<DomainServiceFault> fe)
                        {
                            if (fe.Detail.OperationErrors != null)
                            {
                                validationErrors = fe.Detail.GetValidationErrors();
                            }
                            else
                            {
                                throw WebDomainClient<TContract>.GetExceptionFromServiceFault(fe.Detail);
                            }
                        }

                        if (returnValue != null)
                        {
                            return new QueryCompletedResult(
                                returnValue.GetRootResults().Cast<Entity>(),
                                returnValue.GetIncludedResults().Cast<Entity>(),
                                returnValue.TotalCount,
                                Enumerable.Empty<ValidationResult>());
                        }
                        else
                        {
                            return new QueryCompletedResult(
                                Enumerable.Empty<Entity>(),
                                Enumerable.Empty<Entity>(),
                                /* totalCount */ 0,
                                validationErrors ?? Enumerable.Empty<ValidationResult>());
                        }
                    }
                , cancellationToken);
            }
        }

        /// <summary>
        /// Invokes a method on the specified instance and takes care of unwrapping TargetInvocationException
        /// </summary>
        /// <returns>result of invocation</returns>
        private static object InvokeMethod(MethodInfo method, object instance, object[] parameters)
        {
            try
            {
                return method.Invoke(instance, parameters);
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

        /// <summary>
        /// Submit the specified <see cref="EntityChangeSet"/> to the DomainService, with the results of the operation
        /// being returned on the SubmitCompleted event args.
        /// </summary>
        /// <param name="changeSet">The changeset to submit. If the changeset is empty, an <see cref="InvalidOperationException"/> will
        /// be thrown.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/> to be used for requesting cancellation</param>
        /// <returns>The results returned by the submit request.</returns>
        /// <exception cref="InvalidOperationException">The changeset is empty.</exception>
        protected override Task<SubmitCompletedResult> SubmitAsyncCore(EntityChangeSet changeSet, CancellationToken cancellationToken)
        {
            IEnumerable<ChangeSetEntry> submitOperations = changeSet.GetChangeSetEntries();

            TContract channel = this.ChannelFactory.CreateChannel();
            return CallServiceOperation<SubmitCompletedResult>(channel,
                "SubmitChanges",
                 new Dictionary<string, object>()
                 {
                     {"changeSet", submitOperations}
                 },
                 (state, asyncResult) =>
                 {
                     try
                     {
                         var returnValue = (IEnumerable<ChangeSetEntry>)EndServiceOperationCall(state, asyncResult);
                         return new SubmitCompletedResult(changeSet, returnValue ?? Enumerable.Empty<ChangeSetEntry>());
                     }
                     catch (FaultException<DomainServiceFault> fe)
                     {
                         throw WebDomainClient<TContract>.GetExceptionFromServiceFault(fe.Detail);
                     }
                 }, cancellationToken);
        }

        /// <summary>
        /// Invokes an operation asynchronously.
        /// </summary>
        /// <param name="invokeArgs">The arguments to the Invoke operation.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/> to be used for requesting cancellation</param>
        /// <returns>The results returned by the invocation.</returns>
        /// <exception cref="InvalidOperationException">The specified query does not exist.</exception>
        protected override Task<InvokeCompletedResult> InvokeAsyncCore(InvokeArgs invokeArgs, CancellationToken cancellationToken)
        {
            TContract channel = ChannelFactory.CreateChannel();
            return CallServiceOperation(channel,
                invokeArgs.OperationName,
                invokeArgs.Parameters,
                (state, asyncResult) =>
                {
                    IEnumerable<ValidationResult> validationErrors = null;
                    object returnValue = null;
                    try
                    {
                        returnValue = EndServiceOperationCall(state, asyncResult);
                    }
                    catch (FaultException<DomainServiceFault> fe)
                    {
                        if (fe.Detail.OperationErrors != null)
                        {
                            validationErrors = fe.Detail.GetValidationErrors();
                        }
                        else
                        {
                            throw WebDomainClient<TContract>.GetExceptionFromServiceFault(fe.Detail);
                        }
                    }
                    return new InvokeCompletedResult(returnValue, validationErrors ?? Enumerable.Empty<ValidationResult>());
                },
                cancellationToken);
        }

        /// <summary>
        /// Calls an operation on an already constructed WCF service channel
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="channel">WCF channel</param>
        /// <param name="operationName">name of method/operation to call</param>
        /// <param name="parameters">parameters for the call</param>
        /// <param name="callback">callback responsible for casting return value and epr method error handling</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/> to be used for requesting cancellation</param>
        /// <returns>A <see cref="Task{TResult}"/> which will contain the result of the operation, exception or be cancelled</returns>
        private static Task<TResult> CallServiceOperation<TResult>(TContract channel, string operationName,
            IDictionary<string, object> parameters,
            Func<object, IAsyncResult, TResult> callback, CancellationToken cancellationToken)
        {
            MethodInfo beginInvokeMethod = WebDomainClient<TContract>.ResolveBeginMethod(operationName);
            MethodInfo endInvokeMethod = WebDomainClient<TContract>.ResolveEndMethod(operationName);

            // Pass operation parameters.
            ParameterInfo[] parameterInfos = beginInvokeMethod.GetParameters();
            object[] realParameters = new object[parameterInfos.Length];
            int parametersCount = parameters == null ? 0 : parameters.Count;
            for (int i = 0; i < parametersCount; i++)
            {
                realParameters[i] = parameters[parameterInfos[i].Name];
            }

            var taskCompletionSource = new TaskCompletionSource<TResult>();
            CancellationTokenRegistration cancellationTokenRegistration = default;
            if (cancellationToken.CanBeCanceled)
            {
                cancellationTokenRegistration = cancellationToken.Register(state => { ((IChannel)state).Abort(); }, channel);
            }
            // Pass async operation related parameters.
            realParameters[realParameters.Length - 2] = new AsyncCallback(delegate (IAsyncResult asyncResponseResult)
            {
                cancellationTokenRegistration.Dispose();

                try
                {
                    TResult result = callback(channel, asyncResponseResult);
                    taskCompletionSource.SetResult(result);
                }
                catch (CommunicationException) when (cancellationToken.IsCancellationRequested)
                {
                    taskCompletionSource.SetCanceled();
                }
                catch (Exception ex)
                {
                    taskCompletionSource.SetException(ex);
                }
                finally
                {
                    if (((IChannel)channel).State == CommunicationState.Faulted)
                        ((IChannel)channel).Abort();
                    else
                        ((IChannel)channel).Close();
                }
            });
            realParameters[realParameters.Length - 1] = /*userState*/endInvokeMethod;

            // Call Begin** method
            InvokeMethod(beginInvokeMethod, channel, realParameters);
            return taskCompletionSource.Task;
        }

        /// <summary>
        /// Method to invoke to get result of invocation started by <see cref="CallServiceOperation{TResult}(TContract, string, IDictionary{string, object}, Func{TContract, IAsyncResult, TResult}, CancellationToken)"/>
        /// </summary>
        /// <param name="channel">should be first parameter supplied in callback supplied to <see cref="CallServiceOperation{TResult}(TContract, string, IDictionary{string, object}, Func{TContract, IAsyncResult, TResult}, CancellationToken)"/></param>
        /// <param name="asyncResult">should be second parameter supplied in callback supplied to <see cref="CallServiceOperation" /> </param>
        /// <returns>result of service call</returns>
        private static object EndServiceOperationCall(object channel, IAsyncResult asyncResult)
        {
            return InvokeMethod((MethodInfo)asyncResult.AsyncState, channel, new object[] { asyncResult });
        }

        private static MethodInfo ResolveBeginMethod(string operationName)
        {
            MethodInfo m = typeof(TContract).GetMethod("Begin" + operationName);
            if (m == null)
            {
                throw new MissingMethodException(string.Format(CultureInfo.CurrentCulture, Resource.WebDomainClient_OperationDoesNotExist, operationName));
            }
            return m;
        }

        private static MethodInfo ResolveEndMethod(string operationName)
        {
            MethodInfo m = typeof(TContract).GetMethod("End" + operationName);
            if (m == null)
            {
                throw new MissingMethodException(string.Format(CultureInfo.CurrentCulture, OpenRiaServices.DomainServices.Client.Resource.WebDomainClient_OperationDoesNotExist, operationName));
            }
            return m;
        }

        /// <summary>
        /// Constructs an exception based on a service fault.
        /// </summary>
        /// <param name="serviceFault">The fault received from a service.</param>
        /// <returns>The constructed exception.</returns>
        private static Exception GetExceptionFromServiceFault(DomainServiceFault serviceFault)
        {
            // Status was OK but there still was a server error. We need to transform
            // the error into the appropriate client exception
            if (serviceFault.IsDomainException)
            {
                return new DomainException(serviceFault.ErrorMessage, serviceFault.ErrorCode, serviceFault.StackTrace);
            }
            else if (serviceFault.ErrorCode == 400)
            {
                return new DomainOperationException(serviceFault.ErrorMessage, OperationErrorStatus.NotSupported, serviceFault.ErrorCode, serviceFault.StackTrace);
            }
            else if (serviceFault.ErrorCode == 401)
            {
                return new DomainOperationException(serviceFault.ErrorMessage, OperationErrorStatus.Unauthorized, serviceFault.ErrorCode, serviceFault.StackTrace);
            }
            else
            {
                // for anything else: map to ServerError
                return new DomainOperationException(serviceFault.ErrorMessage, OperationErrorStatus.ServerError, serviceFault.ErrorCode, serviceFault.StackTrace);
            }
        }

#if SILVERLIGHT
        /// <summary>
        /// If the service Uri is relative, this method uses the application
        /// source to create an absolute Uri.
        /// </summary>
        /// <remarks>
        /// If usesHttps in the constructor was true, the Uri will be created using
        /// a https scheme instead.
        /// </remarks>
        private void ComposeAbsoluteServiceUri()
        {
            // if the URI is relative, compose with the source URI
            if (!this._serviceUri.IsAbsoluteUri)
            {
                Application current = Application.Current;

                // Only proceed if we can determine a root uri
                if ((current == null) || (current.Host == null) || (current.Host.Source == null))
                {
                    throw new InvalidOperationException(OpenRiaServices.DomainServices.Client.Resource.DomainClient_UnableToDetermineHostUri);
                }

                string sourceUri = current.Host.Source.AbsoluteUri;
                if (this.UsesHttps)
                {
                    // We want to replace a http scheme (everything before the ':' in a Uri) with https.
                    // Doing this via UriBuilder loses the OriginalString. Unfortunately, this leads
                    // the builder to include the original port in the output which is not what we want.
                    // To stay as close to the original Uri as we can, we'll just do some simple string
                    // replacement.
                    //
                    // Desired output: http://my.domain/mySite.aspx -> https://my.domain/mySite.aspx
                    // Builder output: http://my.domain/mySite.aspx -> https://my.domain:80/mySite.aspx
                    //   The actual port is probably 443, but including it increases the cross-domain complexity.
                    if (sourceUri.StartsWith("http:", StringComparison.OrdinalIgnoreCase))
                    {
                        sourceUri = "https:" + sourceUri.Substring(5 /*("http:").Length*/);
                    }
                }

                this._serviceUri = new Uri(new Uri(sourceUri), this._serviceUri);
            }
        }
#endif
    }
}
