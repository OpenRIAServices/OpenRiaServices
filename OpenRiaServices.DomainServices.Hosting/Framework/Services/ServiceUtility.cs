using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Description;
using OpenRiaServices.DomainServices.Server;
using System.ServiceModel.Web;
using System.Text;
using System.Web;
using System.Xml;

// WARNING: Keep this file in sync with OpenRiaServices.DomainServices.Hosting

namespace OpenRiaServices.DomainServices.Hosting
{
    internal static class ServiceUtility
    {
        private const string DefaultNamespace = "http://tempuri.org/";
        private const int MaxArrayLength = int.MaxValue;
        private const int MaxBytesPerRead = int.MaxValue;
        private const int MaxDepth = int.MaxValue;
        private const int MaxNameTableCharCount = int.MaxValue;
        private const int MaxStringContentLength = int.MaxValue;
        internal const long MaxReceivedMessageSize = int.MaxValue;
        internal const string SubmitOperationName = "SubmitChanges";
        internal const string ServiceFileExtension = ".svc";

        internal static readonly object[] EmptyObjectArray = new object[0];

        private static readonly object _inspectorLock = new object();
        private static AuthenticationSchemes _authenticationSchemes = AuthenticationSchemes.None;
        private static HttpClientCredentialType _credentialType = HttpClientCredentialType.None;

        /// <summary>
        /// Gets the default authentication scheme supported by the server
        /// </summary>
        public static AuthenticationSchemes AuthenticationScheme
        {
            get
            {
                VerifyAuthenticationMode();
                return _authenticationSchemes;
            }
        }

        /// <summary>
        /// Gets the default credential type supported by the server
        /// </summary>
        public static HttpClientCredentialType CredentialType
        {
            get
            {
                VerifyAuthenticationMode();
                return _credentialType;
            }
        }

        private static void VerifyAuthenticationMode()
        {
            // Authentication scheme should never be none.
            if (_authenticationSchemes == AuthenticationSchemes.None)
            {
                lock (_inspectorLock)
                {
                    if (_authenticationSchemes == AuthenticationSchemes.None)
                    {
                        WebServiceHostInspector inspector = new WebServiceHostInspector();
                        inspector.Inspect();
                        _authenticationSchemes = inspector.AuthenticationScheme;
                        _credentialType = inspector.CredentialType;

                        // We can't close the host because it's in a faulted state. Trying to do so will 
                        // cause a CommunicationException to be thrown.
                    }
                }
            }
        }

        // Populates a contract description from a domain service description.
        public static void LoadContractDescription(ContractDescription contractDesc, DomainServiceDescription domainServiceDescription)
        {
            bool isMutable = false;
            OperationDescription operationDesc;

            Type queryOperationType;
            Dictionary<Type, Type> queryOperationTypes = new Dictionary<Type, Type>();
            foreach (DomainOperationEntry operation in domainServiceDescription.DomainOperationEntries)
            {
                switch (operation.Operation)
                {
                    case DomainOperation.Insert:
                    case DomainOperation.Update:
                    case DomainOperation.Custom:
                    case DomainOperation.Delete:
                        isMutable = true;
                        break;
                    case DomainOperation.Query:
                        operationDesc = ServiceUtility.CreateQueryOperationDescription(contractDesc, operation);

                        if (!queryOperationTypes.TryGetValue(operation.AssociatedType, out queryOperationType))
                        {
                            queryOperationType = typeof(QueryOperationBehavior<>).MakeGenericType(operation.AssociatedType);
                            queryOperationTypes.Add(operation.AssociatedType, queryOperationType);
                        }

                        operationDesc.Behaviors.Add((IOperationBehavior)Activator.CreateInstance(queryOperationType, operation));
                        contractDesc.Operations.Add(operationDesc);
                        break;
                    case DomainOperation.Invoke:
                        operationDesc = ServiceUtility.CreateOperationDescription(contractDesc, operation);

                        operationDesc.Behaviors.Add(new InvokeOperationBehavior(operation));
                        contractDesc.Operations.Add(operationDesc);
                        break;
                }
            }

            if (isMutable)
            {
                // Add a SubmitChanges operation.
                operationDesc = ServiceUtility.CreateSubmitOperationDescription(contractDesc, domainServiceDescription);

                operationDesc.Behaviors.Add(new SubmitOperationBehavior());
                contractDesc.Operations.Add(operationDesc);
            }

            ServiceUtility.RegisterSurrogates(contractDesc, domainServiceDescription);
        }

        // Registers surrogates with all of the operations on the specified contract. These surrogates 
        // take care of representing entities in the right shape based on TypeDescriptor extensions.
        private static void RegisterSurrogates(ContractDescription contractDesc, DomainServiceDescription domainServiceDescription)
        {
            // Cache the list of entity types and surrogate types.
            HashSet<Type> exposedTypes = new HashSet<Type>();
            Dictionary<Type, Tuple<Type, Func<object, object>>> exposedTypeToSurrogateMap = new Dictionary<Type, Tuple<Type, Func<object, object>>>();
            HashSet<Type> surrogateTypes = new HashSet<Type>();
            foreach (Type entityType in domainServiceDescription.EntityTypes)
            {
                exposedTypes.Add(entityType);
            }

            // Because complex types and entities cannot share an inheritance relationship, we can add them to the same surrogate set.
            foreach (Type complexType in domainServiceDescription.ComplexTypes)
            {
                exposedTypes.Add(complexType);
            }

            foreach (Type exposedType in exposedTypes)
            {
                Type surrogateType = DataContractSurrogateGenerator.GetSurrogateType(exposedTypes, exposedType);
                Func<object, object> surrogateFactory = (Func<object, object>)DynamicMethodUtility.GetFactoryMethod(surrogateType.GetConstructor(new Type[] { exposedType }), typeof(Func<object, object>));
                Tuple<Type, Func<object, object>> surrogateInfo = new Tuple<Type, Func<object, object>>(surrogateType, surrogateFactory);
                exposedTypeToSurrogateMap.Add(exposedType, surrogateInfo);
                surrogateTypes.Add(surrogateType);
            }

            DomainServiceSerializationSurrogate surrogate = new DomainServiceSerializationSurrogate(domainServiceDescription, exposedTypeToSurrogateMap, surrogateTypes);

            // Register our serialization surrogate with the WSDL exporter.
            DomainServiceWsdlExportExtension wsdlExportExtension = contractDesc.Behaviors.Find<DomainServiceWsdlExportExtension>();
            if (wsdlExportExtension == null)
            {
                wsdlExportExtension = new DomainServiceWsdlExportExtension(surrogate);
                contractDesc.Behaviors.Add(wsdlExportExtension);
            }

            // Register our serialization surrogate with the actual invoke operations.
            foreach (OperationDescription op in contractDesc.Operations)
            {
                foreach (Type surrogateType in surrogateTypes)
                {
                    op.KnownTypes.Add(surrogateType);
                }

                DataContractSerializerOperationBehavior dataContractBehavior = op.Behaviors.Find<DataContractSerializerOperationBehavior>();
                if (dataContractBehavior == null)
                {
                    dataContractBehavior = new DataContractSerializerOperationBehavior(op);
                    op.Behaviors.Add(dataContractBehavior);
                }
                dataContractBehavior.DataContractSurrogate = surrogate;
            }
        }

        private static OperationDescription CreateSubmitOperationDescription(ContractDescription declaringContract, DomainServiceDescription domainServiceDescription)
        {
            OperationDescription operationDesc = ServiceUtility.CreateBasicOperationDescription(declaringContract, ServiceUtility.SubmitOperationName);

            // Propagate behaviors.
            MethodInfo submitMethod = declaringContract.ContractType.GetMethod("Submit");
            foreach (IOperationBehavior behavior in submitMethod.GetCustomAttributes(/* inherit */ true).OfType<IOperationBehavior>())
            {
                operationDesc.Behaviors.Add(behavior);
            }

            string action = ServiceUtility.GetMessageAction(declaringContract, operationDesc.Name, /* action */ null);
            string responseAction = ServiceUtility.GetResponseMessageAction(declaringContract, operationDesc.Name, /* action */ null);

            // Define operation input (IEnumerable<ChangeSetEntry> changeSet).
            MessageDescription inputMessageDesc = new MessageDescription(action, MessageDirection.Input);
            inputMessageDesc.Body.WrapperName = operationDesc.Name;
            inputMessageDesc.Body.WrapperNamespace = ServiceUtility.DefaultNamespace;

            MessagePartDescription changeSetParameterPartDesc = new MessagePartDescription("changeSet", ServiceUtility.DefaultNamespace)
            {
                Type = typeof(IEnumerable<ChangeSetEntry>)
            };
            inputMessageDesc.Body.Parts.Add(changeSetParameterPartDesc);
            operationDesc.Messages.Add(inputMessageDesc);

            // Define operation output (IEnumerable<ChangeSetEntry>).
            MessageDescription outputMessageDesc = new MessageDescription(responseAction, MessageDirection.Output);
            outputMessageDesc.Body.WrapperName = operationDesc.Name + "Response";
            outputMessageDesc.Body.WrapperNamespace = ServiceUtility.DefaultNamespace;

            outputMessageDesc.Body.ReturnValue = new MessagePartDescription(operationDesc.Name + "Result", ServiceUtility.DefaultNamespace)
            {
                Type = typeof(IEnumerable<ChangeSetEntry>)
            };
            operationDesc.Messages.Add(outputMessageDesc);

            // Register types used in custom methods. Custom methods show up as part of the changeset.
            // KnownTypes are required for all non-primitive and non-string since these types will show up in the change set.
            foreach (DomainOperationEntry customOp in domainServiceDescription.DomainOperationEntries.Where(op => op.Operation == DomainOperation.Custom))
            {
                // KnownTypes will be added during surrogate registration for all entity and
                // complex types. We skip the first parameter because it is an entity type. We also
                // skip all complex types. Note, we do not skip complex type collections because
                // the act of registering surrogates only adds the type, and KnownTypes needs to
                // know about any collections.
                foreach (Type parameterType in customOp.Parameters.Skip(1).Select(p => p.ParameterType).Where(
                    t => !t.IsPrimitive && t != typeof(string) && !domainServiceDescription.ComplexTypes.Contains(t)))
                {
                    operationDesc.KnownTypes.Add(parameterType);
                }
            }

            return operationDesc;
        }

        private static OperationDescription CreateQueryOperationDescription(ContractDescription declaringContract, DomainOperationEntry operation)
        {
            OperationDescription operationDesc = ServiceUtility.CreateOperationDescription(declaringContract, operation);

            // Change the return type to QueryResult<TEntity>.
            operationDesc.Messages[1].Body.ReturnValue.Type = typeof(QueryResult<>).MakeGenericType(operation.AssociatedType);

            return operationDesc;
        }

        private static OperationDescription CreateOperationDescription(ContractDescription declaringContract, DomainOperationEntry operation)
        {
            OperationDescription operationDesc = ServiceUtility.CreateBasicOperationDescription(declaringContract, operation.Name);

            // Propagate behaviors.
            foreach (IOperationBehavior behavior in operation.Attributes.OfType<IOperationBehavior>())
            {
                operationDesc.Behaviors.Add(behavior);
            }

            // Add standard behaviors.

            if ((operation.Operation == DomainOperation.Query && ((QueryAttribute)operation.OperationAttribute).HasSideEffects) ||
                (operation.Operation == DomainOperation.Invoke && ((InvokeAttribute)operation.OperationAttribute).HasSideEffects))
            {
                // REVIEW: We should actually be able to remove the following line entirely, since 
                //         all operations are [WebInvoke] by default.
                ServiceUtility.EnsureBehavior<WebInvokeAttribute>(operationDesc);
            }
            else if (operation.Operation == DomainOperation.Query && !((QueryAttribute)operation.OperationAttribute).HasSideEffects)
            {
                // This is a query with HasSideEffects == false, allow both POST and GET
                ServiceUtility.EnsureBehavior<WebInvokeAttribute>(operationDesc)
                    .Method = "*";
            }
            else
            {
                ServiceUtility.EnsureBehavior<WebGetAttribute>(operationDesc);
            }

            string action = ServiceUtility.GetMessageAction(declaringContract, operationDesc.Name, /* action */ null);

            // Define operation input.
            MessageDescription inputMessageDesc = new MessageDescription(action, MessageDirection.Input);
            inputMessageDesc.Body.WrapperName = operationDesc.Name;
            inputMessageDesc.Body.WrapperNamespace = ServiceUtility.DefaultNamespace;

            for (int i = 0; i < operation.Parameters.Count; i++)
            {
                DomainOperationParameter parameter = operation.Parameters[i];

                MessagePartDescription parameterPartDesc = new MessagePartDescription(parameter.Name, ServiceUtility.DefaultNamespace)
                {
                    Index = i,
                    Type = SerializationUtility.GetClientType(parameter.ParameterType)
                };
                inputMessageDesc.Body.Parts.Add(parameterPartDesc);
            }
            operationDesc.Messages.Add(inputMessageDesc);

            // Define operation output.
            string responseAction = ServiceUtility.GetResponseMessageAction(declaringContract, operationDesc.Name, /* action */ null);

            MessageDescription outputMessageDesc = new MessageDescription(responseAction, MessageDirection.Output);
            outputMessageDesc.Body.WrapperName = operationDesc.Name + "Response";
            outputMessageDesc.Body.WrapperNamespace = ServiceUtility.DefaultNamespace;

            if (operation.ReturnType != typeof(void))
            {
                outputMessageDesc.Body.ReturnValue = new MessagePartDescription(operationDesc.Name + "Result", ServiceUtility.DefaultNamespace)
                {
                    Type = SerializationUtility.GetClientType(operation.ReturnType)
                };
            }
            operationDesc.Messages.Add(outputMessageDesc);

            return operationDesc;
        }

        private static OperationDescription CreateBasicOperationDescription(ContractDescription declaringContract, string operationName)
        {
            OperationDescription operationDesc = new OperationDescription(operationName, declaringContract);
            string faultAction = ServiceUtility.GetFaultMessageAction(declaringContract, operationDesc.Name, /* action */ null);
            operationDesc.Faults.Add(new FaultDescription(faultAction)
            {
                Name = typeof(DomainServiceFault).Name,
                Namespace = "DomainServices",
                DetailType = typeof(DomainServiceFault)
            });

            return operationDesc;
        }

        /// <summary>
        /// Sets the default reader quotas.
        /// </summary>
        /// <param name="readerQuotas">The quotas object that needs to be updated.</param>
        internal static void SetReaderQuotas(XmlDictionaryReaderQuotas readerQuotas)
        {
            readerQuotas.MaxArrayLength = ServiceUtility.MaxArrayLength;
            readerQuotas.MaxBytesPerRead = ServiceUtility.MaxBytesPerRead;
            readerQuotas.MaxDepth = ServiceUtility.MaxDepth;
            readerQuotas.MaxNameTableCharCount = ServiceUtility.MaxNameTableCharCount;
            readerQuotas.MaxStringContentLength = ServiceUtility.MaxStringContentLength;
        }

        public static T EnsureBehavior<T>(ServiceDescription serviceDesc) where T : IServiceBehavior, new()
        {
            T behavior = serviceDesc.Behaviors.Find<T>();
            if (behavior == null)
            {
                behavior = new T();
                serviceDesc.Behaviors.Insert(0, behavior);
            }
            return behavior;
        }

        public static T EnsureBehavior<T>(ContractDescription contractDesc) where T : IContractBehavior, new()
        {
            T behavior = contractDesc.Behaviors.Find<T>();
            if (behavior == null)
            {
                behavior = new T();
                contractDesc.Behaviors.Insert(0, behavior);
            }
            return behavior;
        }

        public static T EnsureBehavior<T>(OperationDescription operationDesc) where T : IOperationBehavior, new()
        {
            T behavior = operationDesc.Behaviors.Find<T>();
            if (behavior == null)
            {
                behavior = new T();
                operationDesc.Behaviors.Insert(0, behavior);
            }
            return behavior;
        }

        public static string GetMessageAction(ContractDescription contractName, string opname, string action)
        {
            return ServiceUtility.GetMessageAction(contractName, opname, action, /* isResponse */ false);
        }

        public static string GetResponseMessageAction(ContractDescription contractName, string opname, string action)
        {
            return ServiceUtility.GetMessageAction(contractName, opname, action, /* isResponse */ true);
        }

        public static string GetFaultMessageAction(ContractDescription contractName, string opname, string action)
        {
            return ServiceUtility.GetMessageAction(contractName, opname, action, /* isResponse */ false) + typeof(DomainServiceFault).Name;
        }

        private static string GetMessageAction(ContractDescription contractName, string opname, string action, bool isResponse)
        {
            if (action != null)
            {
                return action;
            }
            StringBuilder builder = new StringBuilder(0x40);
            if (string.IsNullOrEmpty(contractName.Namespace))
            {
                builder.Append("urn:");
            }
            else
            {
                builder.Append(contractName.Namespace);
                if (!contractName.Namespace.EndsWith("/", StringComparison.Ordinal))
                {
                    builder.Append('/');
                }
            }
            builder.Append(contractName.Name);
            builder.Append('/');
            action = isResponse ? (opname + "Response") : opname;
            return CombineUriStrings(builder.ToString(), action);
        }

        private static string CombineUriStrings(string baseUri, string path)
        {
            if (Uri.IsWellFormedUriString(path, UriKind.Absolute) || String.IsNullOrEmpty(path))
            {
                return path;
            }
            if (baseUri.EndsWith("/", StringComparison.Ordinal))
            {
                return (baseUri + (path.StartsWith("/", StringComparison.Ordinal) ? path.Substring(1) : path));
            }
            return (baseUri + (path.StartsWith("/", StringComparison.Ordinal) ? path : ("/" + path)));
        }

        /// <summary>
        /// Based on custom error settings, restrict the level of information
        /// returned in each error.
        /// </summary>
        /// <remarks>
        /// This method will also trace the exception if tracing is enabled.
        /// </remarks>
        /// <param name="validationErrors">The collection of errors to process.</param>
        /// <returns>An exception representing the validation errors.</returns>
        internal static FaultException<DomainServiceFault> CreateFaultException(IEnumerable<ValidationResult> validationErrors)
        {
            Debug.Assert(validationErrors != null, "validationErrors is null.");

            DomainServiceFault fault = new DomainServiceFault();
            IEnumerable<ValidationResultInfo> errors = validationErrors.Select(ve => new ValidationResultInfo(ve.ErrorMessage, ve.MemberNames)).ToList();
            fault.OperationErrors = errors;

            // if custom errors is turned on, clear out the stacktrace.
            HttpContext context = HttpContext.Current;
            foreach (ValidationResultInfo error in errors)
            {
                if (context != null && context.IsCustomErrorEnabled)
                {
                    error.StackTrace = null;
                }
            }

            FaultException<DomainServiceFault> ex = new FaultException<DomainServiceFault>(fault, new FaultReason(new FaultReasonText(fault.ErrorMessage ?? String.Empty, CultureInfo.CurrentCulture)));
            return ex;
        }

        /// <summary>
        /// Transforms the specified exception as appropriate into a fault message that can be sent
        /// back to the client.
        /// </summary>
        /// <remarks>
        /// This method will also trace the exception if tracing is enabled.
        /// </remarks>
        /// <param name="e">The exception that was caught.</param>
        /// <returns>The exception to return.</returns>
        internal static FaultException<DomainServiceFault> CreateFaultException(Exception e)
        {
            Debug.Assert(!e.IsFatal(), "Fatal exception passed in");
            DomainServiceFault fault = new DomainServiceFault();

            HttpContext context = HttpContext.Current;

            // Unwrap any TargetInvocationExceptions to get the real exception.
            while (e.InnerException != null && e is TargetInvocationException)
            {
                e = ((TargetInvocationException)e).InnerException;
            }

            // we always send back a 200 (i.e. not re-throwing) with the actual error code in 
            // the results (except fo 404) because silverlight only supports 404/500 error code. If customErrors 
            // are disabled, we'll also send the error message.
            int errorCode = (int)HttpStatusCode.InternalServerError;

            if (e is InvalidOperationException)
            {
                // invalid operation exception at root level generates BadRequest
                errorCode = (int)HttpStatusCode.BadRequest;
            }
            else if (e is UnauthorizedAccessException)
            {
                errorCode = (int)HttpStatusCode.Unauthorized;
            }
            else
            {
                DomainException dpe = e as DomainException;
                if (dpe != null)
                {
                    // we always propagate error info to the client for DomainServiceExceptions
                    fault.ErrorCode = dpe.ErrorCode;
                    fault.ErrorMessage = FormatExceptionMessage(dpe);
                    fault.IsDomainException = true;
                    if (context != null && context.IsCustomErrorEnabled == false)
                    {
                        // also send the stack trace if custom errors is disabled
                        fault.StackTrace = dpe.StackTrace;
                    }

                    return new FaultException<DomainServiceFault>(fault, new FaultReason(new FaultReasonText(fault.ErrorMessage ?? String.Empty, CultureInfo.CurrentCulture)));
                }
                else
                {
                    HttpException httpException = e as HttpException;
                    if (httpException != null)
                    {
                        errorCode = httpException.GetHttpCode();
                        if (errorCode == (int)HttpStatusCode.NotFound)
                        {
                            // for NotFound errors, we don't provide detailed error
                            // info, we just rethrow
                            throw e;
                        }
                    }
                }
            }

            // set error code. Also set error message if custom errors is disabled
            fault.ErrorCode = errorCode;
            if (context != null && !context.IsCustomErrorEnabled)
            {
                fault.ErrorMessage = FormatExceptionMessage(e);
                fault.StackTrace = e.StackTrace;
            }

            return new FaultException<DomainServiceFault>(fault, new FaultReason(new FaultReasonText(fault.ErrorMessage ?? String.Empty, CultureInfo.CurrentCulture)));
        }

        /// <summary>
        /// For the specified exception, return the error message concatenating
        /// the message of any inner exception to one level deep.
        /// </summary>
        /// <param name="e">The exception</param>
        /// <returns>The formatted exception message.</returns>
        private static string FormatExceptionMessage(Exception e)
        {
            if (e.InnerException == null)
            {
                return e.Message;
            }
            return string.Format(CultureInfo.CurrentCulture, Resource.FaultException_InnerExceptionDetails, e.Message, e.InnerException.Message);
        }

        #region WebServiceHostInspector

        /// <summary>
        /// Uses the WCF <see cref="WebServiceHost"/> to get the default authentication scheme
        /// and credential type for services on the current server.
        /// </summary>
        private class WebServiceHostInspector : WebServiceHost
        {
            private readonly CommunicationException _exception =
                new CommunicationException("No channel should ever be opened with this host.");

            private AuthenticationSchemes _authenticationScheme = AuthenticationSchemes.None;
            private HttpClientCredentialType _credentialType = HttpClientCredentialType.None;

            public WebServiceHostInspector()
                : base(
                    typeof(WebServiceHostInspector.Service),
                    new Uri("http://OpenRiaServices.DomainServices.Hosting.ServiceUtility.WebServiceHostInspector.Service.svc"))
            { }

            public AuthenticationSchemes AuthenticationScheme
            {
                get { return this._authenticationScheme; }
            }

            public HttpClientCredentialType CredentialType
            {
                get { return this._credentialType; }
            }

            public void Inspect()
            {
                try
                {
                    // The WebServerHost determines the default endpoint settings in OnOpening().
                    // We're calling into Open() to get the process started. We want to avoid
                    // actually opening the channel and initializing a socket so we'll abort the
                    // process once we get the endpoint information we need. There is no API for
                    // otherwise determing the authentication scheme.
                    this.Open();
                }
                catch (CommunicationException e)
                {
                    // We will always throw _exception from OnOpening if given the opportunity
                    if (e != this._exception)
                    {
                        throw;
                    }
                }
            }

            protected override void OnOpening()
            {
                base.OnOpening();

                ServiceEndpoint se = this.Description.Endpoints.FirstOrDefault();
                if (se != null)
                {
                    WebHttpBinding whb = se.Binding as WebHttpBinding;
                    if (whb != null)
                    {
                        this._credentialType = whb.Security.Transport.ClientCredentialType;
                        switch (this._credentialType)
                        {
                            case HttpClientCredentialType.Basic:
                                this._authenticationScheme = AuthenticationSchemes.Basic;
                                break;
                            case HttpClientCredentialType.Digest:
                                this._authenticationScheme = AuthenticationSchemes.Digest;
                                break;
                            case HttpClientCredentialType.Ntlm:
                                this._authenticationScheme = AuthenticationSchemes.Ntlm;
                                break;
                            case HttpClientCredentialType.Windows:
                                this._authenticationScheme = AuthenticationSchemes.Negotiate;
                                break;
                            default:
                                break;
                        }

                        // We've successfully determined the authentication scheme, now we'll
                        // abort the open process to avoid touching system resources.
                        throw this._exception;
                    }
                }

                // base.OnOpening() should fail before we ever get here, but even when it doesn't
                // we want to alert users of problems creating the default endpoints.
                throw new CommunicationException(Resource.NoDefaultAuthScheme);
            }

            [ServiceContract]
            private interface IService
            {
                [OperationContract]
                void Operation();
            }

            [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
            private class Service : IService
            {
                public void Operation() { }
            }
        }

        #endregion
    }
}
