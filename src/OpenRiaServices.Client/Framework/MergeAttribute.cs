using System;

namespace OpenRiaServices.Client
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class MergeAttribute : Attribute
    {
        private readonly bool _isMergeable;

        /// <summary>
        /// Default constructor
        /// Set the IsMergable Property True
        /// </summary>
        public MergeAttribute()
        {
            _isMergeable = true;
        }


        /// <summary>
        /// Constructor used to specify a member is mergeable.
        /// </summary>
        /// <param name="isMergeable">The member name for the mergeable member.</param>
        public MergeAttribute(bool isMergeable)
        {
            _isMergeable = isMergeable;
        }

        /// <summary>
        /// Gets the whether the property is IsMergeable 
        /// </summary>
        public bool IsMergeable
        {
            get { return _isMergeable; }
        }
    }
}
