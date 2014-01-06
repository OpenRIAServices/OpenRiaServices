using System;
using System.Collections.Generic;
using System.Runtime.Remoting;

namespace OpenRiaServices.VisualStudio.DomainServices.Tools
{
    public interface IGeneratedCode
    {
        /// <summary>
        /// Gets the generated source code.  It may be empty but it cannot be null.
        /// </summary>
        string SourceCode { get; }

        /// <summary>
        /// Gets the assembly references required to compile the code.  It may be empty but it cannot be null.
        /// </summary>
        IEnumerable<string> References { get; }

        object GetLifetimeService();
        object InitializeLifetimeService();
        ObjRef CreateObjRef(Type requestedType);
    }
}