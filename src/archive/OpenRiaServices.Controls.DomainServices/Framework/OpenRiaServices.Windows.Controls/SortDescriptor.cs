using System.ComponentModel;
using System.Windows;

namespace OpenRiaServices.Controls
{
    /// <summary>
    /// Descriptor used by the <see cref="DomainDataSource"/> to sort data
    /// returned from domain service queries.
    /// </summary>
    public class SortDescriptor : DependencyObject, IRestorable
    {
        #region Static Fields

        /// <summary>
        /// The DependencyProperty for the <see cref="Direction"/> property.
        /// </summary>
        public static readonly DependencyProperty DirectionProperty =
            DependencyProperty.Register(
                "Direction",
                typeof(ListSortDirection),
                typeof(SortDescriptor),
                new PropertyMetadata(SortDescriptor.HandleDirectionChanged));

        /// <summary>
        /// The DependencyProperty for the <see cref="PropertyPath"/> property.
        /// </summary>
        public static readonly DependencyProperty PropertyPathProperty =
            DependencyProperty.Register(
                "PropertyPath",
                typeof(string),
                typeof(SortDescriptor),
                new PropertyMetadata(SortDescriptor.HandlePropertyPathChanged));

        #endregion

        #region Member Fields

        private readonly PropertyChangedNotifier _notifier;

        private ListSortDirection _originalDirection;
        private string _originalPropertyPath;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SortDescriptor"/> class.
        /// </summary>
        public SortDescriptor()
            : this(string.Empty, ListSortDirection.Ascending)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SortDescriptor"/> class.
        /// </summary>
        /// <param name="propertyPath">The sort property path</param>
        /// <param name="direction">The sort direction</param>
        public SortDescriptor(string propertyPath, ListSortDirection direction)
        {
            this._notifier = new PropertyChangedNotifier(this);

            this.PropertyPath = propertyPath;
            this.Direction = direction;

            ((IRestorable)this).StoreOriginalValue();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the sort direction: Ascending or Descending.
        /// </summary>
        public ListSortDirection Direction
        {
            get { return (ListSortDirection)this.GetValue(SortDescriptor.DirectionProperty); }
            set { this.SetValue(SortDescriptor.DirectionProperty, value); }
        }

        /// <summary>
        /// Gets or sets the name of the property path used to sort data.
        /// </summary>
        public string PropertyPath
        {
            get { return (string)this.GetValue(SortDescriptor.PropertyPathProperty); }
            set { this.SetValue(SortDescriptor.PropertyPathProperty, value); }
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

        private static void HandleDirectionChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ((SortDescriptor)sender).RaisePropertyChanged(nameof(Direction));
        }
        private static void HandlePropertyPathChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ((SortDescriptor)sender).RaisePropertyChanged(nameof(PropertyPath));
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
            this._originalDirection = this.Direction;
            this._originalPropertyPath = this.PropertyPath;
        }

        void IRestorable.RestoreOriginalValue()
        {
            this.Direction = this._originalDirection;
            this.PropertyPath = this._originalPropertyPath;
        }

        #endregion
    }
}
