// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using OpenRiaServices.Hosting.AspNetCore;
using OpenRiaServices.Server;

internal class FrameworkEndpointDataSource : EndpointDataSource, IEndpointConventionBuilder
{
    private readonly List<Action<EndpointBuilder>> _conventions;

    public Dictionary<string, DomainServiceDescription> DomainServices { get; } = new ();
    private List<Endpoint> _endpoints;

    public FrameworkEndpointDataSource(RoutePatternTransformer routePatternTransformer)
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

    private List<Endpoint> BuildEndpoints()
    {
        List<Endpoint> endpoints = new List<Endpoint>();
        var getOrPost = new HttpMethodMetadata(new[] { "GET", "POST" });
        var postOnly = new HttpMethodMetadata(new[] { "POST" });

        foreach (var (name, domainService) in DomainServices)
        {
            var serializationHelper = new SerializationHelper(domainService);

            foreach(var operation in domainService.DomainOperationEntries)
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

                NewMethod(endpoints, getOrPost, postOnly, name, hasSideEffects, invoker);
            }

            var submit = new ReflectionDomainServiceDescriptionProvider.ReflectionDomainOperationEntry(domainService.DomainServiceType,
                typeof(DomainService).GetMethod(nameof(DomainService.SubmitAsync)), DomainOperation.Custom);
            
            var submitOperationInvoker = new SubmitOperationInvoker(submit, serializationHelper);
            NewMethod(endpoints, getOrPost, postOnly, name, true, submitOperationInvoker);

            
        }

        return endpoints;
    }

    private void NewMethod(List<Endpoint> endpoints, HttpMethodMetadata getOrPost, HttpMethodMetadata postOnly, string name, bool hasSideEffects, OperationInvoker invoker)
    {
        var route = RoutePatternFactory.Parse($"/{name}/{invoker.Name}");
        var metadata = new EndpointMetadataCollection(hasSideEffects ? postOnly : getOrPost);

        //.RequireAuthorization("AtLeast21")
        // TODO: looka at adding authorization and authentication metadata to endpoiunt
        // authorization - look for any attribute implementing microsoft.aspnetcore.authorization.iauthorizedata 
        // https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.authorization.iauthorizedata?view=aspnetcore-6.0

        //var aut = operation.Attributes.Cast<Attribute>().OfType<Microsoft.spNetCore.Authorization.IAuthorizeData>().ToList();

        var endpointBuilder = new RouteEndpointBuilder(
            invoker.Invoke,
            route,
            1)
        {
            DisplayName = $"{name}.{invoker.Name}"
        };
        //endpointBuilder.Metadata.Add(new EndpointGroupNameAttribute(endpointGroupName));

        foreach (var convention in _conventions)
        {
            convention(endpointBuilder);
        }
        endpoints.Add(endpointBuilder.Build());
    }

    public override IChangeToken GetChangeToken()
    {
        return NullChangeToken.Singleton;
    }

    void IEndpointConventionBuilder.Add(Action<EndpointBuilder> convention)
    {
        _conventions.Add(convention);
    }
}
