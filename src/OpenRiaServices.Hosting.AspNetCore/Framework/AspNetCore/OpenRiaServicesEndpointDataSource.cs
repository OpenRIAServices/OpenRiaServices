// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using OpenRiaServices.Hosting.AspNetCore.Operations;
using OpenRiaServices.Server;

namespace OpenRiaServices.Hosting.AspNetCore
{
    internal class OpenRiaServicesEndpointDataSource : EndpointDataSource, IEndpointConventionBuilder
    {
        private readonly List<Action<EndpointBuilder>> _conventions = new();
        private readonly List<Action<EndpointBuilder>> _finallyConventions = new();

        public Dictionary<string, DomainServiceDescription> DomainServices { get; } = new();
        private List<Endpoint> _endpoints;

        public OpenRiaServicesEndpointDataSource()
        {

        }

        public override IReadOnlyList<Endpoint> Endpoints
        {
            get
            {
                if (_endpoints == null)
                {
                    _endpoints = BuildEndpoints();
                }

                return _endpoints;
            }
        }

        public string Prefix { get; internal set; }

        private List<Endpoint> BuildEndpoints()
        {
            var endpoints = new List<Endpoint>();
            var getOrPost = new HttpMethodMetadata(new[] { "GET", "POST" });
            var postOnly = new HttpMethodMetadata(new[] { "POST" });


            foreach (var (name, domainService) in DomainServices)
            {
                var serializationHelper = new SerializationHelper(domainService);
                List<object> additionalMetadata = new List<object>();
                foreach (Attribute attribute in domainService.Attributes)
                {
                    if (CopyAttributeToEndpointMetadata(attribute))
                        additionalMetadata.Add(attribute);
                }
                // Consider adding additional metadata souch as route groups etc
                //endpointBuilder.Metadata.Add(new EndpointGroupNameAttribute(domainService));

                foreach (var operation in domainService.DomainOperationEntries)
                {
                    bool hasSideEffects;
                    OperationInvoker invoker;

                    if (operation.Operation == DomainOperation.Query)
                    {
                        invoker = (OperationInvoker)Activator.CreateInstance(typeof(QueryOperationInvoker<>).MakeGenericType(operation.AssociatedType),
                            new object[] { operation, serializationHelper });
                        hasSideEffects = ((QueryAttribute)operation.OperationAttribute).HasSideEffects;
                    }
                    else if (operation.Operation == DomainOperation.Invoke)
                    {
                        invoker = new InvokeOperationInvoker(operation, serializationHelper);
                        hasSideEffects = ((InvokeAttribute)operation.OperationAttribute).HasSideEffects;
                    }
                    else
                        continue;

                    AddEndpoint(endpoints, name, invoker, hasSideEffects ? postOnly : getOrPost, additionalMetadata);
                }

                var submit = new ReflectionDomainServiceDescriptionProvider.ReflectionDomainOperationEntry(domainService.DomainServiceType,
                    typeof(DomainService).GetMethod(nameof(DomainService.SubmitAsync)), DomainOperation.Custom);

                var submitOperationInvoker = new SubmitOperationInvoker(submit, serializationHelper);
                AddEndpoint(endpoints, name, submitOperationInvoker, postOnly, additionalMetadata);


            }

            return endpoints;
        }

        private void AddEndpoint(List<Endpoint> endpoints, string domainService, OperationInvoker invoker, HttpMethodMetadata httpMethod, List<object> additionalMetadata)
        {
            var route = RoutePatternFactory.Parse($"{Prefix}/{domainService}/{invoker.OperationName}");

            var endpointBuilder = new RouteEndpointBuilder(
                invoker.Invoke,
                route,
                0)
            {
                DisplayName = $"{domainService}.{invoker.OperationName}"
            };
            endpointBuilder.Metadata.Add(httpMethod);
            endpointBuilder.Metadata.Add(invoker.DomainOperation);

            // Copy all AspNetCore Authorization attributes
            foreach (Attribute attribute in invoker.DomainOperation.Attributes)
            {
                if (CopyAttributeToEndpointMetadata(attribute))
                    endpointBuilder.Metadata.Add(attribute);
            }

            // Try to add MethodInfo
            //if (TryGetMethodInfo(invoker) is MethodInfo method)
            //{
            //    endpointBuilder.Metadata.Add(method);
            //}

            foreach (var metadata in additionalMetadata)
                endpointBuilder.Metadata.Add(metadata);

            foreach (var convention in _conventions)
            {
                convention(endpointBuilder);
            }

            foreach (var finallyConvention in _finallyConventions)
                finallyConvention(endpointBuilder);

            endpoints.Add(endpointBuilder.Build());
        }

        private static bool CopyAttributeToEndpointMetadata(Attribute authorizeAttribute)
        {
            return authorizeAttribute is IAuthorizeData;
        }

        private static MethodInfo TryGetMethodInfo(OperationInvoker invoker)
        {
            MethodInfo method = invoker.DomainOperation.DomainServiceType.GetMethod(invoker.DomainOperation.Name);
            if (method == null && invoker.DomainOperation.IsTaskAsync)
            {
                method = invoker.DomainOperation.DomainServiceType.GetMethod(invoker.DomainOperation.Name + "Async");
            }

            return method;
        }

        public override IChangeToken GetChangeToken()
        {
            return NullChangeToken.Singleton;
        }

        void IEndpointConventionBuilder.Add(Action<EndpointBuilder> convention)
        {
            _conventions.Add(convention);
        }

#if NET7_0_OR_GREATER
        void IEndpointConventionBuilder.Finally(System.Action<Microsoft.AspNetCore.Builder.EndpointBuilder> finallyConvention)
        {
            _finallyConventions.Add(finallyConvention);
        }
#endif
    }
}
