using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using OpenRiaServices.DomainServices.Server;

namespace OpenRiaServices.DomainServices.Tools
{
    /// <summary>
    /// Proxy generator for custom methods.
    /// </summary>
    internal class CustomMethodProxyGenerator : ProxyGenerator
    {
        private Type _entityType;
        private ICollection<DomainServiceDescription> _domainServiceDescriptions;
        private CodeTypeDeclaration _proxyClass;
        private NotificationMethodGenerator _notificationMethodGen;

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomMethodProxyGenerator"/> class.
        /// </summary>
        /// <param name="proxyGenerator">The client proxy generator against which this will generate code.  Cannot be null.</param>
        /// <param name="proxyClass">Entity <see cref="CodeTypeDeclaration"/> into which to generate code</param>
        /// <param name="entityType">The type of the entity.  Cannot be null.</param>
        /// <param name="domainServiceDescriptions">Collection of all <see cref="DomainServiceDescription"/>s defined in this project</param>
        /// <param name="notificationMethodGen">Code generator for OnMethodName() methods</param>
        public CustomMethodProxyGenerator(CodeDomClientCodeGenerator proxyGenerator, CodeTypeDeclaration proxyClass, Type entityType, ICollection<DomainServiceDescription> domainServiceDescriptions, NotificationMethodGenerator notificationMethodGen)
            : base(proxyGenerator)
        {
            this._entityType = entityType;
            this._proxyClass = proxyClass;
            this._domainServiceDescriptions = domainServiceDescriptions;
            this._notificationMethodGen = notificationMethodGen;
        }

        /// <summary>
        /// This is the root method for generating invoke method and guard properties for custom methods
        /// </summary>
        public override void Generate()
        {
            // gather list of custom methods from all the custom services for this entityType
            Dictionary<string, DomainOperationEntry> entityCustomMethods = new Dictionary<string, DomainOperationEntry>();
            Dictionary<string, DomainServiceDescription> customMethodToDescriptionMap = new Dictionary<string, DomainServiceDescription>();
            string methodName;
            bool isDerivedEntityType = false;

            foreach (DomainServiceDescription description in this._domainServiceDescriptions)
            {
                // Determine whether this is a derived entity type, because the
                // generated code will be different
                Type rootEntityType = description.GetRootEntityType(this._entityType);
                isDerivedEntityType |= (rootEntityType != null && rootEntityType != this._entityType);

                foreach (DomainOperationEntry customMethod in description.GetCustomMethods(this._entityType))
                {
                    methodName = customMethod.Name;
                    if (entityCustomMethods.ContainsKey(methodName))
                    {
                        this.ClientProxyGenerator.LogError(string.Format(CultureInfo.CurrentCulture, Resource.EntityCodeGen_DuplicateCustomMethodName, methodName, this._entityType, customMethodToDescriptionMap[methodName].DomainServiceType, description.DomainServiceType));
                    }
                    else if (this._proxyClass.Members.Cast<CodeTypeMember>().Any(member => member.Name == methodName))
                    {
                        this.ClientProxyGenerator.LogError(string.Format(CultureInfo.CurrentCulture, Resource.EntityCodeGen_NamingCollision_EntityCustomMethodNameAlreadyExists, this._entityType, methodName));
                    }
                    else
                    {
                        entityCustomMethods.Add(methodName, customMethod);
                        customMethodToDescriptionMap.Add(methodName, description);
                    }
                }
            }

            foreach (KeyValuePair<string, DomainOperationEntry> methodEntry in entityCustomMethods.OrderBy(entry => entry.Key))
            {
                // generate the invoke method
                this.GenerateCustomMethod(methodEntry.Key, methodEntry.Value);

                // generate guard property
                this.GenerateGuardProperty(methodEntry.Key);
            }
        }

        /// <summary>
        /// Generates an invoke method for the given custom method
        /// </summary>
        /// <param name="customMethodName">name of custom method</param>
        /// <param name="customMethod"><see cref="DomainOperationEntry"/> of the custom method</param>
        [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "CodeDom introduces this complexity")]
        private void GenerateCustomMethod(string customMethodName, DomainOperationEntry customMethod)
        {
            this.ClientProxyGenerator.LogMessage(string.Format(CultureInfo.CurrentCulture, Resource.EntityCodeGen_Generating_InvokeMethod, customMethodName));

            // ----------------------------------------------------------------
            // Method decl
            // ----------------------------------------------------------------
            CodeMemberMethod method = new CodeMemberMethod();
            method.Name = customMethodName;
            method.Attributes = MemberAttributes.Public | MemberAttributes.Final;

            // The custom method parameter list is the same as the domain operation entries -- except for the first parameter
            // which is the entity.  We omit that first parameter in code gen
            DomainOperationParameter[] paramInfos = customMethod.Parameters.ToArray();

            // Generate <summary> doc comment for the custom method body
            string comment = string.Format(CultureInfo.CurrentCulture, Resource.CodeGen_Entity_Custom_Method_Summary_Comment, customMethodName);
            method.Comments.AddRange(CodeGenUtilities.GenerateSummaryCodeComment(comment, this.ClientProxyGenerator.IsCSharp));

            // Generate <param> doc comment for all the parameters
            for (int i = 1; i < paramInfos.Length; ++i)
            {
                comment = string.Format(CultureInfo.CurrentCulture, Resource.CodeGen_Entity_Custom_Method_Parameter_Comment, paramInfos[i].Name);
                method.Comments.AddRange(CodeGenUtilities.GenerateParamCodeComment(paramInfos[i].Name, comment, this.ClientProxyGenerator.IsCSharp));
            }

            // Propagate custom validation attributes from the DomainOperationEntry to this custom method
            var methodAttributes = customMethod.Attributes.Cast<Attribute>().ToList();
            CustomAttributeGenerator.GenerateCustomAttributes(
                this.ClientProxyGenerator,
                this._proxyClass,
                ex => string.Format(CultureInfo.CurrentCulture, Resource.ClientCodeGen_Attribute_ThrewException_CodeMethod, ex.Message, method.Name, this._proxyClass.Name, ex.InnerException.Message),
                methodAttributes,
                method.CustomAttributes,
                method.Comments);

            // Add [CustomMethod("...")] property
            var customMethodAttribute = customMethod.OperationAttribute as EntityActionAttribute;
            bool allowMultipleInvocations = customMethodAttribute != null && customMethodAttribute.AllowMultipleInvocations;
            method.CustomAttributes.Add(
                new CodeAttributeDeclaration("EntityAction",
                    new CodeAttributeArgument(new CodePrimitiveExpression(customMethodName)),
                    new CodeAttributeArgument("AllowMultipleInvocations", 
                                                new CodePrimitiveExpression(allowMultipleInvocations))
                        ));

            // ----------------------------------------------------------------
            // generate custom method body:
            //    this.OnMethodNameInvoking(params);
            //    base.Invoke(methodName, params);
            //    this.OnMethodNameInvoked();
            // ----------------------------------------------------------------
            List<CodeExpression> invokeParams = new List<CodeExpression>();
            invokeParams.Add(new CodePrimitiveExpression(customMethodName));

            // Create an expression for each parameter, and also use this loop to
            // propagate the custom attributes for each parameter in the DomainOperationEntry to the custom method.
            for (int i = 1; i < paramInfos.Length; ++i)
            {
                DomainOperationParameter paramInfo = paramInfos[i];

                // build up the method parameter signature from the DomainOperationEntry.MethodInfo
                CodeParameterDeclarationExpression paramDecl = new CodeParameterDeclarationExpression(
                    CodeGenUtilities.GetTypeReference(
                        CodeGenUtilities.TranslateType(paramInfo.ParameterType),
                        this.ClientProxyGenerator,
                        this._proxyClass),
                    paramInfo.Name);

                // Propagate parameter level validation attributes from custom operation entry.
                IEnumerable<Attribute> paramAttributes = paramInfo.Attributes.Cast<Attribute>();

                string commentHeader = 
                    string.Format(
                        CultureInfo.CurrentCulture, 
                        Resource.ClientCodeGen_Attribute_Parameter_FailedToGenerate, 
                        paramInfo.Name);

                CustomAttributeGenerator.GenerateCustomAttributes(
                    this.ClientProxyGenerator, 
                    this._proxyClass, 
                    ex => string.Format(CultureInfo.CurrentCulture, Resource.ClientCodeGen_Attribute_ThrewException_CodeMethodParameter, ex.Message, paramDecl.Name, method.Name, this._proxyClass.Name, ex.InnerException.Message),
                    paramAttributes, 
                    paramDecl.CustomAttributes, 
                    method.Comments,
                    commentHeader);

                method.Parameters.Add(paramDecl);

                // build up the invoke call parameters
                invokeParams.Add(new CodeVariableReferenceExpression(paramInfo.Name));
            }

            // generate 'OnCustomMethodInvoked/Invoking'
            string methodInvokingName = customMethodName + "Invoking";
            string methodInvokedName = customMethodName + "Invoked";
            this._notificationMethodGen.AddMethodFor(methodInvokingName, method.Parameters, null);
            this._notificationMethodGen.AddMethodFor(methodInvokedName, null);

            method.Statements.Add(this._notificationMethodGen.GetMethodInvokeExpressionStatementFor(methodInvokingName));
            method.Statements.Add(
                new CodeExpressionStatement(
                    new CodeMethodInvokeExpression(
                        new CodeBaseReferenceExpression(), "InvokeAction", invokeParams.ToArray())));
            method.Statements.Add(this._notificationMethodGen.GetMethodInvokeExpressionStatementFor(methodInvokedName));

            this._proxyClass.Members.Add(method);

            // ----------------------------------------------------------------
            // generate Is<CustomMethod>Invoked property:
            // [Display(AutoGenerateField=false)]
            // public bool IsMyCustomMethodInvoked { get { base.IsActionInvoked(methodName);}}
            // ----------------------------------------------------------------
            CodeMemberProperty invokedProperty = new CodeMemberProperty();
            invokedProperty.Attributes = MemberAttributes.Public | MemberAttributes.Final;
            invokedProperty.HasGet = true;
            invokedProperty.HasSet = false;
            invokedProperty.Type = new CodeTypeReference(typeof(bool));
            invokedProperty.GetStatements.Add(new CodeMethodReturnStatement(
                                                    new CodeMethodInvokeExpression(
                                                        new CodeBaseReferenceExpression(),
                                                        "IsActionInvoked",
                                                        new CodeExpression[] { new CodeArgumentReferenceExpression("\"" + customMethodName + "\"") })));
            invokedProperty.Name = GetIsInvokedPropertyName(customMethodName);

            // Generate <summary> doc comment
            comment = string.Format(CultureInfo.CurrentCulture, Resource.CodeGen_Entity_IsInvoked_Property_Summary_Comment, customMethodName);
            invokedProperty.Comments.AddRange(CodeGenUtilities.GenerateSummaryCodeComment(comment, this.ClientProxyGenerator.IsCSharp));

            CodeAttributeDeclaration displayAttribute = CodeGenUtilities.CreateDisplayAttributeDeclaration(this.ClientProxyGenerator, this._proxyClass);
            invokedProperty.CustomAttributes.Add(displayAttribute);

            this._proxyClass.Members.Add(invokedProperty);
        }

        private static string GetIsInvokedPropertyName(string customMethodName)
        {
            return string.Concat("Is", customMethodName, "Invoked");
        }

        /// <summary>
        /// Generates a custom method guard property
        /// </summary>
        /// <param name="customMethodName">name of the custom method to generate guard property for</param>
        private void GenerateGuardProperty(string customMethodName)
        {
            string guardName = GetCanInvokePropertyName(customMethodName);
            this.ClientProxyGenerator.LogMessage(string.Format(CultureInfo.CurrentCulture, Resource.EntityCodeGen_Generating_GuardProperty, guardName));

            // ----------------------------------------------------------------
            // Property decl:
            // [Display(AutoGenerateField=false)]
            // public bool CanMyCustomMethod
            // ----------------------------------------------------------------
            CodeMemberProperty property = new CodeMemberProperty();
            property.Name = guardName;
            property.Type = CodeGenUtilities.GetTypeReference(typeof(bool), this.ClientProxyGenerator, this._proxyClass);
            property.Attributes = MemberAttributes.Public | MemberAttributes.Final; // final needed, else becomes virtual

            // Generate <summary> doc comment
            string comment = string.Format(CultureInfo.CurrentCulture, Resource.CodeGen_Entity_CanInvoke_Property_Summary_Comment, customMethodName);
            property.Comments.AddRange(CodeGenUtilities.GenerateSummaryCodeComment(comment, this.ClientProxyGenerator.IsCSharp));

            CodeAttributeDeclaration displayAttribute = CodeGenUtilities.CreateDisplayAttributeDeclaration(this.ClientProxyGenerator, this._proxyClass);
            property.CustomAttributes.Add(displayAttribute);

            // ----------------------------------------------------------------
            // get
            // {
            //    return base.CanInvoke("XXX");
            // }
            // ----------------------------------------------------------------
            property.GetStatements.Add(
                new CodeMethodReturnStatement(
                    new CodeMethodInvokeExpression(
                        new CodeBaseReferenceExpression(), "CanInvokeAction", new CodePrimitiveExpression(customMethodName))));

            this._proxyClass.Members.Add(property);
        }

        private static string GetCanInvokePropertyName(string customMethodName)
        {
            return string.Format(CultureInfo.InvariantCulture, "Can{0}", customMethodName);
        }
    }
}
