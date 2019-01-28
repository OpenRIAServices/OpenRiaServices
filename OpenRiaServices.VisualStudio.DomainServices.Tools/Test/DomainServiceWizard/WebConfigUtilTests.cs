using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.ServiceModel.Configuration;
using OpenRiaServices.DomainServices.Hosting;
using System.Xml;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Web.Configuration;
using DescriptionAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;

namespace OpenRiaServices.VisualStudio.DomainServices.Tools.Test
{
    [TestClass]
    public class WebConfigUtilTests
    {
        private static readonly string EmptyConfig = "<?xml version=\"1.0\"?><configuration></configuration>";

        [TestMethod]
        [Description("WebConfigUtil helper class API's function correctly")]
        public void WebConfigUtil_UpdateConfiguration()
        {
            string tempConfigFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tempConfigFile, EmptyConfig);
                System.Configuration.Configuration cfg = ConfigurationManager.OpenExeConfiguration(tempConfigFile);
                WebConfigUtil webConfigUtil = new WebConfigUtil(cfg);

                // Verify that none of the sections we wat to set are present
                Assert.IsFalse(webConfigUtil.IsAspNetCompatibilityEnabled(), "Blank config should not have AspNetCompatibility");
                Assert.IsFalse(webConfigUtil.IsMultipleSiteBindingsEnabled(), "Blank config should not have MultiSiteBinding");
                Assert.IsFalse(webConfigUtil.IsEndpointDeclared(BusinessLogicClassConstants.ODataEndpointName), "Blank config should not have OData endpoint");
                Assert.IsTrue(webConfigUtil.DoWeNeedToValidateIntegratedModeToWebServer(), "Blank config should not have validate integrated mode");
                Assert.IsTrue(webConfigUtil.DoWeNeedToAddHttpModule(), "Blank config should not have http module");
                Assert.IsTrue(webConfigUtil.DoWeNeedToAddModuleToWebServer(), "Blank config should not have http module to web server");

                string domainServiceFactoryName = WebConfigUtil.GetDomainServiceModuleTypeName();
                Assert.IsFalse(string.IsNullOrEmpty(domainServiceFactoryName), "Could not find domain service factory name");

                // ------------------------------------
                // Set everything we set from the wizard
                // ------------------------------------
                webConfigUtil.SetAspNetCompatibilityEnabled(true);
                webConfigUtil.SetMultipleSiteBindingsEnabled(true);
                webConfigUtil.AddValidateIntegratedModeToWebServer();
                webConfigUtil.AddHttpModule(domainServiceFactoryName);
                webConfigUtil.AddModuleToWebServer(domainServiceFactoryName);

                // ------------------------------------
                // Verify API's see the changes
                // ------------------------------------
                Assert.IsTrue(webConfigUtil.IsAspNetCompatibilityEnabled(), "Failed to set AspNetCompatibility");
                Assert.IsTrue(webConfigUtil.IsMultipleSiteBindingsEnabled(), "Failed to set MultiSiteBinding");
                Assert.IsFalse(webConfigUtil.DoWeNeedToValidateIntegratedModeToWebServer(), "Failed to set validate integrated mode");
                Assert.IsFalse(webConfigUtil.DoWeNeedToAddHttpModule(), "Failed to set http module");
                Assert.IsFalse(webConfigUtil.DoWeNeedToAddModuleToWebServer(), "Failed to set http module to web server");

                // ------------------------------------
                // Independently verify those changes
                // ------------------------------------
                // AspNetCompat
                ServiceHostingEnvironmentSection section = cfg.GetSection("system.serviceModel/serviceHostingEnvironment") as ServiceHostingEnvironmentSection;
                Assert.IsTrue(section != null && section.AspNetCompatibilityEnabled, "AspNetCompat did not set correct section");

                // MultisiteBindings
                Assert.IsTrue(section != null && section.MultipleSiteBindingsEnabled, "MultisiteBinding did not set correct section");

                // Http modules
                System.Web.Configuration.HttpModulesSection httpModulesSection = cfg.GetSection("system.web/httpModules") as System.Web.Configuration.HttpModulesSection;
                HttpModuleAction module = (httpModulesSection == null)
                                            ? null
                                            : httpModulesSection.Modules.OfType<HttpModuleAction>()
                                                 .FirstOrDefault(a => String.Equals(a.Name, BusinessLogicClassConstants.DomainServiceModuleName, StringComparison.OrdinalIgnoreCase));
                Assert.IsNotNull(module, "Did not find httpModule");
 
                // ------------------------------------
                // Set and verify OData endpoint
                // ------------------------------------
                webConfigUtil.AddEndpointDeclaration(BusinessLogicClassConstants.ODataEndpointName, WebConfigUtil.GetODataEndpointFactoryTypeName());
                Assert.IsTrue(webConfigUtil.IsEndpointDeclared(BusinessLogicClassConstants.ODataEndpointName), "Failed to set OData endpoint");

                DomainServicesSection domainServicesSection = cfg.GetSection("system.serviceModel/domainServices") as DomainServicesSection;
                Assert.IsNotNull(domainServicesSection, "system.serviceModel/domainServices section not found");
                Assert.AreEqual(ConfigurationAllowDefinition.MachineToApplication, domainServicesSection.SectionInformation.AllowDefinition, "AllowDefinition s/b MachineToApplication");
                Assert.IsFalse(domainServicesSection.SectionInformation.RequirePermission, "RequirePermission s/b false");

                ProviderSettings setting = (domainServicesSection == null)
                                            ? null
                                            : domainServicesSection.Endpoints.OfType<ProviderSettings>().FirstOrDefault(p => string.Equals(p.Name, BusinessLogicClassConstants.ODataEndpointName, StringComparison.OrdinalIgnoreCase));
                Assert.IsNotNull(setting, "Did not find OData endpoint in config");

                // ValidateIntegratedMode
                this.CheckValidateIntegratedMode(cfg);

                // WebServer module
                this.CheckWebServerModule(cfg, WebConfigUtil.GetDomainServiceModuleTypeName());

            }
            catch (Exception ex)
            {
                Assert.Fail("Did not expect exception " + ex);
            }
            finally
            {
                File.Delete(tempConfigFile);
            }
        }

        // Helper method to parse ValidateIntegratedMode and verify correct
        private void CheckValidateIntegratedMode(System.Configuration.Configuration cfg)
        {
            IgnoreSection webServerSection = cfg.GetSection("system.webServer") as IgnoreSection;
            if (webServerSection != null)
            {
                SectionInformation sectionInformation = webServerSection.SectionInformation;
                string rawXml = sectionInformation == null ? null : sectionInformation.GetRawXml();
                Assert.IsFalse(string.IsNullOrEmpty(rawXml), "Did not expect empty system.webServer xml");

                XDocument xdoc = null;
                using (StringReader sr = new StringReader(rawXml))
                {
                    using (XmlReader xmlReader = XmlReader.Create(sr))
                    {
                        xdoc = XDocument.Load(xmlReader);
                    }
                }

                XElement xelem = xdoc.Element("system.webServer");
                Assert.IsNotNull(xelem, "system.webServer Xelement was null");

                xelem = xelem.Element("validation");
                Assert.IsNotNull(xelem, "system.webServer validation element was null");

                XAttribute attr = xelem.Attribute("validateIntegratedModeConfiguration");
                Assert.IsNotNull(attr, "system.webServer validateIntegratedMode attribute was null");
                Assert.AreEqual(attr.Value, "false", "validateIntegrateModel value was incorrect");
            }
        }

        // Helper method to parse and check the module name in the system.webServer section
        private void CheckWebServerModule(System.Configuration.Configuration cfg, string moduleName)
        {
            IgnoreSection webServerSection = cfg.GetSection("system.webServer") as IgnoreSection;
            if (webServerSection != null)
            {
                SectionInformation sectionInformation = webServerSection.SectionInformation;
                string rawXml = sectionInformation == null ? null : sectionInformation.GetRawXml();
                Assert.IsFalse(string.IsNullOrEmpty(rawXml), "Did not expect empty system.webServer xml");

                XDocument xdoc = null;
                using (StringReader sr = new StringReader(rawXml))
                {
                    using (XmlReader xmlReader = XmlReader.Create(sr))
                    {
                        xdoc = XDocument.Load(xmlReader);
                    }
                }

                XElement xelem = xdoc.Element("system.webServer");
                Assert.IsNotNull(xelem, "system.webServer Xelement was null");

                xelem = xelem.Element("modules");
                Assert.IsNotNull(xelem, "system.webServer modules Xelement was null");

                XAttribute runAllManagedAttr = xelem.Attribute("runAllManagedModulesForAllRequests");
                Assert.IsNotNull(runAllManagedAttr, "Did not find attribute for runAllManagedModulesForAllRequests");
                Assert.AreEqual("true", runAllManagedAttr.Value, "runAllManagedModulesForAllRequests should have been true");

                IEnumerable<XElement> xelems = xelem.Elements("add");
                Assert.IsNotNull(xelems, "system.webServer modules add elements null");
                xelem = xelems.FirstOrDefault(e => (string)e.Attribute("name") == BusinessLogicClassConstants.DomainServiceModuleName);
                Assert.IsNotNull(xelem, "Did not find DomainServiceModule attribute");
                Assert.AreEqual(moduleName, (string)xelem.Attribute("type"), "DomainServiceModule name is incorrect");
            }
        }
    }
}