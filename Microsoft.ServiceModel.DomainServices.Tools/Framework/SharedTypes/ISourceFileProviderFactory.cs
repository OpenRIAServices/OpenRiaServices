using System;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.ServiceModel.DomainServices.Tools.SharedTypes
{
    /// <summary>
    /// Factory interface to allow creation of <see cref="ISourceFileProvider"/> instances.
    /// </summary>
    internal interface ISourceFileProviderFactory
    {
        /// <summary>
        /// Instantiates a <see cref="ISourceFileProvider"/> object
        /// that can be used to interrogate source file locations
        /// </summary>
        /// <returns>A <see cref="ISourceFileProvider"/> instance.  It will be disposed by the caller.</returns>
        ISourceFileProvider CreateProvider();
    }
}
