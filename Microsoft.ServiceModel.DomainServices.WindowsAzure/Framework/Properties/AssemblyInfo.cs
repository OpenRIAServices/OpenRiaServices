using System;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Microsoft.ServiceModel.DomainServices.WindowsAzure")]
[assembly: AssemblyDescription("Microsoft.ServiceModel.DomainServices.WindowsAzure.dll")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Outercurve Foundation")]
[assembly: AssemblyProduct("Open RIA Services")]
[assembly: AssemblyCopyright("© Outercurve Foundation.  All rights reserved.")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("fd3dcea7-4ea9-4de9-9575-c149f09d3985")]

[assembly: InternalsVisibleTo("Microsoft.ServiceModel.DomainServices.WindowsAzure.Test")]

[assembly: CLSCompliant(false)]
[assembly: NeutralResourcesLanguageAttribute("en-US")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Revision and Build Numbers 
// by using the '*' as shown below:
[assembly: AssemblyVersion("4.0.0.0")]
// AssemblyFileVersion attribute is generated automatically by a custom MSBuild task inside AutomaticAssemblyVersion.targets
//[assembly: AssemblyFileVersion("1.0.0.14")]

// Specifically opt in to the .Net 4.0 transparency rules
// and mark the entire assembly to be SecurityTransparent
#if CODECOV
[assembly: SecurityRules(SecurityRuleSet.Level2, SkipVerificationInFullTrust = true)]
#else
[assembly: SecurityRules(SecurityRuleSet.Level2)]
[assembly: SecurityTransparent]
#endif
