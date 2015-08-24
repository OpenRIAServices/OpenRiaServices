using System;
using System.Windows;

namespace OpenRiaServices.Silverlight.ComboBoxExtensions
{
    public class Parameter : DependencyObject
    {
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

        public event EventHandler PropertyChanged;

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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods")]
        public object Value
        {
            get { return this.GetValue(Parameter.ValueProperty); }
            set { this.SetValue(Parameter.ValueProperty, value); }
        }

        private static void HandleParameterNameChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ((Parameter)sender).RaisePropertyChanged();
        }

        private static void HandleValueChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ((Parameter)sender).RaisePropertyChanged();
        }

        private void RaisePropertyChanged()
        {
            EventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new EventArgs());
            }
        }
    }
}
