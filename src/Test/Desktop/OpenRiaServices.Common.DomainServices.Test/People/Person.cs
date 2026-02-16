#if NET

using System;
using System.ComponentModel.DataAnnotations;

namespace People
{
    public class Person
    {
        [Key]
        public string Name { get; set; }

        // DateOnly should be supported as entity property
        public DateOnly FavouriteDay { get; set; }

        // DateOnly should be supported as entity property (nullable)
        public DateOnly? WeddingDay { get; set; }

        // DateOnly should be supported as entity property (part of complex object)
        public Lifespan Lifespan { get; set; }
    }

    public class Lifespan
    {
        public DateOnly Born { get; set; }

        public DateOnly? Dead { get; set; }
    }
}
#endif
