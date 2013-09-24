using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace OpenRiaServices.DomainServices.Client.Test
{
    using TestDescription = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;
    using System.ComponentModel.DataAnnotations;
    using System.Collections.Generic;
    using Microsoft.Silverlight.Testing;

    [TestClass]
    public class ValidationResultCollectionTest
    {
        /// <summary>
        /// Verifies that the expected collection events are raised from the <see cref="ValidationResultCollection"/>
        /// for validation results with the specified member names.
        /// </summary>
        /// <param name="validationResultMemberNames">
        /// The array of member names to create validation results for that will be
        /// added and removed from the collection.
        /// </param>
        /// <param name="errorsChangedMemberNames">
        /// The array of member names to expect errors changed events for.
        /// </param>
        private static void VerifyAddRemoveCollectionEvents(string[] validationResultMemberNames)
        {
            VerifyAddRemoveCollectionEvents(validationResultMemberNames, validationResultMemberNames);
        }

        /// <summary>
        /// Verifies that the expected collection events are raised from the <see cref="ValidationResultCollection"/>
        /// for validation results with the specified member names.
        /// </summary>
        /// <param name="validationResultMemberNames">
        /// The array of member names to create validation results for that will be
        /// added and removed from the collection.
        /// </param>
        /// <param name="errorsChangedMemberNames">The array of member names to expect errors changed events for.</param>
        private static void VerifyAddRemoveCollectionEvents(string[] validationResultMemberNames, string[] errorsChangedMemberNames)
        {
            int collectionChangedCount = 0;
            int hasErrorsChangedCount = 0;
            List<string> propertyErrorsChangedList = new List<string>();

            Action collectionChanged = () => ++collectionChangedCount;
            Action hasErrorsChanged = () => ++hasErrorsChangedCount;
            Action<string> propertyErrorsChanged = propertyName => propertyErrorsChangedList.Add(propertyName);

            ValidationResultCollection collection = new TestValidationResultCollection(collectionChanged, hasErrorsChanged, propertyErrorsChanged);
            ValidationResult vr1 = new ValidationResult("Error 1", validationResultMemberNames);
            collection.Add(vr1);

            Assert.AreEqual<int>(1, collectionChangedCount, "CollectionChanged count for first add");
            Assert.AreEqual<int>(1, hasErrorsChangedCount, "HasErrorsChanged count for first add");
            Assert.IsTrue(propertyErrorsChangedList.OrderBy(s => s).SequenceEqual(errorsChangedMemberNames.OrderBy(s => s)), "propertyErrorsChangedList for first add");

            collectionChangedCount = 0;
            hasErrorsChangedCount = 0;
            propertyErrorsChangedList.Clear();

            ValidationResult vr2 = new ValidationResult("Error 2", validationResultMemberNames);
            collection.Add(vr2);

            Assert.AreEqual<int>(1, collectionChangedCount, "CollectionChanged count for second add");
            Assert.AreEqual<int>(0, hasErrorsChangedCount, "HasErrorsChanged count for second add");
            Assert.IsTrue(propertyErrorsChangedList.OrderBy(s => s).SequenceEqual(errorsChangedMemberNames.OrderBy(s => s)), "propertyErrorsChangedList for second add");

            collectionChangedCount = 0;
            hasErrorsChangedCount = 0;
            propertyErrorsChangedList.Clear();

            collection.Remove(vr1);

            Assert.AreEqual<int>(1, collectionChangedCount, "CollectionChanged count for first remove");
            Assert.AreEqual<int>(0, hasErrorsChangedCount, "HasErrorsChanged count for first remove");
            Assert.IsTrue(propertyErrorsChangedList.OrderBy(s => s).SequenceEqual(errorsChangedMemberNames.OrderBy(s => s)), "propertyErrorsChangedList for first remove");

            collectionChangedCount = 0;
            hasErrorsChangedCount = 0;
            propertyErrorsChangedList.Clear();

            collection.Remove(vr2);

            Assert.AreEqual<int>(1, collectionChangedCount, "CollectionChanged count for second remove");
            Assert.AreEqual<int>(1, hasErrorsChangedCount, "HasErrorsChanged count for second remove");
            Assert.IsTrue(propertyErrorsChangedList.OrderBy(s => s).SequenceEqual(errorsChangedMemberNames.OrderBy(s => s)), "propertyErrorsChangedList for second remove");
        }

        /// <summary>
        /// Verifies that the expected collection events are raised from the <see cref="ValidationResultCollection"/>
        /// when <see cref="ValidationResultCollection.ReplaceErrors"/> is called for a property.
        /// </summary>
        /// <param name="member">The property that errors are being replaced for</param>
        /// <param name="replacementResults">The results to use as the replacement errors for the property for the replacement.</param>
        /// <param name="errorsChangedMembers">The array of member names to expect errors changed events for.</param>
        private static void VerifyPropertyReplacementEvents(string member, IEnumerable<ValidationResult> replacementResults, string[] errorsChangedMembers)
        {
            int collectionChangedCount = 0;
            int hasErrorsChangedCount = 0;
            List<string> propertyErrorsChangedList = new List<string>();

            Action collectionChanged = () => ++collectionChangedCount;
            Action hasErrorsChanged = () => ++hasErrorsChangedCount;
            Action<string> propertyErrorsChanged = propertyName => propertyErrorsChangedList.Add(propertyName);

            ValidationResultCollection collection = new TestValidationResultCollection(collectionChanged, hasErrorsChanged, propertyErrorsChanged);
            collection.ReplaceErrors(member, replacementResults);

            Assert.AreEqual<int>(1, collectionChangedCount, "CollectionChanged count for first replacement");
            Assert.AreEqual<int>(1, hasErrorsChangedCount, "HasErrorsChanged count for first replacement");
            Assert.IsTrue(propertyErrorsChangedList.OrderBy(s => s).SequenceEqual(errorsChangedMembers.OrderBy(s => s)), "propertyErrorsChangedList for first replacement");

            collection.Add(new ValidationResult("Error", new string[] { member }));
            collectionChangedCount = 0;
            hasErrorsChangedCount = 0;
            propertyErrorsChangedList.Clear();

            collection.ReplaceErrors(member, replacementResults);

            Assert.AreEqual<int>(1, collectionChangedCount, "CollectionChanged count for second replacement");
            Assert.AreEqual<int>(0, hasErrorsChangedCount, "HasErrorsChanged count for second replacement");
            Assert.IsTrue(propertyErrorsChangedList.OrderBy(s => s).SequenceEqual(errorsChangedMembers.OrderBy(s => s)), "propertyErrorsChangedList for second replacement");
        }

        /// <summary>
        /// Verifies that the expected collection events are raised from the <see cref="ValidationResultCollection"/>
        /// when <see cref="ValidationResultCollection.ReplaceErrors"/> is called to replace all errors.
        /// </summary>
        /// <param name="errorAddedBetweenReplacements">The member name for a result to add between the two replacements to ensure as a differentiator for the replacement results.</param>
        /// <param name="replacementResults">The results to use as the replacement errors.</param>
        /// <param name="firstErrorsChangedMembers">The array of member names to expect errors changed events for the first replacement.</param>
        /// <param name="secondErrorsChangedMembers">The array of member names to expect errors changed events for the second replacement.</param>
        private static void VerifyReplacementEvents(string member, IEnumerable<ValidationResult> replacementResults, string[] firstErrorsChangedMembers, string[] secondErrorsChangedMembers)
        {
            int collectionChangedCount = 0;
            int hasErrorsChangedCount = 0;
            List<string> propertyErrorsChangedList = new List<string>();

            Action collectionChanged = () => ++collectionChangedCount;
            Action hasErrorsChanged = () => ++hasErrorsChangedCount;
            Action<string> propertyErrorsChanged = propertyName => propertyErrorsChangedList.Add(propertyName);

            ValidationResultCollection collection = new TestValidationResultCollection(collectionChanged, hasErrorsChanged, propertyErrorsChanged);
            collection.ReplaceErrors(replacementResults);

            Assert.AreEqual<int>(1, collectionChangedCount, "CollectionChanged count for first replacement");
            Assert.AreEqual<int>(1, hasErrorsChangedCount, "HasErrorsChanged count for first replacement");
            Assert.IsTrue(propertyErrorsChangedList.OrderBy(s => s).SequenceEqual(firstErrorsChangedMembers.OrderBy(s => s)), "propertyErrorsChangedList for first replacement");

            collection.Add(new ValidationResult("Error", new string[] { member }));

            collectionChangedCount = 0;
            hasErrorsChangedCount = 0;
            propertyErrorsChangedList.Clear();

            collection.ReplaceErrors(replacementResults);

            Assert.AreEqual<int>(1, collectionChangedCount, "CollectionChanged count for second replacement");
            Assert.AreEqual<int>(0, hasErrorsChangedCount, "HasErrorsChanged count for second replacement");
            Assert.IsTrue(propertyErrorsChangedList.OrderBy(s => s).SequenceEqual(secondErrorsChangedMembers.OrderBy(s => s)), "propertyErrorsChangedList for second replacement");
        }

        [TestMethod]
        [TestDescription("When a property result is added or removed, ensure the callbacks are called appropriately")]
        public void AddRemovePropertyResult()
        {
            VerifyAddRemoveCollectionEvents(new string[] { "Member" });
        }

        [TestMethod]
        [TestDescription("When a multi-property result is added, ensure the callbacks are called appropriately")]
        public void AddRemoveMultiPropertyResult()
        {
            VerifyAddRemoveCollectionEvents(new string[] { "Member 1", "Member 2" });
        }

        [TestMethod]
        [TestDescription("When an entity-level error (with an empty member names array) is added, ensure the callbacks are called appropriately")]
        public void AddRemoveEntityResultWithNoMembers()
        {
            // When null is specified for the member names, we should get notification for the entity-level change
            VerifyAddRemoveCollectionEvents(null, new string[] { null });
        }

        [TestMethod]
        [TestDescription("When an entity-level error (with an array containing null) is added, ensure the callbacks are called appropriately")]
        public void AddRemoveEntityResultWithNullMember()
        {
            VerifyAddRemoveCollectionEvents(new string[] { null });
        }

        [TestMethod]
        [TestDescription("When an entity-level error (with an array containing an empty string) is added, ensure the callbacks are called appropriately")]
        public void AddRemoveEntityResultWithEmptyMember()
        {
            VerifyAddRemoveCollectionEvents(new string[] { string.Empty });
        }

        [TestMethod]
        [TestDescription("When a result is added that has a member name and a null (entity-level), ensure the callbacks are called appropriately")]
        public void AddRemovePropertyAndEntityResultWithNullMember()
        {
            VerifyAddRemoveCollectionEvents(new string[] { "Member", null });
        }

        [TestMethod]
        [TestDescription("When a result is added that has a member name and an empty string (entity-level), ensure the callbacks are called appropriately")]
        public void AddRemovePropertyAndEntityResultWithEmptyMember()
        {
            VerifyAddRemoveCollectionEvents(new string[] { "Member", string.Empty });
        }

        [TestMethod]
        [TestDescription("When we replace errors for Member with nothing, we should get events for that member")]
        public void ReplacePropertyResultsWithNothing()
        {
            // This test differs from the other replacement tests enough that it doesn't use the same helper method

            string member = "Member";
            string[] memberNames = new string[] { member };
            string[] replacementNames = new string[] { member };
            IEnumerable<ValidationResult> initialResults = new ValidationResult[] { new ValidationResult("Error", memberNames) };
            IEnumerable<ValidationResult> replacementResults = Enumerable.Empty<ValidationResult>();

            int collectionChangedCount = 0;
            int hasErrorsChangedCount = 0;
            List<string> propertyErrorsChangedList = new List<string>();

            Action collectionChanged = () => ++collectionChangedCount;
            Action hasErrorsChanged = () => ++hasErrorsChangedCount;
            Action<string> propertyErrorsChanged = propertyName => propertyErrorsChangedList.Add(propertyName);

            ValidationResultCollection collection = new TestValidationResultCollection(collectionChanged, hasErrorsChanged, propertyErrorsChanged);
            collection.ReplaceErrors(member, initialResults);

            Assert.AreEqual<int>(1, collectionChangedCount, "CollectionChanged count for first replacement");
            Assert.AreEqual<int>(1, hasErrorsChangedCount, "HasErrorsChanged count for first replacement");
            Assert.IsTrue(propertyErrorsChangedList.OrderBy(s => s).SequenceEqual(replacementNames.OrderBy(s => s)), "propertyErrorsChangedList for first replacement");

            collection.Add(new ValidationResult("Error", new string[] { member }));
            collectionChangedCount = 0;
            hasErrorsChangedCount = 0;
            propertyErrorsChangedList.Clear();

            collection.ReplaceErrors(member, replacementResults);

            // Notice that HasErrors changes again because we no longer have any errors
            Assert.AreEqual<int>(1, collectionChangedCount, "CollectionChanged count for second replacement");
            Assert.AreEqual<int>(1, hasErrorsChangedCount, "HasErrorsChanged count for second replacement");
            Assert.IsTrue(propertyErrorsChangedList.OrderBy(s => s).SequenceEqual(replacementNames.OrderBy(s => s)), "propertyErrorsChangedList for second replacement");
        }

        [TestMethod]
        [TestDescription("When we replace errors for Member with new errors for that member, we should get events for that member")]
        public void ReplacePropertyResultsWithPropertyResults()
        {
            string member = "Member";
            string[] memberNames = new string[] { member };
            string[] replacementNames = new string[] { member };
            IEnumerable<ValidationResult> replacementResults = new ValidationResult[] { new ValidationResult("Replacement Error", memberNames) };

            VerifyPropertyReplacementEvents(member, replacementResults, replacementNames);
        }

        [TestMethod]
        [TestDescription("When we replace errors for Member with new errors for Member plus errors for another member, we should get events for both members")]
        public void ReplacePropertyResultsWithMultiPropertyResults()
        {
            string member = "Member";
            string otherMember = "OtherMember";
            string[] memberNames = new string[] { member };
            string[] replacementNames = new string[] { member, otherMember };

            IEnumerable<ValidationResult> replacementResults = new ValidationResult[]
            {
                new ValidationResult("Replacement Error", new string[] { member }),
                new ValidationResult("Other Property Replacement Error", new string[] { otherMember })
            };

            VerifyPropertyReplacementEvents(member, replacementResults, replacementNames);
        }

        [TestMethod]
        [TestDescription("When we replace errors for Member with new errors for Member plus errors for the entity (no members), we should get events for the member and the entity")]
        public void ReplacePropertyResultsWithPropertyAndEntityResultsWithNoMembers()
        {
            string member = "Member";
            string[] memberNames = new string[] { member };
            string[] replacementNames = new string[] { member, null };

            IEnumerable<ValidationResult> replacementResults = new ValidationResult[]
            {
                new ValidationResult("Replacement Error", new string[] { member }),
                new ValidationResult("Other Property Replacement Error", null)
            };

            VerifyPropertyReplacementEvents(member, replacementResults, replacementNames);
        }

        [TestMethod]
        [TestDescription("When we replace errors for Member with new errors for Member plus errors for the entity (null member), we should get events for the member and the entity")]
        public void ReplacePropertyResultsWithPropertyAndEntityResultsWithNullMember()
        {
            string member = "Member";
            string[] memberNames = new string[] { member };
            string[] replacementNames = new string[] { member, null };

            IEnumerable<ValidationResult> replacementResults = new ValidationResult[]
            {
                new ValidationResult("Replacement Error", new string[] { member }),
                new ValidationResult("Other Property Replacement Error", new string[] { null })
            };

            VerifyPropertyReplacementEvents(member, replacementResults, replacementNames);
        }

        [TestMethod]
        [TestDescription("When we replace errors for Member with new errors for Member plus errors for the entity (empty string member), we should get events for the member and the entity")]
        public void ReplacePropertyResultsWithPropertyAndEntityResultsWithEmptyMember()
        {
            string member = "Member";
            string[] memberNames = new string[] { member };
            string[] replacementNames = new string[] { member, string.Empty };

            IEnumerable<ValidationResult> replacementResults = new ValidationResult[]
            {
                new ValidationResult("Replacement Error", new string[] { member }),
                new ValidationResult("Other Property Replacement Error", new string[] { string.Empty })
            };

            VerifyPropertyReplacementEvents(member, replacementResults, replacementNames);
        }

        [TestMethod]
        [TestDescription("When we replace errors for Member with new errors for the entity (no members), we should get events for the member and the entity")]
        public void ReplacePropertyResultsWithEntityResultsWithNoMembers()
        {
            string member = "Member";
            string[] memberNames = new string[] { member };
            string[] replacementNames = new string[] { member, null };

            IEnumerable<ValidationResult> replacementResults = new ValidationResult[]
            {
                new ValidationResult("Other Property Replacement Error", null)
            };

            VerifyPropertyReplacementEvents(member, replacementResults, replacementNames);
        }

        [TestMethod]
        [TestDescription("When we replace errors for Member with new errors for the entity (null member), we should get events for the member and the entity")]
        public void ReplacePropertyResultsWithEntityResultsWithNullMember()
        {
            string member = "Member";
            string[] memberNames = new string[] { member };
            string[] replacementNames = new string[] { member, null };

            IEnumerable<ValidationResult> replacementResults = new ValidationResult[]
            {
                new ValidationResult("Other Property Replacement Error", new string[] { null })
            };

            VerifyPropertyReplacementEvents(member, replacementResults, replacementNames);
        }

        [TestMethod]
        [TestDescription("When we replace errors for Member with new errors for the entity (empty string member), we should get events for the member and the entity")]
        public void ReplacePropertyResultsWithEntityResultsWithEmptyMember()
        {
            string member = "Member";
            string[] memberNames = new string[] { member };
            string[] replacementNames = new string[] { member, string.Empty };

            IEnumerable<ValidationResult> replacementResults = new ValidationResult[]
            {
                new ValidationResult("Other Property Replacement Error", new string[] { string.Empty })
            };

            VerifyPropertyReplacementEvents(member, replacementResults, replacementNames);
        }

        public void ReplaceAllResultsWithNothing()
        {
            // This test differs from the other replacement tests enough that it doesn't use the same helper method

            string member = "Member";
            string[] memberNames = new string[] { member };
            string[] replacementNames = new string[] { member };
            IEnumerable<ValidationResult> initialResults = new ValidationResult[] { new ValidationResult("Error", memberNames) };
            IEnumerable<ValidationResult> replacementResults = Enumerable.Empty<ValidationResult>();

            int collectionChangedCount = 0;
            int hasErrorsChangedCount = 0;
            List<string> propertyErrorsChangedList = new List<string>();

            Action collectionChanged = () => ++collectionChangedCount;
            Action hasErrorsChanged = () => ++hasErrorsChangedCount;
            Action<string> propertyErrorsChanged = propertyName => propertyErrorsChangedList.Add(propertyName);

            ValidationResultCollection collection = new TestValidationResultCollection(collectionChanged, hasErrorsChanged, propertyErrorsChanged);
            collection.ReplaceErrors(initialResults);

            Assert.AreEqual<int>(1, collectionChangedCount, "CollectionChanged count for first replacement");
            Assert.AreEqual<int>(1, hasErrorsChangedCount, "HasErrorsChanged count for first replacement");
            Assert.IsTrue(propertyErrorsChangedList.OrderBy(s => s).SequenceEqual(replacementNames.OrderBy(s => s)), "propertyErrorsChangedList for first replacement");

            collection.Add(new ValidationResult("Error", new string[] { member }));
            collectionChangedCount = 0;
            hasErrorsChangedCount = 0;
            propertyErrorsChangedList.Clear();

            collection.ReplaceErrors(replacementResults);

            // Notice that HasErrors changes again because we no longer have any errors
            Assert.AreEqual<int>(1, collectionChangedCount, "CollectionChanged count for second replacement");
            Assert.AreEqual<int>(1, hasErrorsChangedCount, "HasErrorsChanged count for second replacement");
            Assert.IsTrue(propertyErrorsChangedList.OrderBy(s => s).SequenceEqual(replacementNames.OrderBy(s => s)), "propertyErrorsChangedList for second replacement");
        }

        [TestMethod]
        [TestDescription("When we replace all errors with new errors for that member, we should get events for that member")]
        public void ReplaceAllResultsWithPropertyResults()
        {
            string member = "Member";
            string[] memberNames = new string[] { member };
            string[] replacementNames = new string[] { member };
            IEnumerable<ValidationResult> replacementResults = new ValidationResult[] { new ValidationResult("Replacement Error", memberNames) };

            VerifyReplacementEvents(member, replacementResults, replacementNames, replacementNames);
        }

        [TestMethod]
        [TestDescription("When we replace all errors with new errors for Member plus errors for another member, we should get events for both members")]
        public void ReplaceAllResultsWithMultiPropertyResults()
        {
            string member = "Member";
            string otherMember = "OtherMember";
            string[] memberNames = new string[] { member };
            string[] replacementNames = new string[] { member, otherMember };

            IEnumerable<ValidationResult> replacementResults = new ValidationResult[]
            {
                new ValidationResult("Replacement Error", new string[] { member }),
                new ValidationResult("Other Property Replacement Error", new string[] { otherMember })
            };

            VerifyReplacementEvents(member, replacementResults, replacementNames, replacementNames);
        }

        [TestMethod]
        [TestDescription("When we replace all errors with new errors for Member plus errors for the entity (no members), we should get events for the member and the entity")]
        public void ReplaceAllResultsWithPropertyAndEntityResultsWithNoMembers()
        {
            string member = "Member";
            string[] memberNames = new string[] { member };
            string[] replacementNames = new string[] { member, null };

            IEnumerable<ValidationResult> replacementResults = new ValidationResult[]
            {
                new ValidationResult("Replacement Error", new string[] { member }),
                new ValidationResult("Other Property Replacement Error", null)
            };

            VerifyReplacementEvents(member, replacementResults, replacementNames, replacementNames);
        }

        [TestMethod]
        [TestDescription("When we replace all errors with new errors for Member plus errors for the entity (null member), we should get events for the member and the entity")]
        public void ReplaceAllResultsWithPropertyAndEntityResultsWithNullMember()
        {
            string member = "Member";
            string[] memberNames = new string[] { member };
            string[] replacementNames = new string[] { member, null };

            IEnumerable<ValidationResult> replacementResults = new ValidationResult[]
            {
                new ValidationResult("Replacement Error", new string[] { member }),
                new ValidationResult("Other Property Replacement Error", new string[] { null })
            };

            VerifyReplacementEvents(member, replacementResults, replacementNames, replacementNames);
        }

        [TestMethod]
        [TestDescription("When we replace all errors with new errors for Member plus errors for the entity (empty string member), we should get events for the member and the entity")]
        public void ReplaceAllResultsWithPropertyAndEntityResultsWithEmptyMember()
        {
            string member = "Member";
            string[] memberNames = new string[] { member };
            string[] replacementNames = new string[] { member, string.Empty };

            IEnumerable<ValidationResult> replacementResults = new ValidationResult[]
            {
                new ValidationResult("Replacement Error", new string[] { member }),
                new ValidationResult("Other Property Replacement Error", new string[] { string.Empty })
            };

            VerifyReplacementEvents(member, replacementResults, replacementNames, replacementNames);
        }

        [TestMethod]
        [TestDescription("When we replace all errors with new errors for the entity (no members), we should get events for the member and the entity")]
        public void ReplaceAllResultsWithEntityResultsWithNoMembers()
        {
            string member = "Member";
            string[] memberNames = new string[] { member };
            string[] firstReplacementNames = new string[] { null };
            string[] secondReplacementNames = new string[] { member, null };

            IEnumerable<ValidationResult> replacementResults = new ValidationResult[]
            {
                new ValidationResult("Other Property Replacement Error", null)
            };

            VerifyReplacementEvents(member, replacementResults, firstReplacementNames, secondReplacementNames);
        }

        [TestMethod]
        [TestDescription("When we replace all errors with new errors for the entity (null member), we should get events for the member and the entity")]
        public void ReplaceAllResultsWithEntityResultsWithNullMember()
        {
            string member = "Member";
            string[] memberNames = new string[] { member };
            string[] firstReplacementNames = new string[] { null };
            string[] secondReplacementNames = new string[] { member, null };

            IEnumerable<ValidationResult> replacementResults = new ValidationResult[]
            {
                new ValidationResult("Other Property Replacement Error", new string[] { null })
            };

            VerifyReplacementEvents(member, replacementResults, firstReplacementNames, secondReplacementNames);
        }

        [TestMethod]
        [TestDescription("When we replace all errors with new errors for the entity (empty string member), we should get events for the member and the entity")]
        public void ReplaceAllResultsWithEntityResultsWithEmptyMember()
        {
            string member = "Member";
            string[] memberNames = new string[] { member };
            string[] firstReplacementNames = new string[] { string.Empty };
            string[] secondReplacementNames = new string[] { member, string.Empty };

            IEnumerable<ValidationResult> replacementResults = new ValidationResult[]
            {
                new ValidationResult("Other Property Replacement Error", new string[] { string.Empty })
            };

            VerifyReplacementEvents(member, replacementResults, firstReplacementNames, secondReplacementNames);
        }

        [TestMethod]
        [Description("Clearing the validation errors will properly raise events")]
        public void ClearRaisesEvents()
        {
            int collectionChangedCount = 0;
            int hasErrorsChangedCount = 0;
            List<string> propertyErrorsChangedList = new List<string>();

            Action collectionChanged = () => ++collectionChangedCount;
            Action hasErrorsChanged = () => ++hasErrorsChangedCount;
            Action<string> propertyErrorsChanged = propertyName => propertyErrorsChangedList.Add(propertyName);

            ValidationResultCollection collection = new TestValidationResultCollection(collectionChanged, hasErrorsChanged, propertyErrorsChanged);
            collection.Add(new ValidationResult("Property Error", new string[] { "Member" }));
            collection.Add(new ValidationResult("Entity Error", null));

            string[] errorsChangedMemberNames = new string[] { "Member", null };

            Assert.AreEqual<int>(2, collectionChangedCount, "collectionChangedCount after adding");
            Assert.AreEqual<int>(1, hasErrorsChangedCount, "hasErrorsChangedCount after adding");
            Assert.IsTrue(propertyErrorsChangedList.OrderBy(s => s).SequenceEqual(errorsChangedMemberNames.OrderBy(s => s)), "propertyErrorsChangedList after adding");

            collectionChangedCount = 0;
            hasErrorsChangedCount = 0;
            propertyErrorsChangedList.Clear();

            collection.Clear();

            // We only get one collection changed event because it's an atomic action
            Assert.AreEqual<int>(1, collectionChangedCount, "collectionChangedCount after clearing");
            Assert.AreEqual<int>(1, hasErrorsChangedCount, "hasErrorsChangedCount after clearing");
            Assert.IsTrue(propertyErrorsChangedList.OrderBy(s => s).SequenceEqual(errorsChangedMemberNames.OrderBy(s => s)), "propertyErrorsChangedList after clearing");
        }
    }

    internal class TestValidationResultCollection : ValidationResultCollection
    {
        private Action _collectionChanged;
        private Action _hasErrorsChanged;
        private Action<string> _propertyErrorsChanged;

        public TestValidationResultCollection(Action collectionChangedCallback, Action hasErrorsChangedCallback, Action<string> propertyErrorsChangedCallback)
            : base(null)
        {
            this._collectionChanged = collectionChangedCallback;
            this._hasErrorsChanged = hasErrorsChangedCallback;
            this._propertyErrorsChanged = propertyErrorsChangedCallback;
        }

        protected override void OnCollectionChanged()
        {
            if (this._collectionChanged != null)
            {
                this._collectionChanged();
            }
        }

        protected override void OnHasErrorsChanged()
        {
            if (this._hasErrorsChanged != null)
            {
                this._hasErrorsChanged();
            }
        }

        protected override void OnPropertyErrorsChanged(string propertyName)
        {
            if (this._propertyErrorsChanged != null)
            {
                this._propertyErrorsChanged(propertyName);
            }
        }
    }
}
