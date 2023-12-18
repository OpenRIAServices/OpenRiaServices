using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using OpenRiaServices.Server;
using OpenRiaServices.Server.Authentication;

namespace OpenRiaServices.Tools
{
    /// <summary>
    /// Generator for the <c>WebContext</c> application class.
    /// </summary>
    /// <remarks>
    /// The <c>WebContext</c> class will only be generated for Application assemblies.
    /// </remarks>
    internal class WebContextGenerator : ProxyGenerator
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="WebContextGenerator"/> class.
        /// </summary>
        /// <param name="proxyGenerator">The client proxy generator against which this will generate code.</param>
        internal WebContextGenerator(CodeDomClientCodeGenerator proxyGenerator)
            : base(proxyGenerator)
        {
        }

        #endregion

        #region Methods

        /// <summary>
        /// Generates the WebContext class
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "Should be written later.")]
        public override void Generate()
        {
            // ----------------------------------------------------------------
            // namespace
            // ----------------------------------------------------------------
            CodeNamespace ns = this.ClientProxyGenerator.GetOrGenNamespace(this.ClientProxyGenerator.ClientProxyCodeGenerationOptions.ClientRootNamespace);

            // Missing namespace bails out of code-gen -- error has been logged
            if (ns == null)
            {
                return;
            }

            // Log an informational message to help users see progress
            this.ClientProxyGenerator.LogMessage(Resource.CodeGen_Generating_WebContext);

            // Find the AuthenticationServices and if there's just one, use it as the default.
            IEnumerable<DomainServiceDescription> authDescriptions =
                this.ClientProxyGenerator.DomainServiceDescriptions
                    .Where(d => d.IsAuthenticationService());
            DomainServiceDescription defaultAuthDescription = null;
            if (authDescriptions.Count() > 1)
            {
                this.ClientProxyGenerator.LogMessage(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        Resource.WebContext_ManyAuthServices,
                        string.Join(",", authDescriptions.Select(d => d.DomainServiceType.Name).ToArray())));
            }
            else
            {
                defaultAuthDescription = authDescriptions.FirstOrDefault();
            }

            // ----------------------------------------------------------------
            // public partial sealed class WebContext : WebContextBase
            // ----------------------------------------------------------------
            CodeTypeDeclaration proxyClass = CodeGenUtilities.CreateTypeDeclaration("WebContext", ns.Name);

            proxyClass.IsPartial = true;
            proxyClass.TypeAttributes = TypeAttributes.Public | TypeAttributes.Sealed;
            proxyClass.BaseTypes.Add(CodeGenUtilities.GetTypeReference(TypeConstants.WebContextBaseName, ns.Name, false));
            proxyClass.Comments.AddRange(CodeGenUtilities.GetDocComments(Resource.WebContext_CommentClass, this.ClientProxyGenerator.IsCSharp));

            ns.Types.Add(proxyClass);

            // ----------------------------------------------------------------
            // public WebContext()
            // {
            //     <!-- if there's a default authentication service
            //     this.Authentication = new WebUserService();
            //     -->
            //     this.OnCreated();
            // }
            // ----------------------------------------------------------------
            CodeConstructor constructor = new CodeConstructor();

            constructor.Attributes = MemberAttributes.Public;
            //if (defaultAuthDescription != null)
            //{
            //    // TODO: Choose between Forms and Windows when reading from web.config is available
            //    //constructor.Statements.Add(
            //    //    new CodeAssignStatement(
            //    //        new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), "Authentication"),
            //    //        new CodeObjectCreateExpression(WebContextGenerator.FormsAuthenticationName)));
            //}
            NotificationMethodGenerator onCreatedMethodGenerator = new NotificationMethodGenerator(
                this.ClientProxyGenerator,
                this.ClientProxyGenerator.IsCSharp ? IndentationLevel.Namespace : IndentationLevel.GlobalNamespace);
            constructor.Statements.Add(onCreatedMethodGenerator.OnCreatedMethodInvokeExpression);
            constructor.Comments.AddRange(CodeGenUtilities.GetDocComments(Resource.WebContext_CommentConstructor, this.ClientProxyGenerator.IsCSharp));

            proxyClass.Members.Add(constructor);

            // ----------------------------------------------------------------
            // #region Extensibility Method Definitions
            // partial void OnCreated();
            // #endregion
            // ----------------------------------------------------------------
            proxyClass.Members.AddRange(onCreatedMethodGenerator.PartialMethodsSnippetBlock);

            // ----------------------------------------------------------------
            // public static new WebContext Current
            // {
            //     get { return (WebContext)WebContextBase.Current; }
            // }
            // ----------------------------------------------------------------
            CodeMemberProperty currentProperty = new CodeMemberProperty();
            string typeFullName = (string.IsNullOrEmpty(ns.Name) ? string.Empty : ns.Name + ".") + "WebContext";
            CodeTypeReference targetTypeRef = CodeGenUtilities.GetTypeReference(typeFullName, ns.Name, true);
            CodeTypeReference baseTypeRef = CodeGenUtilities.GetTypeReference(TypeConstants.WebContextBaseName, ns.Name, false);
            CodeTypeReferenceExpression baseTypeRefExp = new CodeTypeReferenceExpression(baseTypeRef);

            currentProperty.Attributes = MemberAttributes.Public | MemberAttributes.Static | MemberAttributes.New;
            currentProperty.Type = targetTypeRef;
            currentProperty.Name = "Current";
            currentProperty.HasGet = true;
            currentProperty.GetStatements.Add(
                new CodeMethodReturnStatement(
                    new CodeCastExpression(currentProperty.Type,
                        new CodePropertyReferenceExpression(baseTypeRefExp, "Current"))));
            currentProperty.Comments.AddRange(CodeGenUtilities.GetDocComments(Resource.WebContext_CommentCurrent, this.ClientProxyGenerator.IsCSharp));

            proxyClass.Members.Add(currentProperty);

            // ----------------------------------------------------------------
            // <!-- if there's a default authentication service
            // public new MyUser User
            // {
            //     get { return (MyUser)base.User; }
            // }
            // -->
            // ----------------------------------------------------------------
            if (defaultAuthDescription != null
                && defaultAuthDescription.TryGetAuthenticationServiceType(out Type genericType)
                && (genericType.GetGenericArguments().Length == 1))
            {
                CodeMemberProperty userProperty = new CodeMemberProperty();

                userProperty.Attributes = MemberAttributes.Public | MemberAttributes.New | MemberAttributes.Final;
                userProperty.Type = CodeGenUtilities.GetTypeReference(
                    genericType.GetGenericArguments()[0], this.ClientProxyGenerator, proxyClass);
                userProperty.Name = "User";
                userProperty.HasGet = true;
                userProperty.GetStatements.Add(
                    new CodeMethodReturnStatement(
                        new CodeCastExpression(userProperty.Type,
                            new CodePropertyReferenceExpression(
                                new CodeBaseReferenceExpression(),
                                "User"))));
                userProperty.Comments.AddRange(CodeGenUtilities.GetDocComments(Resource.WebContext_CommentUser, this.ClientProxyGenerator.IsCSharp));

                proxyClass.Members.Add(userProperty);
            }
        }
        #endregion
    }
}
