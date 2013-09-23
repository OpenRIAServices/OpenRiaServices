using System.ComponentModel;
using OpenRiaServices.DomainServices.Client;

namespace System.Windows.Controls
{
    /// <summary>
    /// Event arguments used for the DomainDataSource's Loading event
    /// </summary>
    public sealed class LoadingDataEventArgs : CancelEventArgs
    {
        #region Member Fields

        private LoadBehavior _loadBehavior;
        private EntityQuery _query;
        private bool _restoreLoadSettings;

        #endregion Member Fields

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="LoadingDataEventArgs"/> class.
        /// </summary>
        /// <param name="query">Default query that will be used for the load operation</param>
        /// <param name="loadBehavior">Default load behavior that will be used for the load operation</param>
        internal LoadingDataEventArgs(EntityQuery query, LoadBehavior loadBehavior)
        {
            this.Query = query;
            this.LoadBehavior = loadBehavior;
            this.RestoreLoadSettings = true;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the load behavior applied to this load operation
        /// </summary>
        public LoadBehavior LoadBehavior
        {
            get { return this._loadBehavior; }
            set { this._loadBehavior = value; }
        }

        /// <summary>
        /// Gets or sets the entity query executed remotely
        /// </summary>
        public EntityQuery Query
        {
            get { return this._query; }
            set { this._query = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the DomainDataSource restores the 
        /// load/filter/sort/group settings to the state they were at the last successful 
        /// load operation. This restoration only occurs when both e.Cancel and
        /// e.RestoreLoadSettings are set to True.
        /// </summary>
        public bool RestoreLoadSettings
        {
            get { return this._restoreLoadSettings; }
            set { this._restoreLoadSettings = value; }
        }

        #endregion
    }
}
