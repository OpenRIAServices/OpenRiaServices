namespace System.Windows.Controls
{
    /// <summary>
    /// Used to keep track of <see cref="System.ComponentModel.ICollectionView.DeferRefresh"/> calls on the
    /// <see cref="EntityCollectionView"/>, which will prevent the consumer from
    /// calling <see cref="System.ComponentModel.ICollectionView.Refresh"/> on the view. In order to allow
    /// refreshes again, the consumer will have to call <see cref="IDisposable.Dispose"/>,
    /// to end the operation.
    /// </summary>
    /// <remarks>
    /// Also used by the <see cref="DomainDataSource"/> and its <see cref="DomainDataSource.DeferLoad"/> mechanism.
    /// </remarks>
    internal sealed class DeferHelper : IDisposable
    {
        /// <summary>
        /// The callback to issue when this <see cref="DeferHelper"/> is disposed.
        /// </summary>
        private Action _disposedCallback;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeferHelper"/> class.
        /// </summary>
        /// <param name="disposedCallback">
        /// The action to call when this <see cref="DeferHelper"/> is disposed.
        /// </param>
        public DeferHelper(Action disposedCallback)
        {
            this._disposedCallback = disposedCallback;
        }

        /// <summary>
        /// The consumer of this deferral is finished, thus we will call the callback
        /// provided to the constructor, indicating that this object has been disposed.
        /// </summary>
        public void Dispose()
        {
            if (this._disposedCallback != null)
            {
                this._disposedCallback();
                this._disposedCallback = null;
            }

            GC.SuppressFinalize(this);
        }
    }
}
