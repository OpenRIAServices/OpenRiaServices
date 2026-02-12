#if NET

using System;
using System.Collections.Generic;
using System.Linq;
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
            new() { Name = "Erik", FavouriteDay = new(1970, 1, 1), Lifespan = new() { Born = new(1997, 1, 1) } },
            new() { Name = "Gustav", FavouriteDay = new(1523, 6, 6), WeddingDay = new(1531, 9, 24), Lifespan = new() { Born = new(1496, 5, 12), Dead = new(1560, 9, 29) } },
        ];

        [Query]
        public IQueryable<Person> GetPersons()
        {
            return this._people.AsQueryable<Person>();
        }

        // DateOnly should be supported as method parameters
        [Query]
        public IQueryable<Person> GetPersonsByFavouriteDay(DateOnly favouriteDay)
        {
            return this._people.Where(p => p.FavouriteDay.Equals(favouriteDay)).AsQueryable<Person>();
        }

        // DateOnly should be supported as method parameters (nullable)
        [Query]
        public IQueryable<Person> GetPersonsByWeddingDay(DateOnly? weddingDay)
        {
            return this._people.Where(p => p.WeddingDay.Equals(weddingDay)).AsQueryable<Person>();
        }
        
        /*
        // DateOnly should be supported as method parameters (part of complex object)
        [Query]
        public IQueryable<Person> GetPersonsByLifespan(Lifespan lifespan)
        {
            return this._people.Where(p => p.Lifespan.Equals(lifespan)).AsQueryable<Person>();
        }
        */

        // DateOnly should be supported as return value
        [Invoke]
        public DateOnly GetFavouriteDayByName(string name)
        {
            return this._people.Single(p => p.Name.Equals(name)).FavouriteDay;
        }

        // DateOnly should be supported as return value (nullable)
        [Invoke]
        public DateOnly? GetWeddingDayByName(string name)
        {
            return this._people.Single(p => p.Name.Equals(name)).WeddingDay;
        }

        /*
        // DateOnly should be supported as return value (part of complex object)
        [Invoke]
        public Lifespan GetPersonLifespanByName(string name)
        {
            return this._people.Single(p => p.Name.Equals(name)).Lifespan;
        }
        */
    }
}
#endif
