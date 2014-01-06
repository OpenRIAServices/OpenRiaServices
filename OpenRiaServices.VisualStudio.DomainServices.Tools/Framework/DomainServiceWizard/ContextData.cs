using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace OpenRiaServices.VisualStudio.DomainServices.Tools
{
    /// <summary>
    /// Data-only class used to share state across AppDomain
    /// boundaries between <see cref="BusinessLogicContext"/> and
    /// <see cref="ContextViewModel"/>
    /// </summary>
    [Serializable]
    public class ContextData : MarshalByRefObject, IContextData
    {
        /// <summary>
        /// Gets or sets the user-visible name of this context, including the DAL technology as a string
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [EnableClientAccess] should be generated.
        /// </summary>
        public bool IsClientAccessEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether an OData endpoint should be generated
        /// </summary>
        public bool IsODataEndpointEnabled { get; set; }

        /// <summary>
        /// Gets or sets a unique identifier for this context that can be used
        /// to track shared instances across the AppDomain boundary.  The <see cref="Name"/>
        /// property is not guaranteed to be unique.
        /// </summary>
        public int ID { get; set; }
    }
}
