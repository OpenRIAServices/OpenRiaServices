using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Cities
{
    // Shared resource class to demonstrate globalized strings moving across pipeline
    public class Cities_Resources
    {
        public static string CityCaption { get { return "Name of City"; } }
        public static string CityName { get { return "CityName"; } }
        public static string CityPrompt { get { return "Enter the city name"; } }
        public static string CityHelpText { get { return "This is the name of the city"; } }
    }

    // Custom validation class for zip codes.
    // It ensures a zip code begins with a specific prefix
    public class MustStartWithAttribute : ValidationAttribute
    {
        private int _prefix;

        public MustStartWithAttribute(int prefix)
        {
            this.Prefix = prefix;
        }

        public int Prefix
        {
            get
            {
                return this._prefix;
            }
            private set
            {
                this._prefix = value;
            }
        }

        protected override ValidationResult IsValid(object value, ValidationContext context)
        {
            if (value == null)
                return ValidationResult.Success;

            string valueAsString = value.ToString();
            string prefixAsString = this.Prefix.ToString();

            if (valueAsString.StartsWith(prefixAsString))
            {
                return ValidationResult.Success;
            }
            else
            {
                return new ValidationResult(this.FormatErrorMessage(context.MemberName));
            }
        }

        public override string FormatErrorMessage(string name)
        {
            return name + " must start with the prefix " + this.Prefix;
        }
    }

    // Class we can use inside a [CustomValidation] attribute to validate state names
    public static class StateNameValidator
    {
        public static ValidationResult IsStateNameValid(string stateName, ValidationContext context)
        {
            if (stateName == null)
                return ValidationResult.Success;

            if (stateName.Length <= 1)
                return new ValidationResult("The value for " + context.MemberName + " must have exactly 2 letters");

            return ValidationResult.Success;
        }
    }

    // Class we can use inside a [CustomValidation] attribute to validate state names
    public static class CountiesValidator
    {
        public static ValidationResult AreCountiesValid(IEnumerable<County> counties, ValidationContext context)
        {
            if (counties.Any(c => c.Name == "Invalid"))
                return new ValidationResult("The value must not contain invalid counties", new[] { context.MemberName });

            return ValidationResult.Success;
        }
    }

    // Class we can use inside a [CustomValidation] attribute to validate Zip class (cross-field validation)
    public static class ZipValidator
    {
        public static ValidationResult IsZipValid(Zip zip, ValidationContext context)
        {
            if (string.Equals(zip.StateName, zip.CityName))
            {
                return new ValidationResult("Zip codes cannot have matching city and state names", new string[] { "StateName", "CityName" });
            }
            else
            {
                return ValidationResult.Success;
            }
        }
    }

    // Class we can use inside a [CustomValidation] attribute to validate association properties of type City
    public static class CityPropertyValidator
    {
        public const string InvalidZoneName = "INVALID";

        public static ValidationResult IsValidCity(City city, ValidationContext context)
        {
            if (city != null && !Validator.IsValidCity(city))
            {
                return new ValidationResult(string.Format("Cannot set '{0}.{1}' to an Invalid City!", context.ObjectType, context.MemberName), new string[] { context.MemberName });
            }

            return ValidationResult.Success;
        }

        public static ValidationResult IsValidZoneName(string zoneName, ValidationContext context)
        {
            if (zoneName == InvalidZoneName)
            {
                return new ValidationResult("ZoneName is INVALID", new string[] { context.MemberName });
            }

            return ValidationResult.Success;
        }
    }

    public static class ThrowExValidator
    {
        public static ValidationResult IsThrowExValid(object zipObject, ValidationContext context)
        {
            Zip zip = zipObject as Zip;
            if ((zip.Code == 99999) && !(zip.GetType().Assembly.FullName.Contains("EndToEnd") || zip.GetType().Assembly.FullName.Contains("Client")))
            {
                return new ValidationResult("Server fails validation");
            }
            else
            {
                return ValidationResult.Success;
            }
        }
    }

    // Demonstrates shared business logic -- will be copied onto client
    public static class Validator
    {
        public static bool IsValidCity(City city)
        {
            return city.Name != null && city.Name.Length > 0;
        }
    }

    public partial class City : IValidatableObject
    {
        public bool MakeIValidatableObjectFail { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if(MakeIValidatableObjectFail)
            {
                return new[]
                {
                    new ValidationResult("IValidatableObject", new[]{ "MakeIValidatableObjectFail" })
                };
            }
            else
            {
                return Enumerable.Empty<ValidationResult>();
            }
        }
    }

    public enum TimeZone
    {
        Central,
        Mountain,
        Eastern,
        Pacific
    }
}
