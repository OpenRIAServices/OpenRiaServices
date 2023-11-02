extern alias SSmDsClient;

using System;
using System.ComponentModel.DataAnnotations;

namespace Cities
{
    [CustomValidation(typeof(City), "ValidateCity")]
    public partial class City
    {
        /// <summary>
        /// Initializes a new instance of a <see cref="City"/> using the specified
        /// state name, which can be invalid.
        /// </summary>
        /// <param name="stateName">
        /// The state name to use for initialization. This can be
        /// an invalid value, as validation won't be performed.
        /// </param>
        public City(string stateName)
        {
            this._stateName = stateName;
        }

        /// <summary>
        /// Gets or sets whether or not the entity-level validation for ValidateCity should fail.
        /// </summary>
        internal bool ThrowValidationExceptions { get; set; }

        /// <summary>
        /// Gets or sets a callback to be used whenever the ValidateProperty method is invoked.
        /// </summary>
        internal Action<ValidationContext> ValidatePropertyCallback { get; set; }

        /// <summary>
        /// Gets or sets a callback to be used whenever the ValidateCity validation method is invoked.
        /// </summary>
        internal Action<ValidationContext> ValidateCityCallback { get; set; }

        /// <summary>
        /// Gets or sets the count of calls to the ValidateCity method, which is an
        /// entity-level validation methods.
        /// </summary>
        internal int ValidateCityCallCount { get; set; }

        protected override void ValidateProperty(ValidationContext context, object value)
        {
            this.ValidatePropertyCallback?.Invoke(context);

            if (this.ThrowValidationExceptions)
            {
                System.ComponentModel.DataAnnotations.Validator.ValidateProperty(value, context);
            }
            else
            {
                base.ValidateProperty(context, value);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the entity-level custom validation should fail.
        /// </summary>
        public bool MakeEntityValidationFail { get; set; }

        public static ValidationResult ValidateCity(City entity, ValidationContext validationContext)
        {
            entity.ValidateCityCallback?.Invoke(validationContext);

            // Increment our call counter
            ++entity.ValidateCityCallCount;

            // And if we're supposed to fail, return the failure result
            if (entity.MakeEntityValidationFail)
            {
                return new ValidationResult("MakeEntityValidationFail is true");
            }

            // Otherwise return success
            return ValidationResult.Success;
        }
    }
}
