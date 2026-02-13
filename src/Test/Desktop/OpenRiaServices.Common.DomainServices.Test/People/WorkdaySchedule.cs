#if NET

using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace People
{
    public class WorkdaySchedule
    {
        [Key]
        public int Id { get; set; }

        // TimeOnly should be supported as entity property
        public TimeOnly StartTime { get; set; }

        // TimeOnly should be supported as entity property (nullable)
        public TimeOnly? EndTime { get; set; }

        // TimeOnly should be supported as entity property (part of complex object)
        public LunchBreak LunchBreak { get; set; }
    }

    [Owned]
    public class LunchBreak
    {
        public TimeOnly StartTime { get; set; }

        public TimeOnly? EndTime { get; set; }
    }
}
#endif
