using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TypeLoadExceptionProject
{
    // This test project exists for one reason only, to force
    // a load exception when it is loaded.  It is used where
    // we want to test recovery from this situation
#pragma warning disable 0824
    public class Class1
    {
        extern static Class1();
        public static void Main() { return; }
    }
#pragma warning restore 0824
}
