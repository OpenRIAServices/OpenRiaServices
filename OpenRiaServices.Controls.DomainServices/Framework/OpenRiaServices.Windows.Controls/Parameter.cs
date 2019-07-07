using System.ComponentModel;
using System.Windows;

namespace OpenRiaServices.Controls
{
    /// <summary>
    /// Descriptor used by the <see cref="DomainDataSource"/> to define parameters
    /// for domain service queries.
    /// </summary>
    public class Parameter : DependencyObject, IRestorable
    {
        #region Static Fields

        /// <summary>
        /// The DependencyProperty for the <see cref="ParameterName"/> property.
        /// </summary>
        public static readonly DependencyProperty ParameterNameProperty =
            DependencyProperty.Register(
                "ParameterName",
                typeof(string),
                typeof(Parameter),
                new PropertyMetadata(string.Empty, Parameter.HandleParameterNameChanged));

        /// <summary>
        /// The DependencyProperty for the <see cref="Value"/> property.
        /// </summary>
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                "Value",
                typeof(object),
                typeof(Parameter),
                new PropertyMetadata(Parameter.HandleValueChanged));

        #endregion

        #region Member Fields

        private readonly PropertyChangedNotifier _notifier;

        private string _originalParameterName;
        private object _originalValue;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Parameter"/> class.
        /// </summary>
        public Parameter() 
        {
            this._notifier = new PropertyChangedNotifier(this);

            ((IRestorable)this).StoreOriginalValue();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the name of the <see cref="Parameter"/>.
        /// </summary>
        public string ParameterName
        {
            get { return (string)this.GetValue(Parameter.ParameterNameProperty); }
            set { this.SetValue(Parameter.ParameterNameProperty, value); }
        }

        /// <summary>
        /// Gets or sets the value of the <see cref="Parameter"/>.
        /// </summary>
        public object Value
        {
            get { return this.GetValue(Parameter.ValueProperty); }
            set { this.SetValue(Parameter.ValueProperty, value); }
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

        private static void HandleParameterNameChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ((Parameter)sender).RaisePropertyChanged(nameof(ParameterName));
        }
        private static void HandleValueChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ((Parameter)sender).RaisePropertyChanged(nameof(Value));
        }

        /// <summary>
        /// Raises a changed event when a dependency property changes
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
            this._originalParameterName = this.ParameterName;
            this._originalValue = this.Value;
        }

        void IRestorable.RestoreOriginalValue()
        {
            this.ParameterName = this._originalParameterName;
            this.Value = this._originalValue;
        }

        #endregion
    }
}
