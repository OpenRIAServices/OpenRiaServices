using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using OpenRiaServices.DomainServices;
using OpenRiaServices.DomainServices.Client;
using System.Windows.Common;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace System.Windows.Controls
{
    using Expression = System.Linq.Expressions.Expression;

    /// <summary>
    /// A component to simplify the interaction between the user interface and data from a
    /// <see cref="OpenRiaServices.DomainServices.Client.DomainContext"/>. With the
    /// DomainDataSource, you can <see cref="Load">load</see>, <see cref="FilterDescriptors">filter</see>,
    /// <see cref="GroupDescriptors">group</see>, and <see cref="SortDescriptors">sort</see> data easily.
    /// <para>
    /// After specifying a <see cref="DomainContext"/> and <see cref="QueryName"/>, the
    /// <see cref="DomainDataSource"/> can load data and expose it through the <see cref="Data"/>
    /// and <see cref="DataView"/> properties.
    /// </para>
    /// </summary>
    /// <seealso cref="AutoLoad"/>
    /// <seealso cref="FilterDescriptors"/>
    /// <seealso cref="QueryParameters"/>
    /// <seealso cref="SortDescriptors"/>
    /// <seealso cref="GroupDescriptors"/>
    [TemplateVisualState(Name = StateNormal, GroupName = GroupCommon)]
    [TemplateVisualState(Name = StateDisabled, GroupName = GroupCommon)]

    [TemplateVisualState(Name = StateIdle, GroupName = GroupActivity)]
    [TemplateVisualState(Name = StateLoading, GroupName = GroupActivity)]
    [TemplateVisualState(Name = StateSubmitting, GroupName = GroupActivity)]

    [TemplateVisualState(Name = StateUnchanged, GroupName = GroupChange)]
    [TemplateVisualState(Name = StateChanged, GroupName = GroupChange)]

    [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "Class will be refactored in subsequent releases")]
    public class DomainDataSource : Control
    {
        #region Static Fields and Constants

#if DOMAINDATASOURCE_TRACE
        internal static readonly DebugTrace.TraceSwitch LoadDebug = new DebugTrace.TraceSwitch("LoadDebug");
        internal static readonly DebugTrace.TraceSwitch SubmitDebug = new DebugTrace.TraceSwitch("SubmitDebug");
#else
        internal static readonly DebugTrace.TraceSwitch LoadDebug;
        internal static readonly DebugTrace.TraceSwitch SubmitDebug;
#endif //DOMAINDATASOURCE_TRACE

        #region GroupCommon
        private const string StateNormal = "Normal";
        private const string StateDisabled = "Disabled";
        private const string GroupCommon = "CommonStates";
        #endregion GroupCommon

        #region GroupActivity
        private const string StateIdle = "Idle";
        private const string StateLoading = "Loading";
        private const string StateSubmitting = "Submitting";
        private const string GroupActivity = "ActivityStates";
        #endregion GroupActivity

        #region GroupChange
        private const string StateUnchanged = "Unchanged";
        private const string StateChanged = "Changed";
        private const string GroupChange = "ChangeStates";
        #endregion GroupChange

        /// <summary>
        /// The preferred query name suffix to be added to
        /// <see cref="QueryName"/> values as needed.
        /// </summary>
        private const string QueryNameSuffix = "Query";

        /// <summary>
        /// The <see cref="DependencyProperty"/> for the <see cref="AutoLoad"/> property.
        /// </summary>
        public static readonly DependencyProperty AutoLoadProperty =
            DependencyProperty.Register(
                "AutoLoad",
                typeof(bool),
                typeof(DomainDataSource),
                new PropertyMetadata(AutoLoadPropertyChanged));

        /// <summary>
        /// The <see cref="DependencyProperty"/> for the <see cref="CanLoad"/> property.
        /// </summary>
        public static readonly DependencyProperty CanLoadProperty =
            DependencyProperty.Register(
                "CanLoad",
                typeof(bool),
                typeof(DomainDataSource),
                new PropertyMetadata(true, CanLoadPropertyChanged));

        /// <summary>
        /// The <see cref="DependencyProperty"/> for the <see cref="Data"/> property.
        /// </summary>
        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register(
                "Data",
                typeof(IEnumerable),
                typeof(DomainDataSource),
                new PropertyMetadata(DataPropertyChanged));

        /// <summary>
        /// The <see cref="DependencyProperty"/> for the <see cref="DataView"/> property.
        /// </summary>
        public static readonly DependencyProperty DataViewProperty =
            DependencyProperty.Register(
                "DataView",
                typeof(DomainDataSourceView),
                typeof(DomainDataSource),
                new PropertyMetadata(DataViewPropertyChanged));

        /// <summary>
        /// The property name used by the <see cref="DesignDataProperty"/>.
        /// </summary>
        /// <remarks>
        /// There is a constant for "DesignData" because the property name is
        /// used for both the dependency property registration and the
        /// design-time binding.
        /// </remarks>
        private const string DesignDataPropertyName = "DesignData";

        /// <summary>
        /// The <see cref="DependencyProperty"/> for the <see cref="DesignData"/> property.
        /// </summary>
        public static readonly DependencyProperty DesignDataProperty =
            DependencyProperty.Register(
                DesignDataPropertyName,
                typeof(IEnumerable),
                typeof(DomainDataSource),
                null);

        /// <summary>
        /// The <see cref="DependencyProperty"/> for the <see cref="DomainContext"/> property.
        /// </summary>
        public static readonly DependencyProperty DomainContextProperty =
            DependencyProperty.Register(
                "DomainContext",
                typeof(DomainContext),
                typeof(DomainDataSource),
                new PropertyMetadata(DomainContextPropertyChanged));

        /// <summary>
        /// The <see cref="DependencyProperty"/> for the <see cref="FilterDescriptors"/> property.
        /// </summary>
        private static readonly DependencyProperty FilterDescriptorsProperty =
            DependencyProperty.Register(
                "FilterDescriptors",
                typeof(FilterDescriptorCollection),
                typeof(DomainDataSource),
                new PropertyMetadata(FilterDescriptorsPropertyChanged));

        /// <summary>
        /// The <see cref="DependencyProperty"/> for the <see cref="FilterOperator"/> property.
        /// </summary>
        public static readonly DependencyProperty FilterOperatorProperty =
            DependencyProperty.Register(
                "FilterOperator",
                typeof(object),
                typeof(DomainDataSource),
                new PropertyMetadata(FilterDescriptorLogicalOperator.And, DomainDataSource.FilterOperatorPropertyChanged));

        /// <summary>
        /// The <see cref="DependencyProperty"/> for the <see cref="GroupDescriptors"/> property.
        /// </summary>
        private static readonly DependencyProperty GroupDescriptorsProperty =
            DependencyProperty.Register(
                "GroupDescriptors",
                typeof(GroupDescriptorCollection),
                typeof(DomainDataSource),
                new PropertyMetadata(GroupDescriptorsPropertyChanged));

        /// <summary>
        /// The <see cref="DependencyProperty"/> for the <see cref="HasChanges"/> property.
        /// <para>
        /// This property is read-only.
        /// </para>
        /// </summary>
        public static readonly DependencyProperty HasChangesProperty =
            DependencyProperty.Register(
                "HasChanges",
                typeof(bool),
                typeof(DomainDataSource),
                new PropertyMetadata(HasChangesPropertyChanged));

        /// <summary>
        /// The <see cref="DependencyProperty"/> for the <see cref="IsBusy"/> property.
        /// <para>
        /// This property is read-only.
        /// </para>
        /// </summary>
        public static readonly DependencyProperty IsBusyProperty =
            DependencyProperty.Register(
                "IsBusy",
                typeof(bool),
                typeof(DomainDataSource),
                new PropertyMetadata(IsBusyPropertyChanged));

        /// <summary>
        /// The <see cref="DependencyProperty"/> for the <see cref="IsLoadingData"/> property.
        /// <para>
        /// This property is read-only.
        /// </para>
        /// </summary>
        public static readonly DependencyProperty IsLoadingDataProperty =
            DependencyProperty.Register(
                "IsLoadingData",
                typeof(bool),
                typeof(DomainDataSource),
                new PropertyMetadata(IsLoadingDataPropertyChanged));

        /// <summary>
        /// The <see cref="DependencyProperty"/> for the <see cref="IsSubmittingChanges"/> property.
        /// <para>
        /// This property is read-only.
        /// </para>
        /// </summary>
        public static readonly DependencyProperty IsSubmittingChangesProperty =
            DependencyProperty.Register(
                "IsSubmittingChanges",
                typeof(bool),
                typeof(DomainDataSource),
                new PropertyMetadata(IsSubmittingChangesPropertyChanged));

        /// <summary>
        /// The <see cref="DependencyProperty"/> for the <see cref="LoadDelay"/> property.
        /// </summary>
        public static readonly DependencyProperty LoadDelayProperty =
            DependencyProperty.Register(
                "LoadDelay",
                typeof(TimeSpan),
                typeof(DomainDataSource),
                new PropertyMetadata(LoadDelayPropertyChanged));

        /// <summary>
        /// The <see cref="DependencyProperty"/> for the <see cref="LoadInterval"/> property.
        /// </summary>
        public static readonly DependencyProperty LoadIntervalProperty =
            DependencyProperty.Register(
                "LoadInterval",
                typeof(TimeSpan),
                typeof(DomainDataSource),
                new PropertyMetadata(LoadIntervalPropertyChanged));

        /// <summary>
        /// The <see cref="DependencyProperty"/> for the <see cref="LoadSize"/> property.
        /// </summary>
        public static readonly DependencyProperty LoadSizeProperty =
            DependencyProperty.Register(
                "LoadSize",
                typeof(int),
                typeof(DomainDataSource),
                new PropertyMetadata(LoadSizePropertyChanged));

        /// <summary>
        /// The <see cref="DependencyProperty"/> for the <see cref="PageSize"/> property.
        /// </summary>
        public static readonly DependencyProperty PageSizeProperty =
            DependencyProperty.Register(
                "PageSize",
                typeof(int),
                typeof(DomainDataSource),
                new PropertyMetadata(PageSizePropertyChanged));

        /// <summary>
        /// The <see cref="DependencyProperty"/> for the <see cref="QueryName"/> property.
        /// </summary>
        public static readonly DependencyProperty QueryNameProperty =
            DependencyProperty.Register(
                "QueryName",
                typeof(string),
                typeof(DomainDataSource),
                new PropertyMetadata(QueryNamePropertyChanged));

        /// <summary>
        /// The <see cref="DependencyProperty"/> for the <see cref="QueryParameters"/> property.
        /// </summary>
        private static readonly DependencyProperty QueryParametersProperty =
            DependencyProperty.Register(
                "QueryParameters",
                typeof(ParameterCollection),
                typeof(DomainDataSource),
                new PropertyMetadata(QueryParametersPropertyChanged));

        /// <summary>
        /// The <see cref="DependencyProperty"/> for the <see cref="RefreshInterval"/> property.
        /// </summary>
        public static readonly DependencyProperty RefreshIntervalProperty =
            DependencyProperty.Register(
                "RefreshInterval",
                typeof(TimeSpan),
                typeof(DomainDataSource),
                new PropertyMetadata(RefreshIntervalPropertyChanged));

        /// <summary>
        /// The <see cref="DependencyProperty"/> for the <see cref="SortDescriptors"/> property.
        /// </summary>
        private static readonly DependencyProperty SortDescriptorsProperty =
            DependencyProperty.Register(
                "SortDescriptors",
                typeof(SortDescriptorCollection),
                typeof(DomainDataSource),
                new PropertyMetadata(SortDescriptorsPropertyChanged));

        #endregion Static Fields and Constants

        #region Member Fields

        /// <summary>
        /// Represents the characteristics of the latest load operation.
        /// </summary>
        private LoadContext _currentLoadContext;

        /// <summary>
        /// Represents the pending load operation.
        /// </summary>
        private LoadOperation _currentLoadOperation;

        /// <summary>
        /// Represents the current submit operation requested of the DomainContext.
        /// </summary>
        private SubmitOperation _currentSubmitOperation;

        /// <summary>
        /// Flag used for preventing reentrance during Loading and Submitting events
        /// </summary>
        private bool _preparingOperation;

        /// <summary>
        /// The <see cref="Type"/> of <see cref="Entity"/> loaded for the query.
        /// </summary>
        private Type _entityType;

        // collection managers
        private readonly FilterCollectionManager _filterCollectionManager;
        private readonly GroupCollectionManager _groupCollectionManager;
        private readonly ParameterCollectionManager _parameterCollectionManager;
        private readonly SortCollectionManager _sortCollectionManager;

        // cache objects for validated descriptors
        private readonly ExpressionCache _expressionCache;
        private Expression _filtersExpression;
        private IEnumerable<string> _cachedParameters;

        /// <summary>
        /// Collection populated by the loaded entities and provided as the
        /// source to the <see cref="PagedEntityCollectionView"/>.
        /// </summary>
        private PagedEntityCollection _internalEntityCollection;

        /// <summary>
        /// Collection view used as the source of the <see cref="DataView"/>.
        /// </summary>
        private PagedEntityCollectionView _internalEntityCollectionView;

        // Determines whether the initial auto-load should be executed
        private bool _isAutoLoadInitialized;
        private bool _shouldExecuteInitialAutoLoad;

        /// <summary>
        /// Latest page index requested via <see cref="PagedEntityCollection.MoveToPage"/>.
        /// </summary>
        private int _lastRequestedPageIndex;

        // Keep track of our deferred load state with how many nested deferrals there
        // are and what type of load should be invoked once the deferrals are unwound.
        private int _loadDeferLevel;
        private LoadType? _deferredLoadType = null;

        // Timers used to batch auto-loads, progressive load, and refresh at intervals
        private ITimer _loadTimer;
        private ITimer _progressiveLoadTimer;
        private ITimer _refreshLoadTimer;

        /// <summary>
        /// Number of items downloaded progressively.
        /// </summary>
        private int _progressiveItemCount;

        /// <summary>
        /// The <see cref="EntityQuery{T}"/> method that was discovered for the
        /// <see cref="DomainContext"/> and query method specified.
        /// </summary>
        private MethodInfo _queryMethod;

        // Fields for commands, enabled logic, and notification
        private bool _canSubmitChanges;
        private bool _canRejectChanges;
        private PropertyChangedNotifier _commandPropertyNotifier;
        private ICommand _loadCommand;
        private ICommand _rejectChangesCommand;
        private ICommand _submitChangesCommand;

        /// <summary>
        /// Set to <c>true</c> when a descriptor value cannot be converted.
        /// This indicates that the the subsequent auto-load should be skipped.
        /// </summary>
        private bool _skipNextAutoLoad;

        #endregion Member Fields

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DomainDataSource"/> class.
        /// </summary>
        public DomainDataSource()
        {
            this.InitializeView();
            this.InitializeCommands();

            this.FilterDescriptors = new FilterDescriptorCollection();
            this.GroupDescriptors = new GroupDescriptorCollection();
            this.QueryParameters = new ParameterCollection();
            this.SortDescriptors = new SortDescriptorCollection();

            this.DefaultStyleKey = typeof(DomainDataSource);
            this.IsEnabledChanged += new DependencyPropertyChangedEventHandler(this.DomainDataSource_IsEnabledChanged);
            this.Loaded += new RoutedEventHandler(this.DomainDataSource_Loaded);

            this._expressionCache = new ExpressionCache();
            this._filterCollectionManager = new FilterCollectionManager(this.FilterDescriptors, this._expressionCache, this.CheckFilterDescriptor);
            this._groupCollectionManager = new GroupCollectionManager(this.GroupDescriptors, ((ICollectionView)this.DataView).GroupDescriptions, this._expressionCache, this.CheckGroupDescriptor);
            this._parameterCollectionManager = new ParameterCollectionManager(this.QueryParameters, this.CheckQueryParameter, this.CheckQueryParameters);
            this._sortCollectionManager = new SortCollectionManager(this.SortDescriptors, ((ICollectionView)this.DataView).SortDescriptions, this._expressionCache, this.CheckSortDescriptor);

            this._filterCollectionManager.CollectionChanged += this.HandleManagerCollectionChanged_Filter;
            this._groupCollectionManager.CollectionChanged += this.HandleManagerCollectionChanged_Group;
            this._parameterCollectionManager.CollectionChanged += this.HandleManagerCollectionChanged_Parameter;
            this._sortCollectionManager.CollectionChanged += this.HandleManagerCollectionChanged_Sort;

            this._filterCollectionManager.PropertyChanged += this.HandleManagerPropertyChanged_Filter;
            this._groupCollectionManager.PropertyChanged += this.HandleManagerPropertyChanged_Group;
            this._parameterCollectionManager.PropertyChanged += this.HandleManagerPropertyChanged_Parameter;
            this._sortCollectionManager.PropertyChanged += this.HandleManagerPropertyChanged_Sort;
        }

        #endregion Constructors

        #region Events

        /// <summary>
        /// Event raised whenever a submit operation is completed.
        /// </summary>
        /// <remarks>
        /// This event is raised on the completion of an asynchronous <see cref="SubmitChanges"/> operation.
        /// If the operation was canceled via <see cref="CancelSubmit"/>, the
        /// <see cref="AsyncCompletedEventArgs.Cancelled"/> flag will be <c>true</c>. Also, exceptions that
        /// occurred during the operation will be available on <see cref="AsyncCompletedEventArgs.Error"/>.
        /// If the submit was canceled from the <see cref="SubmittingChanges"/> event, this event will not be
        /// raised. Also, if there were no changes this event will not be raised.
        /// </remarks>
        public event EventHandler<SubmittedChangesEventArgs> SubmittedChanges;

        /// <summary>
        /// Event raised whenever a load operation is completed.
        /// </summary>
        /// <remarks>
        /// This event is raised on the completion of an asynchronous <see cref="Load"/> operation. If the
        /// operation was canceled via <see cref="CancelLoad"/>, the <see cref="AsyncCompletedEventArgs.Cancelled"/>
        /// flag will be <c>true</c>. Also, exceptions that occurred during the operation will be available
        /// on <see cref="AsyncCompletedEventArgs.Error"/>. If the load was canceled from the
        /// <see cref="LoadingData"/> event, this event will not be raised.
        /// </remarks>
        public event EventHandler<LoadedDataEventArgs> LoadedData;

        /// <summary>
        /// Event raised whenever a load operation is launched.
        /// </summary>
        /// <remarks>
        /// This event is raised from <see cref="Load"/> and allows a handler to cancel the load before it
        /// begins. When a handler sets <see cref="CancelEventArgs.Cancel"/> to <c>true</c>, the load
        /// will be aborted and a subsequent <see cref="LoadedData"/> event will not be raised. This
        /// differs slightly from canceling a load via <see cref="CancelLoad"/>.
        /// </remarks>
        /// <seealso cref="CancelLoad"/>
        public event EventHandler<LoadingDataEventArgs> LoadingData;

        /// <summary>
        /// Event raised whenever a submit operation is launched.
        /// </summary>
        /// <remarks>
        /// This event is raised from <see cref="SubmitChanges"/> and allows a handler to cancel the submit
        /// before it begins. When a handler sets <see cref="CancelEventArgs.Cancel"/> to
        /// <c>true</c>, the submit will be aborted and a subsequent <see cref="SubmittedChanges"/> event
        /// will not be raised. This differs slightly from canceling a submit via <see cref="CancelSubmit"/>.
        /// </remarks>
        /// <seealso cref="CancelSubmit"/>
        public event EventHandler<SubmittingChangesEventArgs> SubmittingChanges;

        #endregion Events

        #region Properties

        /// <summary>
        /// Gets or sets a value indicating whether <see cref="Load"/> is automatically invoked when
        /// a change occurs that impacts the query composed by the <see cref="DomainDataSource"/>.
        /// </summary>
        /// <remarks>
        /// When <see cref="AutoLoad"/> is <c>true</c>, any property change affecting the load
        /// query will automatically invoke a <see cref="Load"/> after the specified
        /// <see cref="LoadDelay"/>. Examples of properties that impact the query are
        /// <see cref="PageSize"/> and <see cref="FilterOperator"/>. Also, changes to dependency
        /// object collections like <see cref="FilterDescriptors"/> and changes to the dependency
        /// properties on elements contained in those collections will affect the query and prompt
        /// an automatic <see cref="Load"/>.
        /// </remarks>
        /// <seealso cref="LoadDelay"/>
        public bool AutoLoad
        {
            get
            {
                return (bool)GetValue(AutoLoadProperty);
            }
            set
            {
                SetValue(AutoLoadProperty, value);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the control can perform
        /// a load operation based on the present state.
        /// </summary>
        /// <value>
        /// <c>false</c> whenever <see cref="HasChanges"/> or <see cref="IsSubmittingChanges"/>
        /// is <c>true</c>.
        /// </value>
        public bool CanLoad
        {
            get
            {
                return (bool)GetValue(CanLoadProperty);
            }
            private set
            {
                if (this.CanLoad != value)
                {
                    this.SetValueNoCallback(CanLoadProperty, value);
                    this._internalEntityCollectionView.CanLoad = this.CanLoad;
                }
            }
        }

        /// <summary>
        /// Get a value indicating whether <see cref="RejectChanges"/> can be called.
        /// </summary>
        /// <value>
        /// <c>true</c> whenever <see cref="HasChanges"/> is <c>true</c>. Otherwise, <c>false</c>.
        /// </value>
        internal bool CanRejectChanges
        {
            get
            {
                return this._canRejectChanges;
            }
            private set
            {
                if (this._canRejectChanges != value)
                {
                    this._canRejectChanges = value;
                    this.CommandPropertyNotifier.RaisePropertyChanged("CanRejectChanges");
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether <see cref="SubmitChanges"/> can be called.
        /// </summary>
        /// <value>
        /// <c>true</c> whenever <see cref="HasChanges"/> is <c>true</c> and
        /// <see cref="IsSubmittingChanges"/> is <c>false</c>
        /// </value>
        internal bool CanSubmitChanges
        {
            get
            {
                return this._canSubmitChanges;
            }
            private set
            {
                if (this._canSubmitChanges != value)
                {
                    this._canSubmitChanges = value;
                    this.CommandPropertyNotifier.RaisePropertyChanged("CanSubmitChanges");
                }
            }
        }

        /// <summary>
        /// Gets the <see cref="CultureInfo"/> used for comparisons and conversions.
        /// </summary>
        private CultureInfo Culture
        {
            get
            {
                return ((ICollectionView)this.DataView).Culture;
            }
        }

        /// <summary>
        /// Gets the entities resulting from the last load operation, as an <see cref="IEnumerable"/>.
        /// </summary>
        /// <remarks>
        /// Primarily used when binding other controls to the results of the load; for programmatic
        /// access to the loaded entities, see the <see cref="DataView"/> property.
        /// </remarks>
        /// <seealso cref="DataView"/>
        public IEnumerable Data
        {
            get
            {
                return (IEnumerable)GetValue(DataProperty);
            }
            private set
            {
                if (this.Data != value)
                {
                    this.SetValueNoCallback(DataProperty, value);
                }
            }
        }

        /// <summary>
        /// Gets the current view of entities resulting from the last load operation, using a
        /// <see cref="DomainDataSourceView"/>.
        /// </summary>
        /// <remarks>
        /// The entities returned from this view are the same as those returned from the
        /// <see cref="Data"/> property.
        /// </remarks>
        public DomainDataSourceView DataView
        {
            get
            {
                return (DomainDataSourceView)GetValue(DataViewProperty);
            }
            private set
            {
                if (this.DataView != value)
                {
                    this.SetValueNoCallback(DataViewProperty, value);
                }
            }
        }

        /// <summary>
        /// Gets or sets the data to use at design-time. DesignData
        /// will accept an <see cref="IEnumerable{T}"/> for any
        /// <see cref="Entity"/> type and, at design-time, provide
        /// that value to the <see cref="Data"/> property.
        /// </summary>
        /// <remarks>
        /// This property allows for support of design-time sample data
        /// and it also enhances the design-time support of the
        /// <see cref="DomainDataSource"/> control.
        /// </remarks>
        [Browsable(false)]
        [Bindable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public IEnumerable DesignData
        {
            get
            {
                return (IEnumerable)GetValue(DesignDataProperty);
            }
            set
            {
                this.SetValue(DesignDataProperty, value);
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="OpenRiaServices.DomainServices.Client.DomainContext"/>
        /// instance used for executing the load and submit operations.
        /// </summary>
        public DomainContext DomainContext
        {
            get
            {
                return (DomainContext)GetValue(DomainContextProperty);
            }
            set
            {
                SetValue(DomainContextProperty, value);
            }
        }

        /// <summary>
        /// Gets the collection of <see cref="FilterDescriptor"/> objects used when performing loads.
        /// </summary>
        public FilterDescriptorCollection FilterDescriptors
        {
            get { return (FilterDescriptorCollection)this.GetValue(DomainDataSource.FilterDescriptorsProperty); }
            private set { this.SetValueNoCallback(DomainDataSource.FilterDescriptorsProperty, value); }
        }

        /// <summary>
        /// Gets or sets the logical operator used for combinining <see cref="FilterDescriptors"/> in the filters collection.
        /// <para>The default value is <see cref="FilterDescriptorLogicalOperator.And"/>.</para>
        /// </summary>
        public FilterDescriptorLogicalOperator FilterOperator
        {
            get { return (FilterDescriptorLogicalOperator)this.GetValue(DomainDataSource.FilterOperatorProperty); }
            set { this.SetValue(DomainDataSource.FilterOperatorProperty, value); }
        }

        /// <summary>
        /// Gets the collection of <see cref="GroupDescriptor"/> objects used to organize the loaded entities into groups.
        /// </summary>
        /// <remarks>
        /// When a <see cref="GroupDescriptor"/> is applied, the data will inherently be sorted by the grouped property.
        /// To force a grouped property to be sorted in <see cref="ListSortDirection.Descending"/> order, add a
        /// <see cref="SortDescriptor"/> to the <see cref="SortDescriptors"/> collection for that property using the
        /// <see cref="ListSortDirection.Descending"/> direction.
        /// </remarks>
        public GroupDescriptorCollection GroupDescriptors
        {
            get { return (GroupDescriptorCollection)this.GetValue(DomainDataSource.GroupDescriptorsProperty); }
            private set { this.SetValueNoCallback(DomainDataSource.GroupDescriptorsProperty, value); }
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="DomainContext"/> currently has any entities with
        /// pending changes.
        /// </summary>
        /// <remarks>
        /// <see cref="HasChanges"/> is <c>true</c> whenever any <see cref="Entity"/> within the <see cref="DomainContext"/>
        /// has changes, regardless of whether or not the entity was loaded through this <see cref="DomainDataSource"/>.
        /// </remarks>
        public bool HasChanges
        {
            get
            {
                return (bool)GetValue(HasChangesProperty);
            }
            private set
            {
                if (this.HasChanges != value)
                {
                    this.SetValueNoCallback(HasChangesProperty, value);
                    this.ApplyState(true /*animate*/);

                    // CanLoad is false whenever HasChanges or IsBusy is true
                    this.UpdateCanLoadProperty();
                    this.UpdateCanRejectChangesProperty();
                    this.UpdateCanSubmitChangesProperty();
                }
            }
        }

        /// <summary>
        /// Gets the type of load operation required given the
        /// <see cref="PageSize"/> and <see cref="LoadSize"/> values.
        /// </summary>
        private LoadType InitialLoadType
        {
            get
            {
                if (this.PageSize == 0)
                {
                    if (this.LoadSize == 0)
                    {
                        // Get all items at once
                        return LoadType.LoadAll;
                    }

                    // Progressive load situation
                    return LoadType.LoadFirstItems;
                }

                // Paging is turned on, get the first pages
                return LoadType.LoadFirstPages;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="DomainDataSource"/> is
        /// busy, either loading data or submitting changes.
        /// </summary>
        /// <seealso cref="IsLoadingData"/>
        /// <seealso cref="IsSubmittingChanges"/>
        public bool IsBusy
        {
            get
            {
                return (bool)GetValue(IsBusyProperty);
            }
            private set
            {
                if (this.IsBusy != value)
                {
                    this.SetValueNoCallback(IsBusyProperty, value);
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="DomainDataSource"/> is currently loading data.
        /// </summary>
        public bool IsLoadingData
        {
            // The returned value is independent on whether the DomainContext
            // is actually loading data because of an external Load call.
            get
            {
                return (bool)GetValue(IsLoadingDataProperty);
            }
            private set
            {
                if (this.IsLoadingData != value)
                {
                    this.SetValueNoCallback(IsLoadingDataProperty, value);
                    this.ApplyState(true /*animate*/);
                    this.IsBusy = this.IsLoadingData || this.IsSubmittingChanges;
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="DomainDataSource"/> is currently
        /// submitting changes as a result of a call to <see cref="SubmitChanges"/>.
        /// </summary>
        public bool IsSubmittingChanges
        {
            // Returned value is independent on whether the DomainContext
            // is actually submitting data because of an external SubmitChanges call.
            get
            {
                return (bool)GetValue(IsSubmittingChangesProperty);
            }
            private set
            {
                if (this.IsSubmittingChanges != value)
                {
                    this.SetValueNoCallback(IsSubmittingChangesProperty, value);
                    this.ApplyState(true /*animate*/);
                    this.IsBusy = this.IsLoadingData || this.IsSubmittingChanges;

                    // CanLoad is false whenever HasChanges or IsSubmittingChanges is true
                    this.UpdateCanLoadProperty();
                    this.UpdateCanSubmitChangesProperty();
                }
            }
        }

        /// <summary>
        /// Gets or sets the delay between the time a change that prompts an automatic load occurs
        /// and the time the subsequent <see cref="Load"/> is invoked.
        /// </summary>
        /// <remarks>
        /// Multiple changes that occur within the specified time span are aggregated into a single
        /// <see cref="Load"/> operation. For every change that occurs, the delay timer is reset.
        /// This allows many changes to be combined into a single call as long as each change occurs
        /// within the specified delay from the last. Once the delay timer is allowed to elapse
        /// without a change occurring, <see cref="Load"/> will be invoked.
        /// </remarks>
        /// <seealso cref="AutoLoad"/>
        public TimeSpan LoadDelay
        {
            get
            {
                return (TimeSpan)GetValue(LoadDelayProperty);
            }
            set
            {
                SetValue(LoadDelayProperty, value);
            }
        }

        /// <summary>
        /// Gets or sets the pause duration between two successive load operations in 
        /// progressive loads.
        /// </summary>
        /// <remarks>
        /// Progressive loads allow entities to be loaded in the background when
        /// a <see cref="LoadSize"/> is specified, and <see cref="PageSize"/>
        /// is set to <c>0</c>.
        /// </remarks>
        /// <seealso cref="LoadSize"/>
        public TimeSpan LoadInterval
        {
            get
            {
                return (TimeSpan)GetValue(LoadIntervalProperty);
            }
            set
            {
                SetValue(LoadIntervalProperty, value);
            }
        }

        /// <summary>
        /// Gets or sets the maximum number of items to load each time a <see cref="Load"/> is executed.
        /// When equal to <c>0</c>, all requested entities will be loaded.
        /// </summary>
        /// <remarks>
        /// When <see cref="PageSize"/> and <see cref="LoadSize"/> are both non-zero, entities will be
        /// loaded using the multiple of <see cref="PageSize"/> nearest <see cref="LoadSize"/>, allowing
        /// multiple pages to be loaded at once without loading partial pages.
        /// </remarks>
        /// <seealso cref="LoadInterval"/>
        /// <seealso cref="PageSize"/>
        public int LoadSize
        {
            get
            {
                return (int)GetValue(LoadSizeProperty);
            }
            set
            {
                SetValue(LoadSizeProperty, value);
            }
        }

        /// <summary>
        /// Gets or sets a timer used to aggregate auto-load requests before loading.
        /// </summary>
        /// <seealso cref="LoadDelay"/>
        /// <seealso cref="AutoLoad"/>
        internal ITimer LoadTimer
        {
            get
            {
                return this._loadTimer;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                if (this._loadTimer != value)
                {
                    if (this._loadTimer != null)
                    {
                        this._loadTimer.Tick -= this.LoadTimer_Tick;
                    }

                    this._loadTimer = value;
                    this._loadTimer.Interval = this.LoadDelay;
                    this._loadTimer.Tick += this.LoadTimer_Tick;
                }
            }
        }

        /// <summary>
        /// Event handler for when a property changes on the <see cref="PagedEntityCollection"/>
        /// that is used for the <see cref="PagedEntityCollectionView"/>.
        /// </summary>
        /// <remarks>
        /// The <see cref="PageSize"/> property can be changed through the <see cref="DataView"/>,
        /// and the change is relayed to the <see cref="DomainDataSource"/> through the
        /// <see cref="PagedEntityCollection"/>.
        /// </remarks>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void PagedEntityCollection_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "PageSize":
                    this.PageSize = this._internalEntityCollection.PageSize;
                    break;
            }
        }

        /// <summary>
        /// Gets or sets the number of items displayed on each page of the view
        /// returned from <see cref="Data"/> and <see cref="DataView"/>, or
        /// <c>0</c> to disable paging.
        /// <para>
        /// A non-zero page size will cause the number of entities loaded with each
        /// <see cref="Load"/> operation to be limited as well, using server-side paging.
        /// </para>
        /// </summary>
        /// <remarks>
        /// When <see cref="PageSize"/> and <see cref="LoadSize"/> are both non-zero, entities will be
        /// loaded using the multiple of <see cref="PageSize"/> nearest <see cref="LoadSize"/>, allowing
        /// multiple pages to be loaded at once without loading partial pages.
        /// </remarks>
        /// <seealso cref="LoadSize"/>
        public int PageSize
        {
            get
            {
                return (int)GetValue(PageSizeProperty);
            }
            set
            {
                SetValue(PageSizeProperty, value);
            }
        }

        /// <summary>
        /// Gets or sets a timer used to progressively load data.
        /// </summary>
        internal ITimer ProgressiveLoadTimer
        {
            get
            {
                return this._progressiveLoadTimer;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                if (this._progressiveLoadTimer != value)
                {
                    if (this._progressiveLoadTimer != null)
                    {
                        this._progressiveLoadTimer.Tick -= this.ProgressiveLoadTimer_Tick;
                    }

                    this._progressiveLoadTimer = value;
                    this._progressiveLoadTimer.Interval = this.LoadInterval;
                    this._progressiveLoadTimer.Tick += this.ProgressiveLoadTimer_Tick;
                }
            }
        }

        /// <summary>
        /// Gets or sets the name of the query to use for loading.
        /// </summary>
        /// <remarks>
        /// The <see cref="DomainContext"/> will be searched for a method
        /// that returns an <see cref="EntityQuery{T}"/>, with a name
        /// matching what is provided to <see cref="QueryName"/>, with or
        /// without a "Query" suffix.
        /// </remarks>
        public string QueryName
        {
            get
            {
                return (string)this.GetValue(DomainDataSource.QueryNameProperty);
            }
            set
            {
                this.SetValue(DomainDataSource.QueryNameProperty, value);
            }
        }

        /// <summary>
        /// Gets the collection of <see cref="Parameter"/> objects representing arguments of the
        /// <see cref="EntityQuery{T}"/> referenced by <see cref="QueryName"/>.
        /// </summary>
        public ParameterCollection QueryParameters
        {
            get { return (ParameterCollection)this.GetValue(DomainDataSource.QueryParametersProperty); }
            private set { this.SetValueNoCallback(DomainDataSource.QueryParametersProperty, value); }
        }

        /// <summary>
        /// Gets or sets the interval between automatic <see cref="Load"/> operations, to refresh
        /// the data with any changes that may have occurred on the server.
        /// </summary>
        /// <remarks>
        /// When a non-zero <see cref="TimeSpan"/> is specified, a <see cref="Load"/> operation will automatically
        /// be invoked each time this interval elapses, as long as <see cref="CanLoad"/> is <c>true</c>.
        /// <para>
        /// As soon as this interval is set, a timer will start, regardless of the <see cref="AutoLoad"/>
        /// property or whether or not a <see cref="Load"/> has previously been executed.
        /// </para>
        /// </remarks>
        public TimeSpan RefreshInterval
        {
            get
            {
                return (TimeSpan)GetValue(RefreshIntervalProperty);
            }
            set
            {
                SetValue(RefreshIntervalProperty, value);
            }
        }

        /// <summary>
        /// Gets or sets the timer to use for the <see cref="RefreshInterval"/>.
        /// </summary>
        internal ITimer RefreshLoadTimer
        {
            get
            {
                return this._refreshLoadTimer;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                if (this._refreshLoadTimer != value)
                {
                    if (this._refreshLoadTimer != null)
                    {
                        this._refreshLoadTimer.Tick -= this.RefreshLoadTimer_Tick;
                    }

                    this._refreshLoadTimer = value;
                    this._refreshLoadTimer.Interval = this.RefreshInterval;
                    this._refreshLoadTimer.Tick += this.RefreshLoadTimer_Tick;
                }
            }
        }

        /// <summary>
        /// Gets the collection of <see cref="SortDescriptor"/> objects used to sort the data.
        /// </summary>
        /// <remarks>
        /// During a <see cref="Load"/> operation, the <see cref="SortDescriptors"/> will be used
        /// to perform server-side sorting. The specified sorting will also be used as changes
        /// are made to the loaded entities, with the <see cref="Data"/> and <see cref="DataView"/>
        /// results reflecting the changes.
        /// </remarks>
        public SortDescriptorCollection SortDescriptors
        {
            get { return (SortDescriptorCollection)this.GetValue(DomainDataSource.SortDescriptorsProperty); }
            private set { this.SetValueNoCallback(DomainDataSource.SortDescriptorsProperty, value); }
        }

        /// <summary>
        /// Gets a <see cref="PropertyChangedNotifier"/> that raises property changed events for the
        /// command properties.
        /// </summary>
        /// <remarks>
        /// This raises events for the <see cref="CanLoad"/>, <see cref="CanRejectChanges"/>, and
        /// <see cref="CanSubmitChanges"/> properties used by the <see cref="LoadCommand"/>,
        /// <see cref="RejectChangesCommand"/>, and <see cref="SubmitChangesCommand"/> respectively.
        /// </remarks>
        internal PropertyChangedNotifier CommandPropertyNotifier
        {
            get { return this._commandPropertyNotifier; }
        }

        /// <summary>
        /// Gets an <see cref="ICommand"/> that invokes <see cref="Load"/> on this <see cref="DomainDataSource"/>.
        /// </summary>
        /// <remarks>
        /// The <see cref="ICommand.CanExecute"/> method for this command returns the value of <see cref="CanLoad"/>.
        /// </remarks>
        public ICommand LoadCommand
        {
            get { return this._loadCommand; }
        }

        /// <summary>
        /// Gets an <see cref="ICommand"/> that invokes <see cref="RejectChanges"/> on this <see cref="DomainDataSource"/>.
        /// </summary>
        public ICommand RejectChangesCommand
        {
            get { return this._rejectChangesCommand; }
        }

        /// <summary>
        /// Gets an <see cref="ICommand"/> that invokes <see cref="SubmitChanges"/> on this <see cref="DomainDataSource"/>.
        /// </summary>
        public ICommand SubmitChangesCommand
        {
            get { return this._submitChangesCommand; }
        }

        #endregion Properties

        #region Static Methods

        /// <summary>
        /// Called when <see cref="AutoLoad"/> is changed.
        /// </summary>
        /// <param name="depObj">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private static void AutoLoadPropertyChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            DomainDataSource dds = (DomainDataSource)depObj;
            if (!dds.AutoLoad)
            {
                dds._shouldExecuteInitialAutoLoad = false;

                if (dds.LoadTimer != null)
                {
                    dds.LoadTimer.Stop();
                }
            }
            dds.RequestAutoLoad();
        }

        /// <summary>
        /// Called when <see cref="CanLoad"/> is changed. An exception will be thrown if the property value is changed,
        /// as the underlying property is read-only.
        /// </summary>
        /// <param name="depObj">The event sender.</param>
        /// <param name="e">The event arguments describing the changes to the <see cref="CanLoadProperty"/>.</param>
        /// <exception cref="InvalidOperationException">When the value is changed. The underlying property is read-only.</exception>
        private static void CanLoadPropertyChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            DomainDataSource.ReadOnlyPropertyChanged(depObj, e, "CanLoad");

            DomainDataSource dds = (DomainDataSource)depObj;
            dds.CommandPropertyNotifier.RaisePropertyChanged("CanLoad");
        }

        /// <summary>
        /// Analyze the status of determining the entity query, conditionally returning an exception that should be thrown
        /// by the caller. Scenarios that don't justify an exception will return <c>null</c>.
        /// </summary>
        /// <param name="methodAccessStatus">The status of the query method discovery.</param>
        /// <param name="domainContextType">The type of the domain context being used.</param>
        /// <param name="queryName">The name of the query to be used.</param>
        /// <param name="entityType">The type of the entity returned from the specified query.</param>
        /// <exception cref="ArgumentException">Thrown when the query parameters have an invalid parameter for the query.</exception>
        private static void CheckEntityQueryInformation(MethodAccessStatus methodAccessStatus, Type domainContextType, string queryName, Type entityType)
        {
            switch (methodAccessStatus)
            {
                case MethodAccessStatus.NameNotFound:
                    {
                        Debug.Assert(domainContextType != null, "Unexpected null value domainContextType");
                        Debug.Assert(queryName != null, "Unexpected null value queryName");
                        throw new ArgumentException(string.Format(
                            CultureInfo.InvariantCulture,
                            DomainDataSourceResources.MemberNotFound,
                            domainContextType.GetTypeName(),
                            DomainDataSourceResources.Method,
                            queryName));
                    }


                case MethodAccessStatus.EntitySetNotFound:
                    {
                        Debug.Assert(domainContextType != null, "Unexpected null value domainContextType");
                        Debug.Assert(queryName != null, "Unexpected null value queryName");
                        throw new ArgumentException(string.Format(
                            CultureInfo.InvariantCulture,
                            DomainDataSourceResources.NoEntitySetMember,
                            entityType.GetTypeName()));
                    }

                case MethodAccessStatus.ArgumentSubset:
                    {
                        // Don't raise an error because we don't know
                        // that all of the query parameters have been configured.
                        break;
                    }
                case MethodAccessStatus.ArgumentMismatch:
                    {
                        Debug.Assert(domainContextType != null, "Unexpected null value domainContextType");
                        Debug.Assert(queryName != null, "Unexpected null value queryName");
                        throw new ArgumentException(string.Format(
                            CultureInfo.InvariantCulture,
                            DomainDataSourceResources.EntityQueryMethodHasMismatchedArguments,
                            queryName));
                    }
            }
        }

        /// <summary>
        /// Called when <see cref="Data"/> is changed. An exception will be thrown if the property value is changed at runtime,
        /// as the underlying property is read-only.
        /// </summary>
        /// <remarks>
        /// At design-time, we allow <see cref="Data"/> to be changed, to allow the binding from <see cref="DesignData"/>
        /// to flow through, and we move currency to the first item.
        /// </remarks>
        /// <param name="depObj">The event sender.</param>
        /// <param name="e">The event arguments describing the changes to the <see cref="DataProperty"/>.</param>
        /// <exception cref="InvalidOperationException">When the value is changed at run-time. The underlying property is read-only.</exception>
        private static void DataPropertyChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            if (!DesignerProperties.IsInDesignTool)
            {
                DomainDataSource.ReadOnlyPropertyChanged(depObj, e, "Data");
            }
            else
            {
                // When design-time data is provided, we move current to first to mimic
                // the behavior of the data being loaded. (We call MoveCurrentToFirst())
                // in DomainContext_Loaded. This enables master-details scenarios with
                // sample data at design-time.
                ICollectionView view = e.NewValue as ICollectionView;

                if (view != null)
                {
                    view.MoveCurrentToFirst();
                }
            }
        }

        /// <summary>
        /// Called when <see cref="DataView"/> is changed. An exception will be thrown if the property value is changed,
        /// as the underlying property is read-only.
        /// </summary>
        /// <param name="depObj">The event sender.</param>
        /// <param name="e">The event arguments describing the changes to the <see cref="DataViewProperty"/>.</param>
        /// <exception cref="InvalidOperationException">When the value is changed. The underlying property is read-only.</exception>
        private static void DataViewPropertyChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            DomainDataSource.ReadOnlyPropertyChanged(depObj, e, "DataView");
        }

        /// <summary>
        /// Called when <see cref="DomainContext"/> is changed.
        /// </summary>
        /// <param name="depObj">The event sender.</param>
        /// <param name="e">The event arguments describing the changes to the <see cref="DomainContextProperty"/>.</param>
        private static void DomainContextPropertyChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            DomainDataSource dds = (DomainDataSource)depObj;
            if (dds != null && !dds.IsHandlerSuspended(e.Property))
            {
                if (e.OldValue != null)
                {
                    dds.SetValueNoCallback(e.Property, e.OldValue);
                    throw new InvalidOperationException(DomainDataSourceResources.DomainContextAlreadySet);
                }

                DomainContext domainContext = e.NewValue as DomainContext;
                Debug.Assert(domainContext != null, "Unexpected null value");

                dds._cachedParameters = null;

                if (dds.CanValidateEntityQuery)
                {
                    bool success = false;

                    try
                    {
                        dds.ValidateEntityQuery();
                        success = true;
                    }
                    finally
                    {
                        if (!success)
                        {
                            dds.SetValueNoCallback(e.Property, e.OldValue);
                        }
                    }
                }

                domainContext.PropertyChanged += dds.DomainContext_PropertyChanged;
                dds.HasChanges = domainContext.HasChanges;
                dds.RequestAutoLoad();
            }
        }

        /// <summary>
        /// Analyze the input parameters and attempt to discover the entity query to be used and the type of
        /// <see cref="Entity"/> that will be loaded, as well as what <see cref="EntitySet"/> will contain
        /// the entities.
        /// </summary>
        /// <param name="domainContext">The <see cref="DomainContext"/> containing the specified query.</param>
        /// <param name="queryName">The name of the query to be invoked.</param>
        /// <param name="queryParameters">The collection of parameters to pass to the query method.</param>
        /// <param name="entityQueryMethodInfo">The query method to be invoked.</param>
        /// <param name="entityType">The type of entity to be loaded by the query.</param>
        /// <param name="entitySet">The entity set containing the entities to be loaded.</param>
        /// <returns>A <see cref="MethodAccessStatus"/> describing the results of the query discovery attempt.</returns>
        private static MethodAccessStatus GetEntityQueryInformation(DomainContext domainContext, string queryName, ParameterCollection queryParameters,
            out MethodInfo entityQueryMethodInfo, out Type entityType, out EntitySet entitySet)
        {
            entityQueryMethodInfo = null;
            entityType = null;
            entitySet = null;

            Debug.Assert(domainContext != null, "Unexpected null domainContext");

            // Examples:
            // public EntityQuery<Product> LoadProductsQuery()
            // public EntityQuery<Product> LoadProductsByClassAndStyleQuery(string classFilter, string styleFilter)

            // Find methods with the correct name that return EntityQuery<T> where T : Entity
            // and return a KeyValuePair of the MethodInfo and the Entity type

            // We'll look for an entity query suffixed with "Query" in addition to the name provided
            string suffixedQueryName = queryName + QueryNameSuffix;

            // Find methods with a name matching either the supplied name or the suffixed name
            IEnumerable<KeyValuePair<MethodInfo, Type>> methodTypes
                = from method in domainContext.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
                  where string.Equals(method.Name, suffixedQueryName, StringComparison.Ordinal)
                  || string.Equals(method.Name, queryName, StringComparison.Ordinal)
                  let entityQueryEntityType = GetEntityQueryEntityType(method.ReturnType)
                  where entityQueryEntityType != null
                  select new KeyValuePair<MethodInfo, Type>(method, entityQueryEntityType);

            if (!methodTypes.Any())
            {
                return MethodAccessStatus.NameNotFound;
            }

            // Check the methods with a matching name and filter to the ones that have the
            // correct parameter count and where each parameter has a single matching query parameter
            KeyValuePair<MethodInfo, Type>[] overloads
                = (from methodType in methodTypes
                   where MethodParametersMatchQueryParameters(methodType.Key, queryParameters)
                   select methodType).ToArray();

            // If we have multiple matches, then filter down to only the ones that us the suffix
            // since we prefer the suffixed names.
            if (overloads.Length > 1)
            {
                overloads = overloads.Where(o => o.Key.Name.Equals(suffixedQueryName, StringComparison.Ordinal)).ToArray();
            }

            // We should expect a single match at this point
            if (overloads.Length != 1)
            {
                // There's no exact parameter match. Determine if the query parameters are a valid
                // subset of the method parameters for any of the methods.
                if (methodTypes.Any(mt => CouldMethodParametersMatchQueryParameters(mt.Key, queryParameters)))
                {
                    return MethodAccessStatus.ArgumentSubset;
                }
                else
                {
                    return MethodAccessStatus.ArgumentMismatch;
                }
            }

            // Get the output variables
            KeyValuePair<MethodInfo, Type> match = overloads[0];

            // Grab the entity type from the EntityQuery<T> found
            entityType = match.Value;

            // Discover the EntitySet<T> that will hold entities of this type (this respects inheritance)
            if (domainContext.EntityContainer.TryGetEntitySet(entityType, out entitySet))
            {
                entityQueryMethodInfo = match.Key;
                return MethodAccessStatus.Success;
            }

            return MethodAccessStatus.EntitySetNotFound;
        }

        /// <summary>
        /// Extracts the <see cref="Type"/> T from a <paramref name="type"/> of <see cref="EntityQuery{T}"/> of T,
        /// where T derives from <see cref="Entity"/>.
        /// </summary>
        /// <param name="type">The <see cref="EntityQuery{T}"/> type.</param>
        /// <returns>The <see cref="Type"/> of T when the type is <see cref="EntityQuery{T}"/> of T and
        /// T derives from <see cref="Entity"/>, otherwise <c>null</c>.</returns>
        private static Type GetEntityQueryEntityType(Type type)
        {
            // Look for EntityQuery<T>
            if (type.IsGenericType && typeof(EntityQuery).IsAssignableFrom(type))
            {
                Type[] genericArguments = type.GetGenericArguments();

                if (genericArguments.Count() == 1)
                {
                    // Extract T
                    Type argumentType = genericArguments.First();

                    // Ensure T : Entity
                    if (typeof(Entity).IsAssignableFrom(argumentType))
                    {
                        return argumentType;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Determine if a method's parameter names match the query parameter names of a <see cref="ParameterCollection" />.
        /// </summary>
        /// <param name="method">The <see cref="MethodInfo" /> whose parameters to check.</param>
        /// <param name="queryParameters">The <see cref="ParameterCollection" /> to check against.</param>
        /// <returns><c>true</c> when every parameter name is matched up, otherwise <c>false</c>.</returns>
        private static bool MethodParametersMatchQueryParameters(MethodInfo method, ParameterCollection queryParameters)
        {
            ParameterInfo[] methodParameters = method.GetParameters();

            if (methodParameters.Length != queryParameters.Count)
            {
                return false;
            }

            var parameterMatches = from methodParam in methodParameters
                                   where queryParameters.Where(queryParam => queryParam.ParameterName == methodParam.Name).Count() == 1
                                   select methodParam;

            return (parameterMatches.Count() == queryParameters.Count);
        }

        /// <summary>
        /// Clear all data from the <see cref="DomainDataSource"/> and the underlying <see cref="EntitySet"/>
        /// on the <see cref="DomainContext"/>.
        /// </summary>
        public void Clear()
        {
            this._internalEntityCollection.ClearPageTracking(true);
            this._internalEntityCollection.BackingEntitySet.Clear();
        }

        /// <summary>
        /// Determine if a method's parameter names match a subset of the query parameter names of a <see cref="ParameterCollection" />.
        /// </summary>
        /// <param name="method">The <see cref="MethodInfo" /> whose parameters to check.</param>
        /// <param name="queryParameters">The <see cref="ParameterCollection" /> to check against.</param>
        /// <returns><c>true</c> when every query parameter name is matched up, otherwise <c>false</c>.</returns>
        private static bool CouldMethodParametersMatchQueryParameters(MethodInfo method, ParameterCollection queryParameters)
        {
            ParameterInfo[] methodParameters = method.GetParameters();
            // Each query parameter must match a method parameter
            return queryParameters.All(qp => methodParameters.Any(mp => mp.Name == qp.ParameterName));
        }

        /// <summary>
        /// Determine the actual load size to be used for a query invocation. This accounts for mismatches
        /// between <see cref="PageSize"/> and <see cref="LoadSize"/> ensuring that the load size used is
        /// a multiple of the <see cref="PageSize"/>.
        /// </summary>
        /// <param name="pageSize">The page size being used.</param>
        /// <param name="loadSize">The load size being used.</param>
        /// <returns>The actual load size to be used for the specified scenario.</returns>
        private static int GetLoadSizeCeiling(int pageSize, int loadSize)
        {
            int loadSizeCeiling = Math.Max(pageSize, loadSize);
            int remainder = loadSizeCeiling % pageSize;
            if (remainder != 0)
            {
                Debug.Assert(pageSize - remainder > 0, "Unexpected loadSize increment");
                loadSizeCeiling += pageSize - remainder;
            }
            return loadSizeCeiling;
        }

        /// <summary>
        /// Called when <see cref="FilterDescriptors"/> is changed. An exception will be thrown if the property value is changed,
        /// as the underlying property is read-only.
        /// </summary>
        /// <param name="depObj">The event sender.</param>
        /// <param name="e">The event arguments describing the changes to the <see cref="FilterDescriptorsProperty"/>.</param>
        /// <exception cref="InvalidOperationException">When the value is changed. The underlying property is read-only.</exception>
        private static void FilterDescriptorsPropertyChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            DomainDataSource.ReadOnlyPropertyChanged(depObj, e, "FilterDescriptors");
        }

        /// <summary>
        /// Called when <see cref="FilterOperator"/> is changed.
        /// </summary>
        /// <param name="depObj">The event sender.</param>
        /// <param name="e">The event arguments describing the changes to the <see cref="FilterOperatorProperty"/>.</param>
        private static void FilterOperatorPropertyChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            DomainDataSource dds = (DomainDataSource)depObj;
            dds.HandleManagerPropertyChanged_Filter(depObj, new EventArgs());
        }

        /// <summary>
        /// Called when <see cref="GroupDescriptors"/> is changed. An exception will be thrown if the property value is changed,
        /// as the underlying property is read-only.
        /// </summary>
        /// <param name="depObj">The event sender.</param>
        /// <param name="e">The event arguments describing the changes to the <see cref="GroupDescriptorsProperty"/>.</param>
        /// <exception cref="InvalidOperationException">When the value is changed. The underlying property is read-only.</exception>
        private static void GroupDescriptorsPropertyChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            DomainDataSource.ReadOnlyPropertyChanged(depObj, e, "GroupDescriptors");
        }

        /// <summary>
        /// Called when <see cref="HasChanges"/> is changed. An exception will be thrown if the property value is changed,
        /// as the underlying property is read-only.
        /// </summary>
        /// <param name="depObj">The event sender.</param>
        /// <param name="e">The event arguments describing the changes to the <see cref="HasChangesProperty"/>.</param>
        /// <exception cref="InvalidOperationException">When the value is changed. The underlying property is read-only.</exception>
        private static void HasChangesPropertyChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            DomainDataSource.ReadOnlyPropertyChanged(depObj, e, "HasChanges");
        }

        /// <summary>
        /// Called when <see cref="IsBusy"/> is changed. An exception will be thrown if the property value is changed,
        /// as the underlying property is read-only.
        /// </summary>
        /// <param name="depObj">The event sender.</param>
        /// <param name="e">The event arguments describing the changes to the <see cref="IsBusyProperty"/>.</param>
        /// <exception cref="InvalidOperationException">When the value is changed. The underlying property is read-only.</exception>
        private static void IsBusyPropertyChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            DomainDataSource.ReadOnlyPropertyChanged(depObj, e, "IsBusy");
        }

        /// <summary>
        /// Called when <see cref="IsLoadingData"/> is changed. An exception will be thrown if the property value is changed,
        /// as the underlying property is read-only.
        /// </summary>
        /// <param name="depObj">The event sender.</param>
        /// <param name="e">The event arguments describing the changes to the <see cref="IsLoadingDataProperty"/>.</param>
        /// <exception cref="InvalidOperationException">When the value is changed. The underlying property is read-only.</exception>
        private static void IsLoadingDataPropertyChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            DomainDataSource.ReadOnlyPropertyChanged(depObj, e, "IsLoadingData");
        }

        /// <summary>
        /// Called when <see cref="IsSubmittingChanges"/> is changed. An exception will be thrown if the property value is changed,
        /// as the underlying property is read-only.
        /// </summary>
        /// <param name="depObj">The event sender.</param>
        /// <param name="e">The event arguments describing the changes to the <see cref="IsSubmittingChangesProperty"/>.</param>
        /// <exception cref="InvalidOperationException">When the value is changed. The underlying property is read-only.</exception>
        private static void IsSubmittingChangesPropertyChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            DomainDataSource.ReadOnlyPropertyChanged(depObj, e, "IsSubmittingChanges");

            DomainDataSource dds = (DomainDataSource)depObj;
            dds.CommandPropertyNotifier.RaisePropertyChanged("IsSubmittingChanges");
        }

        /// <summary>
        /// Called when <see cref="LoadDelay"/> is changed.
        /// </summary>
        /// <param name="depObj">The event sender.</param>
        /// <param name="e">The event arguments describing the changes to the <see cref="LoadDelayProperty"/>.</param>
        private static void LoadDelayPropertyChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            DomainDataSource dds = (DomainDataSource)depObj;
            if (dds != null && !dds.IsHandlerSuspended(e.Property))
            {
                TimeSpan newDelay = (TimeSpan)e.NewValue;
                if (newDelay.Ticks <= 0)
                {
                    dds.SetValueNoCallback(e.Property, e.OldValue);
                    throw new ArgumentOutOfRangeException(
                        "value",
                        string.Format(
                            CultureInfo.InvariantCulture,
                            DomainDataSourceResources.InvalidTimeSpan,
                            "LoadDelay",
                            DomainDataSourceResources.StrictlyPositive));
                }

                if (dds.LoadTimer != null)
                {
                    dds.LoadTimer.Interval = newDelay;
                }
            }
        }

        /// <summary>
        /// Called when <see cref="LoadInterval"/> is changed.
        /// </summary>
        /// <param name="depObj">The event sender.</param>
        /// <param name="e">The event arguments describing the changes to the <see cref="LoadIntervalProperty"/>.</param>
        private static void LoadIntervalPropertyChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            DomainDataSource dds = (DomainDataSource)depObj;
            if (dds != null && !dds.IsHandlerSuspended(e.Property))
            {
                TimeSpan newInterval = (TimeSpan)e.NewValue;
                if (newInterval.Ticks <= 0)
                {
                    dds.SetValueNoCallback(e.Property, e.OldValue);
                    throw new ArgumentOutOfRangeException(
                        "value",
                        string.Format(
                            CultureInfo.InvariantCulture,
                            DomainDataSourceResources.InvalidTimeSpan,
                            "LoadInterval",
                            DomainDataSourceResources.StrictlyPositive));
                }

                if (dds.ProgressiveLoadTimer != null)
                {
                    dds.ProgressiveLoadTimer.Interval = newInterval;
                }
            }
        }

        /// <summary>
        /// Called when <see cref="LoadSize"/> is changed.
        /// </summary>
        /// <param name="depObj">The event sender.</param>
        /// <param name="e">The event arguments describing the changes to the <see cref="LoadSizeProperty"/>.</param>
        private static void LoadSizePropertyChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            DomainDataSource dds = (DomainDataSource)depObj;
            if (dds != null && !dds.IsHandlerSuspended(e.Property))
            {
                int newLoadSize = (int)e.NewValue;
                if (newLoadSize < 0)
                {
                    dds.SetValueNoCallback(e.Property, e.OldValue);
                    throw new ArgumentOutOfRangeException(
                        "value",
                        string.Format(
                            CultureInfo.InvariantCulture,
                            DomainDataSourceResources.ValueMustBeGreaterThanOrEqualTo,
                            "LoadSize",
                            0));
                }

                if (dds._preparingOperation)
                {
                    dds.SetValueNoCallback(e.Property, e.OldValue);
                    throw new InvalidOperationException(DomainDataSourceResources.InvalidOperationDuringLoadOrSubmit);
                }

                if (!dds.CanLoad)
                {
                    dds.SetValueNoCallback(e.Property, e.OldValue);
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, DomainDataSourceResources.CannotLoadWhenCanLoadIsFalse_PropertySetter, "LoadSize"));
                }

                if (dds.PageSize > 0)
                {
                    dds.RequestAutoLoad();
                }
            }
        }

        /// <summary>
        /// Called when <see cref="PageSize"/> is changed.
        /// </summary>
        /// <param name="depObj">The event sender.</param>
        /// <param name="e">The event arguments describing the changes to the <see cref="PageSizeProperty"/>.</param>
        private static void PageSizePropertyChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            DomainDataSource dds = (DomainDataSource)depObj;
            if (dds != null && !dds.IsHandlerSuspended(e.Property))
            {
                int newPageSize = (int)e.NewValue;
                if (newPageSize < 0)
                {
                    dds.SetValueNoCallback(e.Property, e.OldValue);
                    throw new ArgumentOutOfRangeException(
                        "value",
                        string.Format(
                            CultureInfo.InvariantCulture,
                            DomainDataSourceResources.ValueMustBeGreaterThanOrEqualTo,
                            "PageSize",
                            0));
                }

                if (dds._preparingOperation)
                {
                    dds.SetValueNoCallback(e.Property, e.OldValue);
                    throw new InvalidOperationException(DomainDataSourceResources.InvalidOperationDuringLoadOrSubmit);
                }

                if (!dds.CanLoad)
                {
                    dds.SetValueNoCallback(e.Property, e.OldValue);
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, DomainDataSourceResources.CannotLoadWhenCanLoadIsFalse_PropertySetter, "PageSize"));
                }

                // Set the PageSize on the PagedEntityCollectionView
                dds._internalEntityCollectionView.PageSize = newPageSize;

                dds.RequestAutoLoad();
            }
        }

        /// <summary>
        /// Called when <see cref="QueryName"/> is changed.
        /// </summary>
        /// <param name="depObj">The event sender.</param>
        /// <param name="e">The event arguments describing the changes to the <see cref="QueryNameProperty"/>.</param>
        private static void QueryNamePropertyChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            DomainDataSource dds = (DomainDataSource)depObj;
            if (dds != null && !dds.IsHandlerSuspended(e.Property))
            {
                if (dds.IsLoadingData || dds._preparingOperation)
                {
                    dds.SetValueNoCallback(DomainDataSource.QueryNameProperty, e.OldValue);
                    throw new InvalidOperationException(DomainDataSourceResources.InvalidOperationDuringLoadOrSubmit);
                }

                if (!dds.CanLoad)
                {
                    dds.SetValueNoCallback(DomainDataSource.QueryNameProperty, e.OldValue);
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, DomainDataSourceResources.CannotLoadWhenCanLoadIsFalse_PropertySetter, "QueryName"));
                }

                dds._cachedParameters = null;

                if (dds.CanValidateEntityQuery)
                {
                    bool success = false;

                    try
                    {
                        dds.ValidateEntityQuery();
                        success = true;
                    }
                    finally
                    {
                        if (!success)
                        {
                            dds.SetValueNoCallback(DomainDataSource.QueryNameProperty, e.OldValue);
                        }
                    }
                }

                dds.RequestAutoLoad();
            }
        }

        /// <summary>
        /// Called when <see cref="QueryParameters"/> is changed. An exception will be thrown if the property value is changed,
        /// as the underlying property is read-only.
        /// </summary>
        /// <param name="depObj">The event sender.</param>
        /// <param name="e">The event arguments describing the changes to the <see cref="QueryParametersProperty"/>.</param>
        /// <exception cref="InvalidOperationException">When the value is changed. The underlying property is read-only.</exception>
        private static void QueryParametersPropertyChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            DomainDataSource.ReadOnlyPropertyChanged(depObj, e, "QueryParameters");
        }

        /// <summary>
        /// A utility method to handle changes to read-only properties. This method will throw
        /// an <see cref="InvalidOperationException"/> if the application attempts to change
        /// the value of the property.
        /// </summary>
        /// <param name="depObj">The event sender.</param>
        /// <param name="e">The event arguments describing the change to the specified property.</param>
        /// <param name="propertyName">The name of the read-only property that changed.</param>
        private static void ReadOnlyPropertyChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e, string propertyName)
        {
            DomainDataSource dds = (DomainDataSource)depObj;

            // If the DDS has suspended the handler for this property, then we allow
            // the change to go through. This allows the DDS itself to modify the
            // property while disallowing application code from changing the value.
            if (dds != null && !dds.IsHandlerSuspended(e.Property))
            {
                dds.SetValueNoCallback(e.Property, e.OldValue);
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        DomainDataSourceResources.UnderlyingPropertyIsReadOnly,
                        propertyName));
            }
        }

        /// <summary>
        /// Called when <see cref="RefreshInterval"/> is changed.
        /// </summary>
        /// <param name="depObj">The event sender.</param>
        /// <param name="e">The event arguments describing the changes to the <see cref="RefreshIntervalProperty"/>.</param>
        /// <exception cref="ArgumentOutOfRangeException">When the specified <see cref="TimeSpan"/> has a negative value.</exception>
        private static void RefreshIntervalPropertyChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            DomainDataSource dds = (DomainDataSource)depObj;
            if (dds != null && !dds.IsHandlerSuspended(e.Property))
            {
                TimeSpan newInterval = (TimeSpan)e.NewValue;
                if (newInterval.Ticks < 0)
                {
                    dds.SetValueNoCallback(e.Property, e.OldValue);
                    throw new ArgumentOutOfRangeException(
                        "value",
                        string.Format(
                            CultureInfo.InvariantCulture,
                            DomainDataSourceResources.InvalidTimeSpan,
                            "RefreshInterval",
                            DomainDataSourceResources.Positive));
                }
                else
                {
                    if (dds.RefreshLoadTimer == null)
                    {
                        dds.RefreshLoadTimer = new Timer();
                    }

                    if (dds.RefreshLoadTimer.IsEnabled)
                    {
                        dds.RefreshLoadTimer.Stop();
                    }

                    dds.RefreshLoadTimer.Interval = dds.RefreshInterval;

                    if (dds.RefreshInterval.Ticks > 0)
                    {
                        dds.RefreshLoadTimer.Start();
                    }
                }
            }
        }

        /// <summary>
        /// Called when <see cref="SortDescriptors"/> is changed. An exception will be thrown if the property value is changed,
        /// as the underlying property is read-only.
        /// </summary>
        /// <param name="depObj">The event sender.</param>
        /// <param name="e">The event arguments describing the changes to the <see cref="SortDescriptorsProperty"/>.</param>
        /// <exception cref="InvalidOperationException">When the value is changed. The underlying property is read-only.</exception>
        private static void SortDescriptorsPropertyChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            DomainDataSource.ReadOnlyPropertyChanged(depObj, e, "SortDescriptors");
        }

        #endregion Static Methods

        #region Methods

        /// <summary>
        /// Apply the current visual state to the control, optionally supporting animation.
        /// </summary>
        /// <param name="animate">Whether or not to support animation in the visual state change.</param>
        private void ApplyState(bool animate)
        {
            // CommonStates
            if (this.IsEnabled ||
                !VisualStateManager.GoToState(this, StateDisabled, animate))
            {
                VisualStateManager.GoToState(this, StateNormal, animate);
            }

            // ActivityStates
            if (this.IsLoadingData)
            {
                if (!VisualStateManager.GoToState(this, StateLoading, animate))
                {
                    VisualStateManager.GoToState(this, StateIdle, animate);
                }
            }
            else if (this.IsSubmittingChanges)
            {
                if (!VisualStateManager.GoToState(this, StateSubmitting, animate))
                {
                    VisualStateManager.GoToState(this, StateIdle, animate);
                }
            }
            else
            {
                VisualStateManager.GoToState(this, StateIdle, animate);
            }

            // ChangeStates
            if (!this.HasChanges ||
                !VisualStateManager.GoToState(this, StateChanged, animate))
            {
                VisualStateManager.GoToState(this, StateUnchanged, animate);
            }
        }

        /// <summary>
        /// Cancels the current load operation performed by this <see cref="DomainDataSource"/>, if any.
        /// </summary>
        /// <remarks>
        /// This method will cancel a <see cref="Load"/> operation already in progress. Operations canceled
        /// using <see cref="CancelLoad"/> will raise a <see cref="LoadedData"/> event when they complete
        /// with the <see cref="CancelEventArgs.Cancel"/> flag set to <c>true</c>. If
        /// <see cref="IsLoadingData"/> is <c>false</c>, this method will not do anything. This
        /// method of cancellation differs slightly from canceling a load via the <see cref="LoadingData"/>
        /// event.
        /// </remarks>
        /// <seealso cref="LoadingData"/>
        public void CancelLoad()
        {
            if (this.DomainContext == null)
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.InvariantCulture,
                    DomainDataSourceResources.OperationNeedsPropertySet,
                    "DomainContext",
                    DomainDataSourceResources.CancelLoadOperation));
            }

            Debug.Assert(this.DomainContext != null, "Unexpected null DomainContext");

            if (this.IsLoadingData)
            {
                this.CancelLoadPrivate();
            }
        }

        /// <summary>
        /// Cancels the current load operation performed by this <see cref="DomainDataSource"/>, if any
        /// </summary>
        private void CancelLoadPrivate()
        {
            Debug.Assert(this.DomainContext != null, "Unexpected null DomainContext");
            Debug.Assert(this.IsLoadingData, "Unexpected false IsLoadingData");
            Debug.Assert(this._currentLoadContext != null, "Unexpected null _currentLoadContext");
            Debug.Assert(this._currentLoadOperation != null, "Unexpected null _currentLoadOperation");

            this._currentLoadOperation.Cancel();
        }

        /// <summary>
        /// Cancels the current submit operation performed by this <see cref="DomainDataSource"/>, if any.
        /// </summary>
        /// <remarks>
        /// This method will cancel a <see cref="SubmitChanges"/> operation already in progress. Operations
        /// canceled using <see cref="CancelSubmit"/> will raise a <see cref="SubmittedChanges"/> event when
        /// they complete with the <see cref="CancelEventArgs.Cancel"/> flag set to <c>true</c>.
        /// If <see cref="IsSubmittingChanges"/> is <c>false</c>, this method will not do anything. This
        /// method of cancellation differs slightly from canceling a submit via the
        /// <see cref="SubmittingChanges"/> event.
        /// <para>
        /// Upon completion of the operation, check the <see cref="OpenRiaServices.DomainServices.Client.OperationBase.IsCanceled"/>
        /// property to determine whether or not the operation was successfully canceled. Note that cancellation
        /// of the operation does not guarantee state changes were prevented from happening on the server.
        /// </para>
        /// </remarks>
        /// <seealso cref="SubmittingChanges"/>
        public void CancelSubmit()
        {
            if (this.DomainContext == null)
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.InvariantCulture,
                    DomainDataSourceResources.OperationNeedsPropertySet,
                    "DomainContext",
                    DomainDataSourceResources.CancelSubmitOperation));
            }

            Debug.Assert(this.DomainContext != null, "Unexpected null DomainContext");

            if (this.IsSubmittingChanges)
            {
                DebugTrace.Trace(SubmitDebug, "Invoking Cancel method");
                this._currentSubmitOperation.Cancel();
            }
        }

        /// <summary>
        /// Checks the validity of a <see cref="FilterDescriptor"/> instance.
        /// </summary>
        /// <param name="filterDescriptor">The descriptor to validate.</param>
        private void CheckFilterDescriptor(FilterDescriptor filterDescriptor)
        {
            if (this.AutoLoad)
            {
                if (string.IsNullOrEmpty(filterDescriptor.PropertyPath))
                {
                    // Prevents the subsequent auto-load if the value is not specified. This occurs at initial
                    // load before a Binding value is set.
                    this._skipNextAutoLoad = true;
                }
                else
                {
                    this.ValidatePropertyPath(filterDescriptor.PropertyPath, "FilterDescriptor", this.FilterDescriptors.IndexOf(filterDescriptor));

                    // Prevents the subsequent auto-load if the value cannot be converted
                    if ((this._entityType != null) && !Object.Equals(filterDescriptor.Value, filterDescriptor.IgnoredValue))
                    {
                        PropertyInfo pi = this._entityType.GetPropertyInfo(filterDescriptor.PropertyPath);

                        try
                        {
                            Utilities.GetConvertedValue(this.Culture, pi.PropertyType, filterDescriptor.Value);
                        }
                        catch (Exception exception)
                        {
                            // If the exception was fatal or any other exception unrelated to conversion,
                            // then throw it as-is.
                            if (exception.IsFatal() || !exception.IsConversionException())
                            {
                                throw;
                            }

                            this._skipNextAutoLoad = true;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Checks the validity of a <see cref="GroupDescriptor"/> instance.
        /// </summary>
        /// <param name="groupDescriptor">The descriptor to validate.</param>
        private void CheckGroupDescriptor(GroupDescriptor groupDescriptor)
        {
            if (this.AutoLoad)
            {
                if (string.IsNullOrEmpty(groupDescriptor.PropertyPath))
                {
                    // Prevents the subsequent auto-load if the value is not specified. This occurs at initial
                    // load before a Binding value is set.
                    this._skipNextAutoLoad = true;
                }
                else
                {
                    this.ValidatePropertyPath(groupDescriptor.PropertyPath, "GroupDescriptor", this.GroupDescriptors.IndexOf(groupDescriptor));
                }
            }
        }

        /// <summary>
        /// Check the validity of a <see cref="Parameter"/> instance.
        /// </summary>
        /// <param name="queryParameter">The parameter to validate.</param>
        private void CheckQueryParameter(Parameter queryParameter)
        {
            // There's a little redundency calling this here, but the _cachedParameters prevent
            // the complex work from being done twice. The benefit is that it simplifies the code.
            this.CheckQueryParameters(this.QueryParameters);

            if (this.AutoLoad)
            {
                if (!string.IsNullOrEmpty(queryParameter.ParameterName))
                {
                    // Prevents the subsequent auto-load if the value cannot be converted
                    if (this._queryMethod != null)
                    {
                        ParameterInfo pi = this._queryMethod.GetParameters().Single(p => p.Name == queryParameter.ParameterName);

                        try
                        {
                            Utilities.GetConvertedValue(this.Culture, pi.ParameterType, queryParameter.Value);
                        }
                        catch (Exception exception)
                        {
                            // If the exception was fatal or any other exception unrelated to conversion,
                            // then throw it as-is.
                            if (exception.IsFatal() || !exception.IsConversionException())
                            {
                                throw;
                            }

                            this._skipNextAutoLoad = true;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Check the validity of a <see cref="ParameterCollection"/> instance.
        /// </summary>
        /// <param name="queryParameters">The parameter collection to validate.</param>
        private void CheckQueryParameters(ParameterCollection queryParameters)
        {
            // Reset the cached parameters if something changed
            if (this._cachedParameters != null)
            {
                IEnumerable<string> parameters = queryParameters.Select(p => p.ParameterName).ToArray();
                if (!this._cachedParameters.SequenceEqual(parameters))
                {
                    this._cachedParameters = null;
                }
            }

            if (this.CanValidateEntityQuery)
            {
                this.ValidateEntityQuery();
            }

            if (this.AutoLoad)
            {
                if (queryParameters.Any(p => string.IsNullOrEmpty(p.ParameterName)))
                {
                    // Prevents the subsequent auto-load if the value is not specified. This occurs at initial
                    // load before a Binding value is set.
                    this._skipNextAutoLoad = true;
                }
            }
        }

        /// <summary>
        /// Checks the validity of a <see cref="SortDescriptor"/> instance.
        /// </summary>
        /// <param name="sortDescriptor">The descriptor to validate.</param>
        private void CheckSortDescriptor(SortDescriptor sortDescriptor)
        {
            if (this.AutoLoad)
            {
                if (string.IsNullOrEmpty(sortDescriptor.PropertyPath))
                {
                    // Prevents the subsequent auto-load if the value is not specified. This occurs at initial
                    // load before a Binding value is set.
                    this._skipNextAutoLoad = true;
                }
                else
                {
                    this.ValidatePropertyPath(sortDescriptor.PropertyPath, "SortDescriptor", this.SortDescriptors.IndexOf(sortDescriptor));
                }
            }
        }

        /// <summary>
        /// Checks the validity of descriptor property path.
        /// </summary>
        /// <param name="propertyPath">The property path to validate.</param>
        /// <param name="descriptorName">The name used when composing error messages.</param>
        /// <param name="descriptorIndex">The index of the descriptor in the containing collection used
        /// when composing error messages.
        /// </param>
        private void ValidatePropertyPath(string propertyPath, string descriptorName, int descriptorIndex)
        {
            if (string.IsNullOrEmpty(propertyPath))
            {
                throw new ArgumentException(string.Format(
                                   CultureInfo.InvariantCulture,
                                   DomainDataSourceResources.DescriptorPropertyPathIsNull,
                                   descriptorName,
                                   descriptorIndex));
            }

            if (this._entityType != null)
            {
                PropertyInfo pi = this._entityType.GetPropertyInfo(propertyPath);
                if (pi == null)
                {
                    throw new ArgumentException(string.Format(
                                CultureInfo.InvariantCulture,
                                CommonResources.PropertyNotFound,
                                propertyPath,
                                this._entityType.GetTypeName()));
                }
            }
        }

        /// <summary>
        /// Used to group changes to multiple load characteristics together, deferring
        /// the resulting load into a single load when the object returned from this
        /// method is disposed.
        /// </summary>
        /// <returns>
        /// <see cref="IDisposable"/> object that will trigger a load operation when disposed
        /// using <see cref="IDisposable.Dispose"/>.
        /// </returns>
        public IDisposable DeferLoad()
        {
            if (!this.CanLoad)
            {
                throw new InvalidOperationException(DomainDataSourceResources.CannotLoadWhenCanLoadIsFalse_DeferLoad);
            }

            System.Threading.Interlocked.Increment(ref this._loadDeferLevel);
            return new DeferHelper(this.EndLoadDefer);
        }

        /// <summary>
        /// If we're paging or using progressive load, then we will defer a load type
        /// that ensures the first items will be loaded upon the next load.
        /// </summary>
        private void DeferLoadFirstItems()
        {
            if (this.PageSize > 0)
            {
                this._deferredLoadType = LoadType.LoadFirstPages;
            }
            else if (this.LoadSize > 0)
            {
                this._deferredLoadType = LoadType.LoadFirstItems;
            }
        }

        /// <summary>
        /// Called when the <see cref="DomainContext" />'s load is completed.
        /// </summary>
        /// <param name="e">The <see cref="LoadedDataEventArgs"/> that result from this load operation.</param>
        /// <param name="loadContext">The <see cref="LoadContext"/> associated with this load operation.</param>
        private void DomainContext_Loaded(LoadedDataEventArgs e, LoadContext loadContext)
        {
            Debug.Assert(this.DomainContext != null, "Unexpected null DomainContext");

            bool successfulLoad = !e.Cancelled && !e.HasError;
            try
            {
                // Events are raised while processing the loaded entities.
                // Be sure to reset our IsLoadingData flag to false when finished,
                // even if an exception is thrown while processing, otherwise
                // subsequent loads will be prevented.
                try
                {
                    if (successfulLoad)
                    {
                        this.StoreLoadSettings();
                        this.ProcessLoadedEntities(loadContext, e.Entities);
                    }
                }
                finally
                {
                    this.IsLoadingData = false;
                }
            }
            catch (Exception processingException)
            {
                if (processingException.IsFatal())
                {
                    throw;
                }

                // Catch any non-fatal exceptions thrown when processing the loaded entities, raising them as errors in the LoadedData event
                LoadedDataEventArgs errorArgs = new LoadedDataEventArgs(e.Entities, e.AllEntities, e.TotalEntityCount, e.Cancelled, processingException);
                this.RaiseLoadedData(errorArgs);

                return;
            }

            // If the operation was cancelled or errant we can go ahead and quit, raising the loaded data event
            if (!successfulLoad)
            {
                this.RaiseLoadedData(e);
                return;
            }

            bool isPaging = (loadContext.PageSize > 0);
            bool isUsingProgressiveLoads = (loadContext.LoadSize > 0);

            Debug.Assert(e.Entities != null, "Unexpected null e.LoadedEntities");
            int loadedEntitiesCount = e.Entities.Count(entity => this._entityType.IsInstanceOfType(entity));

            if (isPaging || isUsingProgressiveLoads)
            {
                if (isPaging)
                {
                    try
                    {
                        // Events are raised when updating the counts.
                        // We need to ensure that a LoadedData event is raised with any exception caught.
                        this.UpdatePagingCounts(e, loadContext, loadedEntitiesCount);
                    }
                    catch (Exception processingException)
                    {
                        if (processingException.IsFatal())
                        {
                            throw;
                        }

                        // Catch any non-fatal exceptions thrown when updating the counts, raising them as errors in the LoadedData event
                        LoadedDataEventArgs errorArgs = new LoadedDataEventArgs(e.Entities, e.AllEntities, e.TotalEntityCount, e.Cancelled, processingException);
                        this.RaiseLoadedData(errorArgs);

                        return;
                    }
                }

                // Now that we've updated the count properties, raise the LoadedData event
                this.RaiseLoadedData(e);

                // Check if no items were downloaded, or no items are in the current page, 
                // in which case we need to decrease PageIndex
                if (loadedEntitiesCount == 0)
                {
                    if (isPaging)
                    {
                        // We went beyond the end of the entity set. 
                        if (loadContext.RequestedPageIndex > 0 && loadContext.StartPageIndex > 0)
                        {
                            int retryPageIndex = 0;
                            LoadType retryLoadType;

                            if (this._internalEntityCollection.TotalItemCount > 0)
                            {
                                // If we know the total item count to be greater than 0, then calculate the last page
                                retryPageIndex = PagingHelper.CalculatePageCount(this._internalEntityCollection.TotalItemCount, this._internalEntityCollection.PageSize) - 1;

                                // But make sure the retry page index is less than the previously requested page index, in case the total item count
                                // was calculated incorrectly. We don't want to get stuck in a loop loading pages that don't exist.
                                retryPageIndex = (retryPageIndex < this._currentLoadContext.RequestedPageIndex ? retryPageIndex : 0);
                            }

                            // If we have a retryPageIndex of zero, then we'll use LoadFirstPages
                            retryLoadType = (retryPageIndex > 0 ? LoadType.LoadLastPages : LoadType.LoadFirstPages);
                            this._currentLoadContext.StartPageIndex = retryPageIndex;

                            this.ExecuteLoad(retryLoadType);
                            return;
                        }

                        // Even the first page does not contain any items; we're done moving pages
                        this.RaisePageChanged(loadContext, 0 /*newStartPageIndex*/, 0 /*newPageIndex*/);
                    }
                }
                else
                {
                    if (isPaging)
                    {
                        if (loadContext.RequestedPageIndex > 0 && loadContext.RequestedPageIndex > this._internalEntityCollection.PageIndex)
                        {
                            // No items are displayed in the current page index, try a smaller one.
                            this.RaisePageChanged(loadContext, this._internalEntityCollection.StartPageIndex, this._internalEntityCollection.PageIndex);
                            return;
                        }

                        // We have data loaded on the requested page index, we're done moving pages
                        this.RaisePageChanged(loadContext, loadContext.StartPageIndex, loadContext.RequestedPageIndex /*newPageIndex*/);
                    }
                    else if (loadedEntitiesCount == loadContext.LoadSize)
                    {
                        // Progressive load situation where all requested items were loaded
                        if (this.ProgressiveLoadTimer == null)
                        {
                            this.ProgressiveLoadTimer = new Timer();
                        }

                        this._progressiveItemCount += loadedEntitiesCount;
                        this.ProgressiveLoadTimer.Start();
                    }
                }
            }
            else
            {
                // If we're not paging or using progressive loads, then we can just set the counts simply
                // and then raise the loaded data event
                try
                {
                    // Events are raised when updating the counts.
                    // We need to ensure that a LoadedData event is raised with any exception caught.
                    this._internalEntityCollection.ItemCount = loadedEntitiesCount;
                    this._internalEntityCollection.TotalItemCount = e.TotalEntityCount;
                }
                catch (Exception processingException)
                {
                    if (processingException.IsFatal())
                    {
                        throw;
                    }

                    // Catch any non-fatal exceptions thrown when updating the counts, raising them as errors in the LoadedData event
                    LoadedDataEventArgs errorArgs = new LoadedDataEventArgs(e.Entities, e.AllEntities, e.TotalEntityCount, e.Cancelled, processingException);
                    this.RaiseLoadedData(errorArgs);

                    return;
                }

                this.RaiseLoadedData(e);
            }

            // Set the currency to the first item for newly loaded views, unless the current position is already set
            if (loadContext.LoadType != LoadType.LoadNextItems && this.DataView.CurrentPosition == -1)
            {
                this.DataView.MoveCurrentToFirst();
            }
        }

        /// <summary>
        /// Called when a property on the <see cref="DomainContext"/> is changed.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments describing the property change event.</param>
        private void DomainContext_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Debug.Assert(this.DomainContext != null, "Unexpected null DomainContext");
            switch (e.PropertyName)
            {
                case "HasChanges":
                    this.HasChanges = this.DomainContext.HasChanges;
                    break;
            }
        }

        /// <summary>
        /// Called when a property on the <see cref="_internalEntityCollectionView"/> is changed.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments describing the property change event.</param>
        private void EntityCollectionView_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "Culture":
                    // Changing the Culture invalidates our caches
                    this.ResetFiltersExpression();
                    this._expressionCache.Clear();
                    break;
            }
        }

        /// <summary>
        /// Called when <see cref="Control.IsEnabled"/> is changed.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments describing the changes to the <see cref="Control.IsEnabledProperty"/>.</param>
        private void DomainDataSource_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            this.ApplyState(true);
        }

        /// <summary>
        /// Called when the <see cref="Control"/> has <see cref="FrameworkElement.Loaded"/>
        /// (not when data has been loaded). Initialize Auto-Load behavior.
        /// </summary>
        /// <param name="sender">The sender of the loaded event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> for this loaded event.</param>
        private void DomainDataSource_Loaded(object sender, RoutedEventArgs e)
        {
            // Enqueue the load on the dispatcher to ensure other controls
            // have the opportunity to load before we start loading data.
            // This will allow a DataPager that sets our PageSize to be loaded
            // before we invoke our initial load, which could otherwise result
            // in a LoadAll being performed, and then a subsequent load being
            // invoked from the PageSize property being updated.
            // Note that even though all unit tests pass with a single dispatch, a double dispatch
            // is necessary so the UI thread first has the opportunity to:
            //  1) Raise the Loaded event for all other controls (before the first dispatch)
            //  2) Allow bindings to update and other application and control code to modify values
            //     that affect the DomainDataSource (before the second dispatch)
            Dispatcher.BeginInvoke(() => Dispatcher.BeginInvoke(() => this.InitializeAutoLoad()));
        }

        /// <summary>
        /// Callback for when a deferred load has ended via disposal of the object
        /// returned from a <see cref="DeferLoad"/> call.
        /// </summary>
        private void EndLoadDefer()
        {
            Debug.Assert(this._loadDeferLevel > 0, "Unexpected negative _loadDeferLevel");

            if (System.Threading.Interlocked.Decrement(ref this._loadDeferLevel) == 0)
            {
                if (!this.CanLoad)
                {
                    throw new InvalidOperationException(DomainDataSourceResources.CannotLoadWhenCanLoadIsFalse_DeferLoad);
                }

                this.ExecuteLoad(this._deferredLoadType ?? this.InitialLoadType);
            }
            return;
        }

        /// <summary>
        /// Requests that a <see cref="Load"/> be invoked when <see cref="AutoLoad"/> is <c>true</c>.
        /// </summary>
        private void RequestAutoLoad()
        {
            if (this.AutoLoad)
            {
                if (!this._isAutoLoadInitialized)
                {
                    // Before we're ready to start auto-loading, we aggregate all requests
                    // into a single load that occurs once the DomainDataSource is Loaded.
                    this._shouldExecuteInitialAutoLoad = true;
                }
                else if (this._queryMethod != null)
                {
                    if (this.LoadTimer == null)
                    {
                        this.LoadTimer = new Timer();
                    }

                    if (this.LoadTimer.IsEnabled)
                    {
                        // An async load request was already pending,
                        // reset the count down.
                        this.LoadTimer.Stop();
                    }

                    this.LoadTimer.Start();
                }
            }
        }

        /// <summary>
        /// Execute a load of the specified <see cref="LoadType"/>. Any pending load will be canceled.
        /// </summary>
        /// <param name="loadType">The <see cref="LoadType"/> to be used for this load.</param>
        private void ExecuteLoad(LoadType loadType)
        {
            if (DesignerProperties.IsInDesignTool)
            {
                return;
            }

            Debug.Assert(this._preparingOperation == false, "We should never be already preparing an operation at this point.");
            this._preparingOperation = true;

            this.ResetPendingLoads();
            // We initialize AutoLoad on the first explicit Load to allow AutoLoad to function outside the visual
            // tree. This call occurs immediately after resetting the pending loads so it is not re-entrant.
            this.InitializeAutoLoad();

            try
            {
                if (this.DomainContext != null && this.IsLoadingData)
                {
                    this.CancelLoadPrivate();
                }

                this.LoadData(loadType);
                this._preparingOperation = false;
            }
            catch (Exception exception)
            {
                // Set this flag before raising the LoadedData event
                // (and not in a finally block) to ensure consistent state.
                this._preparingOperation = false;

                if (exception.IsFatal())
                {
                    throw;
                }

                LoadedDataEventArgs eventArgs = new LoadedDataEventArgs(
                    Enumerable.Empty<Entity>(),
                    Enumerable.Empty<Entity>(),
                    -1,
                    false,
                    exception);

                this.RaiseLoadedData(eventArgs);
            }
        }

        /// <summary>
        /// Appends the query parameters that will filter the list on the server-side
        /// before bringing the data down.
        /// </summary>
        /// <param name="query">The query before the filter descriptions are applied.</param>
        /// <returns>The query for the load operation with the filter descriptors applied.</returns>
        private EntityQuery GetFilterDescriptorsQuery(EntityQuery query)
        {
            if (this.FilterDescriptors.Count == 0)
            {
                return query;
            }

            Debug.Assert(this._entityType != null, "Unexpected null EntityType");

            if (this._filtersExpression == null)
            {
                this.ValidateFilterDescriptors();

                this._filtersExpression = LinqHelper.BuildFiltersExpression(
                    this.FilterDescriptors,
                    this.FilterOperator,
                    this._expressionCache);
            }

            if (this._filtersExpression == null)
            {
                return query;
            }

            return LinqHelper.Where(query, this._filtersExpression);
        }

        /// <summary>
        /// Appends the paging statements (Skip/Take) onto the specified <paramref name="query"/>
        /// based on the <paramref name="loadType"/>, <paramref name="startPageIndex"/>, 
        /// <see cref="PageSize"/>, and <see cref="LoadSize"/>.
        /// </summary>
        /// <param name="query">The query before the paging statements are applied.</param>
        /// <param name="loadType">The type of load being performed.</param>
        /// <param name="startPageIndex">The first page requested for the load.</param>
        /// <returns>The query for the load operation with the paging statements applied.</returns>
        private EntityQuery GetPagingQuery(EntityQuery query, LoadType loadType, ref int startPageIndex)
        {
            Debug.Assert(this.PageSize > 0, "Unexpected PageSize value");

            Debug.Assert(query != null, "Unexpected null query in GetPagingQuery");

            // Make sure that we download at least PageSize items, or an exact multiple of it.
            int loadSize = GetLoadSizeCeiling(this.PageSize, this.LoadSize);
            int pageCount = PagingHelper.CalculateFullPageCount(loadSize, this.PageSize);

            switch (loadType)
            {
                case LoadType.LoadFirstPages:
                    // Start at the very beginning
                    startPageIndex = 0;
                    break;

                case LoadType.LoadLastPages:
                    // Move startPageIndex to the smallest possible value
                    startPageIndex = Math.Max(
                        startPageIndex,
                        0);
                    break;

                default:
                    // Load pages in 'blocks' that align to multiples of the pageCount for the adjusted load size
                    // For instance, when pageCount = 3, the startPageIndices will follow this pattern: 0..3..6..9..
                    startPageIndex = Math.Max(
                        (this._lastRequestedPageIndex / pageCount) * pageCount,
                        0);
                    break;
            }

            if (startPageIndex > 0)
            {
                query = LinqHelper.Skip(query, startPageIndex * this.PageSize);
            }

            return LinqHelper.Take(query, loadSize);
        }

        /// <summary>
        /// Appends the progressive load statements (Skip/Take) ontp the specified <paramref name="query"/>
        /// using the <see cref="LoadSize"/> property.
        /// </summary>
        /// <param name="query">The query before the progressive load statements are applied.</param>
        /// <returns>The query for the load operation with the progressive load statements applied.</returns>
        private EntityQuery GetProgressiveLoadQuery(EntityQuery query)
        {
            Debug.Assert(this.PageSize == 0, "Unexpected PageSize value");
            Debug.Assert(this.LoadSize > 0, "Unexpected LoadSize value");

            Debug.Assert(query != null, "Unexpected null query in GetProgressiveLoadQuery");

            if (this._progressiveItemCount > 0)
            {
                query = LinqHelper.Skip(query, this._progressiveItemCount);
            }
            return LinqHelper.Take(query, this.LoadSize);
        }

        /// <summary>
        /// Returns an array of values to be used for the query parameters.
        /// </summary>
        /// <returns>Array of <see cref="Parameter" /> values to use in the load operation.</returns>
        private object[] GetQueryParameterValues()
        {
            Debug.Assert(this.QueryParameters != null, "Unexpected null QueryParameters");
            Debug.Assert(this._queryMethod != null, "Unexpected null _queryMethod");

            // Check value can be converted
            object[] parameters = new object[this.QueryParameters.Count];
            ParameterInfo[] parameterInfo = this._queryMethod.GetParameters();

            for (int i = 0; i < parameterInfo.Length; i++)
            {
                Parameter parameter = this.QueryParameters.Single(p => p.ParameterName == parameterInfo[i].Name);
                parameters[i] = Utilities.GetConvertedValue(this.Culture, parameterInfo[i].ParameterType, parameter.Value);
            }

            return parameters;
        }

        /// <summary>
        /// Appends the query parameters that will sort/group the list on the server-side.
        /// </summary>
        /// <remarks>
        /// Because we cannot actually pass in a grouping parameter, we translate grouping
        /// into server-side sorting that is applied before the sort descriptors.
        /// </remarks>
        /// <param name="query">The query before the sorting statements are applied.</param>
        /// <returns>The query for the load operation with the sort statements applied.</returns>
        private EntityQuery GetSortDescriptorsQuery(EntityQuery query)
        {
            if ((this.SortDescriptors.Count == 0) && (this.GroupDescriptors.Count == 0))
            {
                return query;
            }

            Debug.Assert(query != null, "Unexpected null query");

            // first check the grouping information
            this.ValidateGroupDescriptors();

            // then check the sorting information
            this.ValidateSortDescriptors();

            // return the composed order by expression
            return LinqHelper.OrderBy(query, this.GroupDescriptors, this.SortDescriptors, this._expressionCache);
        }

        /// <summary>
        /// Called once the control has been loaded and auto-load can safely begin.
        /// </summary>
        private void InitializeAutoLoad()
        {
            if (this._shouldExecuteInitialAutoLoad)
            {
                this.ExecuteLoad(this.InitialLoadType);
            }
            this._isAutoLoadInitialized = true;
        }

        /// <summary>
        /// Sets up the internal entity collection and the paged collection view.
        /// </summary>
        private void InitializeView()
        {
            this._internalEntityCollection = new PagedEntityCollection(pageIndex => this.MoveToPage(pageIndex));
            this._internalEntityCollection.PropertyChanged += new PropertyChangedEventHandler(this.PagedEntityCollection_PropertyChanged);
            this._internalEntityCollection.PageSize = this.PageSize;

            // Create the view around our PagedEntityCollection, calling back into ourself when the view is refreshed
            this._internalEntityCollectionView = new PagedEntityCollectionView(this._internalEntityCollection, () => this.ViewRefreshCallback(), EntitySetOperations.None);
            this._internalEntityCollectionView.PropertyChanged += this.EntityCollectionView_PropertyChanged;

            // Create the DomainDataSourceView (that has a developer-friendly API) that wraps around the collection view
            DomainDataSourceView ddsView = new DomainDataSourceView(this._internalEntityCollectionView);

            // Use this DomainDataSourceView for our DataView property for both run-time and design-time.
            this.DataView = ddsView;

            // At run-time, we also set our Data property to this DomainDataSourceView
            if (!DesignerProperties.IsInDesignTool)
            {
                this.Data = ddsView;
            }
            else
            {
                // But at design-time, we set up a binding from the DesignData to our Data, using
                // a converter to convert an IEnumerable<T> to a collection view.
                // Use a fall-back value of the DomainDataSourceView so that Data is always non-null
                Binding designDataBinding = new Binding
                {
                    Source = this,
                    Path = new PropertyPath(DesignDataPropertyName),
                    Converter = new CollectionViewConverter(),
                    FallbackValue = ddsView
                };

                this.SetBinding(DataProperty, designDataBinding);
            }

            Debug.Assert(((ICollectionView)this.DataView).SortDescriptions == null || ((ICollectionView)this.DataView).SortDescriptions.Count == 0, "Unexpected SortDescriptions");
            Debug.Assert(((ICollectionView)this.DataView).GroupDescriptions == null || ((ICollectionView)this.DataView).GroupDescriptions.Count == 0, "Unexpected GroupDescriptions");

            // Initialize the CanLoad property on the view
            // The view will be updated any time the CanLoad property value changes
            this._internalEntityCollectionView.CanLoad = this.CanLoad;
        }

        /// <summary>
        /// Initialize the various <see cref="ICommand"/> implementations for the control.
        /// </summary>
        private void InitializeCommands()
        {
            this._commandPropertyNotifier = new PropertyChangedNotifier(this);

            this._loadCommand = new DomainDataSourceCommand(
                this,
                "CanLoad",
                () => this.CanLoad,
                () => this.Load());
            this._rejectChangesCommand = new DomainDataSourceCommand(
                this,
                "CanRejectChanges",
                () => this.CanRejectChanges,
                () => this.RejectChanges());
            this._submitChangesCommand = new DomainDataSourceCommand(
                this,
                "CanSubmitChanges",
                () => this.CanSubmitChanges,
                () => this.SubmitChanges());

            this.UpdateCanLoadProperty();
            this.UpdateCanRejectChangesProperty();
            this.UpdateCanSubmitChangesProperty();
        }

        /// <summary>
        /// Starts a load operation synchronously. Any pending load will be implicitly canceled.
        /// </summary>
        /// <remarks>
        /// If you don't wish to cancel a pending load, check <see cref="IsLoadingData"/> before
        /// calling <see cref="Load"/>.
        /// </remarks>
        public void Load()
        {
            if (DesignerProperties.IsInDesignTool)
            {
                return;
            }

            if (this.DomainContext == null)
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.InvariantCulture,
                    DomainDataSourceResources.OperationNeedsPropertySet,
                    "DomainContext",
                    DomainDataSourceResources.LoadOperation));
            }

            if (string.IsNullOrEmpty(this.QueryName))
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.InvariantCulture,
                    DomainDataSourceResources.OperationNeedsPropertySet,
                    "QueryName",
                    DomainDataSourceResources.LoadOperation));
            }

            if (this._loadDeferLevel > 0)
            {
                throw new InvalidOperationException(DomainDataSourceResources.LoadWithinDeferLoad);
            }

            this.ValidateQueryParameters();

            if (this._preparingOperation)
            {
                throw new InvalidOperationException(DomainDataSourceResources.InvalidOperationDuringLoadOrSubmit);
            }

            if (!this.CanLoad)
            {
                throw new InvalidOperationException(DomainDataSourceResources.CannotLoadWhenCanLoadIsFalse);
            }

            this.ExecuteLoad(this.InitialLoadType);
        }

        /// <summary>
        /// Invoke a load operation for the specified <see cref="LoadType"/>. The <see cref="EntityQuery"/> will be
        /// composed and the <see cref="DomainContext"/>'s <see cref="OpenRiaServices.DomainServices.Client.DomainContext.Load"/>
        /// method will be called.
        /// </summary>
        /// <remarks>
        /// A <see cref="LoadingData"/> event will always be raised when this method is called, regardless of success.
        /// </remarks>
        /// <param name="loadType">The type of load to be invoked.</param>
        private void LoadData(LoadType loadType)
        {
            if (this._loadDeferLevel > 0 ||
                this._internalEntityCollectionView.IsRefreshDeferred ||
                this._internalEntityCollectionView.IsRefreshing ||
                this.DomainContext == null ||
                this._queryMethod == null)
            {
                this._deferredLoadType = loadType;
                return;
            }

            // Clear any tracked deferred load type
            this._deferredLoadType = null;

            LoadingDataEventArgs loadingDataEventArgs = new LoadingDataEventArgs(null, LoadBehavior.KeepCurrent);

            object[] parameters;

            try
            {
                // Get the parameters needed to get the root query
                parameters = this.GetQueryParameterValues();
            }
            catch (Exception exception)
            {
                if (exception.IsFatal())
                {
                    throw;
                }

                // An exception occurred getting the query parameter values.
                // Raise a LoadingData event (with a null Query) and allow the
                // application to Cancel the event. If not canceled, re-throw;
                this.RaiseLoadingData(loadingDataEventArgs);

                if (!loadingDataEventArgs.Cancel)
                {
                    throw;
                }

                // If the event was canceled, then we just exit.
                return;
            }


            // Get the root query from the domain context
            EntityQuery query;

            try
            {
                query = this._queryMethod.Invoke(this.DomainContext, parameters) as EntityQuery;
            }
            catch (TargetInvocationException exception)
            {
                if (exception.IsFatal())
                {
                    throw;
                }

                // An exception occurred invoking the query method.
                // Raise a LoadingData event (with a null Query) and allow the
                // application to Cancel the event.
                this.RaiseLoadingData(loadingDataEventArgs);

                if (!loadingDataEventArgs.Cancel)
                {
                    // Throw the inner exception if available
                    if (exception.InnerException != null)
                    {
                        throw exception.InnerException;
                    }

                    throw;
                }

                // If the event was canceled, then we just exit.
                return;
            }
            Debug.Assert(query != null, "Unexpected null query from _queryMethod.Invoke");

            try
            {
                // Apply the filter descriptors to the query
                query = this.GetFilterDescriptorsQuery(query);
            }
            catch (Exception exception)
            {
                if (exception.IsFatal())
                {
                    throw;
                }

                // An exception occurred invoking the query method.
                // Raise a LoadingData event (with a null Query) and allow the
                // application to Cancel the event. If not canceled, re-throw;
                this.RaiseLoadingData(loadingDataEventArgs);

                if (!loadingDataEventArgs.Cancel)
                {
                    throw;
                }

                // If the event was canceled, then we just exit.
                return;
            }
            Debug.Assert(query != null, "Unexpected null query from GetFilterDescriptorsQuery");

            try
            {
                // Apply the sort descriptors to the query
                query = this.GetSortDescriptorsQuery(query);
            }
            catch (Exception exception)
            {
                if (exception.IsFatal())
                {
                    throw;
                }

                // An exception occurred invoking the query method.
                // Raise a LoadingData event (with a null Query) and allow the
                // application to Cancel the event. If not canceled, re-throw;
                this.RaiseLoadingData(loadingDataEventArgs);

                if (!loadingDataEventArgs.Cancel)
                {
                    throw;
                }

                // If the event was canceled, then we just exit.
                return;
            }
            Debug.Assert(query != null, "Unexpected null query from GetSortDescriptorsQuery");

            if (this.PageSize > 0)
            {
                // Since we are paging, we need the TotalCount in the response
                query.IncludeTotalCount = true;
            }

            // Set the query on the event args and allow for further composition or cancellation
            loadingDataEventArgs.Query = query;
            this.RaiseLoadingData(loadingDataEventArgs);

            // If the event was canceled, then we just exit.
            if (loadingDataEventArgs.Cancel)
            {
                return;
            }

            // Prepare to apply sorting or progressive load expressions
            int startPageIndex = 0;
            if (loadType == LoadType.LoadLastPages)
            {
                Debug.Assert(this._currentLoadContext != null, "Unexpected this._currentLoadContext == null");
                startPageIndex = this._currentLoadContext.StartPageIndex;
            }
            else if (loadType == LoadType.LoadFirstItems)
            {
                this._progressiveItemCount = 0;
            }

            if (this.PageSize > 0)
            {
                // Paging situation
                loadingDataEventArgs.Query = this.GetPagingQuery(loadingDataEventArgs.Query, loadType, ref startPageIndex);
                Debug.Assert(loadingDataEventArgs.Query != null, "Unexpected null query");
            }
            else if (this.LoadSize > 0)
            {
                // Progressive load situation
                Debug.Assert(loadType == LoadType.LoadFirstItems || loadType == LoadType.LoadNextItems, "Unexpected loadType value");
                loadingDataEventArgs.Query = this.GetProgressiveLoadQuery(loadingDataEventArgs.Query);
                Debug.Assert(loadingDataEventArgs.Query != null, "Unexpected null query");
            }

            LoadContext loadContext = new LoadContext();
            loadContext.LoadSize = this.LoadSize;
            loadContext.LoadType = loadType;
            loadContext.PageSize = this.PageSize;
            loadContext.RequestedPageIndex = this._lastRequestedPageIndex;
            loadContext.StartPageIndex = startPageIndex;

            DebugTrace.Trace(LoadDebug, "Invoking load method");

            this._currentLoadOperation = this.DomainContext.Load(loadingDataEventArgs.Query, loadingDataEventArgs.LoadBehavior, new Action<LoadOperation>(this.LoadData_Callback), loadContext);
            this.LoadData_PostInvoke(loadContext);
        }

        /// <summary>
        /// The callback used for the <see cref="LoadOperation"/>, indicating completion
        /// of the load.
        /// </summary>
        /// <param name="loadOperation">The LoadOperation that has finished.</param>
        private void LoadData_Callback(LoadOperation loadOperation)
        {
            this._currentLoadOperation = null;

            LoadedDataEventArgs eventArgs = new LoadedDataEventArgs(
                loadOperation.Entities,
                loadOperation.AllEntities,
                loadOperation.TotalEntityCount,
                loadOperation.IsCanceled,
                loadOperation.Error);

            if (loadOperation.HasError)
            {
                // Always mark the underlying load operation error as handled. We'll raise our own exception
                // if the error is unhandled in the LoadedData event.
                loadOperation.MarkErrorAsHandled();
            }

            this.DomainContext_Loaded(eventArgs, (LoadContext)loadOperation.UserState);
        }

        /// <summary>
        /// Post processing that occurs synchronously after invoking a load (after when the load completes).
        /// </summary>
        /// <param name="loadContext">The <see cref="LoadContext"/> that was used for the load.</param>
        private void LoadData_PostInvoke(LoadContext loadContext)
        {
            this._currentLoadContext = loadContext;
            if (loadContext.LoadType != LoadType.LoadNextItems)
            {
                if (loadContext.LoadType != LoadType.LoadAll && loadContext.LoadType != LoadType.LoadFirstItems)
                {
                    // This load operation represents a page move
                    loadContext.RaisePageChanged = true;
                }
            }

            this.IsLoadingData = true;
        }

        /// <summary>
        /// Callback for when the <see cref="LoadTimer"/> ticks. This indicates that
        /// a queued auto-load can now be invoked.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        /// <seealso cref="AutoLoad"/>
        /// <seealso cref="LoadDelay"/>
        private void LoadTimer_Tick(object sender, EventArgs e)
        {
            // If we cannot presently load, then we'll let the timer keep ticking
            if (this.CanLoad)
            {
                this.LoadTimer.Stop();
                this.ExecuteLoad(this.InitialLoadType);
            }
        }

        /// <summary>
        /// Called when the paged collection view requests a new page index.
        /// </summary>
        /// <param name="pageIndex">Index of the requested page.</param>
        /// <returns><c>true</c> if an asynchronous page load was initiated, otherwise <c>false</c>.</returns>
        internal bool MoveToPage(int pageIndex)
        {
            // Check if requested page index is outside the current pages
            if (this._currentLoadContext != null && this._currentLoadContext.PageSize > 0)
            {
                this._lastRequestedPageIndex = pageIndex;

                LoadType loadType = LoadType.None;
                if (pageIndex >= 0 &&
                    pageIndex < this._currentLoadContext.StartPageIndex)
                {
                    // pageIndex comes before the current block of pages
                    loadType = LoadType.LoadPreviousPages;
                }
                else if (pageIndex > this._currentLoadContext.StartPageIndex &&
                         this._internalEntityCollection.StartPageIndex + (this._internalEntityCollection.LoadedItemCount / this._currentLoadContext.PageSize) <= pageIndex)
                {
                    // pageIndex comes after the current block of pages
                    loadType = LoadType.LoadNextPages;
                }

                if (loadType != LoadType.None)
                {
                    this.ExecuteLoad(loadType);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Called when a new template gets applied to the <see cref="Control"/>.
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            this.ApplyState(false /*animate*/);
        }

        /// <summary>
        /// Process the entities that resulted from a load operation, loading those entities
        /// into the <see cref="_internalEntityCollection"/>.
        /// </summary>
        /// <param name="loadContext">The <see cref="LoadContext"/> used to perform the load operation.</param>
        /// <param name="entities">The <see cref="Entity"/> list that was loaded.</param>
        private void ProcessLoadedEntities(LoadContext loadContext, IEnumerable<Entity> entities)
        {
            Debug.Assert(entities != null, "Unexpected null entities");

            // Begin the load transaction
            this._internalEntityCollection.BeginLoad();

            // Unless we're progressively loading items, we clear the collection
            if (loadContext.LoadType != LoadType.LoadNextItems)
            {
                this._internalEntityCollection.Clear(loadContext.IsInitialLoad);
            }

            // If we're paging, we need to do some work to determine the resulting page index that will be shown
            if (loadContext.PageSize > 0)
            {
                this._internalEntityCollection.StartPageIndex = loadContext.StartPageIndex;

                int newPageIndex;

                if (loadContext.IsInitialLoad)
                {
                    // An initial load always goes to the first page
                    newPageIndex = 0;
                }
                else
                {
                    // Other loads need to ensure the page index is within bounds
                    int lastPageWithData = Math.Max(loadContext.StartPageIndex + ((entities.Count() - 1) / loadContext.PageSize), 0);
                    newPageIndex = Math.Min(lastPageWithData, loadContext.RequestedPageIndex);
                }

                if (newPageIndex != this._internalEntityCollection.PageIndex)
                {
                    if (loadContext.RaisePageChanged)
                    {
                        this._internalEntityCollectionView.IsPageChanging = true;
                    }

                    this._internalEntityCollection.PageIndex = newPageIndex;
                }

                // Recalculate the full page count for only the items that are loaded (not the overall page count)
                this._internalEntityCollection.PageCount = Math.Max(1, PagingHelper.CalculateFullPageCount(loadContext.LoadSize, loadContext.PageSize));
            }
            else
            {
                // Paging is disabled, ensure that the PageIndex is -1
                this._internalEntityCollection.PageIndex = -1;
            }

            // Add all of the loaded entities that are not deleted
            foreach (Entity loadedEntity in entities.Where(entity => entity.EntityState != EntityState.Deleted))
            {
                this._internalEntityCollection.AddLoadedEntity(loadedEntity);
            }

            // Close the transaction
            this._internalEntityCollection.CompleteLoad();
        }

        /// <summary>
        /// Callback for when the <see cref="ProgressiveLoadTimer"/> ticks. This indicates that
        /// a progressive load can invoke the next load.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        /// <seealso cref="LoadInterval"/>
        /// <seealso cref="LoadSize"/>
        private void ProgressiveLoadTimer_Tick(object sender, EventArgs e)
        {
            // If we cannot presently load, then we'll let the timer keep ticking
            if (this.CanLoad && !this.IsLoadingData)
            {
                this.ProgressiveLoadTimer.Stop();
                this.ExecuteLoad(LoadType.LoadNextItems);
            }
        }

        /// <summary>
        /// Raise the <see cref="LoadedData"/> event.
        /// </summary>
        /// <param name="e">The event arguments to use for the raised event.</param>
        /// <exception cref="DomainException">thrown when a load error occurred and was not handled.</exception>
        private void RaiseLoadedData(LoadedDataEventArgs e)
        {
            EventHandler<LoadedDataEventArgs> handler = this.LoadedData;
            if (handler != null)
            {
                handler(this, e);
            }

            // If the error is not handled, we throw an exception to be sure that the error doesn't silently disappear
            if (e.HasError && !e.IsErrorHandled)
            {
                throw new DomainException(string.Format(CultureInfo.CurrentCulture, DomainDataSourceResources.LoadErrorWasNotHandled, this.QueryName, this.DomainContext.GetType().Name) + Environment.NewLine + Environment.NewLine + e.Error.Message, e.Error);
            }
        }

        /// <summary>
        /// Raise the <see cref="LoadingData"/> event. If the event is canceled and
        /// <see cref="LoadingDataEventArgs.RestoreLoadSettings"/> is set to <c>true</c>,
        /// then load settings will be restored.
        /// </summary>
        /// <param name="e">The event arguments to use for the raised event.</param>
        private void RaiseLoadingData(LoadingDataEventArgs e)
        {
            EventHandler<LoadingDataEventArgs> handler = this.LoadingData;
            if (handler != null)
            {
                handler(this, e);
            }

            // If the event was canceled and load settings should be restored, then restore them
            if (e.Cancel)
            {
                if (e.RestoreLoadSettings)
                {
                    this.RestoreLoadSettings();
                }
            }
        }

        /// <summary>
        /// Notify the <see cref="PagedEntityCollectionView"/> that the page move was completed.
        /// </summary>
        /// <param name="loadContext">Load characteristics</param>
        /// <param name="newStartPageIndex">Final start page index</param>
        /// <param name="newPageIndex">Final page index</param>
        private void RaisePageChanged(LoadContext loadContext, int newStartPageIndex, int newPageIndex)
        {
            Debug.Assert(loadContext != null, "Unexpected null loadContext");
            Debug.Assert(loadContext.RaisePageChanged, "Unexpected loadContext.RaisePageChanged == false");
            loadContext.RaisePageChanged = false;
            this._internalEntityCollection.NotifyPageChanged(newStartPageIndex, newPageIndex);
        }

        /// <summary>
        /// Raise the <see cref="SubmittedChanges"/> event.
        /// </summary>
        /// <param name="e">The event arguments to use for the raised event.</param>
        private void RaiseSubmittedChanges(SubmittedChangesEventArgs e)
        {
            EventHandler<SubmittedChangesEventArgs> handler = this.SubmittedChanges;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        /// <summary>
        /// Raise the <see cref="SubmittingChanges"/> event.
        /// </summary>
        /// <param name="e">The event arguments to use for the raised event.</param>
        private void RaiseSubmittingChanges(SubmittingChangesEventArgs e)
        {
            EventHandler<SubmittingChangesEventArgs> handler = this.SubmittingChanges;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        /// <summary>
        /// Callback for when the <see cref="RefreshLoadTimer"/> ticks. This indicates that
        /// the data should be refreshed from the server.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        /// <seealso cref="RefreshInterval"/>
        private void RefreshLoadTimer_Tick(object sender, EventArgs e)
        {
            // If we cannot presently load, then we'll just abort the refresh
            if (!this.CanLoad || this.IsLoadingData)
            {
                return;
            }

            LoadType refreshLoadType;

            if (this.PageSize > 0)
            {
                // if we are paging
                refreshLoadType = LoadType.LoadCurrentPages;
            }
            else if (this.LoadSize > 0)
            {
                // if using progressive download
                refreshLoadType = LoadType.LoadFirstItems;
            }
            else
            {
                // load all
                refreshLoadType = LoadType.LoadAll;
            }

            this.ExecuteLoad(refreshLoadType);
        }

        /// <summary>
        /// Rejects the changes for every <see cref="Entity"/> in the <see cref="DomainContext"/>.
        /// </summary>
        /// <remarks>
        /// Changes will be rejected for all entities in the <see cref="DomainContext"/>, including
        /// those that were not loaded through this <see cref="DomainDataSource"/>.
        /// <para>
        /// This will also cancel a pending Add or Edit transaction on the <see cref="DataView"/>.
        /// </para>
        /// </remarks>
        public void RejectChanges()
        {
            if (this.DomainContext == null)
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.InvariantCulture,
                    DomainDataSourceResources.OperationNeedsPropertySet,
                    "DomainContext",
                    DomainDataSourceResources.RejectChangesOperation));
            }

            IEditableCollectionView editableCollectionView = this.DataView;
            if (editableCollectionView.IsAddingNew)
            {
                editableCollectionView.CancelNew();
            }
            if (editableCollectionView.IsEditingItem)
            {
                editableCollectionView.CancelEdit();
            }

            this.DomainContext.RejectChanges();

            // DataView.Refresh would invoke another load. We only need to refresh the view.
            this._internalEntityCollectionView.RefreshView();
        }

        /// <summary>
        /// Resets the backing entity set for the <see cref="_internalEntityCollection"/>
        /// and clears all entities from the collection.
        /// </summary>
        private void ResetEntitySet()
        {
            this._internalEntityCollection.BackingEntitySet = null;
            this._internalEntityCollection.Clear(true);
        }

        /// <summary>
        /// Resets the calculated filters expression.
        /// </summary>
        private void ResetFiltersExpression()
        {
            this._filtersExpression = null;
        }

        /// <summary>
        /// Resets all pending loads by stopping the auto-load <see cref="LoadTimer"/>
        /// and the <see cref="ProgressiveLoadTimer"/>.
        /// </summary>
        private void ResetPendingLoads()
        {
            this._shouldExecuteInitialAutoLoad = false;

            if ((this.LoadTimer != null) && this.LoadTimer.IsEnabled)
            {
                this.LoadTimer.Stop();
            }

            if ((this.ProgressiveLoadTimer != null) && this.ProgressiveLoadTimer.IsEnabled)
            {
                this.ProgressiveLoadTimer.Stop();
            }
        }

        /// <summary>
        /// Stores the descriptor settings used to compose the last successful load query.
        /// </summary>
        private void StoreLoadSettings()
        {
            this._filterCollectionManager.StoreOriginalValues();
            this._groupCollectionManager.StoreOriginalValues();
            this._parameterCollectionManager.StoreOriginalValues();
            this._sortCollectionManager.StoreOriginalValues();
        }

        /// <summary>
        /// Restores the descriptor settings used to compose the last successful load query.
        /// </summary>
        private void RestoreLoadSettings()
        {
            this._filterCollectionManager.RestoreOriginalValues();
            this._groupCollectionManager.RestoreOriginalValues();
            this._parameterCollectionManager.RestoreOriginalValues();
            this._sortCollectionManager.RestoreOriginalValues();
        }

        /// <summary>
        /// Submits the changes for every <see cref="Entity"/> in the <see cref="DomainContext"/>.
        /// </summary>
        /// <remarks>
        /// Changes will be submitted for all entities in the <see cref="DomainContext"/>, including
        /// those that were not loaded through this <see cref="DomainDataSource"/>.
        /// <para>
        /// This will also commit a pending Add or Edit transaction on the <see cref="DataView"/>.
        /// </para>
        /// </remarks>
        public void SubmitChanges()
        {
            if (DesignerProperties.IsInDesignTool)
            {
                return;
            }

            if (this.DomainContext == null)
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.InvariantCulture,
                    DomainDataSourceResources.OperationNeedsPropertySet,
                    "DomainContext",
                    DomainDataSourceResources.SubmitOperation));
            }

            Debug.Assert(this.DomainContext != null, "Unexpected null DomainContext");

            if (this.IsSubmittingChanges)
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.InvariantCulture,
                    DomainDataSourceResources.OperationAlreadyInProgress,
                    DomainDataSourceResources.SubmitOperation));
            }

            if (this._preparingOperation)
            {
                throw new InvalidOperationException(DomainDataSourceResources.InvalidOperationDuringLoadOrSubmit);
            }

            IEditableCollectionView editableCollectionView = this.DataView;
            if (editableCollectionView.IsAddingNew)
            {
                editableCollectionView.CommitNew();
            }
            else if (editableCollectionView.IsEditingItem)
            {
                editableCollectionView.CommitEdit();
            }
            else
            {
                IEditableObject editable = this.DataView.CurrentItem as IEditableObject;

                if (editable != null)
                {
                    editable.EndEdit();
                }
            }

            this._preparingOperation = true;

            SubmittingChangesEventArgs e = new SubmittingChangesEventArgs(this.DomainContext.EntityContainer.GetChanges());
            this.RaiseSubmittingChanges(e);

            if (!e.Cancel)
            {
                this.IsSubmittingChanges = true;
                try
                {
                    this._currentSubmitOperation = this.DomainContext.SubmitChanges(this.SubmitChanges_Callback, null);
                }
                catch
                {
                    // Reset the IsSubmittingChanges flag right away in case of a synchronous exception.
                    // In normal cases, the flag is reset in the DomainContext_Submitted event handler.
                    this.IsSubmittingChanges = false;
                    this._preparingOperation = false;
                    throw;
                }
            }
            this._preparingOperation = false;
        }

        /// <summary>
        /// The callback used for the <see cref="SubmitOperation"/>, when the operation completes.
        /// </summary>
        /// <param name="submitOperation">The <see cref="SubmitOperation"/> that raised this callback.</param>
        private void SubmitChanges_Callback(SubmitOperation submitOperation)
        {
            this._currentSubmitOperation = null;
            this.IsSubmittingChanges = false;

            SubmittedChangesEventArgs eventArgs = new SubmittedChangesEventArgs(
                submitOperation.ChangeSet,
                submitOperation.EntitiesInError,
                submitOperation.Error,
                submitOperation.IsCanceled);

            this.RaiseSubmittedChanges(eventArgs);

            if (submitOperation.HasError)
            {
                // Mark the underlying submit operation error as handled, because we'll raise our own exception
                // if our submit error is unhandled, providing DomainDataSource-specific exception info.
                submitOperation.MarkErrorAsHandled();

                // If the error is not handled, we throw an exception to be sure that the error doesn't silently disappear
                if (!eventArgs.IsErrorHandled)
                {
                    throw new DomainException(string.Format(CultureInfo.CurrentCulture, DomainDataSourceResources.SubmitErrorWasNotHandled, this.DomainContext.GetType().Name), submitOperation.Error);
                }
            }

            if (!submitOperation.IsCanceled && !submitOperation.HasError)
            {
                // When changes are successfully submitted, then we need to clear all page tracking
                // to prevent entities that were removed from being resurrected as new entities,
                // and to reset page tracking for added and edited entities.
                this._internalEntityCollection.ClearPageTracking(true);
            }
        }

        /// <summary>
        /// Update the <see cref="CanLoad"/> property based on the <see cref="HasChanges"/>
        /// and <see cref="IsSubmittingChanges"/> properties.
        /// </summary>
        private void UpdateCanLoadProperty()
        {
            this.CanLoad = !this.IsSubmittingChanges && !this.HasChanges;

            // If we now have changes, let's cancel any pending or queued loads
            if (this.HasChanges && this.IsLoadingData)
            {
                this.CancelLoadPrivate();
            }
        }

        /// <summary>
        /// Updates the <see cref="CanRejectChanges"/> property based on the 
        /// <see cref="HasChanges"/> property.
        /// </summary>
        private void UpdateCanRejectChangesProperty()
        {
            this.CanRejectChanges = this.HasChanges;
        }

        /// <summary>
        /// Updates the <see cref="CanSubmitChanges"/> property based on the 
        /// <see cref="HasChanges"/> and <see cref="IsSubmittingChanges"/> properties.
        /// </summary>
        private void UpdateCanSubmitChangesProperty()
        {
            this.CanSubmitChanges = this.HasChanges && !this.IsSubmittingChanges;
        }

        /// <summary>
        /// Update the count properties involved in paging when a load operation has succeeded.
        /// </summary>
        /// <param name="e">The event args from the load completion.</param>
        /// <param name="loadContext">The context of the load that completed.</param>
        /// <param name="loadedEntitiesCount">The count of top-level entities that were loaded.</param>
        private void UpdatePagingCounts(LoadedDataEventArgs e, LoadContext loadContext, int loadedEntitiesCount)
        {
            // Calculate the number of items that we know to exist based on this load operation
            int knownItemCount = loadedEntitiesCount;

            if (loadedEntitiesCount > 0)
            {
                knownItemCount = loadedEntitiesCount + (loadContext.StartPageIndex * loadContext.PageSize);
            }
            else if (!loadContext.IsInitialLoad)
            {
                knownItemCount = this._internalEntityCollection.ItemCount;
            }

            if (e.TotalEntityCount >= 0)
            {
                // The TotalEntityCount was provided, so we know how many items there are from that
                this._internalEntityCollection.ItemCount = e.TotalEntityCount;
                this._internalEntityCollection.TotalItemCount = e.TotalEntityCount;
            }
            else if (loadContext.LoadType == LoadType.LoadLastPages || loadedEntitiesCount < GetLoadSizeCeiling(loadContext.PageSize, loadContext.LoadSize))
            {
                // We were loading the last page, so use our known item count as the total item count
                this._internalEntityCollection.ItemCount = knownItemCount;
                this._internalEntityCollection.TotalItemCount = knownItemCount;
            }
            else if (loadContext.IsInitialLoad)
            {
                // This was an initial load, so we only know about the entities that were loaded
                this._internalEntityCollection.ItemCount = loadedEntitiesCount;
                this._internalEntityCollection.TotalItemCount = -1;
            }
            else
            {
                // We loaded a full page somewhere in the middle of the data
                // Preserve the previous ItemCount if it was higher than what we know based on this load
                this._internalEntityCollection.ItemCount = Math.Max(this._internalEntityCollection.ItemCount, knownItemCount);
                // And don't update TotalItemCount as we have no information for it
            }
        }

        /// <summary>
        /// Validate all filter descriptors and ensure each is valid.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when a filter descriptor is invalid.</exception>
        private void ValidateFilterDescriptors()
        {
            // We'll have a cached expression for unmodified filters
            if (this._filtersExpression != null)
            {
                return;
            }

            Debug.Assert(this._entityType != null, "Unexpected null EntityType");

            foreach (FilterDescriptor filterDescriptor in this.FilterDescriptors)
            {
                try
                {
                    // We'll have a cached expression for unmodified descriptors
                    if (this._expressionCache.ContainsKey(filterDescriptor))
                    {
                        continue;
                    }

                    // Check PropertyPath is valid
                    this.ValidatePropertyPath(filterDescriptor.PropertyPath, "FilterDescriptor", this.FilterDescriptors.IndexOf(filterDescriptor));

                    // Skip ignored filters
                    // We do this check twice; once with unconverted values and again after they're converted.
                    if (Object.Equals(filterDescriptor.Value, filterDescriptor.IgnoredValue))
                    {
                        continue;
                    }

                    // Check Operator is supported
                    PropertyInfo pi = this._entityType.GetPropertyInfo(filterDescriptor.PropertyPath);

                    if (!LinqHelper.IsSupportedOperator(pi.PropertyType, filterDescriptor.Operator))
                    {
                        throw new NotSupportedException(string.Format(
                                CultureInfo.InvariantCulture,
                                DomainDataSourceResources.FilterNotSupported,
                                filterDescriptor.PropertyPath,
                                this._entityType.GetTypeName(),
                                pi.PropertyType.GetTypeName(),
                                filterDescriptor.Operator));
                    }

                    // Check Value can be converted
                    object convertedValue = Utilities.GetConvertedValue(this.Culture, pi.PropertyType, filterDescriptor.Value);

                    // Convert the IgnoredValue unless it is the default
                    if (filterDescriptor.IgnoredValue != FilterDescriptor.DefaultIgnoredValue)
                    {
                        try
                        {
                            object convertedIgnoredValue = Utilities.GetConvertedValue(this.Culture, pi.PropertyType, filterDescriptor.IgnoredValue);

                            // Skip ignored filters
                            // This is the second check and uses converted values.
                            if (Object.Equals(convertedValue, convertedIgnoredValue))
                            {
                                continue;
                            }
                        }
                        catch (Exception exception)
                        {
                            if (exception.IsFatal())
                            {
                                throw;
                            }

                            // If an exception occurs while trying to convert the IgnoredValue, then we know it doesn't match
                            // the Value (which was successfully converted), so we can proceed with the filter. We don't need
                            // IgnoredValue to be valid at all times, we simply need to know whether or not it matches Value.
                            // In fact, setting IgnoredValue to a value that cannot be converted is supported; such as an int
                            // property with an IgnoredValue of String.Empty, which would ignore the filter if a value hasn't
                            // been entered by the end user.
                        }
                    }

                    // Cache the new expression if it can be built
                    this._expressionCache[filterDescriptor] = LinqHelper.BuildFilterExpression(
                        this._entityType,
                        filterDescriptor.PropertyPath,
                        filterDescriptor.Operator,
                        convertedValue,
                        filterDescriptor.IsCaseSensitive);
                }
                catch (Exception exception)
                {
                    if (exception.IsFatal())
                    {
                        throw;
                    }

                    throw new InvalidOperationException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            DomainDataSourceResources.CannotEvaluateDescriptor,
                            "FilterDescriptor",
                            filterDescriptor.PropertyPath),
                        exception);
                }
            }
        }

        /// <summary>
        /// Process all group descriptors, validating that each is valid.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when a group descriptor is invalid.</exception>
        private void ValidateGroupDescriptors()
        {
            Debug.Assert(this._entityType != null, "Unexpected null EntityType");

            foreach (GroupDescriptor groupDescriptor in this.GroupDescriptors)
            {
                // We'll have a cached expression for unmodified descriptors
                if (this._expressionCache.ContainsKey(groupDescriptor))
                {
                    continue;
                }

                try
                {
                    // Check PropertyPath is valid
                    this.ValidatePropertyPath(groupDescriptor.PropertyPath, "GroupDescriptor", this.GroupDescriptors.IndexOf(groupDescriptor));

                    // Cache the new expression if it can be built
                    // We use a sort expression, as we can only sort on the server side and not group
                    this._expressionCache[groupDescriptor] = LinqHelper.BuildPropertyExpression(this._entityType, groupDescriptor.PropertyPath);
                }
                catch (Exception exception)
                {
                    if (exception.IsFatal())
                    {
                        throw;
                    }

                    throw new InvalidOperationException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            DomainDataSourceResources.CannotEvaluateDescriptor,
                            "GroupDescriptor",
                            groupDescriptor.PropertyPath),
                        exception);
                }
            }
        }

        /// <summary>
        /// Validates all <see cref="Parameter"/>s in the <see cref="QueryParameters"/> collection.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if a parameter name is empty of if the entity query cannot be determined.
        /// </exception>
        private void ValidateQueryParameters()
        {
            // We'll have a cached value for unmodified parameters
            if (this._cachedParameters != null)
            {
                return;
            }

            // Check ParameterName is valid
            foreach (Parameter parameter in this.QueryParameters)
            {
                if (string.IsNullOrEmpty(parameter.ParameterName))
                {
                    throw new InvalidOperationException(string.Format(
                        CultureInfo.InvariantCulture,
                        DomainDataSourceResources.QueryParameterNameIsEmpty,
                        this.QueryParameters.IndexOf(parameter)));
                }
            }

            // Validate the entity query before continuing
            this.ValidateEntityQuery();

            // This is the only remaining failure case we need to handle
            if (this._queryMethod == null)
            {
                // MethodAccessStatus.ArgumentSubset
                throw new InvalidOperationException(string.Format(
                    CultureInfo.InvariantCulture,
                    DomainDataSourceResources.EntityQueryMethodHasMismatchedArguments,
                    this.QueryName));
            }
        }

        /// <summary>
        /// Gets a value that indicates whether the properties required to determine the
        /// entity query have been set.
        /// </summary>
        private bool CanValidateEntityQuery
        {
            get
            {
                return (this._cachedParameters != null) ||
                    ((this.DomainContext != null) &&
                    !string.IsNullOrEmpty(this.QueryName) &&
                    !this.QueryParameters.Any(p => string.IsNullOrEmpty(p.ParameterName)));
            }
        }

        /// <summary>
        /// Validates the entity query by determining whether the <see cref="DomainContext"/>,
        /// <see cref="QueryName"/>, and <see cref="QueryParameters"/> can identify a unique
        /// query to load.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when the query parameters have an invalid parameter for the query.</exception>
        private void ValidateEntityQuery()
        {
            // We'll have a cached value for unmodified parameters
            if (this._cachedParameters != null)
            {
                return;
            }

            // Changing the EntityType invalidates our caches
            this.ResetFiltersExpression();
            this._expressionCache.Clear();

            // Check EntityQuery can be identified
            MethodInfo entityQueryMethodInfo;
            Type entityType;
            EntitySet entitySet;

            MethodAccessStatus methodAccessStatus = GetEntityQueryInformation(
                this.DomainContext,
                this.QueryName,
                this.QueryParameters,
                out entityQueryMethodInfo,
                out entityType,
                out entitySet);

            if (methodAccessStatus == MethodAccessStatus.Success)
            {
                this._queryMethod = entityQueryMethodInfo;
                this._entityType = entityType;
                this._internalEntityCollection.BackingEntitySet = entitySet;
                this._internalEntityCollection.EntityType = entityType;

                this._cachedParameters = this.QueryParameters.Select(p => p.ParameterName).ToArray();
            }
            else
            {
                this._queryMethod = null;
                this._entityType = null;
                this.ResetEntitySet();

                DomainDataSource.CheckEntityQueryInformation(
                    methodAccessStatus,
                    this.DomainContext.GetType(),
                    this.QueryName,
                    entityType);
            }
        }

        /// <summary>
        /// Process all sort descriptors, validating that each is valid.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when a sort descriptor is invalid.</exception>
        private void ValidateSortDescriptors()
        {
            Debug.Assert(this._entityType != null, "Unexpected null EntityType");

            foreach (SortDescriptor sortDescriptor in this.SortDescriptors)
            {
                // We'll have a cached expression for unmodified descriptors
                if (this._expressionCache.ContainsKey(sortDescriptor))
                {
                    continue;
                }

                try
                {
                    // Check PropertyPath is valid
                    this.ValidatePropertyPath(sortDescriptor.PropertyPath, "SortDescriptor", this.SortDescriptors.IndexOf(sortDescriptor));

                    // Cache the new expression if it can be built
                    this._expressionCache[sortDescriptor] = LinqHelper.BuildPropertyExpression(this._entityType, sortDescriptor.PropertyPath);
                }
                catch (Exception exception)
                {
                    if (exception.IsFatal())
                    {
                        throw;
                    }

                    throw new InvalidOperationException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            DomainDataSourceResources.CannotEvaluateDescriptor,
                            "SortDescriptor",
                            sortDescriptor.PropertyPath),
                        exception);
                }
            }
        }

        /// <summary>
        /// Callback provided to the <see cref="PagedEntityCollectionView"/> for when a <see cref="ICollectionView.Refresh"/> occurs.
        /// </summary>
        /// <remarks>
        /// This can be called either from a <see cref="ICollectionView.DeferRefresh"/> disposal, or from a direct
        /// <see cref="ICollectionView.Refresh"/> call. A load will be invoked to refresh the data and the view.
        /// </remarks>
        private void ViewRefreshCallback()
        {
            Debug.Assert(this._internalEntityCollectionView.IsRefreshDeferred == false, "Unexpected call to ViewRefreshed when IsRefreshDeferred == true");

            if (!this.CanLoad)
            {
                throw new InvalidOperationException(DomainDataSourceResources.CannotLoadWhenCanLoadIsFalse_Refresh);
            }

            this.ExecuteLoad(this._deferredLoadType ?? LoadType.LoadCurrentPages);
        }

        /// <summary>
        /// Handles collection changed events raised by the <see cref="FilterCollectionManager"/> and enqueues
        /// an immediate reload.
        /// </summary>
        /// <param name="sender">The collection manager</param>
        /// <param name="e">The event args</param>
        private void HandleManagerCollectionChanged_Filter(object sender, EventArgs e)
        {
            if (!this.CanLoad)
            {
                throw new InvalidOperationException(DomainDataSourceResources.CannotLoadWhenCanLoadIsFalse_Filtering);
            }

            this.ResetFiltersExpression();

            this.HandleManagerCollectionChanged(sender, e);
        }

        /// <summary>
        /// Handles property changed events raised by the <see cref="FilterCollectionManager"/> and enqueues
        /// a delayed reload.
        /// </summary>
        /// <param name="sender">The collection manager</param>
        /// <param name="e">The event args</param>
        private void HandleManagerPropertyChanged_Filter(object sender, EventArgs e)
        {
            if (!this.CanLoad)
            {
                throw new InvalidOperationException(DomainDataSourceResources.CannotLoadWhenCanLoadIsFalse_Filtering);
            }

            this.ResetFiltersExpression();

            this.HandleManagerPropertyChanged(sender, e);
        }

        /// <summary>
        /// Handles collection changed events raised by the <see cref="GroupCollectionManager"/> and enqueues
        /// an immediate reload.
        /// </summary>
        /// <param name="sender">The collection manager</param>
        /// <param name="e">The event args</param>
        private void HandleManagerCollectionChanged_Group(object sender, EventArgs e)
        {
            if (!this.CanLoad)
            {
                throw new InvalidOperationException(DomainDataSourceResources.CannotLoadWhenCanLoadIsFalse_Grouping);
            }

            // Whenever grouping changes, we need to clear page tracking
            this._internalEntityCollection.ClearPageTracking(false);

            this.HandleManagerCollectionChanged(sender, e);
        }

        /// <summary>
        /// Handles property changed events raised by the <see cref="GroupCollectionManager"/> and enqueues
        /// a delayed reload.
        /// </summary>
        /// <param name="sender">The collection manager</param>
        /// <param name="e">The event args</param>
        private void HandleManagerPropertyChanged_Group(object sender, EventArgs e)
        {
            if (!this.CanLoad)
            {
                throw new InvalidOperationException(DomainDataSourceResources.CannotLoadWhenCanLoadIsFalse_Grouping);
            }

            // Whenever grouping changes, we need to clear page tracking
            this._internalEntityCollection.ClearPageTracking(false);

            this.HandleManagerPropertyChanged(sender, e);
        }

        /// <summary>
        /// Handles collection changed events raised by the <see cref="ParameterCollectionManager"/> and enqueues
        /// an immediate reload.
        /// </summary>
        /// <param name="sender">The collection manager</param>
        /// <param name="e">The event args</param>
        private void HandleManagerCollectionChanged_Parameter(object sender, EventArgs e)
        {
            if (!this.CanLoad)
            {
                throw new InvalidOperationException(DomainDataSourceResources.CannotLoadWhenCanLoadIsFalse_QueryParameters);
            }

            this.HandleManagerCollectionChanged(sender, e);
        }

        /// <summary>
        /// Handles property changed events raised by the <see cref="ParameterCollectionManager"/> and enqueues
        /// a delayed reload.
        /// </summary>
        /// <param name="sender">The collection manager</param>
        /// <param name="e">The event args</param>
        private void HandleManagerPropertyChanged_Parameter(object sender, EventArgs e)
        {
            if (!this.CanLoad)
            {
                throw new InvalidOperationException(DomainDataSourceResources.CannotLoadWhenCanLoadIsFalse_QueryParameters);
            }

            this.HandleManagerPropertyChanged(sender, e);
        }

        /// <summary>
        /// Handles collection changed events raised by the <see cref="SortCollectionManager"/> and enqueues
        /// an immediate reload.
        /// </summary>
        /// <param name="sender">The collection manager</param>
        /// <param name="e">The event args</param>
        private void HandleManagerCollectionChanged_Sort(object sender, EventArgs e)
        {
            if (!this.CanLoad)
            {
                throw new InvalidOperationException(DomainDataSourceResources.CannotLoadWhenCanLoadIsFalse_Sorting);
            }

            // Whenever sorting changes, we need to clear page tracking
            this._internalEntityCollection.ClearPageTracking(false);

            this.HandleManagerCollectionChanged(sender, e);
        }

        /// <summary>
        /// Handles property changed events raised by the <see cref="SortCollectionManager"/> and enqueues
        /// a delayed reload.
        /// </summary>
        /// <param name="sender">The collection manager</param>
        /// <param name="e">The event args</param>
        private void HandleManagerPropertyChanged_Sort(object sender, EventArgs e)
        {
            if (!this.CanLoad)
            {
                throw new InvalidOperationException(DomainDataSourceResources.CannotLoadWhenCanLoadIsFalse_Sorting);
            }

            // Whenever sorting changes, we need to clear page tracking
            this._internalEntityCollection.ClearPageTracking(false);

            this.HandleManagerPropertyChanged(sender, e);
        }

        /// <summary>
        /// Handles collection changed events raised by a <see cref="CollectionManager"/> and enqueues
        /// an immediate reload.
        /// </summary>
        /// <param name="sender">The collection manager</param>
        /// <param name="e">The event args</param>
        private void HandleManagerCollectionChanged(object sender, EventArgs e)
        {
            if (this._skipNextAutoLoad)
            {
                this._skipNextAutoLoad = false;
                return;
            }

            // We need to ensure the first items will be loaded
            this.DeferLoadFirstItems();

            this.RequestAutoLoad();
        }

        /// <summary>
        /// Handles property changed events raised by a <see cref="CollectionManager"/> and enqueues
        /// a delayed reload.
        /// </summary>
        /// <param name="sender">The collection manager</param>
        /// <param name="e">The event args</param>
        private void HandleManagerPropertyChanged(object sender, EventArgs e)
        {
            if (this._skipNextAutoLoad)
            {
                this._skipNextAutoLoad = false;
                return;
            }

            // We need to ensure the first items will be loaded
            this.DeferLoadFirstItems();
            this.RequestAutoLoad();
        }

        #endregion Methods

        #region Nested Types and Enums

        /// <summary>
        /// Enum used to indicate success or failure when trying to locate a method through reflection.
        /// </summary>
        private enum MethodAccessStatus
        {
            /// <summary>
            /// Load information is gathered successfully.
            /// </summary>
            Success,

            /// <summary>
            /// <see cref="DomainDataSource.QueryName"/> not found.
            /// </summary>
            NameNotFound,

            /// <summary>
            /// <see cref="DomainDataSource.QueryParameters"/> match a subset of a valid <see cref="DomainDataSource.QueryName"/>.
            /// </summary>
            ArgumentSubset,

            /// <summary>
            /// <see cref="DomainDataSource.QueryParameters"/> do not match any <see cref="DomainDataSource.QueryName"/>.
            /// </summary>
            /// <remarks>
            /// </remarks>
            ArgumentMismatch,

            /// <summary>
            /// The <see cref="EntitySet{T}"/> property couldn't be found
            /// </summary>
            EntitySetNotFound
        }

        /// <summary>
        /// Timer interface used in the <see cref="DomainDataSource"/> to enable deterministic unit testing.
        /// </summary>
        internal interface ITimer
        {
            /// <summary>
            /// Gets a value that indicates whether the timer is running.
            /// </summary>
            bool IsEnabled { get; }

            /// <summary>
            /// Gets or sets the amount of time between timer ticks.
            /// </summary>
            TimeSpan Interval { get; set; }

            /// <summary>
            /// Occurs when the timer interval has elapsed.
            /// </summary>
            event EventHandler Tick;

            /// <summary>
            /// Starts the timer.
            /// </summary>
            void Start();

            /// <summary>
            /// Stops the timer.
            /// </summary>
            void Stop();
        }

        /// <summary>
        /// Default implementation of the <see cref="ITimer"/> interface that delegates to
        /// a <see cref="DispatcherTimer"/>.
        /// </summary>
        internal sealed class Timer : ITimer
        {
            private readonly DispatcherTimer _timer = new DispatcherTimer();

            /// <summary>
            /// Gets a value that indicates whether the timer is running.
            /// </summary>
            public bool IsEnabled
            {
                get { return this._timer.IsEnabled; }
            }

            /// <summary>
            /// Gets or sets the amount of time between timer ticks.
            /// </summary>
            public TimeSpan Interval
            {
                get { return this._timer.Interval; }
                set { this._timer.Interval = value; }
            }

            /// <summary>
            /// Occurs when the timer interval has elapsed.
            /// </summary>
            public event EventHandler Tick
            {
                add { this._timer.Tick += value; }
                remove { this._timer.Tick -= value; }
            }

            /// <summary>
            /// Starts the timer.
            /// </summary>
            public void Start()
            {
                this._timer.Start();
            }

            /// <summary>
            /// Stops the timer.
            /// </summary>
            public void Stop()
            {
                this._timer.Stop();
            }
        }

        #endregion Private Types
    }
}
