// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Controllers;

namespace OpenRiaServices.DomainControllers.Server
{
    internal sealed class DomainControllerActionInvoker : ApiControllerActionInvoker
    {
        public override Task<HttpResponseMessage> InvokeActionAsync(HttpActionContext actionContext, CancellationToken cancellationToken)
        {
            DomainController controller = (DomainController)actionContext.ControllerContext.Controller;
            controller.ActionContext = actionContext;
            return base.InvokeActionAsync(actionContext, cancellationToken);
        }
    }
}
