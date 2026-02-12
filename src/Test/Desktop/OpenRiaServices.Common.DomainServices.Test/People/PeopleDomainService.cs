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
        private readonly List<Person> _people = [
            new() { Name = "Erik", Birthday = new(1997, 1, 1) },
            new() { Name = "Gustav", Birthday = new(1496, 5, 12) }
            ];

        [Query]
        public IQueryable<Person> GetPersons()
        {
            return this._people.AsQueryable<Person>();
        }
    }
}
#endif
