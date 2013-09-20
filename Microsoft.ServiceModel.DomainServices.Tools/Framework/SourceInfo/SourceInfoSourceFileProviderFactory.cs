namespace Microsoft.ServiceModel.DomainServices.Tools.SourceLocation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.SymbolStore;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using Microsoft.ServiceModel.DomainServices.Tools.SharedTypes;

    /// <summary>
    /// Implementation of <see cref="ISourceFileProviderFactory"/> that returns
    /// an <see cref="IServiceProvider"/> that analyzes PDBs.
    /// </summary>
    internal class SourceInfoSourceFileProviderFactory : ISourceFileProviderFactory
    {
        public ISourceFileProvider CreateProvider()
        {
            return new SourceInfoSourceFileProvider();
        }

        /// <summary>
        /// Implementation of <see cref="ISourceFileProvider"/> that relies on
        /// the <c>SourceInfoAttribute</c> attached to type members.
        /// </summary>
        internal class SourceInfoSourceFileProvider : ISourceFileProvider, IDisposable
        {
            #region ISourceFileProvider methods

            /// <summary>
            /// See <see cref="ISourceFileProvider.GetFileForMember"/>.
            /// </summary>
            /// <remarks>
            /// This implementation-specific method uses Reflection to find a
            /// <c>SourceInfoAttribute</c> attached to the member.  If present,
            /// it identifies the source file.
            /// </remarks>
            /// <param name="memberInfo">The member for which a file location is needed.</param>
            /// <returns>The file name or <c>null</c> if it cannot be determined.</returns>
            public string GetFileForMember(MemberInfo memberInfo)
            {
                SourceInfo sourceInfo = SourceInfo.GetSourceInfoFromAttributes(((ICustomAttributeProvider)memberInfo).GetCustomAttributes(false));
                return (sourceInfo == null) ? null : sourceInfo.FileName;
            }
            #endregion // ISourceFileProvider methods

            #region IDisposable methods
            public void Dispose()
            {
            }
            #endregion IDisposable methods
        }
    }
}
