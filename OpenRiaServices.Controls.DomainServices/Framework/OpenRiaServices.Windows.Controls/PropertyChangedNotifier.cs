using System;
using System.ComponentModel;

namespace OpenRiaServices.Controls
{
    /// <summary>
    /// <see cref="INotifyPropertyChanged"/> implementation that is used for internal
    /// change notification between the <see cref="DomainDataSource"/>, Descriptors,
    /// and Commands.
    /// </summary>
    /// <remarks>
    /// This class enables the <see cref="DomainDataSource"/>, <see cref="FilterDescriptor"/>,
    /// <see cref="GroupDescriptor"/>, <see cref="Parameter"/>, and <see cref="SortDescriptor"/>
    /// to support property change notification internally without publicly implementing the
    /// <see cref="INotifyPropertyChanged"/> interface.
    /// </remarks>
    internal class PropertyChangedNotifier : INotifyPropertyChanged
    {
        #region Member fields

        private readonly object _sender;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyChangedNotifier"/> class.
        /// </summary>
        /// <param name="sender">The sender of the property changed events</param>
        internal PropertyChangedNotifier(object sender)
        {
            if (sender == null)
            {
                throw new ArgumentNullException("sender");
            }

            this._sender = sender;
        }

        #endregion

        #region Events

        /// <summary>
        /// Occurs when a property value changes on the owner.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Methods

        /// <summary>
        /// Raises a <see cref="PropertyChanged"/> event for the specified property.
        /// </summary>
        /// <param name="propertyName">The property that changed</param>
        internal void RaisePropertyChanged(string propertyName)
        {
            this.OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Raises a <see cref="PropertyChanged"/> event.
        /// </summary>
        /// <param name="e">The event to raise</param>
        private void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this._sender, e);
            }
        }

        #endregion
    }
}
