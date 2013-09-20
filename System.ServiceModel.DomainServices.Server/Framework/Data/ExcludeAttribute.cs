namespace System.ServiceModel.DomainServices.Server
{
    /// <summary>
    /// Indicates that an entity member should not exist in the code generated 
    /// client view of the entity, and that the value should never be sent to
    /// the client.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class ExcludeAttribute : Attribute
    {
    }
}
