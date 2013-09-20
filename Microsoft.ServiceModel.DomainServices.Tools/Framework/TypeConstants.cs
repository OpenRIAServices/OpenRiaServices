namespace Microsoft.ServiceModel.DomainServices.Tools
{
    /// <summary>
    /// Since this code generator doesn't have assembly references to the server/client
    /// framework assemblies, we're using type name strings rather than type references
    /// during codegen.
    /// </summary>
    internal static class TypeConstants
    {
        /// <summary>
        /// The 'System.ServiceModel.DomainServices.Client.InvokeOperation' type name.
        /// </summary>
        /// <remarks>
        /// Used during code generation.
        /// </remarks>
        public const string InvokeOperationTypeFullName = "System.ServiceModel.DomainServices.Client.InvokeOperation";

        /// <summary>
        /// The 'System.ServiceModel.DomainServices.Client.EntityQuery' type name.
        /// </summary>
        /// <remarks>
        /// Used during code generation.
        /// </remarks>
        public const string EntityQueryTypeFullName = "System.ServiceModel.DomainServices.Client.EntityQuery";

        /// <summary>
        /// The 'System.ServiceModel.DomainServices.Client.EntityKey' type name.
        /// </summary>
        /// <remarks>
        /// Used during code generation.
        /// </remarks>
        public const string EntityKeyTypeFullName = "System.ServiceModel.DomainServices.Client.EntityKey";

        /// <summary>
        /// The 'System.ServiceModel.DomainServices.Client.EntitySet' type name.
        /// </summary>
        /// <remarks>
        /// Used during code generation.
        /// </remarks>
        public const string EntitySetTypeFullName = "System.ServiceModel.DomainServices.Client.EntitySet";

        /// <summary>
        /// The 'System.ServiceModel.DomainServices.Client.DomainContext' type name.
        /// </summary>
        /// <remarks>
        /// Used during code generation.
        /// </remarks>
        public const string DomainContextTypeFullName = "System.ServiceModel.DomainServices.Client.DomainContext";

        /// <summary>
        /// The 'System.ServiceModel.DomainServices.Client.EntityContainer' type name.
        /// </summary>
        /// <remarks>
        /// Used during code generation.
        /// </remarks>
        public const string EntityContainerTypeFullName = "System.ServiceModel.DomainServices.Client.EntityContainer";

        /// <summary>
        /// The 'System.ServiceModel.DomainServices.Client.EntitySetOperations' type name.
        /// </summary>
        /// <remarks>
        /// Used during code generation.
        /// </remarks>
        public const string EntitySetOperationsTypeFullName = "System.ServiceModel.DomainServices.Client.EntitySetOperations";

        /// <summary>
        /// The 'System.ServiceModel.DomainServices.Client.ChangeSetEntry' type name.
        /// </summary>
        /// <remarks>
        /// Used during code generation.
        /// </remarks>
        public const string ChangeSetEntryTypeFullName = "System.ServiceModel.DomainServices.Client.ChangeSetEntry";

        /// <summary>
        /// The 'System.ServiceModel.DomainServices.Client.EntityRef' type name.
        /// </summary>
        /// <remarks>
        /// Used during code generation.
        /// </remarks>
        public const string EntityRefTypeFullName = "System.ServiceModel.DomainServices.Client.EntityRef";

        /// <summary>
        /// The 'System.ServiceModel.DomainServices.Client.EntityCollection' type name.
        /// </summary>
        /// <remarks>
        /// Used during code generation.
        /// </remarks>
        public const string EntityCollectionTypeFullName = "System.ServiceModel.DomainServices.Client.EntityCollection";

        /// <summary>
        /// The 'System.ServiceModel.DomainServices.Client.EntityType' type name.
        /// </summary>
        /// <remarks>
        /// Used during code generation.
        /// </remarks>
        public const string EntityTypeFullName = "System.ServiceModel.DomainServices.Client.Entity";

        /// <summary>
        /// The 'System.ServiceModel.DomainServices.Client.ComplexObject' type name.
        /// </summary>
        /// <remarks>
        /// Used during code generation.
        /// </remarks>
        public const string ComplexObjectTypeFullName = "System.ServiceModel.DomainServices.Client.ComplexObject";

        /// <summary>
        /// The full DomainClient type name.
        /// </summary>
        /// <remarks>
        /// Used during code generation.
        /// </remarks>
        public const string DomainClientTypeFullName = "System.ServiceModel.DomainServices.Client.DomainClient";

        /// <summary>
        /// The full WebDomainClient type name.
        /// </summary>
        /// <remarks>
        /// Used during code generation.
        /// </remarks>
        public const string WebDomainClientTypeFullName = "System.ServiceModel.DomainServices.Client.WebDomainClient";

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
        public const string WebContextBaseName = "System.ServiceModel.DomainServices.Client.ApplicationServices.WebContextBase";

        /// <summary>
        /// The full ServiceQuery type name.
        /// </summary>
        /// <remarks>
        /// Used during code generation.
        /// </remarks>
        public const string ServiceQueryTypeFullName = "System.ServiceModel.DomainServices.Client.ServiceQuery";

        /// <summary>
        /// The full DomainServiceFault type name.
        /// </summary>
        /// <remarks>
        /// Used during code generation.
        /// </remarks>
        public const string DomainServiceFaultFullName = "System.ServiceModel.DomainServices.Client.DomainServiceFault";

        /// <summary>
        /// The the full QueryResult type name.
        /// </summary>
        /// <remarks>
        /// Used during code generation.
        /// </remarks>
        public const string QueryResultFullName = "System.ServiceModel.DomainServices.Client.QueryResult";

        /// <summary>
        /// The 'System.Collections.Generic.IEnumerable' type name.
        /// </summary>
        /// <remarks>
        /// Used during code generation.
        /// </remarks>
        public const string IEnumerableFullName = "System.Collections.Generic.IEnumerable";

        // TODO: Enable when WebContextGenerator.cs supports Forms Authentication.  
        ///// <summary>
        ///// The 'System.ServiceModel.DomainServices.Client.ApplicationServices.FormsAuthentication' type name.
        ///// </summary>
        ///// <remarks>
        ///// Used during code generation.
        ///// </remarks>
        //public const string FormsAuthenticationName = "System.ServiceModel.DomainServices.Client.ApplicationServices.FormsAuthentication";

        // TODO: Enable when WebContextGenerator.cs supports Windows Authentication.  
        ///// <summary>
        ///// The 'System.ServiceModel.DomainServices.Client.ApplicationServices.WindowsAuthentication' type name.
        ///// </summary>
        ///// <remarks>
        ///// Used during code generation.
        ///// </remarks>
        //public const string WindowsAuthenticationName = "System.ServiceModel.DomainServices.Client.ApplicationServices.WindowsAuthentication";
    }
}
