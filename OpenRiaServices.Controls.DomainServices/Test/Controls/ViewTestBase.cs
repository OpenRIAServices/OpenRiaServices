using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using OpenRiaServices.DomainServices.Client;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace System.Windows.Controls.DomainServices.Test
{
    /// <summary>
    /// Base class for testing views used by the <see cref="DomainDataSource"/> control.
    /// </summary>
    public class ViewTestBase : UnitTestBase
    {
        /// <summary>
        /// Asserts that the specified collection changed event occurred or that no event occured if
        /// <paramref name="expected"/> is <c>null</c>.
        /// </summary>
        /// <param name="action">The action that will trigger the event</param>
        /// <param name="collection">The collection to observe</param>
        /// <param name="expected">The expected event args or <c>null</c> if not event is expected</param>
        /// <param name="message">The message to append to the errors</param>
        protected void AssertCollectionChanged(Action action, INotifyCollectionChanged collection, NotifyCollectionChangedEventArgs expected, string message)
        {
            this.AssertCollectionChanged(action, collection, (expected == null) ? null : new NotifyCollectionChangedEventArgs[] { expected }, message);
        }

        /// <summary>
        /// Asserts that the specified collection changed event occurred or that no event occured if
        /// <paramref name="expected"/> is <c>null</c>.
        /// </summary>
        /// <param name="action">The action that will trigger the event</param>
        /// <param name="collection">The collection to observe</param>
        /// <param name="expectedEvents">The expected event args or <c>null</c> if no event is expected</param>
        /// <param name="message">The message to append to the errors</param>
        protected void AssertCollectionChanged(Action action, INotifyCollectionChanged collection, NotifyCollectionChangedEventArgs[] expectedEvents, string message)
        {
            List<NotifyCollectionChangedEventArgs> events = new List<NotifyCollectionChangedEventArgs>();
            NotifyCollectionChangedEventHandler handler = (s, e) => events.Add(e);
            collection.CollectionChanged += handler;

            action();

            if (expectedEvents == null)
            {
                Assert.AreEqual(0, events.Count,
                    "There should not be any events raised " + message +
                    " Events handled: " + string.Join(", ", events.Select(e => e.Action.ToString()).ToArray()));
            }
            else
            {
                Assert.AreEqual(expectedEvents.Length, events.Count,
                    "The actual event count should equal " + expectedEvents.Length + " " + message +
                    " Events handled: " + string.Join(", ", events.Select(e => e.Action.ToString()).ToArray()));
                for (int i = 0; i < expectedEvents.Length; i++)
                {
                    NotifyCollectionChangedEventArgs expected = expectedEvents[i];
                    NotifyCollectionChangedEventArgs actual = events[i];

                    Assert.AreEqual(expected.Action, actual.Action,
                        "The actual Action should equal the expected Action " + message);
                    if (expected.NewItems != null)
                    {
                        Assert.IsNotNull(actual.NewItems,
                            "The actual NewItems should not be null " + message);
                        Assert.AreEqual(expected.NewItems.Count, actual.NewItems.Count,
                            "The actual NewItems count should equal the expected NewItems count " + message);
                        Assert.IsTrue(expected.NewItems.OfType<object>().SequenceEqual(actual.NewItems.OfType<object>()),
                            "The actual NewItems items should equal the expected NewItems items " + message +
                            " Expected=" + string.Join(",", expected.NewItems.OfType<object>().ToArray()) +
                            " Actual=" + string.Join(",", actual.NewItems.OfType<object>().ToArray()));
                    }
                    if (expected.OldItems != null)
                    {
                        Assert.IsNotNull(actual.OldItems,
                            "The actual OldItems should not be null " + message);
                        Assert.AreEqual(expected.OldItems.Count, actual.OldItems.Count,
                            "The actual OldItems count should equal the expected OldItems count " + message);
                        Assert.IsTrue(expected.OldItems.OfType<object>().SequenceEqual(actual.OldItems.OfType<object>()),
                            "The actual OldItems items should equal the expected OldItems items " + message +
                            " Expected=" + string.Join(",", expected.OldItems.OfType<object>().ToArray()) +
                            " Actual=" + string.Join(",", actual.OldItems.OfType<object>().ToArray()));
                    }
                }
            }

            collection.CollectionChanged -= handler;
        }

        public class NotifyingCollection<T> : Collection<T>, INotifyCollectionChanged where T : Entity
        {
            public bool NotifyOnCollectionChanged { get; set; }

            public NotifyingCollection()
            {
                this.NotifyOnCollectionChanged = true;
            }

            protected override void InsertItem(int index, T item)
            {
                base.InsertItem(index, item);

                if (this.NotifyOnCollectionChanged)
                {
                    this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
                }
            }

            protected override void RemoveItem(int index)
            {
                object item = this[index];
                base.RemoveItem(index);

                if (this.NotifyOnCollectionChanged)
                {
                    this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
                }
            }

            protected override void SetItem(int index, T item)
            {
                object oldItem = this[index];
                base.SetItem(index, item);

                if (this.NotifyOnCollectionChanged)
                {
                    this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, item, oldItem, index));
                }
            }

            public void Reset()
            {
                this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }

            private void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
            {
                NotifyCollectionChangedEventHandler handler = this.CollectionChanged;
                if (handler != null)
                {
                    handler(this, e);
                }
            }

            public event NotifyCollectionChangedEventHandler CollectionChanged;
        }
    }
}
