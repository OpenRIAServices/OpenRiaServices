using System;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("System.ServiceModel.DomainServices.Hosting.OData")]
[assembly: AssemblyDescription("System.ServiceModel.DomainServices.Hosting.OData.dll")]
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
[assembly: Guid("82539B51-6BB5-4C04-B37D-4FA850E397CF")]

[assembly: CLSCompliant(true)]
[assembly: NeutralResourcesLanguageAttribute("en-US")]

[assembly: InternalsVisibleTo("System.ServiceModel.DomainServices.Hosting.OData.Test, PublicKey=00240000048000009400000006020000002400005253413100040000010001001dc70401884cdfad2010ce192e1f08a30fb034cf504759943eec3359d4ed09af3ce1616dbb124e479617ec73e4162903766e7a5e7bf1984bb318040118fe0f69dfb8b6e5c7c47a0e1bc9a8984b22f7221cc235986c09c74cab38ea3562c18adb8e3a95b73faf1ed71d7c309058b86d951af2165eb215b47de335e360a6a25da7")]

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


