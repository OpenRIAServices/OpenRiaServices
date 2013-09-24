using System;
using System.ComponentModel;
using System.Windows.Input;

namespace OpenRiaServices.Controls
{
    /// <summary>
    /// Command implementation that delegates to the <see cref="DomainDataSource"/>.
    /// </summary>
    internal class DomainDataSourceCommand : ICommand
    {
        #region Member fields

        private readonly DomainDataSource _domainDataSource;
        private readonly string _propertyFilter;
        private readonly Func<bool> _canExecute;
        private readonly Action _execute;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DomainDataSourceCommand"/> class.
        /// </summary>
        /// <param name="domainDataSource">The <see cref="DomainDataSource"/> to delegate to</param>
        /// <param name="propertyFilter">The filter is compared to the property name in events raised
        /// by the <see cref="DomainDataSource"/> notifier. If it matches, a <see cref="CanExecuteChanged"/>
        /// event will be raised.
        /// </param>
        /// <param name="canExecute">Function used to determine whether this command can be executed</param>
        /// <param name="execute">Action invoked when executing this command</param>
        public DomainDataSourceCommand(DomainDataSource domainDataSource, string propertyFilter, Func<bool> canExecute, Action execute)
        {
            if (domainDataSource == null)
            {
                throw new ArgumentNullException("domainDataSource");
            }
            if (propertyFilter == null)
            {
                throw new ArgumentNullException("propertyFilter");
            }
            if (canExecute == null)
            {
                throw new ArgumentNullException("canExecute");
            }
            if (execute == null)
            {
                throw new ArgumentNullException("execute");
            }

            this._domainDataSource = domainDataSource;
            this._propertyFilter = propertyFilter;
            this._canExecute = canExecute;
            this._execute = execute;

            this._domainDataSource.CommandPropertyNotifier.PropertyChanged += this.HandlePropertyChanged;
        }

        #endregion

        #region Events

        /// <summary>
        /// Occurs when changes occur that affect whether the command should execute.
        /// </summary>
        public event EventHandler CanExecuteChanged;

        #endregion

        #region Methods

        /// <summary>
        /// Determines whether the command can execute in its current state.
        /// </summary>
        /// <param name="parameter">Data used by the command</param>
        /// <returns><c>true</c> if this command can be executed; otherwise, <c>false</c></returns>
        public bool CanExecute(object parameter)
        {
            return this._canExecute();
        }

        /// <summary>
        /// Invokes the command.
        /// </summary>
        /// <param name="parameter">Data used by the command</param>
        /// <exception cref="InvalidOperationException"> is thrown if <see cref="CanExecute"/> returns <c>false</c>
        /// </exception>
        public void Execute(object parameter)
        {
            if (!this.CanExecute(parameter))
            {
                throw new InvalidOperationException(DomainDataSourceResources.CannotExecuteCommand);
            }

            this._execute();
        }

        /// <summary>
        /// Handles a property change event raised by the <see cref="DomainDataSource"/> notifier.
        /// </summary>
        /// <param name="sender">The <see cref="DomainDataSource"/> notifier</param>
        /// <param name="e">The property change event</param>
        private void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (this._propertyFilter.Equals(e.PropertyName))
            {
                this.OnCanExecuteChanged(new EventArgs());
            }
        }

        /// <summary>
        /// Raises a <see cref="CanExecuteChanged"/> event.
        /// </summary>
        /// <param name="e">The event args to raise</param>
        private void OnCanExecuteChanged(EventArgs e)
        {
            EventHandler handler = this.CanExecuteChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        #endregion
    }
}
