using System;
using System.Collections.Generic;
using System.Reflection;

namespace OpenRiaServices.Tools.SharedTypes
{
    /// <summary>
    /// General purpose interface to locate the names of the files
    /// used to define types or type members.
    /// </summary>
    internal interface ISourceFileLocationService
    {
        /// <summary>
        /// Retrieves the collection of file names that jointly define
        /// the given type.
        /// </summary>
        /// <param name="type">The type to test.  It cannot be null.</param>
        /// <returns>A collection of full file names that define the type.  It may be empty.  
        /// There are no guarantees the files in this collection can be opened.</returns>
        IEnumerable<string> GetFilesForType(Type type);

        /// <summary>
        /// Retrieves the name of the file that defines the given type member.
        /// </summary>
        /// <param name="memberInfo">The type member to test.  It cannot be null.</param>
        /// <returns>The full file name or null if it cannot be determined.  
        /// There are no guarantees the files can be opened.</returns>
        string GetFileForMember(MemberInfo memberInfo);
    }
}
