namespace OpenRiaServices.Tools
{
    /// <summary>
    /// Since this code generator doesn't have assembly references to the server/client
    /// framework assemblies, we're using type name strings rather than type references
    /// during codegen.
    /// </summary>
    internal static class TypeConstants
    {
        /// <summary>
        /// The 'OpenRiaServices.Client.InvokeOperation' type name.
        /// </summary>
        /// <remarks>
        /// Used during code generation.
        /// </remarks>
        public const string InvokeOperationTypeFullName = "OpenRiaServices.Client.InvokeOperation";

        /// <summary>
        /// The 'OpenRiaServices.Client.InvokeResult' type name.
        /// </summary>
        /// <remarks>
        /// Used during code generation.
        /// </remarks>
        public const string InvokeResultTypeFullName = "OpenRiaServices.Client.InvokeResult";

        /// <summary>
        /// The 'OpenRiaServices.Client.EntityQuery' type name.
        /// </summary>
        /// <remarks>
        /// Used during code generation.
        /// </remarks>
        public const string EntityQueryTypeFullName = "OpenRiaServices.Client.EntityQuery";

        /// <summary>
        /// The 'OpenRiaServices.Client.EntityKey' type name.
        /// </summary>
        /// <remarks>
        /// Used during code generation.
        /// </remarks>
        public const string EntityKeyTypeFullName = "OpenRiaServices.Client.EntityKey";

        /// <summary>
        /// The 'OpenRiaServices.Client.EntitySet' type name.
        /// </summary>
        /// <remarks>
        /// Used during code generation.
        /// </remarks>
        public const string EntitySetTypeFullName = "OpenRiaServices.Client.EntitySet";

        /// <summary>
        /// The 'OpenRiaServices.Client.DomainContext' type name.
        /// </summary>
        /// <remarks>
        /// Used during code generation.
        /// </remarks>
        public const string DomainContextTypeFullName = "OpenRiaServices.Client.DomainContext";

        /// <summary>
        /// The 'OpenRiaServices.Client.EntityContainer' type name.
        /// </summary>
        /// <remarks>
        /// Used during code generation.
        /// </remarks>
        public const string EntityContainerTypeFullName = "OpenRiaServices.Client.EntityContainer";

        /// <summary>
        /// The 'OpenRiaServices.Client.EntitySetOperations' type name.
        /// </summary>
        /// <remarks>
        /// Used during code generation.
        /// </remarks>
        public const string EntitySetOperationsTypeFullName = "OpenRiaServices.Client.EntitySetOperations";

        /// <summary>
        /// The 'OpenRiaServices.Client.ChangeSetEntry' type name.
        /// </summary>
        /// <remarks>
        /// Used during code generation.
        /// </remarks>
        public const string ChangeSetEntryTypeFullName = "OpenRiaServices.Client.ChangeSetEntry";

        /// <summary>
        /// The 'OpenRiaServices.Client.EntityRef' type name.
        /// </summary>
        /// <remarks>
        /// Used during code generation.
        /// </remarks>
        public const string EntityRefTypeFullName = "OpenRiaServices.Client.EntityRef";

        /// <summary>
        /// The 'OpenRiaServices.Client.EntityCollection' type name.
        /// </summary>
        /// <remarks>
        /// Used during code generation.
        /// </remarks>
        public const string EntityCollectionTypeFullName = "OpenRiaServices.Client.EntityCollection";

        /// <summary>
        /// The 'OpenRiaServices.Client.EntityType' type name.
        /// </summary>
        /// <remarks>
        /// Used during code generation.
        /// </remarks>
        public const string EntityTypeFullName = "OpenRiaServices.Client.Entity";

        /// <summary>
        /// The 'OpenRiaServices.Client.ComplexObject' type name.
        /// </summary>
        /// <remarks>
        /// Used during code generation.
        /// </remarks>
        public const string ComplexObjectTypeFullName = "OpenRiaServices.Client.ComplexObject";

        /// <summary>
        /// The full DomainClient type name.
        /// </summary>
        /// <remarks>
        /// Used during code generation.
        /// </remarks>
        public const string DomainClientTypeFullName = "OpenRiaServices.Client.DomainClient";

        /// <summary>
        /// The full WebDomainClient type name.
        /// </summary>
        /// <remarks>
        /// Used during code generation.
        /// </remarks>
        public const string WebDomainClientTypeFullName = "OpenRiaServices.Client.WebDomainClient";

        /// <summary>
        /// The default DomainClient type name.
        /// </summary>
        /// <remarks>
        /// Used during code generation.
        /// </remarks>
        public const string DefaultDomainClientTypeFullName = TypeConstants.WebDomainClientTypeFullName;

        /// <summary>
        /// The full WebContextBase type name.
        /// </summary>
        /// <remarks>
        /// Used during code generation.
        /// </remarks>
        public const string WebContextBaseName = "OpenRiaServices.Client.Authentication.WebContextBase";

        /// <summary>
        /// The full ServiceQuery type name.
        /// </summary>
        /// <remarks>
        /// Used during code generation.
        /// </remarks>
        public const string ServiceQueryTypeFullName = "OpenRiaServices.Client.ServiceQuery";

        /// <summary>
        /// The full DomainServiceFault type name.
        /// </summary>
        /// <remarks>
        /// Used during code generation.
        /// </remarks>
        public const string DomainServiceFaultFullName = "OpenRiaServices.Client.DomainServiceFault";

        /// <summary>
        /// The the full QueryResult type name.
        /// </summary>
        /// <remarks>
        /// Used during code generation.
        /// </remarks>
        public const string QueryResultFullName = "OpenRiaServices.Client.QueryResult";

        /// <summary>
        /// The 'System.Collections.Generic.IEnumerable' type name.
        /// </summary>
        /// <remarks>
        /// Used during code generation.
        /// </remarks>
        public const string IEnumerableFullName = "System.Collections.Generic.IEnumerable";

        /// <summary>
        /// The 'OpenRiaServices.Client.HasSideEffects' type name.
        /// </summary>
        /// <remarks>
        /// Used during code generation.
        /// </remarks>
        public const string HasSideEffectsFullName = "OpenRiaServices.Client.HasSideEffects";

        /// <summary>
        /// The 'OpenRiaServices.Client.EntityActionAttribute' type name.
        /// </summary>
        /// <remarks>
        /// Used during code generation.
        /// </remarks>
        public const string EntityActionAttributeFullName = "OpenRiaServices.Client.EntityAction";

        // TODO: Enable when WebContextGenerator.cs supports Forms Authentication.  
        ///// <summary>
        ///// The 'OpenRiaServices.Client.Authentication.FormsAuthentication' type name.
        ///// </summary>
        ///// <remarks>
        ///// Used during code generation.
        ///// </remarks>
        //public const string FormsAuthenticationName = "OpenRiaServices.Client.Authentication.FormsAuthentication";

        // TODO: Enable when WebContextGenerator.cs supports Windows Authentication.  
        ///// <summary>
        ///// The 'OpenRiaServices.Client.Authentication.WindowsAuthentication' type name.
        ///// </summary>
        ///// <remarks>
        ///// Used during code generation.
        ///// </remarks>
        //public const string WindowsAuthenticationName = "OpenRiaServices.Client.Authentication.WindowsAuthentication";
    }
}
