namespace OpenRiaServices.DomainServices.Tools
{
    /// <summary>
    /// Since this code generator doesn't have assembly references to the server/client
    /// framework assemblies, we're using type name strings rather than type references
    /// during codegen.
    /// </summary>
    internal static class TypeConstants
    {
        /// <summary>
        /// The 'OpenRiaServices.DomainServices.Client.InvokeOperation' type name.
        /// </summary>
        /// <remarks>
        /// Used during code generation.
        /// </remarks>
        public const string InvokeOperationTypeFullName = "OpenRiaServices.DomainServices.Client.InvokeOperation";

        /// <summary>
        /// The 'OpenRiaServices.DomainServices.Client.InvokeResult' type name.
        /// </summary>
        /// <remarks>
        /// Used during code generation.
        /// </remarks>
        public const string InvokeResultTypeFullName = "OpenRiaServices.DomainServices.Client.InvokeResult";

        /// <summary>
        /// The 'OpenRiaServices.DomainServices.Client.EntityQuery' type name.
        /// </summary>
        /// <remarks>
        /// Used during code generation.
        /// </remarks>
        public const string EntityQueryTypeFullName = "OpenRiaServices.DomainServices.Client.EntityQuery";

        /// <summary>
        /// The 'OpenRiaServices.DomainServices.Client.EntityKey' type name.
        /// </summary>
        /// <remarks>
        /// Used during code generation.
        /// </remarks>
        public const string EntityKeyTypeFullName = "OpenRiaServices.DomainServices.Client.EntityKey";

        /// <summary>
        /// The 'OpenRiaServices.DomainServices.Client.EntitySet' type name.
        /// </summary>
        /// <remarks>
        /// Used during code generation.
        /// </remarks>
        public const string EntitySetTypeFullName = "OpenRiaServices.DomainServices.Client.EntitySet";

        /// <summary>
        /// The 'OpenRiaServices.DomainServices.Client.DomainContext' type name.
        /// </summary>
        /// <remarks>
        /// Used during code generation.
        /// </remarks>
        public const string DomainContextTypeFullName = "OpenRiaServices.DomainServices.Client.DomainContext";

        /// <summary>
        /// The 'OpenRiaServices.DomainServices.Client.EntityContainer' type name.
        /// </summary>
        /// <remarks>
        /// Used during code generation.
        /// </remarks>
        public const string EntityContainerTypeFullName = "OpenRiaServices.DomainServices.Client.EntityContainer";

        /// <summary>
        /// The 'OpenRiaServices.DomainServices.Client.EntitySetOperations' type name.
        /// </summary>
        /// <remarks>
        /// Used during code generation.
        /// </remarks>
        public const string EntitySetOperationsTypeFullName = "OpenRiaServices.DomainServices.Client.EntitySetOperations";

        /// <summary>
        /// The 'OpenRiaServices.DomainServices.Client.ChangeSetEntry' type name.
        /// </summary>
        /// <remarks>
        /// Used during code generation.
        /// </remarks>
        public const string ChangeSetEntryTypeFullName = "OpenRiaServices.DomainServices.Client.ChangeSetEntry";

        /// <summary>
        /// The 'OpenRiaServices.DomainServices.Client.EntityRef' type name.
        /// </summary>
        /// <remarks>
        /// Used during code generation.
        /// </remarks>
        public const string EntityRefTypeFullName = "OpenRiaServices.DomainServices.Client.EntityRef";

        /// <summary>
        /// The 'OpenRiaServices.DomainServices.Client.EntityCollection' type name.
        /// </summary>
        /// <remarks>
        /// Used during code generation.
        /// </remarks>
        public const string EntityCollectionTypeFullName = "OpenRiaServices.DomainServices.Client.EntityCollection";

        /// <summary>
        /// The 'OpenRiaServices.DomainServices.Client.EntityType' type name.
        /// </summary>
        /// <remarks>
        /// Used during code generation.
        /// </remarks>
        public const string EntityTypeFullName = "OpenRiaServices.DomainServices.Client.Entity";

        /// <summary>
        /// The 'OpenRiaServices.DomainServices.Client.ComplexObject' type name.
        /// </summary>
        /// <remarks>
        /// Used during code generation.
        /// </remarks>
        public const string ComplexObjectTypeFullName = "OpenRiaServices.DomainServices.Client.ComplexObject";

        /// <summary>
        /// The full DomainClient type name.
        /// </summary>
        /// <remarks>
        /// Used during code generation.
        /// </remarks>
        public const string DomainClientTypeFullName = "OpenRiaServices.DomainServices.Client.DomainClient";

        /// <summary>
        /// The full WebDomainClient type name.
        /// </summary>
        /// <remarks>
        /// Used during code generation.
        /// </remarks>
        public const string WebDomainClientTypeFullName = "OpenRiaServices.DomainServices.Client.WebDomainClient";

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
        public const string WebContextBaseName = "OpenRiaServices.DomainServices.Client.ApplicationServices.WebContextBase";

        /// <summary>
        /// The full ServiceQuery type name.
        /// </summary>
        /// <remarks>
        /// Used during code generation.
        /// </remarks>
        public const string ServiceQueryTypeFullName = "OpenRiaServices.DomainServices.Client.ServiceQuery";

        /// <summary>
        /// The full DomainServiceFault type name.
        /// </summary>
        /// <remarks>
        /// Used during code generation.
        /// </remarks>
        public const string DomainServiceFaultFullName = "OpenRiaServices.DomainServices.Client.DomainServiceFault";

        /// <summary>
        /// The the full QueryResult type name.
        /// </summary>
        /// <remarks>
        /// Used during code generation.
        /// </remarks>
        public const string QueryResultFullName = "OpenRiaServices.DomainServices.Client.QueryResult";

        /// <summary>
        /// The 'System.Collections.Generic.IEnumerable' type name.
        /// </summary>
        /// <remarks>
        /// Used during code generation.
        /// </remarks>
        public const string IEnumerableFullName = "System.Collections.Generic.IEnumerable";

        /// <summary>
        /// The 'OpenRiaServices.DomainServices.Client.HasSideEffects' type name.
        /// </summary>
        /// <remarks>
        /// Used during code generation.
        /// </remarks>
        public const string HasSideEffectsFullName = "OpenRiaServices.DomainServices.Client.HasSideEffects";

        /// <summary>
        /// The 'OpenRiaServices.DomainServices.Client.EntityActionAttribute' type name.
        /// </summary>
        /// <remarks>
        /// Used during code generation.
        /// </remarks>
        public const string EntityActionAttributeFullName = "OpenRiaServices.DomainServices.Client.EntityActionAttribute";

        // TODO: Enable when WebContextGenerator.cs supports Forms Authentication.  
        ///// <summary>
        ///// The 'OpenRiaServices.DomainServices.Client.ApplicationServices.FormsAuthentication' type name.
        ///// </summary>
        ///// <remarks>
        ///// Used during code generation.
        ///// </remarks>
        //public const string FormsAuthenticationName = "OpenRiaServices.DomainServices.Client.ApplicationServices.FormsAuthentication";

        // TODO: Enable when WebContextGenerator.cs supports Windows Authentication.  
        ///// <summary>
        ///// The 'OpenRiaServices.DomainServices.Client.ApplicationServices.WindowsAuthentication' type name.
        ///// </summary>
        ///// <remarks>
        ///// Used during code generation.
        ///// </remarks>
        //public const string WindowsAuthenticationName = "OpenRiaServices.DomainServices.Client.ApplicationServices.WindowsAuthentication";
    }
}
