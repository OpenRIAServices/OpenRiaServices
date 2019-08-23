﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Web.Http;
using System.Web.Http.Controllers;

namespace OpenRiaServices.DomainControllers.Server
{
    internal sealed class DomainControllerActionSelector : ApiControllerActionSelector
    {
        private const string ActionRouteKey = "action";
        private const string SubmitActionValue = "Submit";

        public override HttpActionDescriptor SelectAction(HttpControllerContext controllerContext)
        {
            // first check to see if this is a call to Submit
            string actionName;
            if (controllerContext.RouteData.Values.TryGetValue(ActionRouteKey, out actionName) && actionName.Equals(SubmitActionValue, StringComparison.Ordinal))
            {
                return new SubmitActionDescriptor(controllerContext.ControllerDescriptor, controllerContext.Controller.GetType());
            }

            // next check to see if this is a direct invocation of a CUD action
            DomainControllerDescription description = DomainControllerDescription.GetDescription(controllerContext.ControllerDescriptor);
            UpdateActionDescriptor action = description.GetUpdateAction(actionName);
            if (action != null)
            {
                return new SubmitProxyActionDescriptor(action);
            }

            return base.SelectAction(controllerContext);
        }
    }
}
