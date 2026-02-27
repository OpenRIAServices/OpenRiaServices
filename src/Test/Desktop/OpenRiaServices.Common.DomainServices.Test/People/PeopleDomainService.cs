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

        private readonly List<WorkdaySchedule> _workdaySchedule = [
            new() { Id = 1, StartTime = new(8, 0), EndTime = new(17, 0), LunchBreak = new() { StartTime = new(12, 0), EndTime = new(13, 0) } },
            new() { Id = 2, StartTime = new(7, 45, 23, 555), EndTime = new(16, 45, 23, 555), LunchBreak = new() { StartTime = new(11, 30, 42), EndTime = new(12, 0) } },
            new() { Id = 3, StartTime = new(7, 10, 0), LunchBreak = new() { StartTime = new(11, 0) } }
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

        // DateOnly should be supported as return value (part of complex object)
        [Invoke]
        public Lifespan GetPersonLifespanByName(string name)
        {
            return this._people.Single(p => p.Name.Equals(name)).Lifespan;
        }

        [Query]
        public IQueryable<WorkdaySchedule> GetWorkdaySchedules()
        {
            return this._workdaySchedule.AsQueryable<WorkdaySchedule>();
        }

        // TimeOnly should be supported as return value
        [Invoke]
        public TimeOnly GetStartTimeById(int id)
        {
            return this._workdaySchedule.Single(p => p.Id.Equals(id)).StartTime;
        }

        // TimeOnly should be supported as return value (nullable)
        [Invoke]
        public TimeOnly? GetEndTimeById(int id)
        {
            return this._workdaySchedule.Single(p => p.Id.Equals(id)).EndTime;
        }

        // TimeOnly should be supported as return value (part of complex object)
        [Invoke]
        public LunchBreak GetLunchBreakById(int id)
        {
            return this._workdaySchedule.Single(p => p.Id.Equals(id)).LunchBreak;
        }

        // TimeOnly should be supported as method parameters
        [Query]
        public IQueryable<WorkdaySchedule> GetWorkdayScheduleByStartTime(TimeOnly startTime)
        {
            return this._workdaySchedule.Where(p => p.StartTime.Equals(startTime)).AsQueryable<WorkdaySchedule>();
        }

        // TimeOnly should be supported as method parameters (nullable)
        [Query]
        public IQueryable<WorkdaySchedule> GetWorkdayScheduleByEndTime(TimeOnly? endTime)
        {
            return this._workdaySchedule.Where(p => p.EndTime.Equals(endTime)).AsQueryable<WorkdaySchedule>();
        }
    }
}
#endif
