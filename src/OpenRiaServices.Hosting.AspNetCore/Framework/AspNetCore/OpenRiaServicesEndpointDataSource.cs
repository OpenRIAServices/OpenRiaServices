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
using OpenRiaServices.Hosting.AspNetCore.Operations;
using OpenRiaServices.Server;

namespace OpenRiaServices.Hosting.AspNetCore
{
    internal sealed class OpenRiaServicesEndpointDataSource : EndpointDataSource, IEndpointConventionBuilder
    {
        private readonly List<Action<EndpointBuilder>> _conventions = new();
        private readonly List<Action<EndpointBuilder>> _finallyConventions = new();
        private readonly HttpMethodMetadata _getOrPost = new(new[] { "GET", "POST" });
        private readonly HttpMethodMetadata _postOnly = new(new[] { "POST" });

        private readonly Dictionary<string, DomainServiceEndpointBuilder> _endpointBuilders = new();
        private List<Endpoint> _endpoints;

        public OpenRiaServicesEndpointDataSource()
        {

        }

        internal IEndpointConventionBuilder AddDomainService(string path, Type type)
        {
            var description = DomainServiceDescription.GetDescription(type);
            var endpointBuilder = new DomainServiceEndpointBuilder(description);

            _endpointBuilders.Add(path, endpointBuilder);
            return endpointBuilder;
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

            foreach (var (name, domainServiceBuilder) in _endpointBuilders)
            {
                var domainService = domainServiceBuilder.Description;
                var serializationHelper = new SerializationHelper(domainService);

                // We could consider using Add and Finally on domainServiceBuilder to copy metadata instead
                // Consider adding additional metadata souch as route groups etc
                List<object> additionalMetadata = new List<object>();
                CopyAttributesToEndpointMetadata(domainService.Attributes, additionalMetadata);

                foreach (var operation in domainService.DomainOperationEntries)
                {
                    OperationInvoker invoker;
                    if (operation.Operation == DomainOperation.Query)
                    {
                        invoker = (OperationInvoker)Activator.CreateInstance(typeof(QueryOperationInvoker<>).MakeGenericType(operation.AssociatedType),
                            new object[] { operation, serializationHelper });
                    }
                    else if (operation.Operation == DomainOperation.Invoke)
                    {
                        invoker = new InvokeOperationInvoker(operation, serializationHelper);
                    }
                    else // Submit related methods are not directly accessible
                        continue;

                    endpoints.Add(BuildEndpoint(name, invoker, domainServiceBuilder, additionalMetadata));
                }

                var submit = new ReflectionDomainServiceDescriptionProvider.ReflectionDomainOperationEntry(domainService.DomainServiceType,
                    typeof(DomainService).GetMethod(nameof(DomainService.SubmitAsync)), DomainOperation.Custom);

                var submitOperationInvoker = new SubmitOperationInvoker(submit, serializationHelper);
                endpoints.Add(BuildEndpoint(name, submitOperationInvoker, domainServiceBuilder, additionalMetadata));
            }

            return endpoints;
        }

        /// <summary>
        /// Per DomainSerivce build 
        /// </summary>
        sealed class DomainServiceEndpointBuilder : IEndpointConventionBuilder
        {
            private readonly DomainServiceDescription _description;
            private readonly List<Action<EndpointBuilder>> _conventions = new();
            private readonly List<Action<EndpointBuilder>> _finallyConventions = new();

            public DomainServiceEndpointBuilder(DomainServiceDescription description)
            {
                _description = description;
            }

            public DomainServiceDescription Description { get { return _description; } }

            public void Add(Action<EndpointBuilder> convention)
            {
                _conventions.Add(convention);
            }

#if NET7_0_OR_GREATER
            public void Finally(System.Action<Microsoft.AspNetCore.Builder.EndpointBuilder> finallyConvention)
            {
                _finallyConventions.Add(finallyConvention);
            }
#endif

            public void ApplyConventions(EndpointBuilder endpointBuilder)
            {
                foreach (var convention in _conventions)
                    convention(endpointBuilder);
            }

            public void ApplyFinallyConventions(EndpointBuilder endpointBuilder)
            {
                foreach (var convention in _finallyConventions)
                    convention(endpointBuilder);
            }
        }

        private Endpoint BuildEndpoint(string domainService, OperationInvoker invoker, DomainServiceEndpointBuilder domainServiceEndpointBuilder, List<object> additionalMetadata)
        {
            var route = RoutePatternFactory.Parse($"{Prefix}/{domainService}/{invoker.OperationName}");

            var endpointBuilder = new RouteEndpointBuilder(
                invoker.Invoke,
                route,
                0)
            {
                DisplayName = $"{domainService}.{invoker.OperationName}"
            };

            endpointBuilder.Metadata.Add(invoker.HasSideEffects ? _postOnly : _getOrPost);
            endpointBuilder.Metadata.Add(invoker.DomainOperation);

            // Copy all AspNetCore Authorization attributes
            CopyAttributesToEndpointMetadata(invoker.DomainOperation.Attributes, endpointBuilder.Metadata);
            foreach (var metadata in additionalMetadata)
                endpointBuilder.Metadata.Add(metadata);

            domainServiceEndpointBuilder.ApplyConventions(endpointBuilder);
            ApplyConventions(endpointBuilder);

            domainServiceEndpointBuilder.ApplyFinallyConventions(endpointBuilder);
            ApplyFinallyConventions(endpointBuilder);

            return endpointBuilder.Build();
        }

        private void ApplyFinallyConventions(RouteEndpointBuilder endpointBuilder)
        {
            foreach (var finallyConvention in _finallyConventions)
                finallyConvention(endpointBuilder);
        }

        private void ApplyConventions(RouteEndpointBuilder endpointBuilder)
        {
            foreach (var convention in _conventions)
            {
                convention(endpointBuilder);
            }
        }

        /// <summary>
        /// Copy all attributes except for attributes handled internally by OpenRiaServices
        /// </summary>
        /// <remarks>
        /// This should enable all middlewares using attributes to control their behaviour to work.
        /// </remarks>
        private static void CopyAttributesToEndpointMetadata(System.ComponentModel.AttributeCollection attributes, IList<object> metadata)
        {
            static bool ShouldCopyAttributeToEndpointMetadata(Attribute attribute)
            {
                return attribute.GetType().Assembly != typeof(DomainService).Assembly
                    && attribute is not System.ComponentModel.DataAnnotations.ValidationAttribute
                    && attribute is not System.ComponentModel.DataAnnotations.AuthorizationAttribute
                    && !attribute.GetType().FullName.StartsWith("System.Diagnostics");
            }

            foreach (Attribute attribute in attributes)
            {
                if (ShouldCopyAttributeToEndpointMetadata(attribute))
                    metadata.Add(attribute);
            }
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
