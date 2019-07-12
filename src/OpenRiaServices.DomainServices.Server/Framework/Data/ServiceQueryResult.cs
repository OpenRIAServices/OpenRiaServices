using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OpenRiaServices.DomainServices.Server
{
    public struct ServiceQueryResult
    {
        public ServiceQueryResult(IEnumerable result, int? totalResult)
        {
            Result = result;
            TotalCount = totalResult ?? DomainService.TotalCountUndefined;
            ValidationErrors = null;
        }

        public ServiceQueryResult(List<ValidationResult> validationErrors)
        {
            ValidationErrors = validationErrors.AsReadOnly();
            TotalCount = DomainService.TotalCountUndefined;
            Result = null;
        }

        public IEnumerable Result { get; }
        public int TotalCount { get; }
        public IReadOnlyCollection<ValidationResult> ValidationErrors { get; }
        public bool HasValidationErrors => ValidationErrors != null;
    }
}
