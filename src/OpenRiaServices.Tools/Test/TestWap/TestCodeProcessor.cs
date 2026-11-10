using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using OpenRiaServices.Server;

namespace TestWap
{
    public class TestCodeProcessor : CodeProcessor
    {
        public TestCodeProcessor(CodeDomProvider codeDomProvider) : base(codeDomProvider) { }
        public override void ProcessGeneratedCode(DomainServiceDescription domainServiceDescription, CodeCompileUnit codeCompileUnit, IDictionary<Type, CodeTypeDeclaration> typeMapping)
        {
            // Get a reference to the entity class
            CodeTypeDeclaration codeGenEntity = typeMapping[typeof(TestEntity)];

            string baseDir = AppContext.BaseDirectory;
            codeGenEntity.Comments.Add(new CodeCommentStatement("[CodeProcessor] BaseDirectory:" + baseDir));
        }
    }
}