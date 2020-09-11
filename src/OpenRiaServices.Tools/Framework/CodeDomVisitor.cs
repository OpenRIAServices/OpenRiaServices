using System;
using System.CodeDom;
using System.Linq;

namespace OpenRiaServices.Tools
{
    /// <summary>
    /// Visits a CodeCompileUnit class.
    /// </summary>
    internal class CodeDomVisitor
    {
        /// <summary>
        /// Visits a <see cref="CodeCompileUnit"/>.
        /// </summary>
        /// <param name="codeCompileUnit">The <see cref="CodeCompileUnit"/> to visit.</param>
        public void Visit(CodeCompileUnit codeCompileUnit)
        {
            if (codeCompileUnit == null)
            {
                throw new ArgumentNullException(nameof(codeCompileUnit));
            }

            this.VisitBase(codeCompileUnit);
        }

        /// <summary>
        /// Visits a <see cref="CodeCompileUnit"/>.
        /// </summary>
        /// <param name="codeCompileUnit">The <see cref="CodeCompileUnit"/> to visit.</param>
        protected virtual void VisitBase(CodeCompileUnit codeCompileUnit)
        {
            CodeSnippetCompileUnit codeSnippetCompileUnit = codeCompileUnit as CodeSnippetCompileUnit;
            if (codeSnippetCompileUnit != null)
            {
                this.VisitCodeSnippetCompileUnit(codeSnippetCompileUnit);
            }

            this.VisitCodeNamespaceCollection(codeCompileUnit.Namespaces);
            this.VisitCodeAttributeDeclarationCollection(codeCompileUnit.AssemblyCustomAttributes);
            this.VisitCodeDirectiveCollection(codeCompileUnit.StartDirectives);
            this.VisitCodeDirectiveCollection(codeCompileUnit.EndDirectives);
        }

        /// <summary>
        /// Visits a <see cref="CodeArgumentReferenceExpression"/>.
        /// </summary>
        /// <param name="codeArgumentReferenceExpression">The <see cref="CodeArgumentReferenceExpression"/> to visit.</param>
        protected virtual void VisitCodeArgumentReferenceExpression(CodeArgumentReferenceExpression codeArgumentReferenceExpression)
        {
        }

        /// <summary>
        /// Visits a <see cref="CodeArrayCreateExpression"/>.
        /// </summary>
        /// <param name="codeArrayCreateExpression">The <see cref="CodeArrayCreateExpression"/> to visit.</param>
        protected virtual void VisitCodeArrayCreateExpression(CodeArrayCreateExpression codeArrayCreateExpression)
        {
            if (codeArrayCreateExpression == null)
            {
                return;
            }

            this.VisitCodeTypeReference(codeArrayCreateExpression.CreateType);
            this.VisitCodeExpressionCollection(codeArrayCreateExpression.Initializers);
            this.VisitCodeExpression(codeArrayCreateExpression.SizeExpression);
        }

        /// <summary>
        /// Visits a <see cref="CodeArrayIndexerExpression"/>.
        /// </summary>
        /// <param name="codeArrayIndexerExpression">The <see cref="CodeArrayIndexerExpression"/> to visit.</param>
        protected virtual void VisitCodeArrayIndexerExpression(CodeArrayIndexerExpression codeArrayIndexerExpression)
        {
            if (codeArrayIndexerExpression == null)
            {
                return;
            }

            this.VisitCodeExpression(codeArrayIndexerExpression.TargetObject);
            this.VisitCodeExpressionCollection(codeArrayIndexerExpression.Indices);
        }

        /// <summary>
        /// Visits a <see cref="CodeAssignStatement"/>.
        /// </summary>
        /// <param name="codeAssignStatement">The <see cref="CodeAssignStatement"/> to visit.</param>
        protected virtual void VisitCodeAssignStatement(CodeAssignStatement codeAssignStatement)
        {
            if (codeAssignStatement == null)
            {
                return;
            }

            this.VisitCodeExpression(codeAssignStatement.Left);
            this.VisitCodeExpression(codeAssignStatement.Right);
            this.VisitCodeLinePragma(codeAssignStatement.LinePragma);
            this.VisitCodeDirectiveCollection(codeAssignStatement.StartDirectives);
            this.VisitCodeDirectiveCollection(codeAssignStatement.EndDirectives);
        }

        /// <summary>
        /// Visits a <see cref="CodeAttachEventStatement"/>.
        /// </summary>
        /// <param name="codeAttachEventStatement">The <see cref="CodeAttachEventStatement"/> to visit.</param>
        protected virtual void VisitCodeAttachEventStatement(CodeAttachEventStatement codeAttachEventStatement)
        {
            if (codeAttachEventStatement == null)
            {
                return;
            }

            this.VisitCodeEventReferenceExpression(codeAttachEventStatement.Event);
            this.VisitCodeExpression(codeAttachEventStatement.Listener);
            this.VisitCodeLinePragma(codeAttachEventStatement.LinePragma);
            this.VisitCodeDirectiveCollection(codeAttachEventStatement.StartDirectives);
            this.VisitCodeDirectiveCollection(codeAttachEventStatement.EndDirectives);
        }

        /// <summary>
        /// Visits a <see cref="CodeAttributeArgument"/>.
        /// </summary>
        /// <param name="codeAttributeArgument">The <see cref="CodeAttributeArgument"/> to visit.</param>
        protected virtual void VisitCodeAttributeArgument(CodeAttributeArgument codeAttributeArgument)
        {
            if (codeAttributeArgument == null)
            {
                return;
            }

            this.VisitCodeExpression(codeAttributeArgument.Value);
        }

        /// <summary>
        /// Visits a <see cref="CodeAttributeArgumentCollection"/>.
        /// </summary>
        /// <param name="codeAttributeArgumentCollection">The <see cref="CodeAttributeArgumentCollection"/> to visit.</param>
        protected virtual void VisitCodeAttributeArgumentCollection(CodeAttributeArgumentCollection codeAttributeArgumentCollection)
        {
            // Visit all of the CodeAttributeArgument items in the collection.
            foreach (CodeAttributeArgument item in codeAttributeArgumentCollection.Cast<CodeAttributeArgument>())
            {
                this.VisitCodeAttributeArgument(item);
            }
        }

        /// <summary>
        /// Visits a <see cref="CodeAttributeDeclaration"/>.
        /// </summary>
        /// <param name="codeAttributeDeclaration">The <see cref="CodeAttributeDeclaration"/> to visit.</param>
        protected virtual void VisitCodeAttributeDeclaration(CodeAttributeDeclaration codeAttributeDeclaration)
        {
            if (codeAttributeDeclaration == null)
            {
                return;
            }

            this.VisitCodeAttributeArgumentCollection(codeAttributeDeclaration.Arguments);
            this.VisitCodeTypeReference(codeAttributeDeclaration.AttributeType);
        }

        /// <summary>
        /// Visits a <see cref="CodeAttributeDeclarationCollection"/>.
        /// </summary>
        /// <param name="codeAttributeDeclarationCollection">The <see cref="CodeAttributeDeclarationCollection"/> to visit.</param>
        protected virtual void VisitCodeAttributeDeclarationCollection(CodeAttributeDeclarationCollection codeAttributeDeclarationCollection)
        {
            // Visit all of the CodeAttributeDeclaration items in the collection.
            foreach (CodeAttributeDeclaration item in codeAttributeDeclarationCollection.Cast<CodeAttributeDeclaration>())
            {
                this.VisitCodeAttributeDeclaration(item);
            }
        }

        /// <summary>
        /// Visits a <see cref="CodeBaseReferenceExpression"/>.
        /// </summary>
        /// <param name="codeBaseReferenceExpression">The <see cref="CodeBaseReferenceExpression"/> to visit.</param>
        protected virtual void VisitCodeBaseReferenceExpression(CodeBaseReferenceExpression codeBaseReferenceExpression)
        {
        }

        /// <summary>
        /// Visits a <see cref="CodeBinaryOperatorExpression"/>.
        /// </summary>
        /// <param name="codeBinaryOperatorExpression">The <see cref="CodeBinaryOperatorExpression"/> to visit.</param>
        protected virtual void VisitCodeBinaryOperatorExpression(CodeBinaryOperatorExpression codeBinaryOperatorExpression)
        {
            // Exit early if null
            if (codeBinaryOperatorExpression == null)
            {
                return;
            }

            this.VisitCodeExpression(codeBinaryOperatorExpression.Right);
            this.VisitCodeExpression(codeBinaryOperatorExpression.Left);
        }

        /// <summary>
        /// Visits a <see cref="CodeCastExpression"/>.
        /// </summary>
        /// <param name="codeCastExpression">The <see cref="CodeCastExpression"/> to visit.</param>
        protected virtual void VisitCodeCastExpression(CodeCastExpression codeCastExpression)
        {
            if (codeCastExpression == null)
            {
                return;
            }

            this.VisitCodeTypeReference(codeCastExpression.TargetType);
            this.VisitCodeExpression(codeCastExpression.Expression);
        }

        /// <summary>
        /// Visits a <see cref="CodeCatchClause"/>.
        /// </summary>
        /// <param name="codeCatchClause">The <see cref="CodeCatchClause"/> to visit.</param>
        protected virtual void VisitCodeCatchClause(CodeCatchClause codeCatchClause)
        {
            if (codeCatchClause == null)
            {
                return;
            }

            this.VisitCodeTypeReference(codeCatchClause.CatchExceptionType);
            this.VisitCodeStatementCollection(codeCatchClause.Statements);
        }

        /// <summary>
        /// Visits a <see cref="CodeCatchClauseCollection"/>.
        /// </summary>
        /// <param name="codeCatchClauseCollection">The <see cref="CodeCatchClauseCollection"/> to visit.</param>
        protected virtual void VisitCodeCatchClauseCollection(CodeCatchClauseCollection codeCatchClauseCollection)
        {
        }

        /// <summary>
        /// Visits a <see cref="CodeChecksumPragma"/>.
        /// </summary>
        /// <param name="codeChecksumPragma">The <see cref="CodeChecksumPragma"/> to visit.</param>
        protected virtual void VisitCodeChecksumPragma(CodeChecksumPragma codeChecksumPragma)
        {
        }

        /// <summary>
        /// Visits a <see cref="CodeComment"/>.
        /// </summary>
        /// <param name="codeComment">The <see cref="CodeComment"/> to visit.</param>
        protected virtual void VisitCodeComment(CodeComment codeComment)
        {
        }

        /// <summary>
        /// Visits a <see cref="CodeCommentStatement"/>.
        /// </summary>
        /// <param name="codeCommentStatement">The <see cref="CodeCommentStatement"/> to visit.</param>
        protected virtual void VisitCodeCommentStatement(CodeCommentStatement codeCommentStatement)
        {
            if (codeCommentStatement == null)
            {
                return;
            }

            this.VisitCodeComment(codeCommentStatement.Comment);
            this.VisitCodeLinePragma(codeCommentStatement.LinePragma);
            this.VisitCodeDirectiveCollection(codeCommentStatement.StartDirectives);
            this.VisitCodeDirectiveCollection(codeCommentStatement.EndDirectives);
        }

        /// <summary>
        /// Visits a <see cref="CodeCommentStatementCollection"/>.
        /// </summary>
        /// <param name="codeCommentStatementCollection">The <see cref="CodeCommentStatementCollection"/> to visit.</param>
        protected virtual void VisitCodeCommentStatementCollection(CodeCommentStatementCollection codeCommentStatementCollection)
        {
            // Visit all of the CodeCommentStatement items in the collection.
            foreach (CodeCommentStatement item in codeCommentStatementCollection.Cast<CodeCommentStatement>())
            {
                this.VisitCodeCommentStatement(item);
            }
        }

        /// <summary>
        /// Visits a <see cref="CodeConditionStatement"/>.
        /// </summary>
        /// <param name="codeConditionStatement">The <see cref="CodeConditionStatement"/> to visit.</param>
        protected virtual void VisitCodeConditionStatement(CodeConditionStatement codeConditionStatement)
        {
            if (codeConditionStatement == null)
            {
                return;
            }

            this.VisitCodeExpression(codeConditionStatement.Condition);
            this.VisitCodeStatementCollection(codeConditionStatement.TrueStatements);
            this.VisitCodeStatementCollection(codeConditionStatement.FalseStatements);
            this.VisitCodeLinePragma(codeConditionStatement.LinePragma);
            this.VisitCodeDirectiveCollection(codeConditionStatement.StartDirectives);
            this.VisitCodeDirectiveCollection(codeConditionStatement.EndDirectives);
        }

        /// <summary>
        /// Visits a <see cref="CodeConstructor"/>.
        /// </summary>
        /// <param name="codeConstructor">The <see cref="CodeConstructor"/> to visit.</param>
        protected virtual void VisitCodeConstructor(CodeConstructor codeConstructor)
        {
            if (codeConstructor == null)
            {
                return;
            }

            this.VisitCodeExpressionCollection(codeConstructor.BaseConstructorArgs);
            this.VisitCodeExpressionCollection(codeConstructor.ChainedConstructorArgs);
            this.VisitCodeTypeReference(codeConstructor.ReturnType);
            this.VisitCodeStatementCollection(codeConstructor.Statements);
            this.VisitCodeParameterDeclarationExpressionCollection(codeConstructor.Parameters);
            this.VisitCodeTypeReference(codeConstructor.PrivateImplementationType);
            this.VisitCodeTypeReferenceCollection(codeConstructor.ImplementationTypes);
            this.VisitCodeAttributeDeclarationCollection(codeConstructor.ReturnTypeCustomAttributes);
            this.VisitCodeTypeParameterCollection(codeConstructor.TypeParameters);
            this.VisitCodeAttributeDeclarationCollection(codeConstructor.CustomAttributes);
            this.VisitCodeLinePragma(codeConstructor.LinePragma);
            this.VisitCodeCommentStatementCollection(codeConstructor.Comments);
            this.VisitCodeDirectiveCollection(codeConstructor.StartDirectives);
            this.VisitCodeDirectiveCollection(codeConstructor.EndDirectives);
        }

        /// <summary>
        /// Visits a <see cref="CodeDefaultValueExpression"/>.
        /// </summary>
        /// <param name="codeDefaultValueExpression">The <see cref="CodeDefaultValueExpression"/> to visit.</param>
        protected virtual void VisitCodeDefaultValueExpression(CodeDefaultValueExpression codeDefaultValueExpression)
        {
            if (codeDefaultValueExpression == null)
            {
                return;
            }

            this.VisitCodeTypeReference(codeDefaultValueExpression.Type);
        }

        /// <summary>
        /// Visits a <see cref="CodeDelegateCreateExpression"/>.
        /// </summary>
        /// <param name="codeDelegateCreateExpression">The <see cref="CodeDelegateCreateExpression"/> to visit.</param>
        protected virtual void VisitCodeDelegateCreateExpression(CodeDelegateCreateExpression codeDelegateCreateExpression)
        {
            if (codeDelegateCreateExpression == null)
            {
                return;
            }

            this.VisitCodeTypeReference(codeDelegateCreateExpression.DelegateType);
            this.VisitCodeExpression(codeDelegateCreateExpression.TargetObject);
        }

        /// <summary>
        /// Visits a <see cref="CodeDelegateInvokeExpression"/>.
        /// </summary>
        /// <param name="codeDelegateInvokeExpression">The <see cref="CodeDelegateInvokeExpression"/> to visit.</param>
        protected virtual void VisitCodeDelegateInvokeExpression(CodeDelegateInvokeExpression codeDelegateInvokeExpression)
        {
            if (codeDelegateInvokeExpression == null)
            {
                return;
            }

            this.VisitCodeExpression(codeDelegateInvokeExpression.TargetObject);
            this.VisitCodeExpressionCollection(codeDelegateInvokeExpression.Parameters);
        }

        /// <summary>
        /// Visits a <see cref="CodeDirectionExpression"/>.
        /// </summary>
        /// <param name="codeDirectionExpression">The <see cref="CodeDirectionExpression"/> to visit.</param>
        protected virtual void VisitCodeDirectionExpression(CodeDirectionExpression codeDirectionExpression)
        {
            if (codeDirectionExpression == null)
            {
                return;
            }

            this.VisitCodeExpression(codeDirectionExpression.Expression);
        }

        /// <summary>
        /// Visits a <see cref="CodeDirective"/>.
        /// </summary>
        /// <param name="codeDirective">The <see cref="CodeDirective"/> to visit.</param>
        protected virtual void VisitCodeDirective(CodeDirective codeDirective)
        {
            if (codeDirective == null)
            {
                return;
            }

            CodeChecksumPragma codeChecksumPragma;
            CodeRegionDirective codeRegionDirective;

            if ((codeChecksumPragma = codeDirective as CodeChecksumPragma) != null)
            {
                this.VisitCodeChecksumPragma(codeChecksumPragma);
            }
            else if ((codeRegionDirective = codeDirective as CodeRegionDirective) != null)
            {
                this.VisitCodeRegionDirective(codeRegionDirective);
            }
        }

        /// <summary>
        /// Visits a <see cref="CodeDirectiveCollection"/>.
        /// </summary>
        /// <param name="codeDirectiveCollection">The <see cref="CodeDirectiveCollection"/> to visit.</param>
        protected virtual void VisitCodeDirectiveCollection(CodeDirectiveCollection codeDirectiveCollection)
        {
            // Visit all of the CodeDirective items in the collection.
            foreach (CodeDirective item in codeDirectiveCollection.Cast<CodeDirective>())
            {
                this.VisitCodeDirective(item);
            }
        }

        /// <summary>
        /// Visits a <see cref="CodeEntryPointMethod"/>.
        /// </summary>
        /// <param name="codeEntryPointMethod">The <see cref="CodeEntryPointMethod"/> to visit.</param>
        protected virtual void VisitCodeEntryPointMethod(CodeEntryPointMethod codeEntryPointMethod)
        {
            if (codeEntryPointMethod == null)
            {
                return;
            }

            this.VisitCodeTypeReference(codeEntryPointMethod.ReturnType);
            this.VisitCodeStatementCollection(codeEntryPointMethod.Statements);
            this.VisitCodeParameterDeclarationExpressionCollection(codeEntryPointMethod.Parameters);
            this.VisitCodeTypeReference(codeEntryPointMethod.PrivateImplementationType);
            this.VisitCodeTypeReferenceCollection(codeEntryPointMethod.ImplementationTypes);
            this.VisitCodeAttributeDeclarationCollection(codeEntryPointMethod.ReturnTypeCustomAttributes);
            this.VisitCodeTypeParameterCollection(codeEntryPointMethod.TypeParameters);
            this.VisitCodeAttributeDeclarationCollection(codeEntryPointMethod.CustomAttributes);
            this.VisitCodeLinePragma(codeEntryPointMethod.LinePragma);
            this.VisitCodeCommentStatementCollection(codeEntryPointMethod.Comments);
            this.VisitCodeDirectiveCollection(codeEntryPointMethod.StartDirectives);
            this.VisitCodeDirectiveCollection(codeEntryPointMethod.EndDirectives);
        }

        /// <summary>
        /// Visits a <see cref="CodeEventReferenceExpression"/>.
        /// </summary>
        /// <param name="codeEventReferenceExpression">The <see cref="CodeEventReferenceExpression"/> to visit.</param>
        protected virtual void VisitCodeEventReferenceExpression(CodeEventReferenceExpression codeEventReferenceExpression)
        {
            if (codeEventReferenceExpression == null)
            {
                return;
            }

            this.VisitCodeExpression(codeEventReferenceExpression.TargetObject);
        }

        /// <summary>
        /// Visits a <see cref="CodeExpression"/>.
        /// </summary>
        /// <param name="codeExpression">The <see cref="CodeExpression"/> to visit.</param>
        protected virtual void VisitCodeExpression(CodeExpression codeExpression)
        {
            if (codeExpression == null)
            {
                return;
            }

            CodeArgumentReferenceExpression codeArgumentReferenceExpression;
            CodeArrayCreateExpression codeArrayCreateExpression;
            CodeArrayIndexerExpression codeArrayIndexerExpression;
            CodeBaseReferenceExpression codeBaseReferenceExpression;
            CodeBinaryOperatorExpression codeBinaryOperatorExpression;
            CodeCastExpression codeCastExpression;
            CodeDefaultValueExpression codeDefaultValueExpression;
            CodeDelegateCreateExpression codeDelegateCreateExpression;
            CodeDelegateInvokeExpression codeDelegateInvokeExpression;
            CodeDirectionExpression codeDirectionExpression;
            CodeEventReferenceExpression codeEventReferenceExpression;
            CodeFieldReferenceExpression codeFieldReferenceExpression;
            CodeIndexerExpression codeIndexerExpression;
            CodeMethodInvokeExpression codeMethodInvokeExpression;
            CodeMethodReferenceExpression codeMethodReferenceExpression;
            CodeObjectCreateExpression codeObjectCreateExpression;
            CodeParameterDeclarationExpression codeParameterDeclarationExpression;
            CodePrimitiveExpression codePrimitiveExpression;
            CodePropertyReferenceExpression codePropertyReferenceExpression;
            CodePropertySetValueReferenceExpression codePropertySetValueReferenceExpression;
            CodeSnippetExpression codeSnippetExpression;
            CodeThisReferenceExpression codeThisReferenceExpression;
            CodeTypeOfExpression codeTypeOfExpression;
            CodeTypeReferenceExpression codeTypeReferenceExpression;
            CodeVariableReferenceExpression codeVariableReferenceExpression;

            if ((codeArgumentReferenceExpression = codeExpression as CodeArgumentReferenceExpression) != null)
            {
                this.VisitCodeArgumentReferenceExpression(codeArgumentReferenceExpression);
            }
            else if ((codeArrayCreateExpression = codeExpression as CodeArrayCreateExpression) != null)
            {
                this.VisitCodeArrayCreateExpression(codeArrayCreateExpression);
            }
            else if ((codeArrayIndexerExpression = codeExpression as CodeArrayIndexerExpression) != null)
            {
                this.VisitCodeArrayIndexerExpression(codeArrayIndexerExpression);
            }
            else if ((codeBaseReferenceExpression = codeExpression as CodeBaseReferenceExpression) != null)
            {
                this.VisitCodeBaseReferenceExpression(codeBaseReferenceExpression);
            }
            else if ((codeBinaryOperatorExpression = codeExpression as CodeBinaryOperatorExpression) != null)
            {
                this.VisitCodeBinaryOperatorExpression(codeBinaryOperatorExpression);
            }
            else if ((codeCastExpression = codeExpression as CodeCastExpression) != null)
            {
                this.VisitCodeCastExpression(codeCastExpression);
            }
            else if ((codeDefaultValueExpression = codeExpression as CodeDefaultValueExpression) != null)
            {
                this.VisitCodeDefaultValueExpression(codeDefaultValueExpression);
            }
            else if ((codeDelegateCreateExpression = codeExpression as CodeDelegateCreateExpression) != null)
            {
                this.VisitCodeDelegateCreateExpression(codeDelegateCreateExpression);
            }
            else if ((codeDelegateInvokeExpression = codeExpression as CodeDelegateInvokeExpression) != null)
            {
                this.VisitCodeDelegateInvokeExpression(codeDelegateInvokeExpression);
            }
            else if ((codeDirectionExpression = codeExpression as CodeDirectionExpression) != null)
            {
                this.VisitCodeDirectionExpression(codeDirectionExpression);
            }
            else if ((codeEventReferenceExpression = codeExpression as CodeEventReferenceExpression) != null)
            {
                this.VisitCodeEventReferenceExpression(codeEventReferenceExpression);
            }
            else if ((codeFieldReferenceExpression = codeExpression as CodeFieldReferenceExpression) != null)
            {
                this.VisitCodeFieldReferenceExpression(codeFieldReferenceExpression);
            }
            else if ((codeIndexerExpression = codeExpression as CodeIndexerExpression) != null)
            {
                this.VisitCodeIndexerExpression(codeIndexerExpression);
            }
            else if ((codeMethodInvokeExpression = codeExpression as CodeMethodInvokeExpression) != null)
            {
                this.VisitCodeMethodInvokeExpression(codeMethodInvokeExpression);
            }
            else if ((codeMethodReferenceExpression = codeExpression as CodeMethodReferenceExpression) != null)
            {
                this.VisitCodeMethodReferenceExpression(codeMethodReferenceExpression);
            }
            else if ((codeObjectCreateExpression = codeExpression as CodeObjectCreateExpression) != null)
            {
                this.VisitCodeObjectCreateExpression(codeObjectCreateExpression);
            }
            else if ((codeParameterDeclarationExpression = codeExpression as CodeParameterDeclarationExpression) != null)
            {
                this.VisitCodeParameterDeclarationExpression(codeParameterDeclarationExpression);
            }
            else if ((codePrimitiveExpression = codeExpression as CodePrimitiveExpression) != null)
            {
                this.VisitCodePrimitiveExpression(codePrimitiveExpression);
            }
            else if ((codePropertyReferenceExpression = codeExpression as CodePropertyReferenceExpression) != null)
            {
                this.VisitCodePropertyReferenceExpression(codePropertyReferenceExpression);
            }
            else if ((codePropertySetValueReferenceExpression = codeExpression as CodePropertySetValueReferenceExpression) != null)
            {
                this.VisitCodePropertySetValueReferenceExpression(codePropertySetValueReferenceExpression);
            }
            else if ((codeSnippetExpression = codeExpression as CodeSnippetExpression) != null)
            {
                this.VisitCodeSnippetExpression(codeSnippetExpression);
            }
            else if ((codeThisReferenceExpression = codeExpression as CodeThisReferenceExpression) != null)
            {
                this.VisitCodeThisReferenceExpression(codeThisReferenceExpression);
            }
            else if ((codeTypeOfExpression = codeExpression as CodeTypeOfExpression) != null)
            {
                this.VisitCodeTypeOfExpression(codeTypeOfExpression);
            }
            else if ((codeTypeReferenceExpression = codeExpression as CodeTypeReferenceExpression) != null)
            {
                this.VisitCodeTypeReferenceExpression(codeTypeReferenceExpression);
            }
            else if ((codeVariableReferenceExpression = codeExpression as CodeVariableReferenceExpression) != null)
            {
                this.VisitCodeVariableReferenceExpression(codeVariableReferenceExpression);
            }
        }

        /// <summary>
        /// Visits a <see cref="CodeExpressionCollection"/>.
        /// </summary>
        /// <param name="codeExpressionCollection">The <see cref="CodeExpressionCollection"/> to visit.</param>
        protected virtual void VisitCodeExpressionCollection(CodeExpressionCollection codeExpressionCollection)
        {
            // Visit all of the CodeExpression items in the collection.
            foreach (CodeExpression item in codeExpressionCollection.Cast<CodeExpression>())
            {
                this.VisitCodeExpression(item);
            }
        }

        /// <summary>
        /// Visits a <see cref="CodeExpressionStatement"/>.
        /// </summary>
        /// <param name="codeExpressionStatement">The <see cref="CodeExpressionStatement"/> to visit.</param>
        protected virtual void VisitCodeExpressionStatement(CodeExpressionStatement codeExpressionStatement)
        {
            if (codeExpressionStatement == null)
            {
                return;
            }

            this.VisitCodeExpression(codeExpressionStatement.Expression);
            this.VisitCodeLinePragma(codeExpressionStatement.LinePragma);
            this.VisitCodeDirectiveCollection(codeExpressionStatement.StartDirectives);
            this.VisitCodeDirectiveCollection(codeExpressionStatement.EndDirectives);
        }

        /// <summary>
        /// Visits a <see cref="CodeFieldReferenceExpression"/>.
        /// </summary>
        /// <param name="codeFieldReferenceExpression">The <see cref="CodeFieldReferenceExpression"/> to visit.</param>
        protected virtual void VisitCodeFieldReferenceExpression(CodeFieldReferenceExpression codeFieldReferenceExpression)
        {
            if (codeFieldReferenceExpression == null)
            {
                return;
            }

            this.VisitCodeExpression(codeFieldReferenceExpression.TargetObject);
        }

        /// <summary>
        /// Visits a <see cref="CodeGotoStatement"/>.
        /// </summary>
        /// <param name="codeGotoStatement">The <see cref="CodeGotoStatement"/> to visit.</param>
        protected virtual void VisitCodeGotoStatement(CodeGotoStatement codeGotoStatement)
        {
            if (codeGotoStatement == null)
            {
                return;
            }

            this.VisitCodeLinePragma(codeGotoStatement.LinePragma);
            this.VisitCodeDirectiveCollection(codeGotoStatement.StartDirectives);
            this.VisitCodeDirectiveCollection(codeGotoStatement.EndDirectives);
        }

        /// <summary>
        /// Visits a <see cref="CodeIndexerExpression"/>.
        /// </summary>
        /// <param name="codeIndexerExpression">The <see cref="CodeIndexerExpression"/> to visit.</param>
        protected virtual void VisitCodeIndexerExpression(CodeIndexerExpression codeIndexerExpression)
        {
            if (codeIndexerExpression == null)
            {
                return;
            }

            this.VisitCodeExpression(codeIndexerExpression.TargetObject);
            this.VisitCodeExpressionCollection(codeIndexerExpression.Indices);
        }

        /// <summary>
        /// Visits a <see cref="CodeIterationStatement"/>.
        /// </summary>
        /// <param name="codeIterationStatement">The <see cref="CodeIterationStatement"/> to visit.</param>
        protected virtual void VisitCodeIterationStatement(CodeIterationStatement codeIterationStatement)
        {
            if (codeIterationStatement == null)
            {
                return;
            }

            this.VisitCodeStatement(codeIterationStatement.InitStatement);
            this.VisitCodeExpression(codeIterationStatement.TestExpression);
            this.VisitCodeStatement(codeIterationStatement.IncrementStatement);
            this.VisitCodeStatementCollection(codeIterationStatement.Statements);
            this.VisitCodeLinePragma(codeIterationStatement.LinePragma);
            this.VisitCodeDirectiveCollection(codeIterationStatement.StartDirectives);
            this.VisitCodeDirectiveCollection(codeIterationStatement.EndDirectives);
        }

        /// <summary>
        /// Visits a <see cref="CodeLabeledStatement"/>.
        /// </summary>
        /// <param name="codeLabeledStatement">The <see cref="CodeLabeledStatement"/> to visit.</param>
        protected virtual void VisitCodeLabeledStatement(CodeLabeledStatement codeLabeledStatement)
        {
            if (codeLabeledStatement == null)
            {
                return;
            }

            this.VisitCodeStatement(codeLabeledStatement.Statement);
            this.VisitCodeLinePragma(codeLabeledStatement.LinePragma);
            this.VisitCodeDirectiveCollection(codeLabeledStatement.StartDirectives);
            this.VisitCodeDirectiveCollection(codeLabeledStatement.EndDirectives);
        }

        /// <summary>
        /// Visits a <see cref="CodeLinePragma"/>.
        /// </summary>
        /// <param name="codeLinePragma">The <see cref="CodeLinePragma"/> to visit.</param>
        protected virtual void VisitCodeLinePragma(CodeLinePragma codeLinePragma)
        {
        }

        /// <summary>
        /// Visits a <see cref="CodeMemberEvent"/>.
        /// </summary>
        /// <param name="codeMemberEvent">The <see cref="CodeMemberEvent"/> to visit.</param>
        protected virtual void VisitCodeMemberEvent(CodeMemberEvent codeMemberEvent)
        {
            if (codeMemberEvent == null)
            {
                return;
            }

            this.VisitCodeTypeReference(codeMemberEvent.Type);
            this.VisitCodeTypeReference(codeMemberEvent.PrivateImplementationType);
            this.VisitCodeTypeReferenceCollection(codeMemberEvent.ImplementationTypes);
            this.VisitCodeAttributeDeclarationCollection(codeMemberEvent.CustomAttributes);
            this.VisitCodeLinePragma(codeMemberEvent.LinePragma);
            this.VisitCodeCommentStatementCollection(codeMemberEvent.Comments);
            this.VisitCodeDirectiveCollection(codeMemberEvent.StartDirectives);
            this.VisitCodeDirectiveCollection(codeMemberEvent.EndDirectives);
        }

        /// <summary>
        /// Visits a <see cref="CodeMemberField"/>.
        /// </summary>
        /// <param name="codeMemberField">The <see cref="CodeMemberField"/> to visit.</param>
        protected virtual void VisitCodeMemberField(CodeMemberField codeMemberField)
        {
            if (codeMemberField == null)
            {
                return;
            }

            this.VisitCodeTypeReference(codeMemberField.Type);
            this.VisitCodeExpression(codeMemberField.InitExpression);
            this.VisitCodeAttributeDeclarationCollection(codeMemberField.CustomAttributes);
            this.VisitCodeLinePragma(codeMemberField.LinePragma);
            this.VisitCodeCommentStatementCollection(codeMemberField.Comments);
            this.VisitCodeDirectiveCollection(codeMemberField.StartDirectives);
            this.VisitCodeDirectiveCollection(codeMemberField.EndDirectives);
        }

        /// <summary>
        /// Visits a <see cref="CodeMemberMethod"/>.
        /// </summary>
        /// <param name="codeMemberMethod">The <see cref="CodeMemberMethod"/> to visit.</param>
        protected virtual void VisitCodeMemberMethod(CodeMemberMethod codeMemberMethod)
        {
            if (codeMemberMethod == null)
            {
                return;
            }

            CodeConstructor codeConstructor;
            CodeEntryPointMethod codeEntryPointMethod;
            CodeTypeConstructor codeTypeConstructor;

            if ((codeConstructor = codeMemberMethod as CodeConstructor) != null)
            {
                this.VisitCodeConstructor(codeConstructor);
            }
            else if ((codeEntryPointMethod = codeMemberMethod as CodeEntryPointMethod) != null)
            {
                this.VisitCodeEntryPointMethod(codeEntryPointMethod);
            }
            else if ((codeTypeConstructor = codeMemberMethod as CodeTypeConstructor) != null)
            {
                this.VisitCodeTypeConstructor(codeTypeConstructor);
            }

            this.VisitCodeTypeReference(codeMemberMethod.ReturnType);
            this.VisitCodeStatementCollection(codeMemberMethod.Statements);
            this.VisitCodeParameterDeclarationExpressionCollection(codeMemberMethod.Parameters);
            this.VisitCodeTypeReference(codeMemberMethod.PrivateImplementationType);
            this.VisitCodeTypeReferenceCollection(codeMemberMethod.ImplementationTypes);
            this.VisitCodeAttributeDeclarationCollection(codeMemberMethod.ReturnTypeCustomAttributes);
            this.VisitCodeTypeParameterCollection(codeMemberMethod.TypeParameters);
            this.VisitCodeAttributeDeclarationCollection(codeMemberMethod.CustomAttributes);
            this.VisitCodeLinePragma(codeMemberMethod.LinePragma);
            this.VisitCodeCommentStatementCollection(codeMemberMethod.Comments);
            this.VisitCodeDirectiveCollection(codeMemberMethod.StartDirectives);
            this.VisitCodeDirectiveCollection(codeMemberMethod.EndDirectives);
        }

        /// <summary>
        /// Visits a <see cref="CodeMemberProperty"/>.
        /// </summary>
        /// <param name="codeMemberProperty">The <see cref="CodeMemberProperty"/> to visit.</param>
        protected virtual void VisitCodeMemberProperty(CodeMemberProperty codeMemberProperty)
        {
            if (codeMemberProperty == null)
            {
                return;
            }

            this.VisitCodeTypeReference(codeMemberProperty.PrivateImplementationType);
            this.VisitCodeTypeReferenceCollection(codeMemberProperty.ImplementationTypes);
            this.VisitCodeTypeReference(codeMemberProperty.Type);
            this.VisitCodeStatementCollection(codeMemberProperty.GetStatements);
            this.VisitCodeStatementCollection(codeMemberProperty.SetStatements);
            this.VisitCodeParameterDeclarationExpressionCollection(codeMemberProperty.Parameters);
            this.VisitCodeAttributeDeclarationCollection(codeMemberProperty.CustomAttributes);
            this.VisitCodeLinePragma(codeMemberProperty.LinePragma);
            this.VisitCodeCommentStatementCollection(codeMemberProperty.Comments);
            this.VisitCodeDirectiveCollection(codeMemberProperty.StartDirectives);
            this.VisitCodeDirectiveCollection(codeMemberProperty.EndDirectives);
        }

        /// <summary>
        /// Visits a <see cref="CodeMethodInvokeExpression"/>.
        /// </summary>
        /// <param name="codeMethodInvokeExpression">The <see cref="CodeMethodInvokeExpression"/> to visit.</param>
        protected virtual void VisitCodeMethodInvokeExpression(CodeMethodInvokeExpression codeMethodInvokeExpression)
        {
            if (codeMethodInvokeExpression == null)
            {
                return;
            }

            this.VisitCodeMethodReferenceExpression(codeMethodInvokeExpression.Method);
            this.VisitCodeExpressionCollection(codeMethodInvokeExpression.Parameters);
        }

        /// <summary>
        /// Visits a <see cref="CodeMethodReferenceExpression"/>.
        /// </summary>
        /// <param name="codeMethodReferenceExpression">The <see cref="CodeMethodReferenceExpression"/> to visit.</param>
        protected virtual void VisitCodeMethodReferenceExpression(CodeMethodReferenceExpression codeMethodReferenceExpression)
        {
            if (codeMethodReferenceExpression == null)
            {
                return;
            }

            this.VisitCodeExpression(codeMethodReferenceExpression.TargetObject);
            this.VisitCodeTypeReferenceCollection(codeMethodReferenceExpression.TypeArguments);
        }

        /// <summary>
        /// Visits a <see cref="CodeMethodReturnStatement"/>.
        /// </summary>
        /// <param name="codeMethodReturnStatement">The <see cref="CodeMethodReturnStatement"/> to visit.</param>
        protected virtual void VisitCodeMethodReturnStatement(CodeMethodReturnStatement codeMethodReturnStatement)
        {
            if (codeMethodReturnStatement == null)
            {
                return;
            }

            this.VisitCodeExpression(codeMethodReturnStatement.Expression);
            this.VisitCodeLinePragma(codeMethodReturnStatement.LinePragma);
            this.VisitCodeDirectiveCollection(codeMethodReturnStatement.StartDirectives);
            this.VisitCodeDirectiveCollection(codeMethodReturnStatement.EndDirectives);
        }

        /// <summary>
        /// Visits a <see cref="CodeNamespace"/>.
        /// </summary>
        /// <param name="codeNamespace">The <see cref="CodeNamespace"/> to visit.</param>
        protected virtual void VisitCodeNamespace(CodeNamespace codeNamespace)
        {
            if (codeNamespace == null)
            {
                return;
            }

            this.VisitCodeTypeDeclarationCollection(codeNamespace.Types);
            this.VisitCodeNamespaceImportCollection(codeNamespace.Imports);
            this.VisitCodeCommentStatementCollection(codeNamespace.Comments);
        }

        /// <summary>
        /// Visits a <see cref="CodeNamespaceCollection"/>.
        /// </summary>
        /// <param name="codeNamespaceCollection">The <see cref="CodeNamespaceCollection"/> to visit.</param>
        protected virtual void VisitCodeNamespaceCollection(CodeNamespaceCollection codeNamespaceCollection)
        {
            // Visit all of the CodeNamespace items in the collection.
            foreach (CodeNamespace item in codeNamespaceCollection.Cast<CodeNamespace>())
            {
                this.VisitCodeNamespace(item);
            }
        }

        /// <summary>
        /// Visits a <see cref="CodeNamespaceImport"/>.
        /// </summary>
        /// <param name="codeNamespaceImport">The <see cref="CodeNamespaceImport"/> to visit.</param>
        protected virtual void VisitCodeNamespaceImport(CodeNamespaceImport codeNamespaceImport)
        {
            if (codeNamespaceImport == null)
            {
                return;
            }

            this.VisitCodeLinePragma(codeNamespaceImport.LinePragma);
        }

        /// <summary>
        /// Visits a <see cref="CodeNamespaceImportCollection"/>.
        /// </summary>
        /// <param name="codeNamespaceImportCollection">The <see cref="CodeNamespaceImportCollection"/> to visit.</param>
        protected virtual void VisitCodeNamespaceImportCollection(CodeNamespaceImportCollection codeNamespaceImportCollection)
        {
            // Visit all of the CodeNamespaceImport items in the collection.
            foreach (CodeNamespaceImport item in codeNamespaceImportCollection.Cast<CodeNamespaceImport>())
            {
                this.VisitCodeNamespaceImport(item);
            }
        }

        /// <summary>
        /// Visits a <see cref="CodeObjectCreateExpression"/>.
        /// </summary>
        /// <param name="codeObjectCreateExpression">The <see cref="CodeObjectCreateExpression"/> to visit.</param>
        protected virtual void VisitCodeObjectCreateExpression(CodeObjectCreateExpression codeObjectCreateExpression)
        {
            if (codeObjectCreateExpression == null)
            {
                return;
            }

            this.VisitCodeTypeReference(codeObjectCreateExpression.CreateType);
            this.VisitCodeExpressionCollection(codeObjectCreateExpression.Parameters);
        }

        /// <summary>
        /// Visits a <see cref="CodeParameterDeclarationExpression"/>.
        /// </summary>
        /// <param name="codeParameterDeclarationExpression">The <see cref="CodeParameterDeclarationExpression"/> to visit.</param>
        protected virtual void VisitCodeParameterDeclarationExpression(CodeParameterDeclarationExpression codeParameterDeclarationExpression)
        {
            if (codeParameterDeclarationExpression == null)
            {
                return;
            }

            this.VisitCodeAttributeDeclarationCollection(codeParameterDeclarationExpression.CustomAttributes);
            this.VisitCodeTypeReference(codeParameterDeclarationExpression.Type);
        }

        /// <summary>
        /// Visits a <see cref="CodeParameterDeclarationExpressionCollection"/>.
        /// </summary>
        /// <param name="codeParameterDeclarationExpressionCollection">The <see cref="CodeParameterDeclarationExpressionCollection"/> to visit.</param>
        protected virtual void VisitCodeParameterDeclarationExpressionCollection(CodeParameterDeclarationExpressionCollection codeParameterDeclarationExpressionCollection)
        {
            // Visit all of the CodeParameterDeclarationExpression items in the collection.
            foreach (CodeParameterDeclarationExpression item in codeParameterDeclarationExpressionCollection.Cast<CodeParameterDeclarationExpression>())
            {
                this.VisitCodeParameterDeclarationExpression(item);
            }
        }

        /// <summary>
        /// Visits a <see cref="CodePrimitiveExpression"/>.
        /// </summary>
        /// <param name="codePrimitiveExpression">The <see cref="CodePrimitiveExpression"/> to visit.</param>
        protected virtual void VisitCodePrimitiveExpression(CodePrimitiveExpression codePrimitiveExpression)
        {
        }

        /// <summary>
        /// Visits a <see cref="CodePropertyReferenceExpression"/>.
        /// </summary>
        /// <param name="codePropertyReferenceExpression">The <see cref="CodePropertyReferenceExpression"/> to visit.</param>
        protected virtual void VisitCodePropertyReferenceExpression(CodePropertyReferenceExpression codePropertyReferenceExpression)
        {
            if (codePropertyReferenceExpression == null)
            {
                return;
            }

            this.VisitCodeExpression(codePropertyReferenceExpression.TargetObject);
        }

        /// <summary>
        /// Visits a <see cref="CodePropertySetValueReferenceExpression"/>.
        /// </summary>
        /// <param name="codePropertySetValueReferenceExpression">The <see cref="CodePropertySetValueReferenceExpression"/> to visit.</param>
        protected virtual void VisitCodePropertySetValueReferenceExpression(CodePropertySetValueReferenceExpression codePropertySetValueReferenceExpression)
        {
        }

        /// <summary>
        /// Visits a <see cref="CodeRegionDirective"/>.
        /// </summary>
        /// <param name="codeRegionDirective">The <see cref="CodeRegionDirective"/> to visit.</param>
        protected virtual void VisitCodeRegionDirective(CodeRegionDirective codeRegionDirective)
        {
        }

        /// <summary>
        /// Visits a <see cref="CodeRemoveEventStatement"/>.
        /// </summary>
        /// <param name="codeRemoveEventStatement">The <see cref="CodeRemoveEventStatement"/> to visit.</param>
        protected virtual void VisitCodeRemoveEventStatement(CodeRemoveEventStatement codeRemoveEventStatement)
        {
            if (codeRemoveEventStatement == null)
            {
                return;
            }

            this.VisitCodeEventReferenceExpression(codeRemoveEventStatement.Event);
            this.VisitCodeExpression(codeRemoveEventStatement.Listener);
            this.VisitCodeLinePragma(codeRemoveEventStatement.LinePragma);
            this.VisitCodeDirectiveCollection(codeRemoveEventStatement.StartDirectives);
            this.VisitCodeDirectiveCollection(codeRemoveEventStatement.EndDirectives);
        }

        /// <summary>
        /// Visits a <see cref="CodeSnippetCompileUnit"/>.
        /// </summary>
        /// <param name="codeSnippetCompileUnit">The <see cref="CodeSnippetCompileUnit"/> to visit.</param>
        protected virtual void VisitCodeSnippetCompileUnit(CodeSnippetCompileUnit codeSnippetCompileUnit)
        {
            if (codeSnippetCompileUnit == null)
            {
                return;
            }

            this.VisitCodeLinePragma(codeSnippetCompileUnit.LinePragma);
            this.VisitCodeNamespaceCollection(codeSnippetCompileUnit.Namespaces);
            this.VisitCodeAttributeDeclarationCollection(codeSnippetCompileUnit.AssemblyCustomAttributes);
            this.VisitCodeDirectiveCollection(codeSnippetCompileUnit.StartDirectives);
            this.VisitCodeDirectiveCollection(codeSnippetCompileUnit.EndDirectives);
        }

        /// <summary>
        /// Visits a <see cref="CodeSnippetExpression"/>.
        /// </summary>
        /// <param name="codeSnippetExpression">The <see cref="CodeSnippetExpression"/> to visit.</param>
        protected virtual void VisitCodeSnippetExpression(CodeSnippetExpression codeSnippetExpression)
        {
        }

        /// <summary>
        /// Visits a <see cref="CodeSnippetStatement"/>.
        /// </summary>
        /// <param name="codeSnippetStatement">The <see cref="CodeSnippetStatement"/> to visit.</param>
        protected virtual void VisitCodeSnippetStatement(CodeSnippetStatement codeSnippetStatement)
        {
            if (codeSnippetStatement == null)
            {
                return;
            }

            this.VisitCodeLinePragma(codeSnippetStatement.LinePragma);
            this.VisitCodeDirectiveCollection(codeSnippetStatement.StartDirectives);
            this.VisitCodeDirectiveCollection(codeSnippetStatement.EndDirectives);
        }

        /// <summary>
        /// Visits a <see cref="CodeSnippetTypeMember"/>.
        /// </summary>
        /// <param name="codeSnippetTypeMember">The <see cref="CodeSnippetTypeMember"/> to visit.</param>
        protected virtual void VisitCodeSnippetTypeMember(CodeSnippetTypeMember codeSnippetTypeMember)
        {
            if (codeSnippetTypeMember == null)
            {
                return;
            }

            this.VisitCodeAttributeDeclarationCollection(codeSnippetTypeMember.CustomAttributes);
            this.VisitCodeLinePragma(codeSnippetTypeMember.LinePragma);
            this.VisitCodeCommentStatementCollection(codeSnippetTypeMember.Comments);
            this.VisitCodeDirectiveCollection(codeSnippetTypeMember.StartDirectives);
            this.VisitCodeDirectiveCollection(codeSnippetTypeMember.EndDirectives);
        }

        /// <summary>
        /// Visits a <see cref="CodeStatement"/>.
        /// </summary>
        /// <param name="codeStatement">The <see cref="CodeStatement"/> to visit.</param>
        protected virtual void VisitCodeStatement(CodeStatement codeStatement)
        {
            if (codeStatement == null)
            {
                return;
            }

            CodeAssignStatement codeAssignStatement;
            CodeAttachEventStatement codeAttachEventStatement;
            CodeCommentStatement codeCommentStatement;
            CodeConditionStatement codeConditionStatement;
            CodeExpressionStatement codeExpressionStatement;
            CodeGotoStatement codeGotoStatement;
            CodeIterationStatement codeIterationStatement;
            CodeLabeledStatement codeLabeledStatement;
            CodeMethodReturnStatement codeMethodReturnStatement;
            CodeRemoveEventStatement codeRemoveEventStatement;
            CodeSnippetStatement codeSnippetStatement;
            CodeThrowExceptionStatement codeThrowExceptionStatement;
            CodeTryCatchFinallyStatement codeTryCatchFinallyStatement;
            CodeVariableDeclarationStatement codeVariableDeclarationStatement;

            if ((codeAssignStatement = codeStatement as CodeAssignStatement) != null)
            {
                this.VisitCodeAssignStatement(codeAssignStatement);
            }
            else if ((codeAttachEventStatement = codeStatement as CodeAttachEventStatement) != null)
            {
                this.VisitCodeAttachEventStatement(codeAttachEventStatement);
            }
            else if ((codeCommentStatement = codeStatement as CodeCommentStatement) != null)
            {
                this.VisitCodeCommentStatement(codeCommentStatement);
            }
            else if ((codeConditionStatement = codeStatement as CodeConditionStatement) != null)
            {
                this.VisitCodeConditionStatement(codeConditionStatement);
            }
            else if ((codeExpressionStatement = codeStatement as CodeExpressionStatement) != null)
            {
                this.VisitCodeExpressionStatement(codeExpressionStatement);
            }
            else if ((codeGotoStatement = codeStatement as CodeGotoStatement) != null)
            {
                this.VisitCodeGotoStatement(codeGotoStatement);
            }
            else if ((codeIterationStatement = codeStatement as CodeIterationStatement) != null)
            {
                this.VisitCodeIterationStatement(codeIterationStatement);
            }
            else if ((codeLabeledStatement = codeStatement as CodeLabeledStatement) != null)
            {
                this.VisitCodeLabeledStatement(codeLabeledStatement);
            }
            else if ((codeMethodReturnStatement = codeStatement as CodeMethodReturnStatement) != null)
            {
                this.VisitCodeMethodReturnStatement(codeMethodReturnStatement);
            }
            else if ((codeRemoveEventStatement = codeStatement as CodeRemoveEventStatement) != null)
            {
                this.VisitCodeRemoveEventStatement(codeRemoveEventStatement);
            }
            else if ((codeSnippetStatement = codeStatement as CodeSnippetStatement) != null)
            {
                this.VisitCodeSnippetStatement(codeSnippetStatement);
            }
            else if ((codeThrowExceptionStatement = codeStatement as CodeThrowExceptionStatement) != null)
            {
                this.VisitCodeThrowExceptionStatement(codeThrowExceptionStatement);
            }
            else if ((codeTryCatchFinallyStatement = codeStatement as CodeTryCatchFinallyStatement) != null)
            {
                this.VisitCodeTryCatchFinallyStatement(codeTryCatchFinallyStatement);
            }
            else if ((codeVariableDeclarationStatement = codeStatement as CodeVariableDeclarationStatement) != null)
            {
                this.VisitCodeVariableDeclarationStatement(codeVariableDeclarationStatement);
            }

            this.VisitCodeLinePragma(codeStatement.LinePragma);
            this.VisitCodeDirectiveCollection(codeStatement.StartDirectives);
            this.VisitCodeDirectiveCollection(codeStatement.EndDirectives);
        }

        /// <summary>
        /// Visits a <see cref="CodeStatementCollection"/>.
        /// </summary>
        /// <param name="codeStatementCollection">The <see cref="CodeStatementCollection"/> to visit.</param>
        protected virtual void VisitCodeStatementCollection(CodeStatementCollection codeStatementCollection)
        {
            // Visit all of the CodeStatement items in the collection.
            foreach (CodeStatement item in codeStatementCollection.Cast<CodeStatement>())
            {
                this.VisitCodeStatement(item);
            }
        }

        /// <summary>
        /// Visits a <see cref="CodeThisReferenceExpression"/>.
        /// </summary>
        /// <param name="codeThisReferenceExpression">The <see cref="CodeThisReferenceExpression"/> to visit.</param>
        protected virtual void VisitCodeThisReferenceExpression(CodeThisReferenceExpression codeThisReferenceExpression)
        {
        }

        /// <summary>
        /// Visits a <see cref="CodeThrowExceptionStatement"/>.
        /// </summary>
        /// <param name="codeThrowExceptionStatement">The <see cref="CodeThrowExceptionStatement"/> to visit.</param>
        protected virtual void VisitCodeThrowExceptionStatement(CodeThrowExceptionStatement codeThrowExceptionStatement)
        {
            if (codeThrowExceptionStatement == null)
            {
                return;
            }

            this.VisitCodeExpression(codeThrowExceptionStatement.ToThrow);
            this.VisitCodeLinePragma(codeThrowExceptionStatement.LinePragma);
            this.VisitCodeDirectiveCollection(codeThrowExceptionStatement.StartDirectives);
            this.VisitCodeDirectiveCollection(codeThrowExceptionStatement.EndDirectives);
        }

        /// <summary>
        /// Visits a <see cref="CodeTryCatchFinallyStatement"/>.
        /// </summary>
        /// <param name="codeTryCatchFinallyStatement">The <see cref="CodeTryCatchFinallyStatement"/> to visit.</param>
        protected virtual void VisitCodeTryCatchFinallyStatement(CodeTryCatchFinallyStatement codeTryCatchFinallyStatement)
        {
            if (codeTryCatchFinallyStatement == null)
            {
                return;
            }

            this.VisitCodeStatementCollection(codeTryCatchFinallyStatement.TryStatements);
            this.VisitCodeCatchClauseCollection(codeTryCatchFinallyStatement.CatchClauses);
            this.VisitCodeStatementCollection(codeTryCatchFinallyStatement.FinallyStatements);
            this.VisitCodeLinePragma(codeTryCatchFinallyStatement.LinePragma);
            this.VisitCodeDirectiveCollection(codeTryCatchFinallyStatement.StartDirectives);
            this.VisitCodeDirectiveCollection(codeTryCatchFinallyStatement.EndDirectives);
        }

        /// <summary>
        /// Visits a <see cref="CodeTypeConstructor"/>.
        /// </summary>
        /// <param name="codeTypeConstructor">The <see cref="CodeTypeConstructor"/> to visit.</param>
        protected virtual void VisitCodeTypeConstructor(CodeTypeConstructor codeTypeConstructor)
        {
            if (codeTypeConstructor == null)
            {
                return;
            }

            this.VisitCodeTypeReference(codeTypeConstructor.ReturnType);
            this.VisitCodeStatementCollection(codeTypeConstructor.Statements);
            this.VisitCodeParameterDeclarationExpressionCollection(codeTypeConstructor.Parameters);
            this.VisitCodeTypeReference(codeTypeConstructor.PrivateImplementationType);
            this.VisitCodeTypeReferenceCollection(codeTypeConstructor.ImplementationTypes);
            this.VisitCodeAttributeDeclarationCollection(codeTypeConstructor.ReturnTypeCustomAttributes);
            this.VisitCodeTypeParameterCollection(codeTypeConstructor.TypeParameters);
            this.VisitCodeAttributeDeclarationCollection(codeTypeConstructor.CustomAttributes);
            this.VisitCodeLinePragma(codeTypeConstructor.LinePragma);
            this.VisitCodeCommentStatementCollection(codeTypeConstructor.Comments);
            this.VisitCodeDirectiveCollection(codeTypeConstructor.StartDirectives);
            this.VisitCodeDirectiveCollection(codeTypeConstructor.EndDirectives);
        }

        /// <summary>
        /// Visits a <see cref="CodeTypeDeclaration"/>.
        /// </summary>
        /// <param name="codeTypeDeclaration">The <see cref="CodeTypeDeclaration"/> to visit.</param>
        protected virtual void VisitCodeTypeDeclaration(CodeTypeDeclaration codeTypeDeclaration)
        {
            if (codeTypeDeclaration == null)
            {
                return;
            }

            CodeTypeDelegate codeTypeDelegate;

            if ((codeTypeDelegate = codeTypeDeclaration as CodeTypeDelegate) != null)
            {
                this.VisitCodeTypeDelegate(codeTypeDelegate);
            }

            this.VisitCodeTypeReferenceCollection(codeTypeDeclaration.BaseTypes);
            this.VisitCodeTypeMemberCollection(codeTypeDeclaration.Members);
            this.VisitCodeTypeParameterCollection(codeTypeDeclaration.TypeParameters);
            this.VisitCodeAttributeDeclarationCollection(codeTypeDeclaration.CustomAttributes);
            this.VisitCodeLinePragma(codeTypeDeclaration.LinePragma);
            this.VisitCodeCommentStatementCollection(codeTypeDeclaration.Comments);
            this.VisitCodeDirectiveCollection(codeTypeDeclaration.StartDirectives);
            this.VisitCodeDirectiveCollection(codeTypeDeclaration.EndDirectives);
        }

        /// <summary>
        /// Visits a <see cref="CodeTypeDeclarationCollection"/>.
        /// </summary>
        /// <param name="codeTypeDeclarationCollection">The <see cref="CodeTypeDeclarationCollection"/> to visit.</param>
        protected virtual void VisitCodeTypeDeclarationCollection(CodeTypeDeclarationCollection codeTypeDeclarationCollection)
        {
            // Visit all of the CodeTypeDeclaration items in the collection.
            foreach (CodeTypeDeclaration item in codeTypeDeclarationCollection.Cast<CodeTypeDeclaration>())
            {
                this.VisitCodeTypeDeclaration(item);
            }
        }

        /// <summary>
        /// Visits a <see cref="CodeTypeDelegate"/>.
        /// </summary>
        /// <param name="codeTypeDelegate">The <see cref="CodeTypeDelegate"/> to visit.</param>
        protected virtual void VisitCodeTypeDelegate(CodeTypeDelegate codeTypeDelegate)
        {
            if (codeTypeDelegate == null)
            {
                return;
            }

            this.VisitCodeTypeReference(codeTypeDelegate.ReturnType);
            this.VisitCodeParameterDeclarationExpressionCollection(codeTypeDelegate.Parameters);
            this.VisitCodeTypeReferenceCollection(codeTypeDelegate.BaseTypes);
            this.VisitCodeTypeMemberCollection(codeTypeDelegate.Members);
            this.VisitCodeTypeParameterCollection(codeTypeDelegate.TypeParameters);
            this.VisitCodeAttributeDeclarationCollection(codeTypeDelegate.CustomAttributes);
            this.VisitCodeLinePragma(codeTypeDelegate.LinePragma);
            this.VisitCodeCommentStatementCollection(codeTypeDelegate.Comments);
            this.VisitCodeDirectiveCollection(codeTypeDelegate.StartDirectives);
            this.VisitCodeDirectiveCollection(codeTypeDelegate.EndDirectives);
        }

        /// <summary>
        /// Visits a <see cref="CodeTypeMember"/>.
        /// </summary>
        /// <param name="codeTypeMember">The <see cref="CodeTypeMember"/> to visit.</param>
        protected virtual void VisitCodeTypeMember(CodeTypeMember codeTypeMember)
        {
            if (codeTypeMember == null)
            {
                return;
            }

            CodeMemberMethod codeMemberMethod;
            CodeMemberEvent codeMemberEvent;
            CodeMemberField codeMemberField;
            CodeMemberProperty codeMemberProperty;
            CodeSnippetTypeMember codeSnippetTypeMember;
            CodeTypeDeclaration codeTypeDeclaration;

            if ((codeMemberMethod = codeTypeMember as CodeMemberMethod) != null)
            {
                this.VisitCodeMemberMethod(codeMemberMethod);
            }
            else if ((codeMemberEvent = codeTypeMember as CodeMemberEvent) != null)
            {
                this.VisitCodeMemberEvent(codeMemberEvent);
            }
            else if ((codeMemberField = codeTypeMember as CodeMemberField) != null)
            {
                this.VisitCodeMemberField(codeMemberField);
            }
            else if ((codeMemberProperty = codeTypeMember as CodeMemberProperty) != null)
            {
                this.VisitCodeMemberProperty(codeMemberProperty);
            }
            else if ((codeSnippetTypeMember = codeTypeMember as CodeSnippetTypeMember) != null)
            {
                this.VisitCodeSnippetTypeMember(codeSnippetTypeMember);
            }
            else if ((codeTypeDeclaration = codeTypeMember as CodeTypeDeclaration) != null)
            {
                this.VisitCodeTypeDeclaration(codeTypeDeclaration);
            }

            this.VisitCodeAttributeDeclarationCollection(codeTypeMember.CustomAttributes);
            this.VisitCodeLinePragma(codeTypeMember.LinePragma);
            this.VisitCodeCommentStatementCollection(codeTypeMember.Comments);
            this.VisitCodeDirectiveCollection(codeTypeMember.StartDirectives);
            this.VisitCodeDirectiveCollection(codeTypeMember.EndDirectives);
        }

        /// <summary>
        /// Visits a <see cref="CodeTypeMemberCollection"/>.
        /// </summary>
        /// <param name="codeTypeMemberCollection">The <see cref="CodeTypeMemberCollection"/> to visit.</param>
        protected virtual void VisitCodeTypeMemberCollection(CodeTypeMemberCollection codeTypeMemberCollection)
        {
            // Visit all of the CodeTypeMember items in the collection.
            foreach (CodeTypeMember item in codeTypeMemberCollection.Cast<CodeTypeMember>())
            {
                this.VisitCodeTypeMember(item);
            }
        }

        /// <summary>
        /// Visits a <see cref="CodeTypeOfExpression"/>.
        /// </summary>
        /// <param name="codeTypeOfExpression">The <see cref="CodeTypeOfExpression"/> to visit.</param>
        protected virtual void VisitCodeTypeOfExpression(CodeTypeOfExpression codeTypeOfExpression)
        {
            if (codeTypeOfExpression == null)
            {
                return;
            }

            this.VisitCodeTypeReference(codeTypeOfExpression.Type);
        }

        /// <summary>
        /// Visits a <see cref="CodeTypeParameter"/>.
        /// </summary>
        /// <param name="codeTypeParameter">The <see cref="CodeTypeParameter"/> to visit.</param>
        protected virtual void VisitCodeTypeParameter(CodeTypeParameter codeTypeParameter)
        {
            if (codeTypeParameter == null)
            {
                return;
            }

            this.VisitCodeTypeReferenceCollection(codeTypeParameter.Constraints);
            this.VisitCodeAttributeDeclarationCollection(codeTypeParameter.CustomAttributes);
        }

        /// <summary>
        /// Visits a <see cref="CodeTypeParameterCollection"/>.
        /// </summary>
        /// <param name="codeTypeParameterCollection">The <see cref="CodeTypeParameterCollection"/> to visit.</param>
        protected virtual void VisitCodeTypeParameterCollection(CodeTypeParameterCollection codeTypeParameterCollection)
        {
            // Visit all of the CodeTypeParameter items in the collection.
            foreach (CodeTypeParameter item in codeTypeParameterCollection.Cast<CodeTypeParameter>())
            {
                this.VisitCodeTypeParameter(item);
            }
        }

        /// <summary>
        /// Visits a <see cref="CodeTypeReference"/>.
        /// </summary>
        /// <param name="codeTypeReference">The <see cref="CodeTypeReference"/> to visit.</param>
        protected virtual void VisitCodeTypeReference(CodeTypeReference codeTypeReference)
        {
            if (codeTypeReference == null)
            {
                return;
            }

            this.VisitCodeTypeReference(codeTypeReference.ArrayElementType);
            this.VisitCodeTypeReferenceCollection(codeTypeReference.TypeArguments);
        }

        /// <summary>
        /// Visits a <see cref="CodeTypeReferenceCollection"/>.
        /// </summary>
        /// <param name="codeTypeReferenceCollection">The <see cref="CodeTypeReferenceCollection"/> to visit.</param>
        protected virtual void VisitCodeTypeReferenceCollection(CodeTypeReferenceCollection codeTypeReferenceCollection)
        {
            // Visit all of the CodeTypeReference items in the collection.
            foreach (CodeTypeReference item in codeTypeReferenceCollection.Cast<CodeTypeReference>())
            {
                this.VisitCodeTypeReference(item);
            }
        }

        /// <summary>
        /// Visits a <see cref="CodeTypeReferenceExpression"/>.
        /// </summary>
        /// <param name="codeTypeReferenceExpression">The <see cref="CodeTypeReferenceExpression"/> to visit.</param>
        protected virtual void VisitCodeTypeReferenceExpression(CodeTypeReferenceExpression codeTypeReferenceExpression)
        {
            if (codeTypeReferenceExpression == null)
            {
                return;
            }

            this.VisitCodeTypeReference(codeTypeReferenceExpression.Type);
        }

        /// <summary>
        /// Visits a <see cref="CodeVariableDeclarationStatement"/>.
        /// </summary>
        /// <param name="codeVariableDeclarationStatement">The <see cref="CodeVariableDeclarationStatement"/> to visit.</param>
        protected virtual void VisitCodeVariableDeclarationStatement(CodeVariableDeclarationStatement codeVariableDeclarationStatement)
        {
            if (codeVariableDeclarationStatement == null)
            {
                return;
            }

            this.VisitCodeExpression(codeVariableDeclarationStatement.InitExpression);
            this.VisitCodeTypeReference(codeVariableDeclarationStatement.Type);
            this.VisitCodeLinePragma(codeVariableDeclarationStatement.LinePragma);
            this.VisitCodeDirectiveCollection(codeVariableDeclarationStatement.StartDirectives);
            this.VisitCodeDirectiveCollection(codeVariableDeclarationStatement.EndDirectives);
        }

        /// <summary>
        /// Visits a <see cref="CodeVariableReferenceExpression"/>.
        /// </summary>
        /// <param name="codeVariableReferenceExpression">The <see cref="CodeVariableReferenceExpression"/> to visit.</param>
        protected virtual void VisitCodeVariableReferenceExpression(CodeVariableReferenceExpression codeVariableReferenceExpression)
        {
        }
    }
}