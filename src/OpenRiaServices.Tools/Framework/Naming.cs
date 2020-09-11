using System;

namespace OpenRiaServices.Tools
{
    // Note: this code taken verbatim from \\cpvsbuild\drops\orcas\rtm\raw\21022.08\sources\ndp\fx\src\DLinq\DbmlShared\Naming.cs

    /// <summary>
    /// Contains code useful for creating and validating names. 
    /// Pluralization support, unique name creating, valid identifiers, etc.
    /// </summary>
    internal static class Naming
    {
        #region Pluralization

        /// <summary>
        /// Changes the string in name to be plural. In place string edit.
        /// </summary>
        /// <param name="name">The name to pluralize.</param>
        /// <returns>A reference to name for convenience.</returns>
        public static string MakePluralName(string name)
        {
            if (name.EndsWith("x", StringComparison.OrdinalIgnoreCase)
                || name.EndsWith("ch", StringComparison.OrdinalIgnoreCase)
                || name.EndsWith("ss", StringComparison.OrdinalIgnoreCase)
                || name.EndsWith("sh", StringComparison.OrdinalIgnoreCase))
            {
                name += "es";
            }
            else if (name.EndsWith("y", StringComparison.OrdinalIgnoreCase)
                && name.Length > 1 && !IsVowel(name[name.Length - 2]))
            {
                name = name.Remove(name.Length - 1, 1);
                name += "ies";
            }
            else if (!name.EndsWith("s", StringComparison.OrdinalIgnoreCase))
            {
                name += "s";
            }
            return name;
        }

        private static bool IsVowel(char c)
        {
            switch (c)
            {
                case 'a':
                case 'e':
                case 'i':
                case 'o':
                case 'u':
                case 'y':
                case 'A':
                case 'E':
                case 'I':
                case 'O':
                case 'U':
                case 'Y':
                    return true;
                default:
                    return false;
            }
        }

        #endregion Pluralization
    }
}
