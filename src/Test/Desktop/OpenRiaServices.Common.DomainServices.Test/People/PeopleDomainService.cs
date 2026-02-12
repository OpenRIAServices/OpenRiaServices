#if NET

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        [Query]
        public IQueryable<Person> GetPersonsByDate(DateOnly date)
        {
            return this._people.Where(p => p.Birthday.Equals(date)).AsQueryable<Person>();
        }

        [Invoke]
        public IEnumerable<DateOnly> GetBirthdays()
        {
            return this._people.Select(p => p.Birthday);
        }
    }
}
#endif
