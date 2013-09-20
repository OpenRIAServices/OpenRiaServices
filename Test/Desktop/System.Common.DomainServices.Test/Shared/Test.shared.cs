using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace TestDomainServices
{
    /// <summary>
    /// This custom validator can be used in testing to verify custom validation is
    /// being called correctly. Call Monitor in your test to start tracking and be sure
    /// to turn it off afterwards. After validation, all the calls can be accessed via
    /// the ValidationCalls member.
    /// </summary>
    public static class DynamicTestValidator
    {
        private static bool _monitor = false;
        private static List<ValidationContext> _validationCalls = new List<ValidationContext>();
        private static Dictionary<object, ValidationResult> _validationResultMap = new Dictionary<object, ValidationResult>();

        public static List<ValidationContext> ValidationCalls 
        { 
            get
            {
                return _validationCalls;
            }
        }

        public static void Monitor(bool monitor)
        {
            if (!monitor)
            {
                _validationCalls.Clear();
            }

            _monitor = monitor;
        }

        public static ValidationResult Validate(object o, ValidationContext context)
        {
            if (_monitor)
            {
                ValidationContext copy = new ValidationContext(context.ObjectInstance, context, context.Items);
                copy.MemberName = context.MemberName;
                copy.DisplayName = context.DisplayName;
                ValidationCalls.Add(copy);
            }

            if (o != null)
            {
                ValidationResult result = null;
                string msg = string.Empty;
                if (_validationResultMap.TryGetValue(o, out result))
                {
                    if (!string.IsNullOrEmpty(context.MemberName))
                    {
                        msg = GetMemberError(result.ErrorMessage, context.MemberName);
                    }
                    else
                    {
                        msg = result.ErrorMessage;
                    }
                    return new ValidationResult(msg, result.MemberNames);
                }
                else if (_validationResultMap.TryGetValue(o.GetType(), out result))
                {
                    msg += GetTypeError(result);
                    return new ValidationResult(msg, result.MemberNames);
                }
            }

            return ValidationResult.Success;
        }

        public static Dictionary<object, ValidationResult> ForcedValidationResults
        {
            get
            {
                return _validationResultMap;
            }
        }

        public static void Reset()
        {
            ForcedValidationResults.Clear();
            Monitor(false);
        }

        public static string GetMemberError(string errorMessage, string memberName)
        {
            return errorMessage + "-" + memberName;
        }

        public static string GetTypeError(ValidationResult result)
        {
            return DynamicTestValidator.GetTypeError(result.ErrorMessage);
        }

        public static string GetTypeError(string errorMessage)
        {
            return errorMessage + "-" + "TypeLevel";
        }
    }
}
