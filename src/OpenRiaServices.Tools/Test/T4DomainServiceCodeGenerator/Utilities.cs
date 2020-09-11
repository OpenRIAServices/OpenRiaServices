using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRiaServices.Tools.Test.T4Generator
{
    public static class Utilities
    {
        public static string AttributeShortName(object attribute)
        {
            string name = attribute.GetType().Name;
            if (name.EndsWith("Attribute"))
            {
                name = name.Substring(0, name.Length - 9);
            }
            return name;
        }
    }
}
