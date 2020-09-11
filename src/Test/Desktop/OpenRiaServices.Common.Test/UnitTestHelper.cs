using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#if SERVERFX
using OpenRiaServices.Hosting;
#else

#endif

#if SERVERFX
namespace OpenRiaServices.Hosting.Test
#else
namespace OpenRiaServices.Client.Test
#endif
{
    [System.Security.SecuritySafeCritical]  // Because our assembly is [APTCA] and we are used from partial trust tests
    public static class UnitTestHelper {
        public static bool EnglishBuildAndOS {
            get {
                bool englishBuild = String.Equals(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName, "en",
                    StringComparison.OrdinalIgnoreCase);
                bool englishOS = String.Equals(CultureInfo.CurrentCulture.TwoLetterISOLanguageName, "en",
                    StringComparison.OrdinalIgnoreCase);
                return englishBuild && englishOS;
            }
        }

        /// <summary>
        /// Creates a temporary folder under the current temporary path.
        /// </summary>
        /// <returns>The name of the folder that has been created.</returns>
        public static string CreateTempFolder()
        {
            string tempFolderName = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempFolderName);
            return tempFolderName;
        }

        /// <summary>
        /// Helper method to assert a string item appears in a list of strings
        /// </summary>
        /// <param name="list">actual list to search</param>
        /// <param name="expected">expected string</param>
        public static void AssertListContains(IEnumerable<string> list, string expected)
        {
            foreach (string s in list)
            {
                if (s.Equals(expected))
                {
                    return;
                }
            }
            string result = "Expected <" + expected + "> in list, but actual list was:";
            foreach (string s in list)
                result += ("\r\n" + s);
            Assert.Fail(result);
        }

        /// <summary>
        /// Helper method to assert a string item appears in a list of strings
        /// </summary>
        /// <param name="list">actual list to search</param>
        /// <param name="expected">expected string</param>
        public static void AssertListContains(IEnumerable<ValidationResult> list, string expected)
        {
            foreach (ValidationResult v in list)
            {
                string s = v.ErrorMessage;
                if (s.Equals(expected))
                {
                    return;
                }
            }
            string result = "Expected <" + expected + "> in list, but actual list was:";
            foreach (ValidationResult v in list)
                result += ("\r\n" + v.ErrorMessage);
            Assert.Fail(result);
        }

        /// <summary>
        /// Helper method to assert at least an item satisfying the expected condition appear in the given list
        /// </summary>
        /// <typeparam name="T">type of items in list</typeparam>
        /// <param name="list">actual list to search</param>
        /// <param name="expected">filter condition for the search</param>
        public static void AssertListContains<T>(IEnumerable<T> list, Func<T, bool> expected)
        {
            if (list.Where(expected).Any())
            {
                return;
            }

            // list does not contain any items that satisfy the expected predicate
            Assert.Fail();
        }

        public static void AssertListsAreEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual)
        {
            UnitTestHelper.AssertListsAreEqual<T>(expected, actual, (s, t) => s + ", " + t.ToString(), string.Empty);
        }

        public static void AssertListsAreEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual, string message)
        {
            UnitTestHelper.AssertListsAreEqual<T>(expected, actual, (s, t) => s + ", " + t.ToString(), message);
        }

        public static void AssertListsAreEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual, Func<string, T, string> aggregate)
        {
            UnitTestHelper.AssertListsAreEqual<T>(expected, actual, aggregate, string.Empty);
        }

        public static void AssertListsAreEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual, Func<string, T, string> aggregate, string message)
        {
            string countFormat = "Count = {0}";
            string expectedCount = string.Format(countFormat, expected.Count());
            string expectedResult = expected.Aggregate<T, string>(expectedCount, aggregate);

            string actualCount = string.Format(countFormat, actual.Count());
            string actualResult = actual.Aggregate<T, string>(actualCount, aggregate);

            Assert.AreEqual(expectedResult, actualResult, message);
        }

        public static void AssertValidationResultsAreEqual(IEnumerable<ValidationResult> expected, IEnumerable<ValidationResult> actual)
        {
            UnitTestHelper.AssertListsAreEqual<ValidationResult>(expected, actual, (s, vr) =>
            {
                return string.Format("{0}; {1} -- [{2}]", s, vr.ErrorMessage, vr.MemberNames.Aggregate((s1, s2) => string.Format("{0}, {1}", s1, s2)));
            });
        }
    }
}
