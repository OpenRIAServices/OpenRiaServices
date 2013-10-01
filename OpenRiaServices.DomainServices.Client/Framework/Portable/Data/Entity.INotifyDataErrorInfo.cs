using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace OpenRiaServices.DomainServices.Client
{
#if SILVERLIGHT
    public abstract partial class Entity : INotifyDataErrorInfo
    {
        private EventHandler<DataErrorsChangedEventArgs> _validationErrorsChanged;

        /// <summary>
        /// Raises the event whenever validation errors have changed for a property.
        /// </summary>
        /// <param name="propertyName">The property whose errors have changed.</param>
        private void RaiseValidationErrorsChanged(string propertyName)
        {
            EventHandler<DataErrorsChangedEventArgs> handler = this._validationErrorsChanged;

            if (handler != null)
            {
                handler(this, new DataErrorsChangedEventArgs(propertyName));
            }
        }

        /// <summary>
        /// Explicitly implement the <see cref="INotifyDataErrorInfo.ErrorsChanged"/> event.
        /// </summary>
        event EventHandler<DataErrorsChangedEventArgs> INotifyDataErrorInfo.ErrorsChanged
        {
            add { this._validationErrorsChanged += value; }
            remove { this._validationErrorsChanged -= value; }
        }

        /// <summary>
        /// Get the errors for the specified property, or the entity-level
        /// errors if <paramref name="propertyName"/> is <c>null</c> of empty.
        /// </summary>
        /// <param name="propertyName">
        /// The property name to get errors for.  When <c>null</c> or empty,
        /// errors that apply to at the entity level will be returned.
        /// </param>
        /// <returns>
        /// The list of errors for the specified <paramref name="propertyName"/>,
        /// or entity-level errors when <paramref name="propertyName"/> is
        /// <c>null</c> or empty.
        /// </returns>
        IEnumerable INotifyDataErrorInfo.GetErrors(string propertyName)
        {
            IEnumerable<ValidationResult> results;

            if (string.IsNullOrEmpty(propertyName))
            {
                // If the property name is null or empty, then we want to include errors
                // where the member names array is empty, or where the member names array
                // contains a null or empty string.
                results = this.ValidationErrors.Where(e => !e.MemberNames.Any() || e.MemberNames.Contains(propertyName));
            }
            else
            {
                // Otherwise, only return the errors that contain the property name
                results = this.ValidationErrors.Where(e => e.MemberNames.Contains(propertyName));
            }

            // Prevent deferred enumeration
            return results.ToArray();
        }

        /// <summary>
        /// Gets a value indicating whether or not the entity presently has errors.
        /// </summary>
        bool INotifyDataErrorInfo.HasErrors
        {
            get { return this.HasValidationErrors; }
        }
#endif
    }
}
