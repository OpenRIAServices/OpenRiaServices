using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using OpenRiaServices.DomainServices.Hosting;
using OpenRiaServices.DomainServices.Server;
using System.Text;

namespace OpenRiaServices.VisualStudio.DomainServices.Tools
{
    /// <summary>
    /// View model class for the <see cref="BusinessLogicContext"/> class.
    /// </summary>
    /// <remarks>
    /// This class operates with its corresponding <see cref="BusinessLogicContext"/>
    /// counterpart across AppDomain boundaries by using a shared copy of 
    /// <see cref="ContextData"/> state.
    /// </remarks>
    public class ContextViewModel : INotifyPropertyChanged
    {
        private IBusinessLogicModel _businessLogicModel;
        private IContextData _contextData;
        private List<EntityViewModel> _entities;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContextViewModel"/> class.
        /// </summary>
        /// <param name="businessLogicModel">The <see cref="BusinessLogicModel"/> in the other AppDomain with which to communicate.</param>
        /// <param name="contextData">The shared state with the corresponding <see cref="BusinessLogicContext"/> instance in the other AppDomain.</param>
        public ContextViewModel(IBusinessLogicModel businessLogicModel, IContextData contextData)
        {
            System.Diagnostics.Debug.Assert(businessLogicModel != null, "businessLogicModel cannot be null");
            System.Diagnostics.Debug.Assert(contextData != null, "contextData cannot be null");

            this._businessLogicModel = businessLogicModel;
            this._contextData = contextData;
        }

        /// <summary>
        /// Gets the user visible name of the context (typically the type name)
        /// </summary>
        public string Name
        {
            get
            {
                return this.ContextData.Name;
            }
        }

        /// <summary>
        /// Gets the <see cref="IContextData"/> state shared with the corresponding
        /// <see cref="BusinessLogicContext"/> in the other AppDomain.
        /// </summary>
        public IContextData ContextData
        {
            get
            {
                return this._contextData;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether client access for this
        /// DomainService will be enabled.
        /// </summary>
        public bool IsClientAccessEnabled
        {
            get
            {
                return this.ContextData.IsClientAccessEnabled;
            }

            set
            {
                if (value != this.ContextData.IsClientAccessEnabled)
                {
                    this.ContextData.IsClientAccessEnabled = value;
                    this.RaisePropertyChanged("IsClientAccessEnabled");
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the user has enabled
        /// the exposure of an OData endpoint
        /// </summary>
        public bool IsODataEndpointEnabled
        {
            get
            {
                return this.ContextData.IsODataEndpointEnabled;
            }

            set
            {
                if (value != this.ContextData.IsODataEndpointEnabled)
                {
                    this.ContextData.IsODataEndpointEnabled = value;
                    this.RaisePropertyChanged("IsODataEndpointEnabled");
                }
            }
        }

        /// <summary>
        /// Gets the collection of entities exposed by this context, sorted by name
        /// </summary>
        public IEnumerable<EntityViewModel> Entities
        {
            get
            {
                if (this._entities == null)
                {
                    IEntityData[] entityStates = this._businessLogicModel.GetEntityDataItemsForContext(this.ContextData);
                    this._entities = new List<EntityViewModel>();

                    foreach (EntityData entityState in entityStates)
                    {
                        EntityViewModel entityViewModel = new EntityViewModel(this, entityState);
                        this._entities.Add(entityViewModel);
                    }
                    
                    this._entities.Sort(new Comparison<EntityViewModel>((x, y) => String.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase)));
                }
                return this._entities;
            }
        }

        /// <summary>
        /// Invoked internally whenever any state changes on an entity
        /// belonging to this context.  Used to refresh calculated fields
        /// </summary>
        public void EntityStateChanged()
        {
            // Raise a property change for our calculated properties
            // to force them to be re-evaluated by any UI bound to them
            this.RaisePropertyChanged("IsMetadataClassGenerationAllowed");
            this.RaisePropertyChanged("IsMetadataClassGenerationRequested");
        }

        /// <summary>
        /// Override of <see cref="Object.ToString()"/> to facilitate accessibility
        /// </summary>
        /// <returns>The name of the current context.</returns>
        public override string ToString()
        {
            return this.Name;
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        /// <summary>
        /// Raises a property changed event for the given property name
        /// </summary>
        /// <param name="propertyName">The name of the property.</param>
        private void RaisePropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
