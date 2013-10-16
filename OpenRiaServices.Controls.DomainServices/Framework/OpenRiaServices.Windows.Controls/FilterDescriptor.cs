using System.ComponentModel;
using System.Windows;

namespace OpenRiaServices.Controls
{
    /// <summary>
    /// Descriptor used by the <see cref="DomainDataSource"/> to filter data
    /// returned from domain service queries.
    /// </summary>
    public class FilterDescriptor : DependencyObject, IRestorable
    {
        #region Static Fields

        /// <summary>
        /// The default value of the <see cref="IgnoredValue"/> property.
        /// </summary>
        public static readonly object DefaultIgnoredValue = new object();

        /// <summary>
        /// The DependencyProperty for the <see cref="IgnoredValue"/> property.
        /// </summary>
        public static readonly DependencyProperty IgnoredValueProperty =
            DependencyProperty.Register(
                "IgnoredValue",
                typeof(object),
                typeof(FilterDescriptor),
                new PropertyMetadata(FilterDescriptor.DefaultIgnoredValue, FilterDescriptor.HandleIgnoredValueChanged));

        /// <summary>
        /// The DependencyProperty for the <see cref="IsCaseSensitive"/> property.
        /// </summary>
        public static readonly DependencyProperty IsCaseSensitiveProperty =
            DependencyProperty.Register(
                "IsCaseSensitive",
                typeof(bool),
                typeof(FilterDescriptor),
                new PropertyMetadata(FilterDescriptor.HandleIsCaseSensitiveChanged));

        /// <summary>
        /// The DependencyProperty for the <see cref="Operator"/> property.
        /// </summary>
        public static readonly DependencyProperty OperatorProperty =
            DependencyProperty.Register(
                "Operator",
                typeof(FilterOperator),
                typeof(FilterDescriptor),
                new PropertyMetadata(FilterDescriptor.HandleOperatorChanged));

        /// <summary>
        /// The DependencyProperty for the <see cref="PropertyPath"/> property.
        /// </summary>
        public static readonly DependencyProperty PropertyPathProperty =
            DependencyProperty.Register(
                "PropertyPath",
                typeof(string),
                typeof(FilterDescriptor),
                new PropertyMetadata(FilterDescriptor.HandlePropertyPathChanged));

        /// <summary>
        /// The DependencyProperty for the <see cref="Value"/> property.
        /// </summary>
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                "Value",
                typeof(object),
                typeof(FilterDescriptor),
                new PropertyMetadata(FilterDescriptor.HandleValueChanged));

        #endregion

        #region Member Fields

        private readonly PropertyChangedNotifier _notifier;

        private object _originalIgnoredValue;
        private bool _originalIsCaseSensitive;
        private FilterOperator _originalOperator;
        private string _originalPropertyPath;
        private object _originalValue;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="FilterDescriptor"/> class.
        /// </summary>
        public FilterDescriptor()
            : this(string.Empty, FilterOperator.IsEqualTo, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FilterDescriptor"/> class using
        /// the specified parameters.
        /// </summary>
        /// <param name="propertyPath">The filter property path</param>
        /// <param name="filterOperator">The filter operator</param>
        /// <param name="filterValue">The filter value</param>
        public FilterDescriptor(string propertyPath, FilterOperator filterOperator, object filterValue)
        {
            this._notifier = new PropertyChangedNotifier(this);

            this.PropertyPath = propertyPath;
            this.Operator = filterOperator;
            this.Value = filterValue;

            ((IRestorable)this).StoreOriginalValue();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the value for the right operand for which this filter should be ignored.
        /// </summary>
        /// <remarks>
        /// If <see cref="Value"/> matches <see cref="IgnoredValue"/>, this filter will not be applied
        /// to the load query by the <see cref="DomainDataSource"/>. The <see cref="IgnoredValue"/> is
        /// compared to <see cref="Value"/> twice in the <see cref="DomainDataSource"/>. First, it is
        /// strictly compared using an <see cref="System.Object.Equals(Object, Object)"/> comparison. Second,
        /// both values are converted to type of the property specified by the <see cref="PropertyPath"/>
        /// and compared again. If either conversion matches, this filter is ignored.
        /// <para>
        /// For example, the following Value/IgnoredValue pairs will all match for an integer property
        /// and result in the filter being ignored: 0/0, 0/"0", "0"/"0", and "0"/0.
        /// </para>
        /// <para>
        /// This property is set to <see cref="DefaultIgnoredValue"/> by default. The default value
        /// will only match if <see cref="Value"/> is also set to <see cref="DefaultIgnoredValue"/>.
        /// </para>
        /// </remarks>
        public object IgnoredValue
        {
            get { return this.GetValue(FilterDescriptor.IgnoredValueProperty); }
            set { this.SetValue(FilterDescriptor.IgnoredValueProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the filter is case sensitive for <c>string</c> values.
        /// </summary>
        /// <remarks>
        /// When this is <c>true</c>, the load query in the <see cref="DomainDataSource"/> will represent
        /// case sensitivity. However, it is not guaranteed that the data store used by the domain service
        /// will also respect case sensitivity.
        /// </remarks>
        public bool IsCaseSensitive
        {
            get { return (bool)this.GetValue(FilterDescriptor.IsCaseSensitiveProperty); }
            set { this.SetValue(FilterDescriptor.IsCaseSensitiveProperty, value); }
        }

        /// <summary>
        /// Gets or sets the filter operator.
        /// </summary>
        public FilterOperator Operator
        {
            get { return (FilterOperator)this.GetValue(FilterDescriptor.OperatorProperty); }
            set { this.SetValue(FilterDescriptor.OperatorProperty, value); }
        }

        /// <summary>
        /// Gets or sets the name of the property path used as the left operand.
        /// </summary>
        public string PropertyPath
        {
            get { return (string)this.GetValue(FilterDescriptor.PropertyPathProperty); }
            set { this.SetValue(FilterDescriptor.PropertyPathProperty, value); }
        }

        /// <summary>
        /// Gets or sets the value of the right operand.
        /// </summary>
        /// <remarks>
        /// This will be used by the <see cref="DomainDataSource"/> to compose a filter for the load
        /// query. It will be applied following the pattern
        /// <c>[Entity].[PropertyPath] [Operator] [Value]</c>. For example, a query might look like
        /// <c>Customer.Name == "CurrentCustomerName"</c>.
        /// </remarks>
        public object Value
        {
            get { return this.GetValue(FilterDescriptor.ValueProperty); }
            set { this.SetValue(FilterDescriptor.ValueProperty, value); }
        }

        /// <summary>
        /// Gets a notifier instance that will raise property changes events for this descriptor 
        /// when the dependency properties change.
        /// </summary>
        internal INotifyPropertyChanged Notifier
        {
            get { return this._notifier; }
        }

        #endregion

        #region Methods

        private static void HandleIgnoredValueChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ((FilterDescriptor)sender).RaisePropertyChanged("IgnoredValue");
        }
        private static void HandleIsCaseSensitiveChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ((FilterDescriptor)sender).RaisePropertyChanged("IsCaseSensitive");
        }
        private static void HandleOperatorChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ((FilterDescriptor)sender).RaisePropertyChanged("Operator");
        }
        private static void HandlePropertyPathChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ((FilterDescriptor)sender).RaisePropertyChanged("PropertyPath");
        }
        private static void HandleValueChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ((FilterDescriptor)sender).RaisePropertyChanged("Value");
        }

        /// <summary>
        /// Raises a changed event when a dependency property changes.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed</param>
        private void RaisePropertyChanged(string propertyName)
        {
            this._notifier.RaisePropertyChanged(propertyName);
        }

        #endregion

        #region IRestorable

        void IRestorable.StoreOriginalValue()
        {
            this._originalIgnoredValue = this.IgnoredValue;
            this._originalIsCaseSensitive = this.IsCaseSensitive;
            this._originalOperator = this.Operator;
            this._originalPropertyPath = this.PropertyPath;
            this._originalValue = this.Value;
        }

        void IRestorable.RestoreOriginalValue()
        {
            this.IgnoredValue = this._originalIgnoredValue;
            this.IsCaseSensitive = this._originalIsCaseSensitive;
            this.Operator = this._originalOperator;
            this.PropertyPath = this._originalPropertyPath;
            this.Value = this._originalValue;
        }

        #endregion
    }
}
