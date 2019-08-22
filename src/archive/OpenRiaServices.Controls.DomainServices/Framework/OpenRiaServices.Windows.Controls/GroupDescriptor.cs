using System.ComponentModel;
using System.Windows;

namespace OpenRiaServices.Controls
{
    /// <summary>
    /// Descriptor used by the <see cref="DomainDataSource"/> to group data
    /// returned from domain service queries.
    /// </summary>
    public class GroupDescriptor : DependencyObject, IRestorable
    {
        #region Static Fields

        /// <summary>
        /// The DependencyProperty for the <see cref="PropertyPath"/> property.
        /// </summary>
        public static readonly DependencyProperty PropertyPathProperty =
            DependencyProperty.Register(
                "PropertyPath",
                typeof(string),
                typeof(GroupDescriptor),
                new PropertyMetadata(GroupDescriptor.HandlePropertyPathChanged));

        #endregion

        #region Member Fields

        private readonly PropertyChangedNotifier _notifier;

        private string _originalPropertyPath;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="GroupDescriptor"/> class.
        /// </summary>
        public GroupDescriptor()
            : this(string.Empty)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GroupDescriptor"/> class.
        /// </summary>
        /// <param name="propertyPath">The group property path</param>
        public GroupDescriptor(string propertyPath)
        {
            this._notifier = new PropertyChangedNotifier(this);

            this.PropertyPath = propertyPath;

            ((IRestorable)this).StoreOriginalValue();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the name of the property path used to group data.
        /// </summary>
        public string PropertyPath
        {
            get { return (string)this.GetValue(GroupDescriptor.PropertyPathProperty); }
            set { this.SetValue(GroupDescriptor.PropertyPathProperty, value); }
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

        private static void HandlePropertyPathChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ((GroupDescriptor)sender).RaisePropertyChanged(nameof(PropertyPath));
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
            this._originalPropertyPath = this.PropertyPath;
        }

        void IRestorable.RestoreOriginalValue()
        {
            this.PropertyPath = this._originalPropertyPath;
        }

        #endregion
    }
}
