namespace System.Windows.Controls
{
    /// <summary>
    /// Interface for <see cref="System.Windows.DependencyObject"/>s that supports change tracking
    /// and reverting.
    /// </summary>
    internal interface IRestorable
    {
        /// <summary>
        /// Stores the current dependency property values as original values.
        /// </summary>
        void StoreOriginalValue();

        /// <summary>
        /// Restores the original values stored the last time <see cref="StoreOriginalValue"/> was called.
        /// </summary>
        void RestoreOriginalValue();
    }
}
