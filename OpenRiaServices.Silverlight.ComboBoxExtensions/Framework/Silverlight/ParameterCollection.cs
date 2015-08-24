using System;
using System.Collections.Specialized;
using System.Windows;

namespace OpenRiaServices.Silverlight.ComboBoxExtensions
{
    /// <summary>
    /// Collection of <see cref="Parameter"/> dependency objects.
    /// </summary>
    public class ParameterCollection : DependencyObjectCollection<Parameter>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterCollection"/> class.
        /// </summary>
        public ParameterCollection()
        {
            this.CollectionChanged += this.OnCollectionChanged;
        }

        public event EventHandler CollectionOrPropertyChanged;

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (Parameter p in e.NewItems)
                {
                    p.PropertyChanged += this.OnPropertyChanged;
                }
            }
            // We see a Reset action in the design tool, but only support Add at runtime
            else if (!System.ComponentModel.DesignerProperties.IsInDesignTool)
            {
                throw new NotImplementedException("Support for NotifyCollectionChangedActions other than Add is not implemented. Action=" + e.Action);
            }
            this.RaiseCollectionOrPropertyChanged();
        }

        private void OnPropertyChanged(object sender, EventArgs e)
        {
            this.RaiseCollectionOrPropertyChanged();
        }

        private void RaiseCollectionOrPropertyChanged()
        {
            EventHandler handler = this.CollectionOrPropertyChanged;
            if (handler != null)
            {
                handler(this, new EventArgs());
            }
        }
    }
}
