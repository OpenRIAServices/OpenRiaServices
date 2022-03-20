// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using OpenRiaServices.Server;

public class FrameworkConfigurationBuilder
{
    private readonly FrameworkEndpointDataSource _dataSource;

    internal FrameworkConfigurationBuilder(FrameworkEndpointDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public void AddDomainService(Type type)
    {
        var longName = type.FullName.Replace('.', '-') + ".svc";
        DomainServiceDescription description = DomainServiceDescription.GetDescription(type);

        _dataSource.DomainServices.Add(longName + "/binary", description);
    }
}
