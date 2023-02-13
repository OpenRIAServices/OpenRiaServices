﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
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
        private readonly List<Action<EndpointBuilder>> _conventions;

        public Dictionary<string, DomainServiceDescription> DomainServices { get; } = new();
        private List<Endpoint> _endpoints;

        public OpenRiaServicesEndpointDataSource(RoutePatternTransformer routePatternTransformer)
        {
            _conventions = new List<Action<EndpointBuilder>>();
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

                    AddEndpoint(endpoints, name, invoker, hasSideEffects ? postOnly : getOrPost);
                }

                var submit = new ReflectionDomainServiceDescriptionProvider.ReflectionDomainOperationEntry(domainService.DomainServiceType,
                    typeof(DomainService).GetMethod(nameof(DomainService.SubmitAsync)), DomainOperation.Custom);

                var submitOperationInvoker = new SubmitOperationInvoker(submit, serializationHelper);
                AddEndpoint(endpoints, name, submitOperationInvoker, postOnly);


            }

            return endpoints;
        }

        private void AddEndpoint(List<Endpoint> endpoints, string domainService, OperationInvoker invoker, HttpMethodMetadata httpMethod)
        {
            var route = RoutePatternFactory.Parse($"{Prefix}/{domainService}/{invoker.OperationName}");

            // TODO: looka at adding authorization and authentication metadata to endpoiunt
            // authorization - look for any attribute implementing microsoft.aspnetcore.authorization.iauthorizedata 
            // https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.authorization.iauthorizedata?view=aspnetcore-6.0

            //var aut = operation.Attributes.Cast<Attribute>().OfType<Microsoft.spNetCore.Authorization.IAuthorizeData>().ToList();

            var endpointBuilder = new RouteEndpointBuilder(
                invoker.Invoke,
                route,
                0)
            {
                DisplayName = $"{domainService}.{invoker.OperationName}"
            };
            endpointBuilder.Metadata.Add(httpMethod);
            endpointBuilder.Metadata.Add(invoker.DomainOperation);

            // Try to add MethodInfo
            MethodInfo method = invoker.DomainOperation.DomainServiceType.GetMethod(invoker.DomainOperation.Name);
            if (method == null && invoker.DomainOperation.IsTaskAsync)
            {
                method = invoker.DomainOperation.DomainServiceType.GetMethod(invoker.DomainOperation.Name + "Async");
            }
            if (method is not null)
            {
                endpointBuilder.Metadata.Add(method);
            }

            //endpointBuilder.Metadata.Add(GenerateOpenApi(invoker));

            //endpointBuilder.Metadata.Add(new EndpointGroupNameAttribute(domainService));
            foreach (var convention in _conventions)
            {
                convention(endpointBuilder);
            }
            endpoints.Add(endpointBuilder.Build());
        }

        //private OpenApiOperation GenerateOpenApi(OperationInvoker invoker)
        //{
        //    throw new NotImplementedException();
        //}

        public override IChangeToken GetChangeToken()
        {
            return NullChangeToken.Singleton;
        }

        void IEndpointConventionBuilder.Add(Action<EndpointBuilder> convention)
        {
            _conventions.Add(convention);
        }
    }
}
