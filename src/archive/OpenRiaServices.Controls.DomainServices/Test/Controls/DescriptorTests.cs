using System;
using System.ComponentModel;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.Silverlight.Testing;
using DescriptionAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;
using System.Collections.ObjectModel;

namespace OpenRiaServices.Controls.DomainServices.Test
{
    /// <summary>
    /// Tests the <see cref="FilterDescriptor"/>, <see cref="GroupDescriptor"/>, <see cref="Parameter"/>,
    /// and <see cref="SortDescriptor"/> classes.
    /// </summary>
    [TestClass]
    public class DescriptorTests : UnitTestBase
    {
        [TestMethod]
        [Description("Tests the default values, PropertyChanged notification, and IRestorable implementation of FilterDescriptor.")]
        public void FilterDescriptorDefaultsEventsAndRestoration()
        {
            FilterDescriptor descriptor = new FilterDescriptor();

            // Check defaults
            Assert.AreEqual(FilterDescriptor.DefaultIgnoredValue, descriptor.IgnoredValue,
                "Default IgnoredValue should equal DefaultIgnoredValue.");
            Assert.IsFalse(descriptor.IsCaseSensitive,
                "Default IsCaseSensitive should be false.");
            Assert.AreEqual(FilterOperator.IsEqualTo, descriptor.Operator,
                "Default Operator should be IsEqualTo.");
            Assert.AreEqual(string.Empty, descriptor.PropertyPath,
                "Default PropertyPath should be empty.");
            Assert.IsNull(descriptor.Value,
                "Default Value should be null.");

            // Check change notification
            object ignoredValue = "ignoredValue";
            bool isCaseSensitive = true;
            FilterOperator fOperator = FilterOperator.IsGreaterThanOrEqualTo;
            string propertyPath = "propertyPath";
            object value = "value";

            this.CheckPropertyChange(() => descriptor.IgnoredValue = ignoredValue, descriptor.Notifier, "IgnoredValue");
            this.CheckPropertyChange(() => descriptor.IsCaseSensitive = isCaseSensitive, descriptor.Notifier, "IsCaseSensitive");
            this.CheckPropertyChange(() => descriptor.Operator = fOperator, descriptor.Notifier, "Operator");
            this.CheckPropertyChange(() => descriptor.PropertyPath = propertyPath, descriptor.Notifier, "PropertyPath");
            this.CheckPropertyChange(() => descriptor.Value = value, descriptor.Notifier, "Value");

            Assert.AreEqual(ignoredValue, descriptor.IgnoredValue,
                "IgnoredValues should be equal.");
            Assert.AreEqual(isCaseSensitive, descriptor.IsCaseSensitive,
                "IsCaseSensitive flags should be equal.");
            Assert.AreEqual(fOperator, descriptor.Operator,
                "Operators should be equal.");
            Assert.AreEqual(propertyPath, descriptor.PropertyPath,
                "PropertyPaths should be equal.");
            Assert.AreEqual(value, descriptor.Value,
                "Values should be equal.");

            // Check restoration
            int count = 0;
            descriptor.Notifier.PropertyChanged += (sender, e) => count++;

            ((IRestorable)descriptor).StoreOriginalValue();

            descriptor.IgnoredValue = null;
            descriptor.IsCaseSensitive = false;
            descriptor.Operator = FilterOperator.Contains;
            descriptor.PropertyPath = null;
            descriptor.Value = null;

            ((IRestorable)descriptor).RestoreOriginalValue();

            Assert.AreEqual(10, count,
                "10 PropertyChanged events should have occurred.");
            Assert.AreEqual(ignoredValue, descriptor.IgnoredValue,
                "IgnoredValues should be equal.");
            Assert.AreEqual(isCaseSensitive, descriptor.IsCaseSensitive,
                "IsCaseSensitive flags should be equal.");
            Assert.AreEqual(fOperator, descriptor.Operator,
                "Operators should be equal.");
            Assert.AreEqual(propertyPath, descriptor.PropertyPath,
                "PropertyPaths should be equal.");
            Assert.AreEqual(value, descriptor.Value,
                "Values should be equal.");
        }

        [TestMethod]
        [Description("Tests the default values, PropertyChanged notification, and IRestorable implementation of GroupDescriptor.")]
        public void GroupDescriptorDefaultsEventsAndRestoration()
        {
            GroupDescriptor descriptor = new GroupDescriptor();

            // Check defaults
            Assert.AreEqual(string.Empty, descriptor.PropertyPath,
                "Default PropertyPath should be empty.");

            // Check change notification
            string propertyPath = "propertyPath";

            this.CheckPropertyChange(() => descriptor.PropertyPath = propertyPath, descriptor.Notifier, "PropertyPath");
            Assert.AreEqual(propertyPath, descriptor.PropertyPath,
                "PropertyPaths should be equal.");

            // Check restoration
            int count = 0;
            descriptor.Notifier.PropertyChanged += (sender, e) => count++;

            ((IRestorable)descriptor).StoreOriginalValue();

            descriptor.PropertyPath = null;

            ((IRestorable)descriptor).RestoreOriginalValue();

            Assert.AreEqual(2, count,
                "2 PropertyChanged events should have occurred.");
            Assert.AreEqual(propertyPath, descriptor.PropertyPath,
                "PropertyPaths should be equal.");
        }

        [TestMethod]
        [Description("Tests the default values, PropertyChanged notification, and IRestorable implementation of Parameter.")]
        public void ParameterDefaultsEventsAndRestoration()
        {
            Parameter parameter = new Parameter();

            // Check defaults
            Assert.AreEqual(string.Empty, parameter.ParameterName,
                "Default ParameterName should be empty.");
            Assert.IsNull(parameter.Value,
                "Default Value should be null.");

            // Check change notification
            string parameterName = "parameterName";
            object value = "value";

            this.CheckPropertyChange(() => parameter.ParameterName = parameterName, parameter.Notifier, "ParameterName");
            this.CheckPropertyChange(() => parameter.Value = value, parameter.Notifier, "Value");

            Assert.AreEqual(parameterName, parameter.ParameterName,
                "ParameterNames should be equal.");
            Assert.AreEqual(value, parameter.Value,
                "Values should be equal.");

            // Check restoration
            int count = 0;
            parameter.Notifier.PropertyChanged += (sender, e) => count++;

            ((IRestorable)parameter).StoreOriginalValue();

            parameter.ParameterName = null;
            parameter.Value = null;

            ((IRestorable)parameter).RestoreOriginalValue();

            Assert.AreEqual(4, count,
                "4 PropertyChanged events should have occurred.");
            Assert.AreEqual(parameterName, parameter.ParameterName,
                "ParameterNames should be equal.");
            Assert.AreEqual(value, parameter.Value,
                "Values should be equal.");
        }

        [TestMethod]
        [Description("Tests the default values, PropertyChanged notification, and IRestorable implementation of SortDescriptor.")]
        public void SortDescriptorDefaultsEventsAndRestoration()
        {
            SortDescriptor descriptor = new SortDescriptor();

            // Check defaults
            Assert.AreEqual(ListSortDirection.Ascending, descriptor.Direction,
                "Default Operator should be Ascending.");
            Assert.AreEqual(string.Empty, descriptor.PropertyPath,
                "Default PropertyPath should be empty.");

            // Check change notification
            ListSortDirection direction = ListSortDirection.Descending;
            string propertyPath = "propertyPath";

            this.CheckPropertyChange(() => descriptor.Direction = direction, descriptor.Notifier, "Direction");
            this.CheckPropertyChange(() => descriptor.PropertyPath = propertyPath, descriptor.Notifier, "PropertyPath");

            Assert.AreEqual(direction, descriptor.Direction,
                "Directions should be equal.");
            Assert.AreEqual(propertyPath, descriptor.PropertyPath,
                "PropertyPaths should be equal.");

            // Check restoration
            int count = 0;
            descriptor.Notifier.PropertyChanged += (sender, e) => count++;

            ((IRestorable)descriptor).StoreOriginalValue();

            descriptor.Direction = ListSortDirection.Ascending;
            descriptor.PropertyPath = null;

            ((IRestorable)descriptor).RestoreOriginalValue();

            Assert.AreEqual(4, count,
                "4 PropertyChanged events should have occurred.");
            Assert.AreEqual(direction, descriptor.Direction,
                "Directions should be equal.");
            Assert.AreEqual(propertyPath, descriptor.PropertyPath,
                "PropertyPaths should be equal.");
        }

        [TestMethod]
        [Description("Tests the event collation in the FilterCollectionManager.")]
        public void CollectionManagerCollatesFilterDescriptorEvents()
        {
            FilterDescriptorCollection collection = new FilterDescriptorCollection();
            ExpressionCache cache = new ExpressionCache();
            FilterDescriptor descriptor = null;

            this.CollectionManagerCollatesTemplate(
                (validationAction) => 
                {
                    return new FilterCollectionManager(collection, cache, fd => validationAction());
                },
                () =>
                {
                    collection.Add(new FilterDescriptor());
                },
                () =>
                {
                    collection[0].PropertyPath = "First";
                },
                () =>
                {
                    collection.Add(new FilterDescriptor());
                },
                () =>
                {
                    collection[1].PropertyPath = "Second";
                },
                () =>
                {
                    collection[1] = new FilterDescriptor();
                },
                () =>
                {
                    descriptor = collection[0];
                    collection.Remove(descriptor);
                },
                () =>
                {
                    descriptor.PropertyPath = "Removed";
                });
        }

        [TestMethod]
        [Description("Tests the event collation in the GroupCollectionManager.")]
        public void CollectionManagerCollatesGroupDescriptorEvents()
        {
            GroupDescriptorCollection collection = new GroupDescriptorCollection();
            ObservableCollection<GroupDescription> descriptionCollection = new ObservableCollection<GroupDescription>();
            ExpressionCache cache = new ExpressionCache();
            GroupDescriptor descriptor = null;

            this.CollectionManagerCollatesTemplate(
                (validationAction) =>
                {
                    return new GroupCollectionManager(collection, descriptionCollection, cache, gd => validationAction());
                },
                () =>
                {
                    collection.Add(new GroupDescriptor());
                },
                () =>
                {
                    collection[0].PropertyPath = "First";
                },
                () =>
                {
                    collection.Add(new GroupDescriptor());
                },
                () =>
                {
                    collection[1].PropertyPath = "Second";
                },
                () =>
                {
                    collection[1] = new GroupDescriptor();
                },
                () =>
                {
                    descriptor = collection[0];
                    collection.Remove(descriptor);
                },
                () =>
                {
                    descriptor.PropertyPath = "Removed";
                });
        }

        [TestMethod]
        [Description("Tests the event collation in the ParameterCollectionManager.")]
        public void CollectionManagerCollatesParameterEvents()
        {
            ParameterCollection collection = new ParameterCollection();
            Parameter descriptor = null;

            int expectedcollectionValidationCount = 6;
            Action collectionValidationAction = () =>
            {
                expectedcollectionValidationCount--;
                if (expectedcollectionValidationCount < 0)
                {
                    Assert.Fail("Too many collection validation action invocations.");
                }
            };

            this.CollectionManagerCollatesTemplate(
                (validationAction) =>
                {
                    return new ParameterCollectionManager(collection, p => validationAction(), pc => collectionValidationAction());
                },
                () =>
                {
                    collection.Add(new Parameter());
                },
                () =>
                {
                    collection[0].ParameterName = "First";
                },
                () =>
                {
                    collection.Add(new Parameter());
                },
                () =>
                {
                    collection[1].ParameterName = "Second";
                },
                () =>
                {
                    collection[1] = new Parameter();
                },
                () =>
                {
                    descriptor = collection[0];
                    collection.Remove(descriptor);
                },
                () =>
                {
                    descriptor.ParameterName = "Removed";
                });

            Assert.AreEqual(0, expectedcollectionValidationCount,
                "Collection validation count should be 0.");
        }

        [TestMethod]
        [Description("Tests the event collation in the SortCollectionManager.")]
        public void CollectionManagerCollatesSortDescriptorEvents()
        {
            SortDescriptorCollection collection = new SortDescriptorCollection();
            SortDescriptionCollection descriptionCollection = new SortDescriptionCollection();
            ExpressionCache cache = new ExpressionCache();
            SortDescriptor descriptor = null;

            this.CollectionManagerCollatesTemplate(
                (validationAction) =>
                {
                    return new SortCollectionManager(collection, descriptionCollection, cache, sd => validationAction());
                },
                () =>
                {
                    collection.Add(new SortDescriptor());
                },
                () =>
                {
                    collection[0].PropertyPath = "First";
                },
                () =>
                {
                    collection.Add(new SortDescriptor());
                },
                () =>
                {
                    collection[1].PropertyPath = "Second";
                },
                () =>
                {
                    collection[1] = new SortDescriptor();
                },
                () =>
                {
                    descriptor = collection[0];
                    collection.Remove(descriptor);
                },
                () =>
                {
                    descriptor.PropertyPath = "Removed";
                });
        }

        private void CollectionManagerCollatesTemplate(
            Func<Action, CollectionManager> create,
            Action add1,
            Action modify1,
            Action add2,
            Action modify2,
            Action replace,
            Action remove,
            Action modifyRemoved)
        {
            int expectedValidationCount = 0;
            Action validationAction = () =>
            {
                expectedValidationCount--;
                if (expectedValidationCount < 0)
                {
                    Assert.Fail("Too many validation action invocations.");
                }
            };

            int expectedCollectionChangedCount = 0;
            EventHandler collectionChangedHandler = (sender, e) =>
            {
                expectedCollectionChangedCount--;
                if (expectedCollectionChangedCount < 0)
                {
                    Assert.Fail("Too many collection changed events.");
                }
            };

            int expectedPropertyChangedCount = 0;
            EventHandler propertyChangedHandler = (sender, e) =>
            {
                expectedPropertyChangedCount--;
                if (expectedPropertyChangedCount < 0)
                {
                    Assert.Fail("Too many property changed events.");
                }
            };

            Action<string> assertEventsOccurred = (message) =>
            {
                Assert.AreEqual(0, expectedValidationCount,
                    "Validation count should be 0 when " + message);
                Assert.AreEqual(0, expectedCollectionChangedCount,
                    "CollectionChanged count should be 0 when " + message);
                Assert.AreEqual(0, expectedPropertyChangedCount,
                    "PropertyChanged count should be 0 when " + message);
            };

            CollectionManager manager = create(validationAction);
            manager.CollectionChanged += collectionChangedHandler;
            manager.PropertyChanged += propertyChangedHandler;

            // Add
            expectedValidationCount = 1;
            expectedCollectionChangedCount = 1;

            add1();

            assertEventsOccurred("adding a descriptor.");

            // Modify
            expectedValidationCount = 1;
            expectedPropertyChangedCount = 1;

            modify1();

            assertEventsOccurred("modifying a descriptor.");

            // Add another
            expectedValidationCount = 1;
            expectedCollectionChangedCount = 1;

            add2();

            assertEventsOccurred("adding another descriptor.");

            // Modify another
            expectedValidationCount = 1;
            expectedPropertyChangedCount = 1;

            modify2();

            assertEventsOccurred("modifying another descriptor.");

            // Replace
            expectedValidationCount = 1;
            expectedCollectionChangedCount = 1;

            replace();

            assertEventsOccurred("replacing a descriptor.");

            // Remove
            expectedCollectionChangedCount = 1;

            remove();

            assertEventsOccurred("removing a descriptor.");

            // Modify removed
            modifyRemoved();

            assertEventsOccurred("modifying a removed descriptor.");
        }

        /// <summary>
        /// Ensures a single property change occurs during the specified <paramref name="action"/>.
        /// </summary>
        /// <param name="action">The <see cref="Action"/> to invoke</param>
        /// <param name="notifier">The notifier raising the <see cref="INotifyPropertyChanged.PropertyChanged"/> event</param>
        /// <param name="propertyName">The name of the property for the event</param>
        private void CheckPropertyChange(Action action, INotifyPropertyChanged notifier, string propertyName)
        {
            int events = 0;
            PropertyChangedEventHandler handler = (sender, e) =>
            {
                Assert.AreEqual(propertyName, e.PropertyName,
                    "PropertyNames should be equal.");
                Assert.AreEqual(1, ++events,
                    "There should only be 1 event.");
            };

            notifier.PropertyChanged += handler;

            action();

            notifier.PropertyChanged -= handler;
        }
    }
}
