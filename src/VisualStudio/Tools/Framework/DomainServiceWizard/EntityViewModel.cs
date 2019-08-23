using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace OpenRiaServices.VisualStudio.DomainServices.Tools
{
    /// <summary>
    /// View model class entity in the corresponding <see cref="BusinessLogicEntity"/> class.
    /// </summary>
    /// <remarks>This view model class shares state with its corresponding <see cref="BusinessLogicEntity"/>
    /// across AppDomin boundaries via state shared in <see cref="EntityData"/>.
    /// </remarks>
    public class EntityViewModel : INotifyPropertyChanged
    {
        private readonly ContextViewModel _contextViewModel;
        private readonly EntityData _entityData;

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityViewModel"/> class.
        /// </summary>
        /// <param name="contextViewModel">The owning <see cref="ContextViewModel"/>.</param>
        /// <param name="entityData">The shared <see cref="EntityData"/> state.</param>
        public EntityViewModel(ContextViewModel contextViewModel, EntityData entityData)
        {
            this._contextViewModel = contextViewModel;
            this._entityData = entityData;
        }

        /// <summary>
        /// Gets the <see cref="ContextViewModel"/> owner of the current instance.
        /// </summary>
        public ContextViewModel ContextViewModel
        {
            get
            {
                return this._contextViewModel;
            }
        }

        /// <summary>
        /// Gets the <see cref="EntityData"/> state shared with the corresponding
        /// <see cref="BusinessLogicEntity"/>.
        /// </summary>
        public EntityData EntityData
        {
            get
            {
                return this._entityData;
            }
        }

        /// <summary>
        /// Gets the user visible name of the entity
        /// </summary>
        public string Name
        {
            get
            {
                return this.EntityData.Name;
            }
        }

        /// <summary>
        /// Gets or sets the value indicating whether this entity will be included in code gen
        /// </summary>
        public bool IsIncluded
        { 
            get 
            {
                return this.EntityData.IsIncluded && EntityData.CanBeIncluded; 
            } 

            set 
            {
                if (this.EntityData.IsIncluded != value)
                {
                    this.EntityData.IsIncluded = value;
                    this.RaisePropertyChanged(nameof(IsIncluded));
                }
            }  
        }

        /// <summary>
        /// Gets or sets the value indicating whether this entity will generate additional code for create/update/delete
        /// </summary>
        /// <remarks>Note that setting this value to <c>true</c> also sets <see cref="IsIncluded"/> as well.</remarks>
        public bool IsEditable
        { 
            get 
            {
                return this.EntityData.IsEditable && this.EntityData.CanBeEdited; 
            } 

            set 
            {
                // No-change bypasses setting field and property notification.
                // Also, any attempt to set it to true when the entity cannot be edited is a silent nop
                if (value != this.EntityData.IsEditable && (!value || this.EntityData.CanBeEdited))
                {
                    this.EntityData.IsEditable = value;
                    this.RaisePropertyChanged(nameof(IsEditable));

                    // Setting editability also sets include for convenience
                    if (value)
                    {
                        this.IsIncluded = true;
                    }
                }
            }  
        }

        /// <summary>
        /// Gets a value indicating whether it is legal to include this entity
        /// </summary>
        public bool CanBeIncluded
        {
            get
            {
                return this.EntityData.CanBeIncluded;
            }
        }

        /// <summary>
        /// Gets a value indicating whether it is legal for this entity to be edited
        /// </summary>
        public bool CanBeEdited
        {
            get
            {
                return this.EntityData.CanBeEdited;
            }
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        /// <summary>
        /// Raises a property changed event for the given property
        /// </summary>
        /// <param name="propertyName">The name of the property.</param>
        private void RaisePropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }

            // Inform parent context some property changed
            this.ContextViewModel.EntityStateChanged();
        }

        /// <summary>
        /// Override of <see cref="Object.ToString()"/> to facilitate accessibility
        /// </summary>
        /// <returns>The name of the current context.</returns>
        public override string ToString()
        {
            return this.Name;
        }
    }
}
