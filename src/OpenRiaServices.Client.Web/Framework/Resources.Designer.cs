﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace OpenRiaServices.Client {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "16.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("OpenRiaServices.Client.Web.Resources", typeof(Resources).Assembly);
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
        ///   Looks up a localized string similar to The DomainContextType is null or invalid and there are no contexts generated from AuthenticationBase&lt;T&gt;..
        /// </summary>
        internal static string ApplicationServices_CannotInitializeDomainContext {
            get {
                return ResourceManager.GetString("ApplicationServices_CannotInitializeDomainContext", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The User type must extend UserBase and provide a default constructor..
        /// </summary>
        internal static string ApplicationServices_CannotInitializeUser {
            get {
                return ResourceManager.GetString("ApplicationServices_CannotInitializeUser", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The current user is anonymous. Data may only be saved for authenticated users..
        /// </summary>
        internal static string ApplicationServices_CannotSaveAnonymous {
            get {
                return ResourceManager.GetString("ApplicationServices_CannotSaveAnonymous", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to GetUser should have returned a single user..
        /// </summary>
        internal static string ApplicationServices_LoadNoUser {
            get {
                return ResourceManager.GetString("ApplicationServices_LoadNoUser", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Logout should have returned a single, anonymous user..
        /// </summary>
        internal static string ApplicationServices_LogoutNoUser {
            get {
                return ResourceManager.GetString("ApplicationServices_LogoutNoUser", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Derived service does not contain a LoadUser method..
        /// </summary>
        internal static string ApplicationServices_NoLoadUserMethod {
            get {
                return ResourceManager.GetString("ApplicationServices_NoLoadUserMethod", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Errors occurred while submitting the user changes..
        /// </summary>
        internal static string ApplicationServices_SaveErrors {
            get {
                return ResourceManager.GetString("ApplicationServices_SaveErrors", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The service must be inactive to update state..
        /// </summary>
        internal static string ApplicationServices_ServiceMustNotBeActive {
            get {
                return ResourceManager.GetString("ApplicationServices_ServiceMustNotBeActive", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Windows authentication does not support logging in..
        /// </summary>
        internal static string ApplicationServices_WANoLogin {
            get {
                return ResourceManager.GetString("ApplicationServices_WANoLogin", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Windows authentication does not support logging out..
        /// </summary>
        internal static string ApplicationServices_WANoLogout {
            get {
                return ResourceManager.GetString("ApplicationServices_WANoLogout", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Method can only be invoked once..
        /// </summary>
        internal static string MethodCanOnlyBeInvokedOnce {
            get {
                return ResourceManager.GetString("MethodCanOnlyBeInvokedOnce", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The operation was canceled..
        /// </summary>
        internal static string OperationCancelled {
            get {
                return ResourceManager.GetString("OperationCancelled", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The operation has not completed..
        /// </summary>
        internal static string OperationNotComplete {
            get {
                return ResourceManager.GetString("OperationNotComplete", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Bitwise operators are not supported in queries..
        /// </summary>
        internal static string QuerySerialization_BitwiseOperatorsNotSupported {
            get {
                return ResourceManager.GetString("QuerySerialization_BitwiseOperatorsNotSupported", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Method &apos;{0}&apos; on type &apos;{1}&apos; is not accessible. Only methods on primitive types, System.Math and System.Convert are supported in queries..
        /// </summary>
        internal static string QuerySerialization_MethodNotAccessible {
            get {
                return ResourceManager.GetString("QuerySerialization_MethodNotAccessible", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Nested query expressions are not supported..
        /// </summary>
        internal static string QuerySerialization_NestedQueriesNotSupported {
            get {
                return ResourceManager.GetString("QuerySerialization_NestedQueriesNotSupported", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &apos;New&apos; Expressions are not supported in queries..
        /// </summary>
        internal static string QuerySerialization_NewExpressionsNotSupported {
            get {
                return ResourceManager.GetString("QuerySerialization_NewExpressionsNotSupported", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Select projections are not supported..
        /// </summary>
        internal static string QuerySerialization_ProjectionsNotSupported {
            get {
                return ResourceManager.GetString("QuerySerialization_ProjectionsNotSupported", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Binary operation &apos;{0}&apos; is not supported..
        /// </summary>
        internal static string QuerySerialization_UnsupportedBinaryOp {
            get {
                return ResourceManager.GetString("QuerySerialization_UnsupportedBinaryOp", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Query operator &apos;{0}&apos; is not supported..
        /// </summary>
        internal static string QuerySerialization_UnsupportedQueryOperator {
            get {
                return ResourceManager.GetString("QuerySerialization_UnsupportedQueryOperator", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Value of type &apos;{0}&apos; cannot be serialized as part of the query. &apos;{0}&apos; is not a supported type..
        /// </summary>
        internal static string QuerySerialization_UnsupportedType {
            get {
                return ResourceManager.GetString("QuerySerialization_UnsupportedType", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unary operation &apos;{0}&apos; is not supported..
        /// </summary>
        internal static string QuerySerialization_UnsupportedUnaryOp {
            get {
                return ResourceManager.GetString("QuerySerialization_UnsupportedUnaryOp", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to WebContextBase.Authentication has not been initialized. This member is only supported in valid implementations..
        /// </summary>
        internal static string WebContext_AuthenticationNotSet {
            get {
                return ResourceManager.GetString("WebContext_AuthenticationNotSet", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Authentication cannot be set after the Application.Startup event has occurred..
        /// </summary>
        internal static string WebContext_CannotModifyAuthentication {
            get {
                return ResourceManager.GetString("WebContext_CannotModifyAuthentication", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The current instance of WebContext is not available.  You must instantiate a WebContext and add it to Application.ApplicationLifetimeObjects within the default App constructor..
        /// </summary>
        internal static string WebContext_NoContexts {
            get {
                return ResourceManager.GetString("WebContext_NoContexts", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Only one WebContextBase can be created per application..
        /// </summary>
        internal static string WebContext_OnlyOne {
            get {
                return ResourceManager.GetString("WebContext_OnlyOne", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to IAsyncResult object did not come from the corresponding async method on this Type..
        /// </summary>
        internal static string WrongAsyncResult {
            get {
                return ResourceManager.GetString("WrongAsyncResult", resourceCulture);
            }
        }
    }
}
