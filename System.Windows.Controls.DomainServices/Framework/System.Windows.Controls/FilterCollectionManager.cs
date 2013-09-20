namespace System.Windows.Controls
{
    /// <summary>
    /// Manager that observes and collates the events raised by a <see cref="FilterDescriptorCollection"/>
    /// and all the <see cref="FilterDescriptor"/>s it contains.
    /// </summary>
    internal class FilterCollectionManager : CollectionManager
    {
        #region Member fields

        private readonly FilterDescriptorCollection _sourceCollection;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="FilterCollectionManager"/> class.
        /// </summary>
        /// <param name="sourceCollection">The collection of <see cref="FilterDescriptor"/>s to manage</param>
        /// <param name="expressionCache">The cache with entries for the <see cref="FilterDescriptor"/>s</param>
        /// <param name="validationAction">The callback for validating items that are added or changed</param>
        public FilterCollectionManager(FilterDescriptorCollection sourceCollection, ExpressionCache expressionCache, Action<FilterDescriptor> validationAction)
        {
            if (sourceCollection == null)
            {
                throw new ArgumentNullException("sourceCollection");
            }
            if (expressionCache == null)
            {
                throw new ArgumentNullException("expressionCache");
            }
            if (validationAction == null)
            {
                throw new ArgumentNullException("validationAction");
            }

            this.ExpressionCache = expressionCache;
            this._sourceCollection = sourceCollection;
            this.ValidationAction = (item) => validationAction((FilterDescriptor)item);
            this.AsINotifyPropertyChangedFunc = (item) => ((FilterDescriptor)item).Notifier;

            this.AddCollection(this._sourceCollection);
        }

        #endregion
    }
}
