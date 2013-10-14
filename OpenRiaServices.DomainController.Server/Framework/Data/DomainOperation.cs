using System.Runtime.Serialization;

namespace OpenRiaServices.DomainController.Server
{
    /// <summary>
    /// Enumeration of the types of operations a <see cref="DomainController"/> can perform.
    /// </summary>
    [DataContract(Namespace = "DomainControllers")]
    public enum DomainOperation
    {
        /// <summary>
        /// Indicates that no operation is to be performed
        /// </summary>
        [EnumMember]
        None = 0,

        /// <summary>
        /// Indicates a query operation
        /// </summary>
        [EnumMember]
        Query = 1,

        /// <summary>
        /// Indicates an operation that inserts new data
        /// </summary>
        [EnumMember]
        Insert = 2,

        /// <summary>
        /// Indicates an operation that updates existing data
        /// </summary>
        [EnumMember]
        Update = 3,

        /// <summary>
        /// Indicates an operation that deletes existing data
        /// </summary>
        [EnumMember]
        Delete = 4,

        /// <summary>
        /// Indicates a custom domain operation that is executed in a deferred manner
        /// </summary>
        [EnumMember]
        Custom = 5,

        /// <summary>
        /// Indicates a custom domain operation that is executed immediately upon invocation
        /// </summary>
        [EnumMember]
        Invoke = 6
    }
}
