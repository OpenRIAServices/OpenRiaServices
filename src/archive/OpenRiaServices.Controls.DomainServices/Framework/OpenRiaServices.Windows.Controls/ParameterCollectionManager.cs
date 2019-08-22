using System;

namespace OpenRiaServices.Controls
{
    /// <summary>
    /// Manager that observes and collates the events raised by a <see cref="ParameterCollection"/>
    /// and all the <see cref="Parameter"/>s it contains.
    /// </summary>
    internal class ParameterCollectionManager : CollectionManager
    {
        #region Member fields

        private readonly ParameterCollection _sourceCollection;
        private readonly Action<ParameterCollection> _collectionValidationAction;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterCollectionManager"/> class.
        /// </summary>
        /// <param name="sourceCollection">The collection of <see cref="Parameter"/>s to manage</param>
        /// <param name="validationAction">The callback for validating items that are added or changed</param>
        /// <param name="collectionValidationAction">The callback for validating the collection when it is changed</param>
        public ParameterCollectionManager(ParameterCollection sourceCollection, Action<Parameter> validationAction, Action<ParameterCollection> collectionValidationAction)
        {
            if (sourceCollection == null)
            {
                throw new ArgumentNullException("sourceCollection");
            }
            if (validationAction == null)
            {
                throw new ArgumentNullException("validationAction");
            }
            if (collectionValidationAction == null)
            {
                throw new ArgumentNullException("collectionValidationAction");
            }

            this._sourceCollection = sourceCollection;
            this._collectionValidationAction = collectionValidationAction;
            this.ValidationAction = (item) => validationAction((Parameter)item);
            this.AsINotifyPropertyChangedFunc = (item) => ((Parameter)item).Notifier;

            this.AddCollection(sourceCollection);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Overridden to support validation of the entire collection.
        /// </summary>
        protected override void OnCollectionChanged()
        {
            this._collectionValidationAction(this._sourceCollection);

            base.OnCollectionChanged();
        }

        /// <summary>
        /// Overridden to support validation of the entire collection.
        /// </summary>
        protected override void OnPropertyChanged()
        {
            this._collectionValidationAction(this._sourceCollection);

            base.OnPropertyChanged();
        }

        #endregion
    }
}
