﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;

[assembly: AssemblyCompany("Microsoft Open Technologies, Inc.")]
[assembly: AssemblyCopyright("© Microsoft Open Technologies, Inc. All rights reserved.")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyTrademark("")]
[assembly: ComVisible(false)]
[assembly: CLSCompliant(true)]

[assembly: NeutralResourcesLanguage("en-US")]

// ===========================================================================
//  DO NOT EDIT OR REMOVE ANYTHING BELOW THIS COMMENT.
//  Version numbers are automatically generated based on regular expressions.
// ===========================================================================

#if ASPNETMVC && ASPNETWEBPAGES
#error Runtime projects cannot define both ASPNETMVC and ASPNETWEBPAGES
#elif ASPNETMVC
[assembly: AssemblyVersion("4.1.0.0")] // ASPNETMVC
[assembly: AssemblyFileVersion("4.1.0.0")] // ASPNETMVC
[assembly: AssemblyProduct("Microsoft ASP.NET MVC")]
#elif ASPNETWEBPAGES
[assembly: AssemblyVersion("2.1.0.0")] // ASPNETWEBPAGES
[assembly: AssemblyFileVersion("2.1.0.0")] // ASPNETWEBPAGES
[assembly: AssemblyProduct("Microsoft ASP.NET Web Pages")]
#else
#error Runtime projects must define either ASPNETMVC or ASPNETWEBPAGES
#endif
