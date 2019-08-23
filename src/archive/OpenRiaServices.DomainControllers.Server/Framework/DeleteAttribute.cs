﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;

namespace OpenRiaServices.DomainControllers.Server
{
    /// <summary>
    /// Attribute applied to a <see cref="DomainController"/> method to indicate that it is a delete method.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Field | AttributeTargets.Method | AttributeTargets.Property,
        AllowMultiple = false, Inherited = true)]
    public sealed class DeleteAttribute : Attribute
    {
    }
}
