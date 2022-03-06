using System;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Dispatcher;
using System.Threading.Tasks;
using System.Web;
using System.Web.Caching;
using System.Web.Configuration;
using OpenRiaServices.Server;

namespace OpenRiaServices.Hosting.Wcf.Behaviors
{
    internal abstract class DomainOperationInvoker : IOperationInvoker
    {
        private readonly DomainOperationType operationType;
        private static readonly string[] s_unsupportedQueryParameters = { "$where", "$orderby", "$skip", "$take", "$includeTotalCount" };
        private static readonly char[] colonDelimiter = new char[] { ':' };
        private static readonly char[] semiColonDelimiter = new char[] { ';' };
        private static readonly object syncRoot = new object();
        private static OutputCacheProfileCollection cacheProfiles;

        public DomainOperationInvoker(DomainOperationType operationType)
        {
            this.operationType = operationType;
        }

        public bool IsSynchronous
        {
            get
            {
                return false;
            }
        }

        protected abstract string Name
        {
            get;
        }

        public abstract object[] AllocateInputs();

        public object Invoke(object instance, object[] inputs, out object[] outputs)
        {
            throw new NotSupportedException();
        }

        protected virtual void ConvertInputs(object[] inputs)
        {
        }

        protected virtual object ConvertReturnValue(object returnValue)
        {
            return returnValue;
        }

        protected abstract ValueTask<object> InvokeCoreAsync(DomainService instance, object[] inputs, bool disableStackTraces);

        public IAsyncResult InvokeBegin(object instance, object[] inputs, AsyncCallback callback, object state)
        {
            return TaskExtensions.BeginApm(InvokeAsync(instance, inputs), callback, state);
        }

        public object InvokeEnd(object instance, out object[] outputs, IAsyncResult result)
        {
            outputs = ServiceUtility.EmptyObjectArray;
            return TaskExtensions.EndApm<object>(result);
        }

        private async ValueTask<object> InvokeAsync(object instance, object[] inputs)
        {
            long startTicks = DiagnosticUtility.GetTicks();
            bool disableStackTraces = true;
            var operationContext = OperationContext.Current;
            try
            {
                // create and initialize the DomainService for this request
                var instanceInfo = (DomainServiceBehavior.DomainServiceInstanceInfo)instance;
                var user = HttpContext.Current?.User ?? operationContext.ClaimsPrincipal;
                WcfDomainServiceContext context = new WcfDomainServiceContext(instanceInfo.ServiceScope.ServiceProvider, user, this.operationType);
                disableStackTraces = context.DisableStackTraces;

                DiagnosticUtility.OperationInvoked(this.Name, operationContext);

                DomainService domainService = this.GetDomainService(instanceInfo, context);

                // invoke the operation and process the result
                this.ConvertInputs(inputs);
                var result = await this.InvokeCoreAsync(domainService, inputs, disableStackTraces).ConfigureAwait(false);
                result = this.ConvertReturnValue(result);

                DiagnosticUtility.OperationCompleted(this.Name, DiagnosticUtility.GetDuration(startTicks), operationContext);

                return result;
            }
            catch (FaultException)
            {
                DiagnosticUtility.OperationFaulted(this.Name, DiagnosticUtility.GetDuration(startTicks), operationContext);

                // if the exception has already been transformed to a fault
                // just rethrow it
                throw;
            }
            catch (Exception ex)
            {
                if (ex.IsFatal())
                {
                    throw;
                }
                DiagnosticUtility.OperationFailed(this.Name, DiagnosticUtility.GetDuration(startTicks), operationContext);

                // We need to ensure that any time an exception is thrown by the
                // service it is transformed to a properly sanitized/configured
                // fault exception.
                throw ServiceUtility.CreateFaultException(ex, disableStackTraces);
            }
        }

        private DomainService GetDomainService(DomainServiceBehavior.DomainServiceInstanceInfo instanceInfo, WcfDomainServiceContext context)
        {
            // create and initialize the DomainService for this request
            if (instanceInfo.ServiceScope.ServiceProvider.GetService(instanceInfo.DomainServiceType) is DomainService service)
            {
                // Do NOT save instance in instanceInfo.DomainServiceInstance since container will dispose instance
                service.Initialize(context);
                return service;
            }

            try
            {
                DomainService domainService = DomainService.Factory.CreateDomainService(instanceInfo.DomainServiceType, context);
                instanceInfo.DomainServiceInstance = domainService;
                return domainService;
            }
            catch (TargetInvocationException tie)
            {
                if (tie.InnerException != null)
                {
                    throw ServiceUtility.CreateFaultException(tie.InnerException, context.DisableStackTraces);
                }

                throw ServiceUtility.CreateFaultException(tie, context.DisableStackTraces);
            }
        }

        /// <summary>
        /// Clears the output cache policy.
        /// </summary>
        protected static void ClearOutputCachingPolicy(HttpContext context)
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
        protected static void SetOutputCachingPolicy(HttpContext context, DomainOperationEntry domainOperationEntry)
        {
            if (context == null)
                return;

            if (SupportsCaching(context, domainOperationEntry))
            {
                OutputCacheAttribute outputCacheInfo = GetOutputCacheInformation(domainOperationEntry);
                if (outputCacheInfo != null)
                {
                    HttpCachePolicy policy = context.Response.Cache;
                    if (outputCacheInfo.UseSlidingExpiration)
                    {
                        policy.SetSlidingExpiration(true);
                        policy.AddValidationCallback((HttpCacheValidateHandler)delegate (HttpContext c, object d, ref HttpValidationStatus status)
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

                    policy.SetCacheability(GetCacheability(outputCacheInfo.Location));

                    if (!String.IsNullOrEmpty(outputCacheInfo.SqlCacheDependencies))
                    {
                        // Syntax is <databaseEntry>:<tableName>[;<databaseEntry>:<tableName>]*.
                        string[] dependencies = outputCacheInfo.SqlCacheDependencies.Split(semiColonDelimiter, StringSplitOptions.RemoveEmptyEntries);
                        foreach (string dependency in dependencies)
                        {
                            string[] dependencyTokens = dependency.Split(colonDelimiter, StringSplitOptions.RemoveEmptyEntries);
                            if (dependencyTokens.Length != 2)
                            {
                                throw new InvalidOperationException(Resource.DomainService_InvalidSqlDependencyFormat);
                            }

                            context.Response.AddCacheDependency(new SqlCacheDependency(dependencyTokens[0], dependencyTokens[1]));
                        }
                    }

                    if (!String.IsNullOrEmpty(outputCacheInfo.VaryByHeaders))
                    {
                        string[] headers = outputCacheInfo.VaryByHeaders.Split(semiColonDelimiter, StringSplitOptions.RemoveEmptyEntries);
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
                    foreach (string queryParameter in s_unsupportedQueryParameters)
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
                foreach (string queryParameter in s_unsupportedQueryParameters)
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
                    if (cacheProfiles == null)
                    {
                        lock (syncRoot)
                        {
                            if (cacheProfiles == null)
                            {
                                OutputCacheSettingsSection outputCacheSettings = (OutputCacheSettingsSection)WebConfigurationManager.GetWebApplicationSection("system.web/caching/outputCacheSettings");
                                cacheProfiles = outputCacheSettings.OutputCacheProfiles;
                            }
                        }
                    }

                    OutputCacheProfile profile = cacheProfiles[cacheAttribute.CacheProfile];
                    OutputCacheAttribute cacheInfo = new OutputCacheAttribute(GetCacheLocation(profile.Location), profile.Duration);
                    cacheInfo.VaryByHeaders = profile.VaryByHeader;
                    cacheInfo.SqlCacheDependencies = profile.SqlDependency;
                    return cacheInfo;
                }

                return cacheAttribute;
            }
            return null;
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
