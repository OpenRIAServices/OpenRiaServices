namespace OpenRiaServices.Tools.TextTemplate.CSharpGenerators
{
    using System;
    using OpenRiaServices.Server;

    /// <summary>
    /// C# Generator for DomainService proxies.
    /// </summary>
    public partial class CSharpDomainContextGenerator
    {
        /// <summary>
        /// Generates DomainContext code in C#.
        /// </summary>
        /// <returns>The generated code</returns>
        protected override string GenerateDomainContextClass()
        {
            // Clear environment to remove contents from previously generated DomainContexts
            base.GenerationEnvironment.Clear();
            return this.TransformText();
        }

        internal override void Initialize()
        {
            base.Initialize();
            this.DomainContextTypeName = CodeGenUtilities.GetSafeName(DomainContextGenerator.GetDomainContextTypeName(this.DomainServiceDescription));
        }

        private void Generate()
        {
            this.Initialize();
            this.GenerateDomainServiceProxyClass();
        }

        private void GenerateDomainServiceProxyClass()
        {
            this.GenerateNamespace(this.DomainServiceDescription.DomainServiceType.Namespace);
            this.GenerateOpeningBrace();
            this.GenerateUsings();
            this.GenerateClassDeclaration();

            this.GenerateOpeningBrace();
            this.GenerateBody();
            this.GenerateClosingBrace();

            this.GenerateClosingBrace();
        }

        /// <summary>
        /// Generates DomainContext class body.
        /// </summary>
        /// <remarks>
        /// The default implementation of this method invokes <see cref="GenerateConstructors"/>,
        /// <see cref="GenerateEntityContainer"/>, <see cref="GenerateQueryMethods"/>,
        /// <see cref="GenerateCustomMethods"/>, <see cref="GenerateInvokeOperations"/>, 
        /// <see cref="GenerateEntitySets"/>, <see cref="GenerateServiceContractInterface"/> 
        /// and <see cref="GenerateExtensibilityMethods"/>.
        /// </remarks>
        protected virtual void GenerateBody()
        {
            this.GenerateConstructors();
            this.GenerateEntityContainer();
            this.GenerateMethods();
            this.GenerateEntitySets();
            this.GenerateServiceContractInterface();
            this.GenerateExtensibilityMethods();
        }

        private void GenerateMethods()
        {
            this.GenerateQueryMethods();
            this.GenerateCustomMethods();
            this.GenerateInvokeOperations();
        }

        internal string GetEntitySetOperationsEnumValue(Type entityType)
        {
            bool canInsert = this.DomainServiceDescription.IsOperationSupported(entityType, DomainOperation.Insert);
            bool canEdit = this.DomainServiceDescription.IsOperationSupported(entityType, DomainOperation.Update);
            bool canDelete = this.DomainServiceDescription.IsOperationSupported(entityType, DomainOperation.Delete);

            string entitySetOperationsEnumValue = null;
            if (!canInsert && !canEdit && !canDelete)
            {
                // if no update operations are supported, set to 'None'
                entitySetOperationsEnumValue = "EntitySetOperations.None";
            }
            else if (canInsert && canEdit && canDelete)
            {
                // if all operations are supported, set to 'All'
                entitySetOperationsEnumValue = "EntitySetOperations.All";
            }
            else
            {
                if (canInsert)
                {
                    entitySetOperationsEnumValue = "EntitySetOperations.Add";
                }
                if (canEdit)
                {
                    entitySetOperationsEnumValue += (entitySetOperationsEnumValue == null ? string.Empty : " | ") + "EntitySetOperations.Edit";
                }
                if (canDelete)
                {
                    entitySetOperationsEnumValue += (entitySetOperationsEnumValue == null ? string.Empty : " | ") + "EntitySetOperations.Remove";
                }
            }
            return entitySetOperationsEnumValue;
        }

        internal string GetInvokeMethodReturnTypeName(DomainOperationEntry domainOperationEntry, InvokeKind invokeKind)
        {
            Type returnType = CodeGenUtilities.TranslateType(domainOperationEntry.ReturnType);
            string returnTypeString = (invokeKind == InvokeKind.Async) ? "InvokeResult" :  "InvokeOperation";
            if (returnType != typeof(void))
            {
                if (!this.RegisterEnumTypeIfNecessary(returnType, domainOperationEntry))
                {
                    return String.Empty;
                }
                returnTypeString = returnTypeString + "<" + CodeGenUtilities.GetTypeName(returnType) + ">";
            }

            if (invokeKind == InvokeKind.Async)
                returnTypeString = string.Format("System.Threading.Tasks.Task<{0}>", returnTypeString);
            return returnTypeString;
        }

        internal static string GetEndOperationReturnType(DomainOperationEntry operation)
        {
            string returnTypeName = null;
            if (operation.Operation == DomainOperation.Query)
            {
                if (operation.ReturnType == typeof(void))
                {
                    returnTypeName = "OpenRiaServices.Client.QueryResult";
                }
                else
                {
                    returnTypeName = "OpenRiaServices.Client.QueryResult<" + CodeGenUtilities.GetTypeName(CodeGenUtilities.TranslateType(operation.AssociatedType)) + ">";
                }
            }
            else
            {
                returnTypeName = CodeGenUtilities.GetTypeName(CodeGenUtilities.TranslateType(operation.ReturnType));
            }
            return returnTypeName;
        }
    }
}

