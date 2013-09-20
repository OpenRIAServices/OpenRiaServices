using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.ServiceModel.DomainServices.Server;

namespace Microsoft.ServiceModel.DomainServices.Server.UnitTesting
{
    /// <summary>
    /// Utility to for identifying and reporting error conditions
    /// </summary>
    internal static class ErrorUtility
    {
        /// <summary>
        /// Throws an exception if there are any validation errors
        /// </summary>
        /// <param name="context">The <see cref="OperationContext"/> of the validation errors</param>
        /// <param name="validationErrors">The validation errors</param>
        /// <exception cref="DomainServiceTestHostException">is thrown if there are any validation errors</exception>
        public static void AssertNoValidationErrors(OperationContext context, IEnumerable<ValidationResult> validationErrors)
        {
            if ((validationErrors != null) && validationErrors.Any())
            {
                ErrorUtility.ReportErrors(context, validationErrors.Select(vr => ErrorUtility.GetErrorMessageForValidation(vr)));
            }
        }

        /// <summary>
        /// Throws an exception if there are any change set errors
        /// </summary>
        /// <param name="context">The <see cref="OperationContext"/> of the change set</param>
        /// <param name="changeSet">The change set</param>
        /// <exception cref="DomainServiceTestHostException">is thrown if there are any change set errors</exception>
        public static void AssertNoChangeSetErrors(OperationContext context, ChangeSet changeSet)
        {
            if (changeSet.HasError)
            {
                List<string> errorMessages = new List<string>();
                foreach (ChangeSetEntry entry in changeSet.ChangeSetEntries)
                {
                    if ((entry.ValidationErrors != null) && entry.ValidationErrors.Any())
                    {
                        errorMessages.Add(ErrorUtility.GetErrorMessageForValidation(entry));
                    }
                    if ((entry.ConflictMembers != null) && entry.ConflictMembers.Any())
                    {
                        errorMessages.Add(ErrorUtility.GetErrorMessageForConflicts(entry));
                    }
                }
                ErrorUtility.ReportErrors(context, errorMessages);
            }
        }

        /// <summary>
        /// Throws an exception containing information about the context and error
        /// </summary>
        /// <param name="context">The <see cref="OperationContext"/> where the exception occurred</param>
        /// <param name="errorMessages">The error messages to include</param>
        /// <exception cref="DomainServiceTestHostException">is thrown</exception>
        private static void ReportErrors(OperationContext context, IEnumerable<string> errorMessages)
        {
            if ((errorMessages != null) && errorMessages.Any())
            {
                string message = string.Format(
                    CultureInfo.CurrentCulture,
                    "One or more errors occurred while calling the '{0}' operation on '{1}':\n {2}",
                    context.OperationName,
                    context.DomainServiceDescription.DomainServiceType,
                    string.Join(", ", errorMessages));
                throw new DomainServiceTestHostException(message);
            }
        }

        private static string GetErrorMessageForValidation(ValidationResult validationResult)
        {
            return validationResult.ToString();
        }

        private static string GetErrorMessageForValidation(ValidationResultInfo validationResultInfo)
        {
            return validationResultInfo.ToString();
        }

        private static string GetErrorMessageForValidation(ChangeSetEntry changeSetEntry)
        {
            return string.Format(
                CultureInfo.CurrentCulture,
                "Validation failed for the entity '{0}' with one or more errors: {1}.",
                changeSetEntry.Entity.GetType(),
                string.Join(", ", changeSetEntry.ValidationErrors.Select(vri => ErrorUtility.GetErrorMessageForValidation(vri))));
        }

        private static string GetErrorMessageForConflicts(ChangeSetEntry changeSetEntry)
        {
            return string.Format(
                CultureInfo.CurrentCulture,
                "There are conflicts for one or more members on the entity '{0}': {1}.",
                changeSetEntry.Entity.GetType(),
                string.Join(", ", changeSetEntry.ConflictMembers));
        }
    }
}
