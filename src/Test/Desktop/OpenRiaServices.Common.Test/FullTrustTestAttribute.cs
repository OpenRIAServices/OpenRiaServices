using System;

namespace OpenRiaServices.DomainServices.Client.Test
{
    /// <summary>
    /// Provides a hint to test hosts that a test requires full trust.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class FullTrustTestAttribute : Attribute
    {
    }
}
