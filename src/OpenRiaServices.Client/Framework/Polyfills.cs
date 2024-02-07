using System;

namespace System.Collections.Generic
{
#if !NET
    /// <summary>
    /// Helper methods to allow "newer" .NET methods on older frameworks
    /// </summary>
    static class Polyfills
    {
        public static bool TryAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            // This is expected to be used in scenarios where the add will almost always succeed, so we pay the cost of an exception
            // on duplicates instead of checking if the Key exists first
            try
            {
                dictionary.Add(key, value);
                return true;
            }
            catch (ArgumentException)
            {
                // Duplicate key
                return false;
            }
        }
    }
#endif
}
