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

namespace OpenRiaServices.Hosting.Wcf.Behaviors
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
                    SetOutputCachingPolicy(httpContext, this.operation);
                    result = await QueryProcessor.ProcessAsync<TEntity>(instance, this.operation, inputs, serviceQuery);
                }
                catch (Exception ex)
                {
                    if (ex.IsFatal())
                    {
                        throw;
                    }
                    ClearOutputCachingPolicy(httpContext);
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
        }
    }
}
