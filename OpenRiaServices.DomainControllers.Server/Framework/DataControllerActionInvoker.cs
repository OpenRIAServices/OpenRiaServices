// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Controllers;

namespace OpenRiaServices.DomainControllers.Server
{
    internal sealed class DataControllerActionInvoker : ApiControllerActionInvoker
    {
        public override Task<HttpResponseMessage> InvokeActionAsync(HttpActionContext actionContext, CancellationToken cancellationToken)
        {
            DataController controller = (DataController)actionContext.ControllerContext.Controller;
            controller.ActionContext = actionContext;
            return base.InvokeActionAsync(actionContext, cancellationToken);
        }
    }
}
