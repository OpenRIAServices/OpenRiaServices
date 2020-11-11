using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using OpenRiaServices.Server;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpenRiaServices.Tools.Test
{
    /// <summary>
    /// Tests the <see cref="DomainServiceProxyGeneratorAttribute"/> class
    /// </summary>
    [TestClass]
    public class DomainServiceCodeGeneratorAttributeTests
    {
        public DomainServiceCodeGeneratorAttributeTests()
        {
        }

        [TestMethod]
        [Description("DomainServiceClientProxyGenerator ctor taking strings work properly")]
        public void DomainServiceCodeGeneratorAttribute_Ctor_Strings()
        {
            // nulls allowed
            DomainServiceClientCodeGeneratorAttribute attr = new DomainServiceClientCodeGeneratorAttribute((string) null, null);
            Assert.IsNull(attr.GeneratorName, "Generator name not null");
            Assert.IsNull(attr.Language, "Language not null");

            // empty strings allowed
            attr = new DomainServiceClientCodeGeneratorAttribute(string.Empty, string.Empty);
            Assert.AreEqual(string.Empty, attr.GeneratorName, "Generator name not empty");
            Assert.AreEqual(string.Empty, attr.Language, "Language not empty");

            // valid strings accepted
            attr = new DomainServiceClientCodeGeneratorAttribute("AName", "ALanguage");
            Assert.AreEqual("AName", attr.GeneratorName, "Generator name not respected");
            Assert.AreEqual("ALanguage", attr.Language, "Language not respected");
        }

        [TestMethod]
        [Description("DomainServiceClientProxyGenerator ctor taking Type work properly")]
        public void DomainServiceCodeGeneratorAttribute_Ctor_Type()
        {
            // nulls allowed
            DomainServiceClientCodeGeneratorAttribute attr = new DomainServiceClientCodeGeneratorAttribute((Type) null, null);
            Assert.AreEqual(string.Empty, attr.GeneratorName, "Generator name not empty");
            Assert.IsNull(attr.Language, "Language not null");

            // empty strings allowed
            attr = new DomainServiceClientCodeGeneratorAttribute((Type) null, string.Empty);
            Assert.AreEqual(string.Empty, attr.GeneratorName, "Generator name not empty");
            Assert.AreEqual(string.Empty, attr.Language, "Language not empty");

            // valid type accepted
            attr = new DomainServiceClientCodeGeneratorAttribute(typeof(DSCPG_Generator), "ALanguage");
            Assert.AreEqual(typeof(DSCPG_Generator).FullName, attr.GeneratorName, "Generator name the type's full name");
            Assert.AreEqual("ALanguage", attr.Language, "Language not respected");
        }
    }

    public class DSCPG_Generator : IDomainServiceClientCodeGenerator
    {

        public string GenerateCode(ICodeGenerationHost codeGenerationHost, IEnumerable<DomainServiceDescription> domainServiceDescriptions, ClientCodeGenerationOptions options)
        {
            throw new NotImplementedException();
        }
    }
}
