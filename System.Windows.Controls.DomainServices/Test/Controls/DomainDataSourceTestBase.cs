using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using Cities;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows.Data;

namespace System.Windows.Controls.DomainServices.Test
{
    /// <summary>
    /// Base class for testing the <see cref="DomainDataSource"/> control.
    /// </summary>
    public abstract class DomainDataSourceTestBase : UnitTestBase
    {
        #region Protected Constants

        /// <summary>
        /// The timeout used when waiting to ensure no events (such as a load) occur.
        /// </summary>
        /// <remarks>
        /// Used for negative tests, so this timeout will be reached every time it's
        /// referenced.
        /// </remarks>
        protected const int EnsureNoEventsTimeout = 2500;

        #endregion Protected Constants

        #region Protected Statics

        /// <summary>
        /// The load interval used for progressive loading tests.
        /// </summary>
        /// <remarks>
        /// The previous value of 100 caused some tests to fail due to timing issues.
        /// </remarks>
        protected static TimeSpan defaultLoadInterval = TimeSpan.FromMilliseconds(500);

        #endregion Protected Statics

        #region Protected Fields

        protected DomainDataSource _dds;
        protected DomainDataSourceView _view;
        protected ICollectionView _collectionView;
        protected IEditableCollectionView _editableCollectionView;
        protected IPagedCollectionView _pagedCollectionView;
        protected TextBox _textBox;
        protected ComboBox _comboBox;
        protected string _asyncEventFailureMessage;
        protected bool _ddsLoadErrorExpected;
        protected int _ddsLoadingData;
        protected int _ddsLoadingDataExpected;
        protected int _ddsLoadedData;
        protected int _ddsLoadedDataExpected;
        protected LoadedDataEventArgs _ddsLoadedDataEventArgs;
        protected int _ddsSubmittingChangesExpected;
        protected int _viewPageChangedExpected;
        protected bool _reload;

        #endregion Protected Fields

        #region Protected Properties

        protected IValueProvider TextBoxValueProvider
        {
            get
            {
                return this._textBoxAutomationPeer.GetPattern(PatternInterface.Value) as IValueProvider;
            }
        }

        #endregion Protected Properties

        #region Private Fields

        private TextBoxAutomationPeer _textBoxAutomationPeer;
        private bool _ddsLoaded;
        private bool _ddsLoadErrorHandled;
        private int _ddsSubmittingChanges;
        private int _ddsSubmittedChanges;
        private int _ddsSubmittedChangesExpected;
        private int _viewPageChanged;
        private Dictionary<NotifyCollectionChangedAction, int> _viewCollectionChangedEvents;
        private bool _textBoxLoaded;
        private bool _comboBoxLoaded;
        private LoadTimer _loadTimer;
        private ProgressiveLoadTimer _progressiveLoadTimer;
        private RefreshLoadTimer _refreshLoadTimer;

        #endregion Private Fields

        #region Initialization Methods

        /// <summary>
        /// Initializes the DomainDataSource to be used in testing.
        /// </summary>
        [TestInitialize]
        public void Initialize()
        {
            this._dds = new DomainDataSource();
            this._dds.Loaded += new RoutedEventHandler(this.DomainDataSourceLoaded);
            this._dds.LoadingData += new EventHandler<System.Windows.Controls.LoadingDataEventArgs>(this.DomainDataSourceLoadingData);
            this._dds.LoadedData += new EventHandler<LoadedDataEventArgs>(this.DomainDataSourceLoadedData);
            this._dds.SubmittingChanges += new EventHandler<SubmittingChangesEventArgs>(this.DomainDataSourceSubmittingChanges);
            this._dds.SubmittedChanges += new EventHandler<SubmittedChangesEventArgs>(this.DomainDataSourceSubmittedChanges);

            this._view = this._dds.DataView;
            this._view.PageChanged += this.ViewPageChanged;
            this._collectionView = this._view;
            this._collectionView.CollectionChanged += this.ViewCollectionChanged;
            this._editableCollectionView = this._view;
            this._pagedCollectionView = this._view;

            this._comboBox = new ComboBox { Name = "_comboBox" };
            this._comboBox.Loaded += new RoutedEventHandler(this.ComboBoxLoaded);

            this._textBox = new TextBox { Name = "_textBox" };
            this._textBox.Loaded += new RoutedEventHandler(this.TextBoxLoaded);

            this.ResetLoadState();
            this.ResetPageChanged();
            this.ResetSubmitState();

            this._asyncEventFailureMessage = null;
        }

        #endregion Initialization Methods

        #region Cleanup Methods

        /// <summary>
        /// Initializes the DomainDataSource to be used in testing.
        /// </summary>
        [TestCleanup]
        public void Cleanup()
        {
            this._reload = false;

            // Clear out pending operations and state to prevent any unintended loads after the completion of a test
            this._dds.AutoLoad = false;
            this._dds.QueryName = null; // prevents any straggler load from being invoked
            this._dds.CancelLoad();
            this._dds.CancelSubmit();

            this._dds.RefreshInterval = TimeSpan.Zero; // Blocks the refresh interval
            this._dds.LoadSize = 0; // Blocks progressive loading

            this._loadTimer = null;
            this._progressiveLoadTimer = null;
            this._refreshLoadTimer = null;

            // Unsubscribe from all events
            this._dds.Loaded -= new RoutedEventHandler(this.DomainDataSourceLoaded);
            this._dds.LoadingData -= new EventHandler<System.Windows.Controls.LoadingDataEventArgs>(this.DomainDataSourceLoadingData);
            this._dds.LoadedData -= new EventHandler<LoadedDataEventArgs>(this.DomainDataSourceLoadedData);
            this._dds.SubmittingChanges -= new EventHandler<SubmittingChangesEventArgs>(this.DomainDataSourceSubmittingChanges);
            this._dds.SubmittedChanges -= new EventHandler<SubmittedChangesEventArgs>(this.DomainDataSourceSubmittedChanges);

            this._view.PageChanged -= new EventHandler<EventArgs>(this.ViewPageChanged);

            this._comboBox.Loaded -= new RoutedEventHandler(this.ComboBoxLoaded);
            this._textBox.Loaded -= new RoutedEventHandler(this.TextBoxLoaded);
        }

        #endregion Cleanup Methods

        #region Helper Methods

        /// <summary>
        /// Helper method that verifies that the test () => raises the specified exception.
        /// </summary>
        /// <typeparam name="TException">Type of exception</typeparam>
        /// <param name="exceptionPrototype">Exception prototype, with the expected exception message populated.</param>
        /// <param name="test">Action () => to expect exception from.</param>
        protected static void AssertExpectedException<TException>(TException exceptionPrototype, Action test)
            where TException : Exception
        {
            AssertExpectedException<TException>(exceptionPrototype, true /*checkMessage*/, test);
        }

        /// <summary>
        /// Helper method that verifies that the test () => raises the specified exception.
        /// </summary>
        /// <typeparam name="TException">Type of exception</typeparam>
        /// <param name="exceptionPrototype">Exception prototype.</param>
        /// <param name="checkMessage">If True, the exception prototype must be populated with the expected message, then the expected and actual exception messages are compared to see if they match.</param>
        /// <param name="test">Action () => to expect exception from.</param>
        protected static void AssertExpectedException<TException>(TException exceptionPrototype, bool checkMessage, Action test)
            where TException : Exception
        {
            TException exception = null;

            try
            {
                test();
            }
            catch (TException e)
            {
                // Looking for exact matches
                if (e.GetType() == typeof(TException))
                {
                    exception = e;
                }
            }

            if (exception == null)
            {
                Assert.Fail("Expected {0} with message \"{1}\". \nActual: none.", typeof(TException).FullName, exceptionPrototype.Message);
            }
            else if (checkMessage && exception.Message != exceptionPrototype.Message)
            {
                Assert.Fail("Expected {0} with message \"{1}\". \nActual: {2} => \"{3}\".", typeof(TException).FullName, exceptionPrototype.Message, exception.GetType().FullName, exception.Message);
            }
        }

        protected void LoadComboBoxControl()
        {
            EnqueueCallback(() =>
            {
                this._comboBoxLoaded = false;
                this.TestPanel.Children.Add(this._comboBox);
            });

            EnqueueConditional(() => this._comboBoxLoaded);
        }

        protected void LoadDomainDataSourceControl()
        {
            EnqueueCallback(() =>
            {
                this._ddsLoaded = false;
                this.TestPanel.Children.Add(this._dds);
            });

            EnqueueConditional(() => this._ddsLoaded);
        }

        protected void LoadTextBoxControl()
        {
            EnqueueCallback(() =>
            {
                this._textBoxLoaded = false;
                this.TestPanel.Children.Add(this._textBox);
            });

            EnqueueConditional(() => this._textBoxLoaded);

            EnqueueCallback(() =>
            {
                this._textBoxAutomationPeer = TextBoxAutomationPeer.CreatePeerForElement(this._textBox) as TextBoxAutomationPeer;
            });
        }

        protected void ResetLoadState()
        {
            this._ddsLoadingData = 0;
            this._ddsLoadingDataExpected = 1;
            this._ddsLoadedData = 0;
            this._ddsLoadedDataExpected = 1;
            this._ddsLoadErrorHandled = false;
        }

        protected void ResetPageChanged()
        {
            this._viewPageChanged = 0;
            this._viewPageChangedExpected = 1;
        }

        protected void ResetSubmitState()
        {
            this._ddsSubmittingChanges = 0;
            this._ddsSubmittingChangesExpected = 1;
            this._ddsSubmittedChanges = 0;
            this._ddsSubmittedChangesExpected = 1;
        }

        protected void AssertNoLoadingData()
        {
            this.AssertNoLoadingData(EnsureNoEventsTimeout);
        }

        protected void AssertNoLoadingData(string failureMessage)
        {
            this.AssertNoLoadingData(EnsureNoEventsTimeout, failureMessage);
        }

        protected void AssertNoLoadingData(int millisecondsTimeout)
        {
            this.AssertNoLoadingData(millisecondsTimeout, null);
        }

        protected void AssertNoLoadingData(int millisecondsTimeout, string failureMessage)
        {
            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this._ddsLoadingDataExpected = 0;
                this._ddsLoadedDataExpected = 0;
                this._asyncEventFailureMessage = failureMessage;
            });

            // Our event handlers will report violations while we wait
            EnqueueDelay(millisecondsTimeout);

            EnqueueCallback(() => this.ResetLoadState());
        }

        protected void AssertLoadingData()
        {
            this.AssertLoadingData(1, true);
        }

        protected void AssertLoadingData(int count)
        {
            this.AssertLoadingData(count, true);
        }

        protected void AssertLoadingData(bool expectLoadedData)
        {
            this.AssertLoadingData(1, expectLoadedData);
        }

        protected void AssertLoadingData(int count, bool expectLoadedData)
        {
            EnqueueCallback(() =>
            {
                this._ddsLoadingDataExpected = count;
                this._ddsLoadedDataExpected = count;
            });

            EnqueueConditional(() =>
                (this._ddsLoadingData == count) && (!expectLoadedData || (this._ddsLoadedData == count)),
                this._asyncEventFailureMessage);
        }

        protected void AssertLoadedData()
        {
            this.AssertLoadedData(1);
        }

        protected void AssertLoadedData(int count)
        {
            EnqueueCallback(() =>
            {
                this._ddsLoadedDataExpected = count;
            });

            EnqueueConditional(() => (this._ddsLoadedData == count), this._asyncEventFailureMessage);
        }

        protected void AssertNoPageChanged()
        {
            this.AssertNoPageChanged(EnsureNoEventsTimeout);
        }

        protected void AssertNoPageChanged(int millisecondsTimeout)
        {
            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this._viewPageChangedExpected = 0;
            });

            // Our event handlers will report violations while we wait
            EnqueueDelay(millisecondsTimeout);

            EnqueueCallback(() => this.ResetPageChanged());
        }

        /// <summary>
        /// Waits for the PageChanged event to be raise using a default timeout.
        /// </summary>
        protected void AssertPageChanged()
        {
            this.AssertPageChanged(1);
        }

        protected void AssertPageChanged(int count)
        {
            EnqueueCallback(() =>
            {
                this._viewPageChangedExpected = count;
            });

            EnqueueConditional(() => (this._viewPageChanged == count), this._asyncEventFailureMessage);
        }

        protected void AssertSubmittingChanges()
        {
            this.AssertSubmittingChanges(1);
        }

        protected void AssertSubmittingChanges(int count)
        {
            EnqueueCallback(() =>
            {
                this._ddsSubmittingChangesExpected = count;
                this._ddsSubmittedChangesExpected = count;
            });

            EnqueueConditional(() =>
                (this._ddsSubmittingChanges == count) && (this._ddsSubmittedChanges == count),
                this._asyncEventFailureMessage);
        }

        /// <summary>
        /// Enqueue a callback that will result in a load error, ensuring that
        /// the load error occurs and is handled properly for the test.
        /// </summary>
        /// <param name="actionResultingInLoadError"></param>
        protected void AssertExpectedLoadError(Action actionResultingInLoadError)
        {
            this.AssertExpectedLoadError(null, null, actionResultingInLoadError);
        }

        /// <summary>
        /// Enqueue a callback that will result in a load error, ensuring that
        /// the load error occurs and is handled properly for the test.
        /// </summary>
        /// <param name="expectedErrorType">The type of error expected. <c>null</c> if unchecked.</param>
        /// <param name="expectedErrorMessage">The error message expected.  <c>null</c> if unchecked.</param>
        /// <param name="actionResultingInLoadError"></param>
        protected void AssertExpectedLoadError(Type expectedErrorType, string expectedErrorMessage, Action actionResultingInLoadError)
        {
            EnqueueCallback(() =>
            {
                this._ddsLoadErrorExpected = true;
            });

            // Queue up the action that should result in a load error
            EnqueueCallback(() => actionResultingInLoadError());

            // Ensure the LoadingData event is raised, but don't expect a LoadedData event
            this.AssertLoadingData(false);

            // Wait until the expected error is handled
            EnqueueConditional(() => this._ddsLoadErrorHandled, this._asyncEventFailureMessage);

            // Once the expected error count is reached, unsubscribe from the loaded data event ensuring
            // that any unexpected errors do not get marked as handled
            EnqueueCallback(() =>
            {
                if (expectedErrorType != null)
                {
                    Assert.IsInstanceOfType(this._ddsLoadedDataEventArgs.Error, expectedErrorType);
                }

                if (expectedErrorMessage != null)
                {
                    Assert.AreEqual<string>(expectedErrorMessage, this._ddsLoadedDataEventArgs.Error.Message);
                }

                this._ddsLoadErrorExpected = false;
                this._ddsLoadErrorHandled = false;
            });
        }

        /// <summary>
        /// Handles a load error when expected, marking the error as handled.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args for the load error.</param>
        private void MarkLoadErrorAsHandled(object sender, LoadedDataEventArgs e)
        {
            e.MarkErrorAsHandled();
            this._ddsLoadErrorHandled = true;
            this._ddsLoadedDataEventArgs = e;
        }

        protected void ComboBoxLoaded(object sender, RoutedEventArgs e)
        {
            this._comboBoxLoaded = true;
        }

        protected void DomainDataSourceLoaded(object sender, RoutedEventArgs e)
        {
            this._ddsLoaded = true;
        }

        protected void DomainDataSourceLoadedData(object sender, LoadedDataEventArgs e)
        {
            this._ddsLoadedData++;
            string messagePrefix = !string.IsNullOrEmpty(this._asyncEventFailureMessage) ? this._asyncEventFailureMessage + "; " : "";
            Assert.IsTrue(this._ddsLoadedData <= this._ddsLoadedDataExpected,
                messagePrefix + "There should only be {0} LoadedData events.", this._ddsLoadedDataExpected);
            if (this._reload)
            {
                this._reload = false;
                this._dds.Load();
            }

            if (e.HasError)
            {
                MarkLoadErrorAsHandled(sender, e);

                if (!this._ddsLoadErrorExpected)
                {
                    Assert.Fail("An unexpected load error occurred.  If this unit test is expecting a load error, set this._ddsLoadErrorExpected to true before invoking the load.  Errors will be marked as handled automatically. " + this._ddsLoadedDataEventArgs.Error.ToString());
                }
            }
        }

        protected void DomainDataSourceLoadedDataWithPageSizeIncrement(object sender, LoadedDataEventArgs e)
        {
            if (this._dds.PageSize < 2)
            {
                this._dds.PageSize++;
                this._dds.Load();
            }
        }

        protected void DomainDataSourceLoadingData(object sender, System.Windows.Controls.LoadingDataEventArgs e)
        {
            this._ddsLoadingData++;
            string messagePrefix = !string.IsNullOrEmpty(this._asyncEventFailureMessage) ? this._asyncEventFailureMessage + "; " : "";
            Assert.IsTrue(this._ddsLoadingData <= this._ddsLoadingDataExpected,
                messagePrefix + "There should only be {0} LoadingData events.", this._ddsLoadingDataExpected);
        }

        protected void DomainDataSourceSubmittedChanges(object sender, SubmittedChangesEventArgs e)
        {
            this._ddsSubmittedChanges++;
            string messagePrefix = !string.IsNullOrEmpty(this._asyncEventFailureMessage) ? this._asyncEventFailureMessage + "; " : "";
            Assert.IsTrue(this._ddsSubmittedChanges <= this._ddsSubmittedChangesExpected,
                messagePrefix + "There should only be {0} SubmittedChanges events.", this._ddsSubmittedChangesExpected);
        }

        protected void DomainDataSourceSubmittingChanges(object sender, SubmittingChangesEventArgs e)
        {
            this._ddsSubmittingChanges++;
            string messagePrefix = !string.IsNullOrEmpty(this._asyncEventFailureMessage) ? this._asyncEventFailureMessage + "; " : "";
            Assert.IsTrue(this._ddsSubmittingChanges <= this._ddsSubmittingChangesExpected,
                messagePrefix + "There should only be {0} SubmittingChanges events.", this._ddsSubmittingChangesExpected);
        }

        protected void ViewPageChanged(object sender, EventArgs e)
        {
            this._viewPageChanged++;
            string messagePrefix = !string.IsNullOrEmpty(this._asyncEventFailureMessage) ? this._asyncEventFailureMessage + "; " : "";
            Assert.IsTrue(this._viewPageChanged <= this._viewPageChangedExpected,
                messagePrefix + "There should only be {0} PageChanged events.", this._viewPageChangedExpected);
        }

        protected void TrackCollectionChanged()
        {
            this._viewCollectionChangedEvents = new Dictionary<NotifyCollectionChangedAction, int>();
            this._viewCollectionChangedEvents[NotifyCollectionChangedAction.Add] = 0;
            this._viewCollectionChangedEvents[NotifyCollectionChangedAction.Remove] = 0;
            this._viewCollectionChangedEvents[NotifyCollectionChangedAction.Replace] = 0;
            this._viewCollectionChangedEvents[NotifyCollectionChangedAction.Reset] = 0;
        }

        protected void ViewCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (this._viewCollectionChangedEvents != null)
            {
                this._viewCollectionChangedEvents[e.Action]++;
            }
        }

        protected void AssertCollectionChanged(int addCount, int removeCount, int resetCount, string message)
        {
            // A single assert lets us see the big picture on a failure
            Assert.IsTrue(
                addCount == this._viewCollectionChangedEvents[NotifyCollectionChangedAction.Add] &&
                removeCount == this._viewCollectionChangedEvents[NotifyCollectionChangedAction.Remove] &&
                resetCount == this._viewCollectionChangedEvents[NotifyCollectionChangedAction.Reset] &&
                0 == this._viewCollectionChangedEvents[NotifyCollectionChangedAction.Replace],
                message +
                "; Add Expected: " + addCount + ", Actual: " + this._viewCollectionChangedEvents[NotifyCollectionChangedAction.Add] +
                "; Remove Expected: " + removeCount + ", Actual: " + this._viewCollectionChangedEvents[NotifyCollectionChangedAction.Remove] +
                "; Reset Expected: " + resetCount + ", Actual: " + this._viewCollectionChangedEvents[NotifyCollectionChangedAction.Reset] +
                "; Replace Expected: 0, Actual: " + this._viewCollectionChangedEvents[NotifyCollectionChangedAction.Replace]);

            this.TrackCollectionChanged();
        }

        protected void TextBoxLoaded(object sender, RoutedEventArgs e)
        {
            this._textBoxLoaded = true;
        }

        protected List<string> TrackEventDetails()
        {
            List<string> events = new List<string>();
            this._collectionView.CollectionChanged += (s, e) => events.Add("CollectionChanged: " + e.Action);
            this._view.CurrentChanged += (s, e) => events.Add("CurrentChanged: " + (s as DomainDataSourceView).CurrentItem.ToString());
            this._view.CurrentChanging += (s, e) => events.Add("CurrentChanging");
            this._view.PageChanged += (s, e) => events.Add("PageChanged: " + (s as DomainDataSourceView).PageIndex.ToString());
            this._view.PageChanging += (s, e) => events.Add("PageChanging: " + e.NewPageIndex);
            this._dds.LoadedData += (s, e) => events.Add("LoadedData: " + (s as DomainDataSource).DataView.Count.ToString());
            this._dds.LoadingData += (s, e) => events.Add("LoadingData: " + e.Query.QueryName);

            ((INotifyPropertyChanged)this._view).PropertyChanged += (s, e) =>
            {
                // We can get notifications for properties that are explicitly implemented, which
                // produces a null property.  For those properties, we'll just state the name.
                System.Reflection.PropertyInfo prop = s.GetType().GetProperty(e.PropertyName);
                string value = null;

                if (prop != null)
                {
                    value = string.Format("({0})", prop.GetValue(s, null));
                }

                events.Add("PropertyChanged: " + e.PropertyName + value);
            };

            return events;
        }

        protected void AssertEventDetailsMatch(List<string> expectedEvents, List<string> actualEvents, params object[] args)
        {
            List<string> formattedEvents = new List<string>();
            expectedEvents.ForEach(s => formattedEvents.Add(string.Format(s, args)));

            if (!formattedEvents.SequenceEqual(actualEvents))
            {
                Assert.Fail(this.GetEventDetailMismatch(formattedEvents, actualEvents));
            }
        }

        protected string GetEventDetailMismatch(List<string> expected, List<string> actual)
        {
            System.Text.StringBuilder result = new System.Text.StringBuilder();
            result.Append("<br>Expected (" + expected.Count.ToString() + "):<br>");

            for (int ctr = 0; ctr < expected.Count; ctr++)
            {
                result.Append("\"" + expected[ctr] + "\"" + (ctr < expected.Count - 1 ? ",<br>" : ". "));
            }

            result.Append("<br><br>Actual (" + actual.Count.ToString() + "):<br>");

            for (int ctr = 0; ctr < actual.Count; ctr++)
            {
                result.Append("\"" + actual[ctr] + "\"" + (ctr < actual.Count - 1 ? ",<br>" : ". "));
            }

            return result.ToString();
        }

        protected FilterDescriptor CreateValueBoundFilterDescriptor(string propertyPath, FilterOperator filterOperator, object source, string path)
        {
            FilterDescriptor filterDescriptor = new FilterDescriptor() { PropertyPath = propertyPath, Operator = filterOperator };
            this.CreateBinding(filterDescriptor, FilterDescriptor.ValueProperty, source, path);
            return filterDescriptor;
        }

        protected Parameter CreateValueBoundParameter(string parameterName, object source, string path)
        {
            Parameter parameter = new Parameter() { ParameterName = parameterName };
            this.CreateBinding(parameter, Parameter.ValueProperty, source, path);
            return parameter;
        }

        protected SortDescriptor CreatePathBoundSortDescriptor(ListSortDirection direction, object source, string path)
        {
            SortDescriptor sortDescriptor = new SortDescriptor() { Direction = direction };
            this.CreateBinding(sortDescriptor, SortDescriptor.PropertyPathProperty, source, path);
            return sortDescriptor;
        }

        protected Binding CreateBinding(DependencyObject target, DependencyProperty dp, object source, string path)
        {
            return this.CreateBinding(target, dp, source, path, null, null);
        }

        protected Binding CreateBinding(DependencyObject target, DependencyProperty dp, object source, string path, IValueConverter converter, object converterParameter)
        {
            Binding binding = new Binding(path);
            binding.Source = source;
            binding.Converter = converter;
            binding.ConverterParameter = converterParameter;
            BindingOperations.SetBinding(target, dp, binding);
            return binding;
        }

        protected LoadTimer UseLoadTimer()
        {
            if (this._loadTimer == null)
            {
                this._loadTimer = new LoadTimer();
                this._loadTimer.UseWithDDS(this._dds);
            }
            return this._loadTimer;
        }

        protected ProgressiveLoadTimer UseProgressiveLoadTimer()
        {
            if (this._progressiveLoadTimer == null)
            {
                this._progressiveLoadTimer = new ProgressiveLoadTimer();
                this._progressiveLoadTimer.UseWithDDS(this._dds);
            }
            return this._progressiveLoadTimer;
        }

        protected RefreshLoadTimer UseRefreshLoadTimer()
        {
            if (this._refreshLoadTimer == null)
            {
                this._refreshLoadTimer = new RefreshLoadTimer();
                this._refreshLoadTimer.UseWithDDS(this._dds);
            }
            return this._refreshLoadTimer;
        }

        #endregion Helper Methods

        #region Common Cities Methods

        /// <summary>
        /// Enqueue the necessary calls to load the cities, with the specified page size
        /// and auto load properties.
        /// </summary>
        /// <param name="pageSize">The <see cref="DomainDataSource.PageSize"/> to use.</param>
        /// <param name="autoLoad">What to set <see cref="DomainDataSource.AutoLoad"/> to.</param>
        protected void LoadCities(int pageSize, bool autoLoad)
        {
            EnqueueCallback(() =>
            {
                this._dds.AutoLoad = autoLoad;
                this._dds.QueryName = "GetCities";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.PageSize = pageSize;

                if (!autoLoad)
                {
                    this._dds.Load();
                }

                this._asyncEventFailureMessage = "LoadCities";
            });

            this.AssertLoadingData();

            if (pageSize > 0)
            {
                this.AssertPageChanged();
            }

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();
                this._asyncEventFailureMessage = null;
            });
        }

        /// <summary>
        /// Enqueue the necessary calls to move the view to the next page;
        /// </summary>
        protected void MoveToNextPage()
        {
            this.MoveToNextPage("MoveToNextPage()");
        }

        /// <summary>
        /// Enqueue the necessary calls to move the view to the next page.
        /// </summary>
        /// <param name="message">The message to include in the assert.</param>
        protected void MoveToNextPage(string message)
        {
            EnqueueCallback(() =>
            {
                this._view.MoveToNextPage();
                this._asyncEventFailureMessage = message;
            });

            this.AssertLoadingData();
            this.AssertPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();
                this._asyncEventFailureMessage = null;
            });
        }

        /// <summary>
        /// Assert that the first page is loaded as the result of a previously enqueued callback.
        /// </summary>
        /// <param name="message">The message to include in the assert.</param>
        protected void AssertFirstPageLoaded(string message)
        {
            EnqueueCallback(() => this._asyncEventFailureMessage = message);

            this.AssertLoadingData();
            this.AssertPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                Assert.AreEqual(0, this._view.PageIndex, "PageIndex should be 0 after applying the sort. " + message);
            });

        }

        /// <summary>
        /// Assert that the view is on the first page and the cities on the view are sorted by name in ascending order.
        /// </summary>
        /// <param name="message">The message to include in the assert.</param>
        protected void AssertFirstPageLoadedSortedByName(string message)
        {
            this.AssertFirstPageLoaded(message);

            EnqueueCallback(() =>
            {
                AssertHelper.AssertSequenceSorting(this._view.Cast<City>().Select(c => c.Name), ListSortDirection.Ascending, "The cities should be sorted by Name. " + message);
            });
        }

        #endregion Common Cities Methods
    }
}
