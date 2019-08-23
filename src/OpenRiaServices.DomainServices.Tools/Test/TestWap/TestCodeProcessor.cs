using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using OpenRiaServices.DomainServices.Server;
using System.Web;
using System.Web.Configuration;

namespace TestWap
{
    public class TestCodeProcessor : CodeProcessor
    {
        public TestCodeProcessor(CodeDomProvider codeDomProvider) : base(codeDomProvider) { }
        public override void ProcessGeneratedCode(DomainServiceDescription domainServiceDescription, CodeCompileUnit codeCompileUnit, IDictionary<Type, CodeTypeDeclaration> typeMapping)
        {
            // Get a reference to the entity class
            CodeTypeDeclaration codeGenEntity = typeMapping[typeof(TestEntity)];

            AppDomain appDomain = AppDomain.CurrentDomain;
            AppDomainSetup setup = appDomain.SetupInformation;

            string baseDir = appDomain.BaseDirectory;
            codeGenEntity.Comments.Add(new CodeCommentStatement("[CodeProcessor] BaseDirectory:" + baseDir));

            Configuration cfg = WebConfigurationManager.OpenWebConfiguration(null);

            AuthenticationSection authSection = (AuthenticationSection)cfg.GetSection("system.web/authentication");
            FormsAuthenticationConfiguration formsAuth = authSection.Forms;
            if (formsAuth != null)
            {
                codeGenEntity.Comments.Add(new CodeCommentStatement("[CodeProcessor] Authentication:forms"));
            }

            ConnectionStringsSection connSect = cfg.ConnectionStrings;
            if (connSect != null)
            {
                ConnectionStringSettingsCollection connColl = connSect.ConnectionStrings;
                foreach (ConnectionStringSettings connSetting in connColl)
                {
                    codeGenEntity.Comments.Add(new CodeCommentStatement("[CodeProcessor] ConnectionString:" + connSetting.ConnectionString));
                }
            }
        }
    }
}