using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceModel.Configuration;
using OpenRiaServices.DomainServices.Hosting;
using System.Web.Configuration;
using System.Xml;
using System.Xml.Linq;
using EnvDTE;

namespace OpenRiaServices.VisualStudio.DomainServices.Tools
{
    /// <summary>
    /// public helper class for reading and writing sections of the
    /// configuration file significant to the domain service wizards
    /// </summary>
    public class WebConfigUtil
    {
        private const string ValidationSectionName = "validation";
        private const string ValidateIntegratedModeConfigurationAttributeName = "validateIntegratedModeConfiguration";

        private const string SystemWebServerSectionName = "system.webServer";
        private const string SystemServiceModelSectionName = "system.serviceModel";
        private const string DomainServicesSectionName = "domainServices";
        private const string DomainServicesFullSectionName = "system.serviceModel/domainServices";
        private const string ServiceHostingEnvironmentFullSectionName = "system.serviceModel/serviceHostingEnvironment";

        // Boolean.FalseString cannot be used for boolean attribute values.  It is the wrong case.
        private const string FalseAttributeValue = "false";

        private readonly System.Configuration.Configuration _configuration;

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="configuration">The configuration to use.</param>
        public WebConfigUtil(System.Configuration.Configuration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }
            this._configuration = configuration;
        }

        /// <summary>
        /// Retrieves or creates the system.webServer section
        /// </summary>
        /// <remarks>
        /// This section self initializes to empty xml, so if we detect that condition,
        /// we initialize it to empty but valid xml so that it can be manipulated.
        /// </remarks>
        /// <returns>The existing or new system.webServer section.</returns>
        public IgnoreSection GetOrCreateSystemWebServerSection()
        {
            // This section has no strongly typed equivalent, so we treat it only as an IgnoreSection
            IgnoreSection webServerSection = this._configuration.GetSection(SystemWebServerSectionName) as IgnoreSection;
            if (webServerSection == null)
            {
                webServerSection = new IgnoreSection();
                this._configuration.Sections.Add(SystemWebServerSectionName, webServerSection);
            }

            // Detect empty xml and initialize it to legal empty state if found.
            // We do this to simplify the logic of parsing it and adding new sections.
            SectionInformation sectionInformation = webServerSection.SectionInformation;
            string rawXml = sectionInformation.GetRawXml();
            if (string.IsNullOrEmpty(rawXml))
            {
                rawXml = "<" + SystemWebServerSectionName + "/>";
                sectionInformation.SetRawXml(rawXml);
            }
            return webServerSection;
        }

        /// <summary>
        /// Returns the <see cref="ServiceModelSectionGroup"/> for the "system.serviceModel"
        /// section group.  If it does not exist, a new default one will be created.
        /// </summary>
        /// <returns>A new or existing <see cref="ServiceModelSectionGroup"/></returns>
        public ServiceModelSectionGroup GetOrCreateServiceModelSectionGroup()
        {
            ServiceModelSectionGroup serviceModelSectionGroup = this._configuration.GetSectionGroup(SystemServiceModelSectionName) as ServiceModelSectionGroup;
            if (serviceModelSectionGroup == null)
            {
                serviceModelSectionGroup = new ServiceModelSectionGroup();
                this._configuration.SectionGroups.Add(SystemServiceModelSectionName, serviceModelSectionGroup);
            }
            return serviceModelSectionGroup;
        }

        /// <summary>
        /// Returns the <see cref="DomainServicesSection"/> for the "system.serviceModel/domainServices"
        /// section.  If it does not exist, a new default one will be created.
        /// </summary>
        /// <param name="created">Output parameter that is set to <c>true</c> if the section did not exist and was created here.</param>
        /// <returns>A new or existing <see cref="DomainServicesSection"/>.</returns>
        public DomainServicesSection GetOrCreateDomainServicesSection(out bool created)
        {
            created = false;
            DomainServicesSection domainServicesSection = this._configuration.GetSection(DomainServicesFullSectionName) as DomainServicesSection;
            if (domainServicesSection == null)
            {
                ServiceModelSectionGroup serviceModelSectionGroup = this.GetOrCreateServiceModelSectionGroup();

                domainServicesSection = this._configuration.GetSection(DomainServicesFullSectionName) as DomainServicesSection;
                if (domainServicesSection == null)
                {
                    domainServicesSection = new DomainServicesSection();
                    domainServicesSection.SectionInformation.AllowDefinition = ConfigurationAllowDefinition.MachineToApplication;
                    domainServicesSection.SectionInformation.RequirePermission = false;
                    serviceModelSectionGroup.Sections.Add(DomainServicesSectionName, domainServicesSection);
                    created = true;
                }
            }
            return domainServicesSection;
        }

        /// <summary>
        /// Determines whether system.serviceModel/domainServices has an endpoint
        /// declared of the given name. 
        /// </summary>
        /// <param name="endpointName">The name of the endpoint to check.</param>
        /// <returns><c>true</c> means there is an endpoint with that name declared.</returns>
        public bool IsEndpointDeclared(string endpointName)
        {
            Debug.Assert(!string.IsNullOrEmpty(endpointName), "endpointName cannot be empty");

            DomainServicesSection domainServicesSection = this._configuration.GetSection(DomainServicesFullSectionName) as DomainServicesSection;
            return ((domainServicesSection != null) &&
                     domainServicesSection.Endpoints.OfType<ProviderSettings>().Any(p => string.Equals(p.Name, endpointName, StringComparison.OrdinalIgnoreCase)));
        }

        /// <summary>
        /// Adds the specified endpoint to the "system.serviceModel/domainServices" endpoint collection.
        /// </summary>
        /// <param name="endpointName">The name of the new endpoint to add.</param>
        /// <param name="endpointFactoryTypeName">The fully qualified type name of the endpoint factory type.</param>
        public void AddEndpointDeclaration(string endpointName, string endpointFactoryTypeName)
        {
            Debug.Assert(!string.IsNullOrEmpty(endpointName), "endpointName cannot be empty");
            Debug.Assert(!string.IsNullOrEmpty(endpointFactoryTypeName), "endpointFactoryTypeName cannot be empty");

            if (!string.IsNullOrEmpty(endpointName) && !string.IsNullOrEmpty(endpointFactoryTypeName))
            {
                bool createdDomainServicesSection = false;
                DomainServicesSection domainServicesSection = this.GetOrCreateDomainServicesSection(out createdDomainServicesSection);
                if (domainServicesSection != null)
                {
                    // If the DomainServicesSection existed, just add the new endpoint
                    if (!createdDomainServicesSection)
                    {
                        domainServicesSection.Endpoints.Add(new ProviderSettings(endpointName, endpointFactoryTypeName));
                    }
                    else
                    {
                        // However, if we created it manually, we need to insert the new endpoint manually,
                        // otherwise the configuration logic believes we are overriding the defaults
                        // and emits extra <remove> elements.
                        string rawXml = domainServicesSection.SectionInformation.GetRawXml();
                        if (string.IsNullOrEmpty(rawXml))
                        {
                            rawXml = "<domainServices><endpoints /></domainServices>";
                        }
                        XDocument xdoc = WebConfigUtil.CreateXDoc(rawXml);

                        XElement xelem = xdoc.Element("domainServices");
                        XElement endpointsElem = xelem == null ? null : xelem.Element("endpoints");
                        if (endpointsElem != null)
                        {
                            // Add a new <add name="xxx" type="yyy" /> element to the endpoints collection
                            XElement newEndpointElem = new XElement("add");
                            newEndpointElem.Add(new XAttribute("name", endpointName));
                            newEndpointElem.Add(new XAttribute("type", endpointFactoryTypeName));
                            endpointsElem.Add(newEndpointElem);
                            rawXml = WebConfigUtil.CreateRawXml(xdoc);
                            domainServicesSection.SectionInformation.SetRawXml(rawXml);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Determines whether system.serviceModel/serviceHostingEnvironment aspNetCompatibilityEnabled property
        /// is properly configured to support our domain service.
        /// </summary>
        /// <returns><c>true</c> means the aspNetCompatibilityEnabled property is set in the configuration.</returns>
        public bool IsAspNetCompatibilityEnabled()
        {
            ServiceHostingEnvironmentSection section = this._configuration.GetSection(ServiceHostingEnvironmentFullSectionName) as ServiceHostingEnvironmentSection;
            return (section != null && section.AspNetCompatibilityEnabled);
        }

        /// <summary>
        /// Determines whether system.serviceModel/serviceHostingEnvironment multipleSiteBindingsEnabled property
        /// is properly configured to support our domain service.
        /// </summary>
        /// <returns><c>true</c> means the aspNetCompatibilityEnabled property is set in the configuration.</returns>
        public bool IsMultipleSiteBindingsEnabled()
        {
            ServiceHostingEnvironmentSection section = this._configuration.GetSection(ServiceHostingEnvironmentFullSectionName) as ServiceHostingEnvironmentSection;
            return (section != null && section.MultipleSiteBindingsEnabled);
        }

        /// <summary>
        /// Sets the system.serviceModel/serviceHostingEnvironment aspNetCompatibilityEnabled property to the
        /// specified value.
        /// </summary>
        /// <param name="enabled">The value to set for the aspNetCompatibilityEnabled property.</param>
        public void SetAspNetCompatibilityEnabled(bool enabled)
        {
            ServiceHostingEnvironmentSection section = this._configuration.GetSection(ServiceHostingEnvironmentFullSectionName) as ServiceHostingEnvironmentSection;
            if (section != null)
            {
                section.AspNetCompatibilityEnabled = enabled;
            }
        }

        /// <summary>
        /// Sets the system.serviceModel/serviceHostingEnvironment multipleSiteBindingsEnabled property to the
        /// specified value.
        /// </summary>
        /// <param name="enabled">The value to set for the multipleSiteBindingsEnabled property.</param>
        public void SetMultipleSiteBindingsEnabled(bool enabled)
        {
            ServiceHostingEnvironmentSection section = this._configuration.GetSection(ServiceHostingEnvironmentFullSectionName) as ServiceHostingEnvironmentSection;
            if (section != null)
            {
                section.MultipleSiteBindingsEnabled = enabled;
            }
        }

        /// <summary>
        /// Determines whether we need to add an httpModule to system.web for our domain service module
        /// </summary>
        /// <returns><c>true</c> means we need to modify the configuration to add an httpModule</returns>
        public bool DoWeNeedToAddHttpModule()
        {
            System.Web.Configuration.HttpModulesSection httpModulesSection = (System.Web.Configuration.HttpModulesSection)this._configuration.GetSection("system.web/httpModules");
            if (httpModulesSection != null)
            {
                return !httpModulesSection.Modules.OfType<HttpModuleAction>()
                    .Any(a => String.Equals(a.Name, BusinessLogicClassConstants.DomainServiceModuleName, StringComparison.OrdinalIgnoreCase));
            }
            return true;
        }

        /// <summary>
        /// Determines whether we need to add a module to the system.webServer section
        /// </summary>
        /// <remarks>This module section is used by IIS in integrated mode and is necessary to deploy this domain service.</remarks>
        /// <returns><c>true</c> if we need to add a module to the system.webServer section.</returns>
        public bool DoWeNeedToAddModuleToWebServer()
        {
            IgnoreSection webServerSection = this._configuration.GetSection(SystemWebServerSectionName) as IgnoreSection;
            if (webServerSection != null)
            {
                SectionInformation sectionInformation = webServerSection.SectionInformation;
                string rawXml = sectionInformation == null ? null : sectionInformation.GetRawXml();
                if (string.IsNullOrEmpty(rawXml))
                {
                    return true;
                }

                XDocument xdoc = WebConfigUtil.CreateXDoc(rawXml);

                // The logic is actually the following, but we null check to protect against malformed xml
                // xdoc.Element("system.webServer")
                //                    .Element("modules")
                //                    .Elements("add")
                //                    .Any(e => (string)e.Attribute("name") == BusinessLogicClassConstants.DomainServiceModuleName);
                XElement xelem = xdoc.Element(SystemWebServerSectionName);
                if (xelem != null)
                {
                    xelem = xelem.Element("modules");

                    if (xelem == null)
                    {
                        return true;
                    }

                    IEnumerable<XElement> xelems = xelem.Elements("add");
                    return xelems == null ? false : !xelems.Any(e => (string)e.Attribute("name") == BusinessLogicClassConstants.DomainServiceModuleName);
                }
            }
            return true;
        }

        /// <summary>
        /// Determines whether we need to add the "validateIntegratedModeConfiguration=false" attribute to the system.webServer section
        /// </summary>
        /// <remarks>This section is used by IIS in integrated mode and is necessary to deploy this domain service.</remarks>
        /// <returns><c>true</c> if we need to add validateIntegratedModeConfiguration to the system.webServer section.</returns>
        public bool DoWeNeedToValidateIntegratedModeToWebServer()
        {
            IgnoreSection webServerSection = this._configuration.GetSection(SystemWebServerSectionName) as IgnoreSection;
            if (webServerSection != null)
            {
                SectionInformation sectionInformation = webServerSection.SectionInformation;
                string rawXml = sectionInformation == null ? null : sectionInformation.GetRawXml();
                if (string.IsNullOrEmpty(rawXml))
                {
                    return true;
                }

                XDocument xdoc = WebConfigUtil.CreateXDoc(rawXml);

                XElement xelem = xdoc.Element(SystemWebServerSectionName);
                if (xelem != null)
                {
                    xelem = xelem.Element(WebConfigUtil.ValidationSectionName);
                    XAttribute attr = xelem == null ? null : xelem.Attribute(WebConfigUtil.ValidateIntegratedModeConfigurationAttributeName);
                    return attr == null ? true : !string.Equals(attr.Value, WebConfigUtil.FalseAttributeValue, StringComparison.OrdinalIgnoreCase);
                }
            }

            return true;
        }


        /// <summary>
        /// Adds an httpModule to the system.web section to point to our domain service module
        /// </summary>
        /// <param name="domainServiceModuleTypeName">Full type name to the domain service module</param>
        public void AddHttpModule(string domainServiceModuleTypeName)
        {
            Debug.Assert(!string.IsNullOrEmpty(domainServiceModuleTypeName), "domainServiceModuleTypeName cannot be empty");

            if (!string.IsNullOrEmpty(domainServiceModuleTypeName))
            {
                System.Web.Configuration.HttpModulesSection httpModulesSection = (System.Web.Configuration.HttpModulesSection)this._configuration.GetSection("system.web/httpModules");
                if (httpModulesSection != null)
                {
                    HttpModuleActionCollection modules = httpModulesSection.Modules;
                    modules.Add(new HttpModuleAction(BusinessLogicClassConstants.DomainServiceModuleName,
                                                        domainServiceModuleTypeName));
                }
            }
        }

        /// <summary>
        /// Adds a module to the system.webServer section to point to our domain service module
        /// </summary>
        /// <param name="domainServiceModuleTypeName">Full type name of the domain service module</param>
        public void AddModuleToWebServer(string domainServiceModuleTypeName)
        {
            IgnoreSection webServerSection = this.GetOrCreateSystemWebServerSection();
            SectionInformation sectionInformation = webServerSection.SectionInformation;
            string rawXml = sectionInformation.GetRawXml();
            if (!string.IsNullOrEmpty(rawXml))
            {
                XDocument xdoc = WebConfigUtil.CreateXDoc(rawXml);

                XElement webSvrElement = xdoc.Element(SystemWebServerSectionName);
                XElement xelem = webSvrElement.Element("modules");

                if (xelem == null)
                {
                    xelem = new XElement("modules");
                    webSvrElement.Add(xelem);
                }

                // Ensure we have the runAllManagedModulesForAllRequests attribute.
                // If it is present, we do not alter it
                XAttribute runAllManagedAttr = xelem.Attribute("runAllManagedModulesForAllRequests");
                if (runAllManagedAttr == null)
                {
                    runAllManagedAttr = new XAttribute("runAllManagedModulesForAllRequests", "true");
                    xelem.Add(runAllManagedAttr);
                }

                XElement newElem = new XElement("add",
                                                new XAttribute("name", BusinessLogicClassConstants.DomainServiceModuleName),
                                                new XAttribute("preCondition", BusinessLogicClassConstants.ManagedHandler),
                                                new XAttribute("type", domainServiceModuleTypeName));
                xelem.Add(newElem);

                rawXml = WebConfigUtil.CreateRawXml(xdoc);

                sectionInformation.SetRawXml(rawXml);
            }
        }

        /// <summary>
        /// Adds the validateIntegratedModeConfiguration to the system.webServer/validation section
        /// </summary>
        public void AddValidateIntegratedModeToWebServer()
        {
            IgnoreSection webServerSection = this.GetOrCreateSystemWebServerSection();
   
            SectionInformation sectionInformation = webServerSection.SectionInformation;
            string rawXml = sectionInformation.GetRawXml();
            if (!string.IsNullOrEmpty(rawXml))
            {
                XDocument xdoc = WebConfigUtil.CreateXDoc(rawXml);

                XElement webSvrElement = xdoc.Element(SystemWebServerSectionName);
                XElement xelem = webSvrElement.Element(WebConfigUtil.ValidationSectionName);

                if (xelem == null)
                {
                    xelem = new XElement(WebConfigUtil.ValidationSectionName);
                    webSvrElement.Add(xelem);
                }

                XAttribute attr = xelem.Attribute(WebConfigUtil.ValidateIntegratedModeConfigurationAttributeName);
                if (attr != null)
                {
                    attr.SetValue(WebConfigUtil.FalseAttributeValue);
                }
                else
                {
                    attr = new XAttribute(WebConfigUtil.ValidateIntegratedModeConfigurationAttributeName, WebConfigUtil.FalseAttributeValue);
                    xelem.Add(attr);
                }

                rawXml = WebConfigUtil.CreateRawXml(xdoc);

                sectionInformation.SetRawXml(rawXml);
            }
        }

        /// <summary>
        /// Generates the raw xml for the given <see cref="XDocument"/>
        /// </summary>
        /// <param name="xdoc">The document whose Xml is needed.</param>
        /// <returns>The xml as a string.</returns>
        private static string CreateRawXml(XDocument xdoc)
        {
            string rawXml = null;
            if (xdoc != null)
            {
                using (StringWriter sw = new StringWriter(CultureInfo.InvariantCulture))
                {
                    XmlWriterSettings settings = new XmlWriterSettings();
                    settings.OmitXmlDeclaration = true;
                    using (XmlWriter xmlWriter = XmlWriter.Create(sw, settings))
                    {
                        xdoc.WriteTo(xmlWriter);
                    }
                    rawXml = sw.ToString();
                }
            }
            return rawXml;
        }

        /// <summary>
        /// Generates an <see cref="XDocument"/> for the given raw xml.
        /// </summary>
        /// <param name="rawXml">The Xml to generate the document for.</param>
        /// <returns>The xml as an <see cref="XDocument"/></returns>
        private static XDocument CreateXDoc(string rawXml)
        {
            if (rawXml == null)
            {
                return null;
            }
            StringReader sr = null;
            try
            {
                sr = new StringReader(rawXml);
                using (XmlReader xmlReader = XmlReader.Create(sr))
                {
                    sr = null;
                    return XDocument.Load(xmlReader);
                }
            }
            finally
            {
                if (sr != null)
                {
                    sr.Dispose();
                }
            }
        }

        /// <summary>
        /// Obtains the full type name of the domain service module.
        /// </summary>
        /// <returns>The type name of the domain service module, suitable for inclusion in web.config</returns>
        public static string GetDomainServiceModuleTypeName()
        {
            return typeof(OpenRiaServices.DomainServices.Hosting.DomainServiceHttpModule).AssemblyQualifiedName;
        }

        /// <summary>
        /// Obtains the full type name of the OData endpoint factory type.
        /// </summary>
        /// <returns>The type name of the OData endpoint factory, suitable for inclusion in web.config</returns>
        public static string GetODataEndpointFactoryTypeName()
        {
            return typeof(OpenRiaServices.DomainServices.Hosting.ODataEndpointFactory).AssemblyQualifiedName;
        }
    }
}
