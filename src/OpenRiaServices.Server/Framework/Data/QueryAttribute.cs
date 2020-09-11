using System;

namespace OpenRiaServices.Server
{
    /// <summary>
    /// Attribute applied to a <see cref="DomainService"/> method to indicate that it is a query method.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Field | AttributeTargets.Method | AttributeTargets.Property,
        AllowMultiple = false, Inherited = true)]
    public sealed class QueryAttribute : Attribute
    {
        private bool _hasSideEffects;
        private bool _isComposable = true;
        private int _resultLimit;
        private bool _isDefault;

        /// <summary>
        /// Gets or sets a value indicating whether the query method has side-effects.
        /// </summary>
        /// <remarks>
        /// Queries with side-effects may be invoked differently by consumers of a <see cref="DomainService"/>. For example, 
        /// clients that invoke a <see cref="DomainService"/> over HTTP may use POST requests for queries with side-effects, 
        /// while GET may be used otherwise.
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

        /// <summary>
        /// Gets or sets a value indicating whether the query method allows query composition.
        /// </summary>
        public bool IsComposable
        {
            get
            {
                return this._isComposable;
            }
            set
            {
                this._isComposable = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum number of results a query operation should return. The default 
        /// is <value>0</value>, which means there is no limit.
        /// </summary>
        public int ResultLimit
        {
            get
            {
                return this._resultLimit;
            }
            set
            {
                this._resultLimit = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the query method should be treated
        /// as the default query when multiple query methods are available.
        /// </summary>
        /// <value>This value defaults to <c>false</c> unless otherwise specified.</value>
        public bool IsDefault
        {
            get
            {
                return this._isDefault;
            }
            set
            {
                this._isDefault = value;
            }
        }
    }
}
