namespace System.ServiceModel.DomainServices.Server
{
    /// <summary>
    /// Attribute applied to a <see cref="DomainService"/> member to indicate that domain
    /// operations should not be inferred from the member.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Field | AttributeTargets.Method | AttributeTargets.Property,
        AllowMultiple = false, Inherited = true)]
    public sealed class IgnoreAttribute : Attribute
    {
    }
}
