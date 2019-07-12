using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OpenRiaServices.DomainServices.Server
{
    public struct ServiceInvokeResult
    {
        public ServiceInvokeResult(object result)
        {
            Result = result;
            ValidationErrors = null;
        }

        public ServiceInvokeResult(List<ValidationResult> validationErrors)
        {
            ValidationErrors = validationErrors.AsReadOnly();
            Result = null;
        }

        public object Result { get; }
        public IReadOnlyCollection<ValidationResult> ValidationErrors { get; }
        public bool HasValidationErrors => ValidationErrors != null;
    }
}
