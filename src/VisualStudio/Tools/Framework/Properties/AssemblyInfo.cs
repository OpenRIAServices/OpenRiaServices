﻿using System;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]   // xaml elements not ComVisible
[assembly: CLSCompliant(false)]

[assembly: NeutralResourcesLanguageAttribute("en-US")]
[assembly: InternalsVisibleTo("OpenRiaServices.VisualStudio.DomainServices.Tools.Test, PublicKey=00240000048000009400000006020000002400005253413100040000010001006165f2d838c2494ed8ed9644fbc060f22fea4941d552916f6ca7078f64b7d5a6053ff36e63eb312fa909ae4223e8393d20eed67217e46747cebb6fa5b348738c5785568d642b0bf499f7863829242e655372773636d6c974d2d5abd97c57640893f7dd390cfd1015268ee85611f51c71068e8a15a829a97ea9dad46d1619b3b0")]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("ffba88cc-80fb-4495-82d3-efe26e296a81")]


// Assembly verification must be skipped for Code Coverage runs
#if CODECOV
[assembly: SecurityRules(SecurityRuleSet.Level2, SkipVerificationInFullTrust = true)]
#endif
