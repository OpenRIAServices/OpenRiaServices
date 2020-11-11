using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using OpenRiaServices.Server;
using System.Web;
using System.Web.Caching;
using System.Web.Configuration;
using System.Threading.Tasks;

namespace OpenRiaServices.Hosting.WCF.Behaviors
{
    internal class QueryOperationBehavior<TEntity> : IOperationBehavior, IQueryOperationSettings
    {
        private readonly DomainOperationEntry _operation;

        public QueryOperationBehavior(DomainOperationEntry operation)
        {
            this._operation = operation;
        }

        bool IQueryOperationSettings.HasSideEffects
        {
            get
            {
                return ((QueryAttribute)this._operation.OperationAttribute).HasSideEffects;
            }
        }

        public void AddBindingParameters(OperationDescription operationDescription, BindingParameterCollection bindingParameters)
        {
        }

        public void ApplyClientBehavior(OperationDescription operationDescription, ClientOperation clientOperation)
        {
        }

        public void ApplyDispatchBehavior(OperationDescription operationDescription, DispatchOperation dispatchOperation)
        {
            dispatchOperation.Invoker = new QueryOperationInvoker(this._operation);
        }

        public void Validate(OperationDescription operationDescription)
        {
        }

        internal class QueryOperationInvoker : DomainOperationInvoker
        {
            private static readonly string[] supportedQueryParameters = { "$where", "$orderby", "$skip", "$take", "$includeTotalCount" };
            private static readonly char[] colonDelimiter = new char[] { ':' };
            private static readonly char[] semiColonDelimiter = new char[] { ';' };
            private static readonly object syncRoot = new object();
            private static OutputCacheProfileCollection cacheProfiles;

            private readonly DomainOperationEntry operation;

            public QueryOperationInvoker(DomainOperationEntry operation)
                : base(DomainOperationType.Query)
            {
                this.operation = operation;
            }

            protected override string Name
            {
                get
                {
                    return this.operation.Name;
                }
            }

            public override object[] AllocateInputs()
            {
                return new object[this.operation.Parameters.Count];
            }

            protected override async ValueTask<object> InvokeCoreAsync(DomainService instance, object[] inputs, bool disableStackTraces)
            {
                ServiceQuery serviceQuery = null;
                QueryAttribute queryAttribute = (QueryAttribute)this.operation.OperationAttribute;
                // httpContext is lost on await so need to save it for later ise
                HttpContext httpContext = HttpContext.Current;

                if (queryAttribute.IsComposable)
                {
                    object value;
                    if (OperationContext.Current.IncomingMessageProperties.TryGetValue(ServiceQuery.QueryPropertyName, out value))
                    {
                        serviceQuery = (ServiceQuery)value;
                    }
                }

                QueryResult<TEntity> result;
                try
                {
                    QueryOperationInvoker.SetOutputCachingPolicy(httpContext, this.operation);
                    result = await QueryProcessor.ProcessAsync<TEntity>(instance, this.operation, inputs, serviceQuery);
                }
                catch (Exception ex)
                {
                    if (ex.IsFatal())
                    {
                        throw;
                    }
                    QueryOperationInvoker.ClearOutputCachingPolicy(httpContext);
                    throw ServiceUtility.CreateFaultException(ex, disableStackTraces);
                }


                if (result.ValidationErrors != null && result.ValidationErrors.Any())
                {
                    throw ServiceUtility.CreateFaultException(result.ValidationErrors, disableStackTraces);
                }

                return result;
            }

            protected override void ConvertInputs(object[] inputs)
            {
                for (int i = 0; i < this.operation.Parameters.Count; i++)
                {
                    DomainOperationParameter parameter = this.operation.Parameters[i];
                    inputs[i] = SerializationUtility.GetServerValue(parameter.ParameterType, inputs[i]);
                }
            }

            /// <summary>
            /// Clears the output cache policy.
            /// </summary>
            private static void ClearOutputCachingPolicy(HttpContext context)
            {
                if (context == null)
                {
                    return;
                }

                context.Response.Cache.SetCacheability(HttpCacheability.NoCache);
            }

            /// <summary>
            /// Sets the output cache policy for the specified domain operation entry.
            /// </summary>
            /// <param name="context">Current HttpContext</param>
            /// <param name="domainOperationEntry">The domain operation entry we need to define the cache policy for.</param>
            private static void SetOutputCachingPolicy(HttpContext context, DomainOperationEntry domainOperationEntry)
            {
                if (context == null)
                    return;

                if (QueryOperationInvoker.SupportsCaching(context, domainOperationEntry))
                {
                    OutputCacheAttribute outputCacheInfo = QueryOperationInvoker.GetOutputCacheInformation(domainOperationEntry);
                    if (outputCacheInfo != null)
                    {
                        HttpCachePolicy policy = context.Response.Cache;
                        if (outputCacheInfo.UseSlidingExpiration)
                        {
                            policy.SetSlidingExpiration(true);
                            policy.AddValidationCallback((HttpCacheValidateHandler)delegate(HttpContext c, object d, ref HttpValidationStatus status)
                            {
                                SlidingExpirationValidator validator = (SlidingExpirationValidator)d;
                                if (validator.IsValid())
                                {
                                    status = HttpValidationStatus.Valid;
                                }
                                else
                                {
                                    status = HttpValidationStatus.Invalid;
                                }
                            }, new SlidingExpirationValidator(outputCacheInfo.Duration));
                        }

                        if (outputCacheInfo.Duration > -1)
                        {
                            // When sliding expiration is set, ASP.NET will use the following to figure out the sliding expiration delta.
                            policy.SetExpires(DateTime.UtcNow.AddSeconds(outputCacheInfo.Duration));
                            policy.SetMaxAge(TimeSpan.FromSeconds(outputCacheInfo.Duration));
                            policy.SetValidUntilExpires(/* validUntilExpires */ true);
                        }

                        policy.SetCacheability(QueryOperationInvoker.GetCacheability(outputCacheInfo.Location));

                        if (!String.IsNullOrEmpty(outputCacheInfo.SqlCacheDependencies))
                        {
                            // Syntax is <databaseEntry>:<tableName>[;<databaseEntry>:<tableName>]*.
                            string[] dependencies = outputCacheInfo.SqlCacheDependencies.Split(QueryOperationInvoker.semiColonDelimiter, StringSplitOptions.RemoveEmptyEntries);
                            foreach (string dependency in dependencies)
                            {
                                string[] dependencyTokens = dependency.Split(QueryOperationInvoker.colonDelimiter, StringSplitOptions.RemoveEmptyEntries);
                                if (dependencyTokens.Length != 2)
                                {
                                    throw new InvalidOperationException(Resource.DomainService_InvalidSqlDependencyFormat);
                                }

                                context.Response.AddCacheDependency(new SqlCacheDependency(dependencyTokens[0], dependencyTokens[1]));
                            }
                        }

                        if (!String.IsNullOrEmpty(outputCacheInfo.VaryByHeaders))
                        {
                            string[] headers = outputCacheInfo.VaryByHeaders.Split(QueryOperationInvoker.semiColonDelimiter, StringSplitOptions.RemoveEmptyEntries);
                            foreach (string header in headers)
                            {
                                policy.VaryByHeaders[header] = true;
                            }
                        }

                        // The cache is based on the values of the domain operation entry's parameters.
                        foreach (DomainOperationParameter pi in domainOperationEntry.Parameters)
                        {
                            policy.VaryByParams[pi.Name] = true;
                        }

                        // We don't cache when query parameters are used. We need to vary by query parameters 
                        // though such that we can intercept requests with query parameters and by-pass the cache.
                        foreach (string queryParameter in supportedQueryParameters)
                        {
                            policy.VaryByParams[queryParameter] = true;
                        }

                        return;
                    }
                }

                // By default, don't let clients/proxies cache anything.
                context.Response.Cache.SetCacheability(HttpCacheability.NoCache);
            }

            /// <summary>
            /// Converts the specified <see cref="System.Web.UI.OutputCacheLocation"/> enum value to a <see cref="OpenRiaServices.Server"/> enum value.
            /// </summary>
            /// <param name="outputCacheLocation">The <see cref="System.Web.UI.OutputCacheLocation"/>.</param>
            /// <returns>The equivalent <see cref="OpenRiaServices.Server.OutputCacheLocation"/> value.</returns>
            private static OpenRiaServices.Server.OutputCacheLocation GetCacheLocation(System.Web.UI.OutputCacheLocation outputCacheLocation)
            {
                switch (outputCacheLocation)
                {
                    case System.Web.UI.OutputCacheLocation.Any:
                        return OpenRiaServices.Server.OutputCacheLocation.Any;
                    case System.Web.UI.OutputCacheLocation.Client:
                        return OpenRiaServices.Server.OutputCacheLocation.Client;
                    case System.Web.UI.OutputCacheLocation.Downstream:
                        return OpenRiaServices.Server.OutputCacheLocation.Downstream;
                    case System.Web.UI.OutputCacheLocation.Server:
                        return OpenRiaServices.Server.OutputCacheLocation.Server;
                    case System.Web.UI.OutputCacheLocation.ServerAndClient:
                        return OpenRiaServices.Server.OutputCacheLocation.ServerAndClient;
                    default:
                        return OpenRiaServices.Server.OutputCacheLocation.None;
                }
            }

            /// <summary>
            /// Converts the specified <see cref="OpenRiaServices.Server.OutputCacheLocation"/> enum value to a <see cref="HttpCacheability"/> enum value.
            /// </summary>
            /// <param name="outputCacheLocation">The <see cref="OpenRiaServices.Server.OutputCacheLocation"/>.</param>
            /// <returns>The equivalent <see cref="HttpCacheability"/> value.</returns>
            private static HttpCacheability GetCacheability(OpenRiaServices.Server.OutputCacheLocation outputCacheLocation)
            {
                // Following conversion is taken from System.Web.UI.Page.InitOutputCache.
                switch (outputCacheLocation)
                {
                    case OpenRiaServices.Server.OutputCacheLocation.Client:
                        return HttpCacheability.Private;
                    case OpenRiaServices.Server.OutputCacheLocation.Any:
                    case OpenRiaServices.Server.OutputCacheLocation.Downstream:
                        return HttpCacheability.Public;
                    case OpenRiaServices.Server.OutputCacheLocation.Server:
                        return HttpCacheability.Server;
                    case OpenRiaServices.Server.OutputCacheLocation.ServerAndClient:
                        return HttpCacheability.ServerAndPrivate;
                    case OpenRiaServices.Server.OutputCacheLocation.None:
                    default:
                        return HttpCacheability.NoCache;
                }
            }

            /// <summary>
            /// Gets cache information for the specified domain operation entry.
            /// </summary>
            /// <param name="method">The domain operation entry to get cache information for.</param>
            /// <returns>Cache information.</returns>
            private static OutputCacheAttribute GetOutputCacheInformation(DomainOperationEntry method)
            {
                OutputCacheAttribute cacheAttribute = method.Attributes.OfType<OutputCacheAttribute>().FirstOrDefault();
                if (cacheAttribute != null)
                {
                    if (!String.IsNullOrEmpty(cacheAttribute.CacheProfile))
                    {
                        if (QueryOperationInvoker.cacheProfiles == null)
                        {
                            lock (QueryOperationInvoker.syncRoot)
                            {
                                if (QueryOperationInvoker.cacheProfiles == null)
                                {
                                    OutputCacheSettingsSection outputCacheSettings = (OutputCacheSettingsSection)WebConfigurationManager.GetWebApplicationSection("system.web/caching/outputCacheSettings");
                                    QueryOperationInvoker.cacheProfiles = outputCacheSettings.OutputCacheProfiles;
                                }
                            }
                        }

                        OutputCacheProfile profile = QueryOperationInvoker.cacheProfiles[cacheAttribute.CacheProfile];
                        OutputCacheAttribute cacheInfo = new OutputCacheAttribute(QueryOperationInvoker.GetCacheLocation(profile.Location), profile.Duration);
                        cacheInfo.VaryByHeaders = profile.VaryByHeader;
                        cacheInfo.SqlCacheDependencies = profile.SqlDependency;
                        return cacheInfo;
                    }

                    return cacheAttribute;
                }
                return null;
            }

            /// <summary>
            /// Determines whether caching is supported for the current request to the specified domain operation entry.
            /// </summary>
            /// <param name="context">The context for the request.</param>
            /// <param name="domainOperationEntry">The requested domain operation entry, if any.</param>
            /// <returns>True if caching is supported.</returns>
            private static bool SupportsCaching(HttpContext context, DomainOperationEntry domainOperationEntry)
            {
                if (domainOperationEntry != null
                    && context.Request.HttpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (string queryParameter in QueryOperationInvoker.supportedQueryParameters)
                    {
                        if (context.Request.QueryString[queryParameter] != null)
                        {
                            return false;
                        }
                    }
                    return true;
                }
                return false;
            }

            /// <summary>
            /// Used to check whether a cache entry with sliding expiration has been expired.
            /// </summary>
            private class SlidingExpirationValidator
            {
                private readonly int _duration;
                private DateTime _lastAccessed;

                public SlidingExpirationValidator(int duration)
                {
                    this._duration = duration;
                    this._lastAccessed = DateTime.UtcNow;
                }

                public bool IsValid()
                {
                    bool isValid = DateTime.UtcNow.Subtract(this._lastAccessed).TotalSeconds < (double)this._duration;
                    if (isValid)
                    {
                        this._lastAccessed = DateTime.UtcNow;
                    }
                    return isValid;
                }
            }
        }
    }
}
