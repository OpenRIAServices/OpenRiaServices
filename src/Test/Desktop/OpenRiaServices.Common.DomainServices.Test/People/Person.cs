#if NET

using System;
using System.ComponentModel.DataAnnotations;

namespace People
{
    public class Person
    {
        [Key]
        public string Name { get; set; }

        public DateOnly Birthday { get; set; }
    }
}
#endif
