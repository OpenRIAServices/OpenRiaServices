using System;
using System.Collections.Generic;
using System.Reflection;

namespace OpenRiaServices.Tools.SharedTypes
{
    /// <summary>
    /// General purpose interface to locate the names of the files
    /// used to define types or type members.
    /// </summary>
    internal interface ISourceFileProvider : IDisposable
    {
        /// <summary>
        /// Retrieves the name of the file that defines the given type member.
        /// </summary>
        /// <param name="memberInfo">The type member to test.  It cannot be null.</param>
        /// <returns>The full file name or null if it cannot be determined.  
        /// There are no guarantees the files can be opened.</returns>
        string GetFileForMember(MemberInfo memberInfo);
    }
}
