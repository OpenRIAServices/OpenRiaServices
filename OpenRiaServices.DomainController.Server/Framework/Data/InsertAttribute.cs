using System;

namespace OpenRiaServices.DomainController.Server
{
    /// <summary>
    /// Attribute applied to a <see cref="DomainController"/> method to indicate that it is an insert method.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Field | AttributeTargets.Method | AttributeTargets.Property,
        AllowMultiple = false, Inherited = true)]
    public sealed class InsertAttribute : Attribute
    {
    }
}
