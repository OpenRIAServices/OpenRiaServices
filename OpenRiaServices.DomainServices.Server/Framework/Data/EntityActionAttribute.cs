using System;

namespace OpenRiaServices.DomainServices.Server
{
    /// <summary>
    /// Attribute applied to a <see cref="DomainService"/> method to indicate that it is an 
    /// entity action (previously called "custom update method").
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class EntityActionAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets a value indicating whether multiple calls by the same entity to a method is allowed.
        /// </summary>
        public bool AllowMultipleInvocations { get; set; }
    }
}
