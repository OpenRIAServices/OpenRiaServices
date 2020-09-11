using System.Collections.Generic;
using System.Linq;

namespace OpenRiaServices.Hosting
{
    /// <summary>
    /// Represents an <see cref="IQueryable"/>.
    /// </summary>
    internal class ServiceQuery
    {
        internal const string QueryPropertyName = "DomainServiceQuery";

        /// <summary>
        /// Gets or sets a list of query parts.
        /// </summary>
        public IEnumerable<ServiceQueryPart> QueryParts
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the total entity count 
        /// property is required in the result.
        /// </summary>
        public bool IncludeTotalCount
        {
            get;
            set;
        }
    }
}
