using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using OpenRiaServices.DomainServices;
using OpenRiaServices.DomainServices.Server;

namespace OpenRiaServices.DomainServices.Tools
{
    /// <summary>
    /// Proxy generator for a <see cref="DomainOperationEntry"/> that represents an invoke operation.
    /// </summary>
    internal class InvokeOperationProxyGenerator : ProxyGenerator
    {
        private CodeTypeDeclaration _proxyClass;
        private DomainServiceDescription _domainServiceDescription;

        /// <summary>
        /// Initializes a new instance of the <see cref="InvokeOperationProxyGenerator"/> class.
        /// </summary>
        /// <param name="clientProxyGenerator">The client proxy generator against which this will generate code.  Cannot be null.</param>
        /// <param name="proxyClass">The class into which to inject the generated code</param>
        /// <param name="domainServiceDescription">The description for the DomainService we're generating for</param>
        public InvokeOperationProxyGenerator(CodeDomClientCodeGenerator clientProxyGenerator, CodeTypeDeclaration proxyClass, DomainServiceDescription domainServiceDescription)
            : base(clientProxyGenerator)
        {
            this._proxyClass = proxyClass;
            this._domainServiceDescription = domainServiceDescription;
        }

        /// <summary>
        /// Generates the client proxy code for the domain operation entries
        /// </summary>
        public override void Generate()
        {
            foreach (DomainOperationEntry operation in this._domainServiceDescription.DomainOperationEntries.Where(p => p.Operation == DomainOperation.Invoke).OrderBy(m => m.Name))
            {
                // generates 2 overloads for each invoke operation: one with a callback and one without
                this.GenerateInvokeOperation(operation, InvokeKind.WithCallback);
                this.GenerateInvokeOperation(operation, InvokeKind.WithoutCallback);
                this.GenerateInvokeOperation(operation, InvokeKind.Async);
            }
        }

        public enum InvokeKind
        {
            WithoutCallback,
            WithCallback,
            Async
        };


        /// <summary>
        /// Generates an invoke operation.
        /// </summary>
        /// <param name="domainOperationEntry">The invoke operation.</param>
        /// <param name="invokeKind">the type of invoke method to generate.</param>
        private void GenerateInvokeOperation(DomainOperationEntry domainOperationEntry, InvokeKind invokeKind)
        {
            // Determine the name of the generated invoke function
            string methodName = domainOperationEntry.Name;

            // ----------------------------------------------------------------
            // Check for name conflicts
            // ----------------------------------------------------------------
            if ((invokeKind ==  InvokeKind.WithCallback) && this._proxyClass.Members.Cast<CodeTypeMember>().Any(c => c.Name == methodName && c.GetType() != typeof(CodeMemberMethod)))
            {
                this.ClientProxyGenerator.LogError(
                    string.Format(CultureInfo.CurrentCulture,
                        Resource.ClientCodeGen_NamingCollision_MemberAlreadyExists,
                        this._proxyClass.Name,
                        methodName));
                return;
            }

            // ----------------------------------------------------------------
            // InvokeResult<T> InvokeOperation(args);
            //
            // InvokeResult<T> InvokeOperation(args, callback, userState);
            //
            // Task<T> InvokeOperationAsync(args);
            // ----------------------------------------------------------------
            CodeTypeReference operationReturnType = null;
            Type returnType = CodeGenUtilities.TranslateType(domainOperationEntry.ReturnType);
            var methodReturnTypeName = (invokeKind == InvokeKind.Async) ? typeof (System.Threading.Tasks.Task).FullName : TypeConstants.InvokeOperationTypeFullName;

            CodeTypeReference invokeOperationType = CodeGenUtilities.GetTypeReference(methodReturnTypeName, (string)this._proxyClass.UserData["Namespace"], false);
            if (returnType != typeof(void))
            {
                // If this is an enum type, we need to ensure it is either shared or
                // can be generated.  Failure to use this enum is only a warning and causes
                // this invoke operation to be skipped.  The test for legality also causes
                // the enum to be generated if required.
                Type enumType = TypeUtility.GetNonNullableType(returnType);
                if (enumType.IsEnum)
                {
                    string errorMessage = null;
                    if (!this.ClientProxyGenerator.CanExposeEnumType(enumType, out errorMessage))
                    {
                        this.ClientProxyGenerator.LogError(string.Format(CultureInfo.CurrentCulture,
                                                                Resource.ClientCodeGen_Domain_Op_Enum_Error,
                                                                methodName,
                                                                this._proxyClass.Name,
                                                                enumType.FullName,
                                                                errorMessage));
                        return;
                    }
                    else
                    {
                        // Register use of this enum type, which could cause deferred generation
                        this.ClientProxyGenerator.RegisterUseOfEnumType(enumType);
                    }
                }

                operationReturnType = CodeGenUtilities.GetTypeReference(returnType, this.ClientProxyGenerator, this._proxyClass);
                operationReturnType.Options |= CodeTypeReferenceOptions.GenericTypeParameter;
                invokeOperationType.TypeArguments.Add(operationReturnType);
            }
            CodeMemberMethod method = new CodeMemberMethod()
            {
                Attributes = MemberAttributes.Public | MemberAttributes.Final,
                Name =  (invokeKind == InvokeKind.Async) ? (methodName + "Async") : methodName,
                ReturnType = invokeOperationType,
            };
            this._proxyClass.Members.Add(method);

            ReadOnlyCollection<DomainOperationParameter> operationParameters = domainOperationEntry.Parameters;

            // Generate the <summary> doc comments
            string comment = string.Format(CultureInfo.CurrentCulture, Resource.CodeGen_DomainContext_Invoke_Method_Summary_Comment, domainOperationEntry.Name);
            method.Comments.AddRange(CodeGenUtilities.GenerateSummaryCodeComment(comment, this.ClientProxyGenerator.IsCSharp));

            // Generate <param> doc comments
            foreach (DomainOperationParameter parameter in operationParameters)
            {
                comment = string.Format(CultureInfo.CurrentCulture, Resource.CodeGen_DomainContext_Invoke_Method_Parameter_Comment, parameter.Name);
                method.Comments.AddRange(CodeGenUtilities.GenerateParamCodeComment(parameter.Name, comment, this.ClientProxyGenerator.IsCSharp));
            }

            // Conditionally add the callback and userState <param> doc comments
            if (invokeKind == InvokeKind.WithCallback)
            {
                method.Comments.AddRange(CodeGenUtilities.GenerateParamCodeComment("callback", Resource.CodeGen_DomainContext_Invoke_Method_Callback_Parameter_Comment, this.ClientProxyGenerator.IsCSharp));
                method.Comments.AddRange(CodeGenUtilities.GenerateParamCodeComment("userState", Resource.CodeGen_DomainContext_Invoke_Method_UserState_Parameter_Comment, this.ClientProxyGenerator.IsCSharp));
            }
            else if(invokeKind == InvokeKind.Async)
            {
                method.Comments.AddRange(CodeGenUtilities.GenerateParamCodeComment("cancellationToken", Resource.CodeGen_DomainContext_Invoke_Method_CancellationToken_Parameter_Comment, this.ClientProxyGenerator.IsCSharp)); 
            }

            // Generate <returns> doc comments
            method.Comments.AddRange(CodeGenUtilities.GenerateReturnsCodeComment(Resource.CodeGen_DomainContext_Invoke_Returns_Comment, this.ClientProxyGenerator.IsCSharp));

            // Propagate custom validation attributes from the DomainOperationEntry to this invoke operation.
            IEnumerable<Attribute> methodAttributes = domainOperationEntry.Attributes.Cast<Attribute>();
            CustomAttributeGenerator.GenerateCustomAttributes(
                this.ClientProxyGenerator,
                this._proxyClass,
                ex => string.Format(CultureInfo.CurrentCulture, Resource.ClientCodeGen_Attribute_ThrewException_CodeMethod, ex.Message, method.Name, this._proxyClass.Name, ex.InnerException.Message),
                methodAttributes,
                method.CustomAttributes,
                method.Comments);

            // ----------------------------------------------------------------
            // generate invoke operation body:
            //     return base.InvokeOperation<T>(methodName, typeof(T), parameters, hasSideEffects, callback, userState);
            // or  return base.InvokeOperationAsync<T>(methodName, parameters, hasSideEffects);
            // ----------------------------------------------------------------
            List<CodeExpression> invokeParams = new List<CodeExpression>();
            invokeParams.Add(new CodePrimitiveExpression(methodName));

            // add the return Type parameter
            if (invokeKind != InvokeKind.Async)
                invokeParams.Add(new CodeTypeOfExpression(operationReturnType));

            // add any operation parameters

            CodeVariableReferenceExpression paramsRef = new CodeVariableReferenceExpression("parameters");
            if (operationParameters.Count > 0)
            {
                // need to generate the user parameters dictionary
                CodeTypeReference dictionaryTypeReference = CodeGenUtilities.GetTypeReference(
                    typeof(Dictionary<string, object>),
                    this.ClientProxyGenerator,
                    this._proxyClass);

                CodeVariableDeclarationStatement paramsDef = new CodeVariableDeclarationStatement(
                    dictionaryTypeReference,
                    "parameters",
                    new CodeObjectCreateExpression(dictionaryTypeReference, new CodeExpression[0]));
                method.Statements.Add(paramsDef);
            }
            foreach (DomainOperationParameter paramInfo in operationParameters)
            {
                // If this is an enum type, we need to ensure it is either shared or
                // can be generated.  Failure to use this enum logs an error and exits.
                // The test for legality also causes the enum to be generated if required.
                Type enumType = TypeUtility.GetNonNullableType(paramInfo.ParameterType);
                if (enumType.IsEnum)
                {
                    string errorMessage = null;
                    if (!this.ClientProxyGenerator.CanExposeEnumType(enumType, out errorMessage))
                    {
                        this.ClientProxyGenerator.LogError(string.Format(CultureInfo.CurrentCulture,
                                                                Resource.ClientCodeGen_Domain_Op_Enum_Error,
                                                                method.Name,
                                                                this._proxyClass.Name,
                                                                enumType.FullName,
                                                                errorMessage));
                        return;
                    }
                    else
                    {
                        // Register use of this enum type, which could cause deferred generation
                        this.ClientProxyGenerator.RegisterUseOfEnumType(enumType);
                    }
                }

                // add the parameter to the method
                CodeParameterDeclarationExpression paramDecl = new CodeParameterDeclarationExpression(
                        CodeGenUtilities.GetTypeReference(
                            CodeGenUtilities.TranslateType(paramInfo.ParameterType),
                            this.ClientProxyGenerator,
                            this._proxyClass),
                        paramInfo.Name);

                // Propagate parameter level validation attributes from domain operation entry
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

                // add the parameter and value to the params dictionary
                method.Statements.Add(new CodeMethodInvokeExpression(
                    new CodeMethodReferenceExpression(paramsRef, "Add"),
                    new CodePrimitiveExpression(paramInfo.Name),
                    new CodeVariableReferenceExpression(paramInfo.Name)));
            }

            // add parameters if present
            if (operationParameters.Count > 0)
            {
                invokeParams.Add(paramsRef);
            }
            else
            {
                invokeParams.Add(new CodePrimitiveExpression(null));
            }

            InvokeAttribute invokeAttribute = (InvokeAttribute)domainOperationEntry.OperationAttribute;
            invokeParams.Add(new CodePrimitiveExpression(invokeAttribute.HasSideEffects));

            switch (invokeKind)
            {
                case InvokeKind.WithCallback:
                {
                    CodeTypeReference callbackType = CodeGenUtilities.GetTypeReference(typeof(Action).FullName, (string)this._proxyClass.UserData["Namespace"], false);
                    callbackType.TypeArguments.Add(invokeOperationType);

                    // add callback method parameter
                    method.Parameters.Add(new CodeParameterDeclarationExpression(callbackType, "callback"));
                    invokeParams.Add(new CodeVariableReferenceExpression("callback"));

                    // add the userState parameter to the end
                    method.Parameters.Add(new CodeParameterDeclarationExpression(CodeGenUtilities.GetTypeReference(typeof(object), this.ClientProxyGenerator, this._proxyClass), "userState"));
                    invokeParams.Add(new CodeVariableReferenceExpression("userState"));
                }
                    break;
                case InvokeKind.WithoutCallback:
                    invokeParams.Add(new CodePrimitiveExpression(null));
                    invokeParams.Add(new CodePrimitiveExpression(null));
                    break;
                case InvokeKind.Async:
                    var cancellationTokenParmeter = new CodeParameterDeclarationExpression(CodeGenUtilities.GetTypeReference(typeof (CancellationToken), this.ClientProxyGenerator,this._proxyClass), "cancellationToken");
                    // Add [Optional] attribute, this is the same as adding "= default(CancellationToken)"
                    cancellationTokenParmeter.CustomAttributes.Add(
                        new CodeAttributeDeclaration(new CodeTypeReference(typeof(OptionalAttribute))));
                    method.Parameters.Add(cancellationTokenParmeter);
                    invokeParams.Add(new CodeVariableReferenceExpression(cancellationTokenParmeter.Name));
                    break;
            }

            // this.ValidateMethod("methodName", parameters);
            CodeExpression paramsExpr = new CodePrimitiveExpression(null);
            if (operationParameters.Count > 0)
            {
                paramsExpr = paramsRef;
            }
            CodeExpressionStatement validateMethodCall = new CodeExpressionStatement(
                new CodeMethodInvokeExpression(
                    new CodeThisReferenceExpression(),
                    "ValidateMethod",
                    new CodeExpression[] 
                    {
                        new CodePrimitiveExpression(methodName), 
                        paramsExpr
                    }));

            method.Statements.Add(validateMethodCall);

            var invokeMethod = (invokeKind == InvokeKind.Async) ? "InvokeOperationAsync" : "InvokeOperation";
            var typeParameters = (domainOperationEntry.ReturnType == typeof(void)) ? new CodeTypeReference[0] : new[] { operationReturnType };
            var invokeMethodReference = new CodeMethodReferenceExpression(new CodeThisReferenceExpression(), invokeMethod, typeParameters);

            CodeExpression invokeCall = new CodeMethodInvokeExpression(invokeMethodReference, invokeParams.ToArray());
            method.Statements.Add(new CodeMethodReturnStatement(invokeCall));
        }
    }
}
