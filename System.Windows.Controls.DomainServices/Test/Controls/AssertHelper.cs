using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Windows.Controls.DomainServices.Test
{
    /// <summary>
    /// A collection of assert helpers to be used for DomainDataSource unit tests.
    /// </summary>
    public static class AssertHelper
    {
        /// <summary>
        /// Evaluate the sorting of the given <paramref name="sequence"/> of strings, ensuring that it's
        /// sorted in the <paramref name="direction"/> specified.
        /// </summary>
        /// <param name="sequence">The enumerable of strings to evaluate.</param>
        /// <param name="direction">The <see cref="ListSortDirection"/> expected.</param>
        public static void AssertSequenceSorting(IEnumerable<string> sequence, ListSortDirection direction, string message)
        {
            int expectedCompare = direction == ListSortDirection.Ascending ? -1 : +1;

            string previousItem = null;

            foreach (string item in sequence)
            {
                if (previousItem != null)
                {
                    int actualCompare = previousItem.CompareTo(item);

                    if (!(actualCompare == expectedCompare || actualCompare == 0))
                    {
                        Assert.Fail(string.Format("Sequence is not sorted in {0} order: {1}. {2}", direction.ToString(), string.Join(", ", sequence.ToArray()), message));
                    }
                }

                previousItem = item;
            }
        }
    }
}
