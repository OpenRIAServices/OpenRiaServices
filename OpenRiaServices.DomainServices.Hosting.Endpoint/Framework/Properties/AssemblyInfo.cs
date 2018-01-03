﻿using System;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Security;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("OpenRiaServices.DomainServices.Hosting.EndPoints")]
[assembly: AssemblyDescription("OpenRiaServices.DomainServices.Hosting.EndPoints")]
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
[assembly: Guid("cad3cfb7-c7f6-4563-8efa-d6cf3ae55bf0")]

[assembly: CLSCompliant(true)]
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


#if SIGNED
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("OpenRiaServices.DomainServices.Hosting.Local.Test, PublicKey=002400000480000094000000060200000024000052534131000400000100010057f9918c4d29954e1a3fd925872f43beb4ad085112b4c5e4c0d66d5f759fa319d9fc2cd8db2d3f14c0c033b196c5d79b69fff0f8b96cc387b75771dc0cfc7874b10b502b37d6d01879e9370c20e17b7cca74b46eb90b6f4d88abfe9706de49d9fbac2d67f372bf22e5675fc11a164ed9f72ffa4a596b99f6458a106d88e9fbc1")]
    [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("OpenRiaServices.DomainServices.Hosting.Endpoint.Test, PublicKey=002400000480000094000000060200000024000052534131000400000100010057f9918c4d29954e1a3fd925872f43beb4ad085112b4c5e4c0d66d5f759fa319d9fc2cd8db2d3f14c0c033b196c5d79b69fff0f8b96cc387b75771dc0cfc7874b10b502b37d6d01879e9370c20e17b7cca74b46eb90b6f4d88abfe9706de49d9fbac2d67f372bf22e5675fc11a164ed9f72ffa4a596b99f6458a106d88e9fbc1")]
#else
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("OpenRiaServices.DomainServices.Hosting.Local.Test")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("OpenRiaServices.DomainServices.Hosting.Endpoint.Test")]
#endif