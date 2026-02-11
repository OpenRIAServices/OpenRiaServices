#if NET

using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using OpenRiaServices;
using OpenRiaServices.Server;

namespace People
{
    /// <summary>
    /// DomainService used to test functionality which only works for .NET (not .NET Framework) such as DateOnly.
    /// </summary>
    [EnableClientAccess]
    public class PeopleDomainService : DomainService
    {
        private readonly List<Person> _people = [];

        [Query]
        public IQueryable<Person> GetPersons()
        {
            return this._people.AsQueryable<Person>();
        }
    }
}
#endif
