using System;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Microsoft.VisualStudio.ServiceModel.DomainServices.Tools")]
[assembly: AssemblyDescription("Microsoft.VisualStudio.ServiceModel.DomainServices.Tools.dll")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Outercurve Foundation")]
[assembly: AssemblyProduct("Open RIA Services")]
[assembly: AssemblyCopyright("© Outercurve Foundation.  All rights reserved.")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]   // xaml elements not ComVisible
[assembly: CLSCompliant(false)]

[assembly: NeutralResourcesLanguageAttribute("en-US")]

[assembly: InternalsVisibleTo("Microsoft.VisualStudio.ServiceModel.DomainServices.Tools.Test")]


// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("ffba88cc-80fb-4495-82d3-efe26e296a81")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("4.0.0.0")]
// AssemblyFileVersion attribute is generated automatically by a custom MSBuild task inside AutomaticAssemblyVersion.targets
//[assembly: AssemblyFileVersion("1.0.0.14")]

// Assembly verification must be skipped for Code Coverage runs
#if CODECOV
[assembly: SecurityRules(SecurityRuleSet.Level2, SkipVerificationInFullTrust = true)]
#endif
