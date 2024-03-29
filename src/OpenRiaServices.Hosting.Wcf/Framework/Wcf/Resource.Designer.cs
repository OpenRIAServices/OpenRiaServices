﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace OpenRiaServices.Hosting.Wcf {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resource {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resource() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("OpenRiaServices.Hosting.Wcf.Resource", typeof(Resource).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to One or more associated objects were passed for collection property &apos;{1}&apos; on type &apos;{0}&apos;, but the target collection is null..
        /// </summary>
        internal static string DomainService_AssociationCollectionPropertyIsNull {
            get {
                return ResourceManager.GetString("DomainService_AssociationCollectionPropertyIsNull", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Association collection member &apos;{0}&apos; does not implement IList and does not have an Add method..
        /// </summary>
        internal static string DomainService_InvalidCollectionMember {
            get {
                return ResourceManager.GetString("DomainService_InvalidCollectionMember", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Incorrect SQL cache dependency syntax. The correct syntax is: &lt;databaseEntry&gt;:&lt;tableName&gt;..
        /// </summary>
        internal static string DomainService_InvalidSqlDependencyFormat {
            get {
                return ResourceManager.GetString("DomainService_InvalidSqlDependencyFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The specified resource was not found.
        /// </summary>
        internal static string DomainService_ResourceNotFound {
            get {
                return ResourceManager.GetString("DomainService_ResourceNotFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to A contract with the name &apos;{0}&apos; already exists..
        /// </summary>
        internal static string DomainServiceHost_DuplicateContractName {
            get {
                return ResourceManager.GetString("DomainServiceHost_DuplicateContractName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Service provider must be non null and support scopes.
        /// </summary>
        internal static string DomainServiceHostingConfiguration_ServiceProvider_MustSupportScope {
            get {
                return ResourceManager.GetString("DomainServiceHostingConfiguration_ServiceProvider_MustSupportScope", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to DomainServices &apos;{0}&apos; and &apos;{1}&apos; have the same Type name and cannot be exposed from the same application..
        /// </summary>
        internal static string DomainServiceVirtualPathProvider_DuplicateDomainServiceName {
            get {
                return ResourceManager.GetString("DomainServiceVirtualPathProvider_DuplicateDomainServiceName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {0} InnerException message: {1}.
        /// </summary>
        internal static string FaultException_InnerExceptionDetails {
            get {
                return ResourceManager.GetString("FaultException_InnerExceptionDetails", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unable to determine the authentication scheme to use with the default endpoint bindings..
        /// </summary>
        internal static string NoDefaultAuthScheme {
            get {
                return ResourceManager.GetString("NoDefaultAuthScheme", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Singleton is not supported since DomainServices are not thread safe.
        /// </summary>
        internal static string OpenRiaServicesServiceCollectionExtensions_SingletonNotAllowed {
            get {
                return ResourceManager.GetString("OpenRiaServicesServiceCollectionExtensions_SingletonNotAllowed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The PoxBinaryMessageEncoder only supports content type {0}..
        /// </summary>
        internal static string PoxBinaryMessageEncoder_InvalidContentType {
            get {
                return ResourceManager.GetString("PoxBinaryMessageEncoder_InvalidContentType", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The message has MessageVersion {0} but the encoder is configured for MessageVersion {1}..
        /// </summary>
        internal static string PoxBinaryMessageEncoder_InvalidMessageVersion {
            get {
                return ResourceManager.GetString("PoxBinaryMessageEncoder_InvalidMessageVersion", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The PoxBinaryMessageEncoder only supports MessageVersion.None..
        /// </summary>
        internal static string PoxBinaryMessageEncoder_MessageVersionNotSupported {
            get {
                return ResourceManager.GetString("PoxBinaryMessageEncoder_MessageVersionNotSupported", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Invalid query operator &apos;{0}&apos;..
        /// </summary>
        internal static string Query_InvalidOperator {
            get {
                return ResourceManager.GetString("Query_InvalidOperator", resourceCulture);
            }
        }
    }
}
