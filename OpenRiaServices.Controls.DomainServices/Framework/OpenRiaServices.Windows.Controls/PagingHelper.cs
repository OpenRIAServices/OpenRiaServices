using System.Diagnostics;

namespace OpenRiaServices.Controls
{
    internal static class PagingHelper
    {
        /// <summary>
        /// Calculate the number of pages (full or partial) given an
        /// <paramref name="itemCount"/> and <paramref name="pageSize"/>.
        /// </summary>
        /// <param name="itemCount">The number of items across all pages.</param>
        /// <param name="pageSize">The size of each page.</param>
        /// <returns>The number of pages needed to represent all items.</returns>
        internal static int CalculatePageCount(int itemCount, int pageSize)
        {
            Debug.Assert(pageSize > 0, "PageSize cannot be 0");

            // We can use integer math only here instead of having to do a
            // Math.Ceiling call after dividing the two numbers as doubles.
            return (itemCount + pageSize - 1) / pageSize;
        }

        /// <summary>
        /// Calculate the number of full pages given an
        /// <paramref name="itemCount"/> and <paramref name="pageSize"/>.
        /// </summary>
        /// <param name="itemCount">The number of items across all pages.</param>
        /// <param name="pageSize">The size of each page.</param>
        /// <returns>The number of pages that would be filled by the items.</returns>
        internal static int CalculateFullPageCount(int itemCount, int pageSize)
        {
            Debug.Assert(pageSize > 0, "PageSize cannot be 0");

            // We can use integer math only here since we are only interested in
            // whole pages.
            return itemCount / pageSize;
        }
    }
}
