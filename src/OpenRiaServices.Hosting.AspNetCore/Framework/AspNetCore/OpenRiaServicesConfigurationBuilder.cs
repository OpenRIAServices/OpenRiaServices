// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using OpenRiaServices.Server;

namespace OpenRiaServices.Hosting.AspNetCore
{
    public class OpenRiaServicesConfigurationBuilder
    {
        private readonly OpenRiaServicesEndpointDataSource _dataSource;

        internal OpenRiaServicesConfigurationBuilder(OpenRiaServicesEndpointDataSource dataSource)
        {
            _dataSource = dataSource;
        }

        public void AddDomainService(Type type)
        {
            var longName = type.FullName.Replace('.', '-') + ".svc";
            var description = DomainServiceDescription.GetDescription(type);

            _dataSource.DomainServices.Add(longName + "/binary", description);
        }

        public void AddDomainService<T>()
        {
            var type = typeof(T);
            var longName = type.FullName.Replace('.', '-') + ".svc";
            var description = DomainServiceDescription.GetDescription(type);

            _dataSource.DomainServices.Add(longName + "/binary", description);
        }
    }
}
