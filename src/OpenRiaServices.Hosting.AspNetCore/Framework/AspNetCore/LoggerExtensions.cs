// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using Microsoft.Extensions.Logging;

namespace OpenRiaServices.Hosting.AspNetCore
{
    static partial class LoggerExtensions
    {
        private static readonly Action<ILogger, Type, Exception?> _logSkippingDomainService =
            LoggerMessage.Define<Type>(
                LogLevel.Trace,
                new EventId(0, nameof(NoEnableClientAccessAttributeSkipping)),
                "Skipping DomainService '{DomainServiceType}' since it is not marked with EnableClientAccessAttribute");

        private static readonly Action<ILogger, Type, string, Exception?> _logWarningSkippingDomainService =
            LoggerMessage.Define<Type, string>(
                LogLevel.Warning,
                new EventId(0, nameof(NotMappingDomainServiceDueToException)),
                "Skipped domain service '{DomainServiceType}' since it resulted in error: {Error}");

        public static void NoEnableClientAccessAttributeSkipping(this ILogger logger, Type serviceType)
        {
            _logSkippingDomainService(logger, serviceType, null);
        }

        public static void NotMappingDomainServiceDueToException(this ILogger logger, Type serviceType, Exception ex)
        {
            _logWarningSkippingDomainService(logger, serviceType, ex.Message, ex);
        }
    }
}
