using System;
using System.Runtime.Remoting;

namespace OpenRiaServices.VisualStudio.DomainServices.Tools
{
    public interface IContextData
    {
        /// <summary>
        /// Gets or sets the user-visible name of this context, including the DAL technology as a string
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [EnableClientAccess] should be generated.
        /// </summary>
        bool IsClientAccessEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether an OData endpoint should be generated
        /// </summary>
        bool IsODataEndpointEnabled { get; set; }

        /// <summary>
        /// Gets or sets a unique identifier for this context that can be used
        /// to track shared instances across the AppDomain boundary.  The <see cref="Name"/>
        /// property is not guaranteed to be unique.
        /// </summary>
        int ID { get; set; }

        object GetLifetimeService();
        object InitializeLifetimeService();
        ObjRef CreateObjRef(Type requestedType);
    }
}