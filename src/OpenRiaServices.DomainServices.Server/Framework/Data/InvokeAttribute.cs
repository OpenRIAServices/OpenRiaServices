using System;

namespace OpenRiaServices.DomainServices.Server
{
    /// <summary>
    /// Attribute applied to a <see cref="DomainService"/> method to indicate that it is an invoke operation.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Field | AttributeTargets.Method | AttributeTargets.Property,
        AllowMultiple = false, Inherited = true)]
    public sealed class InvokeAttribute : Attribute
    {
        private bool _hasSideEffects = true;

        /// <summary>
        /// Gets or sets a value indicating whether the invoke operation has side-effects.
        /// </summary>
        /// <remarks>
        /// Operations with side-effects may be invoked differently by consumers of a <see cref="DomainService"/>. For example, 
        /// clients that invoke a <see cref="DomainService"/> over HTTP may use POST requests for invoke operations with side-effects, 
        /// while GET may be used otherwise.
        /// 
        /// The value of this property is <c>true</c> by default.
        /// </remarks>
        public bool HasSideEffects
        {
            get
            {
                return this._hasSideEffects;
            }
            set
            {
                this._hasSideEffects = value;
            }
        }
    }
}