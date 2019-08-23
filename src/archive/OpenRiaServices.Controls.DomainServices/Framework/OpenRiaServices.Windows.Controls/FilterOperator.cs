namespace OpenRiaServices.Controls
{
    /// <summary>
    /// Operator used in FilterDescriptor class
    /// </summary>
    public enum FilterOperator
    {
        /// <summary>
        /// Left operand must be smaller than the right one
        /// </summary>
        IsLessThan,

        /// <summary>
        /// Left operand must be smaller than or equal to the right one
        /// </summary>
        IsLessThanOrEqualTo,

        /// <summary>
        /// Left operand must be equal to the right one
        /// </summary>
        IsEqualTo,

        /// <summary>
        /// Left operand must be different from the right one
        /// </summary>
        IsNotEqualTo,

        /// <summary>
        /// Left operand must be larger than the right one
        /// </summary>
        IsGreaterThanOrEqualTo,

        /// <summary>
        /// Left operand must be larger than or equal to the right one
        /// </summary>
        IsGreaterThan,

        /// <summary>
        /// Left operand must start with the right one
        /// </summary>
        StartsWith,

        /// <summary>
        /// Left operand must end with the right one
        /// </summary>
        EndsWith,

        /// <summary>
        /// Left operand must contain the right one
        /// </summary>
        Contains,

        /// <summary>
        /// Left operand must be contained in the right one
        /// </summary>
        IsContainedIn,
    }
}
