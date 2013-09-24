using System.Collections.Generic;

namespace OpenRiaServices.Controls
{
    using Expression = System.Linq.Expressions.Expression;

    /// <summary>
    /// A custom dictionary for caching Linq <see cref="System.Linq.Expressions.Expression"/>s.
    /// </summary>
    internal class ExpressionCache : Dictionary<object, Expression>
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpressionCache"/> class.
        /// </summary>
        public ExpressionCache() { }

        #endregion
    }
}
