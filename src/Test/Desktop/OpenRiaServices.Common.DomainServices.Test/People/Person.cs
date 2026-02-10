#if NET

using System;

namespace People
{
    public partial class Person
    {
        public string Name { get; set; }

        public DateOnly Birthday { get; set; }
    }
}
#endif
