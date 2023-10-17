[![Stand With Ukraine](https://raw.githubusercontent.com/vshymanskyy/StandWithUkraine/main/banner2-direct.svg)](https://vshymanskyy.github.io/StandWithUkraine)

The software is provided free of charge, but **I urge you to use some of the money saved by using this software to support Ukraine**
The civilian suffering due to the Russian invasion are enourmous, the attacks on hospitals and other war crimes performed by the Russian invaders are horrifying.
You can find links for donating to various projects at https://standforukraine.com/ and https://stand-with-ukraine.pp.ua/

# OpenRiaServiecs

Azure Pipelines: [![Build Status](https://dev.azure.com/OpenRiaServices/OpenRiaServices/_apis/build/status/OpenRIAServices.OpenRiaServices?branchName=main)](https://dev.azure.com/OpenRiaServices/OpenRiaServices/_build/latest?definitionId=1&branchName=main)
[![Tests](https://img.shields.io/azure-devops/tests/OpenRiaServices/OpenRiaServices/1/main.svg)](https://dev.azure.com/OpenRiaServices/OpenRiaServices/_build/latest?definitionId=1&branchName=main)
[![Coverage](https://img.shields.io/azure-devops/coverage/OpenRiaServices/OpenRiaServices/1/main)](https://dev.azure.com/OpenRiaServices/OpenRiaServices/_build/latest?definitionId=1&branchName=main)



Sonarqube:
[![Maintainability Rating](https://sonarcloud.io/api/project_badges/measure?project=OpenRIAServices_OpenRiaServices&metric=sqale_rating)](https://sonarcloud.io/summary/overall?id=OpenRIAServices_OpenRiaServices)
[![Security Rating](https://sonarcloud.io/api/project_badges/measure?project=OpenRIAServices_OpenRiaServices&metric=security_rating)](https://sonarcloud.io/summary/overall?id=OpenRIAServices_OpenRiaServices)
[![Vulnerabilities](https://sonarcloud.io/api/project_badges/measure?project=OpenRIAServices_OpenRiaServices&metric=vulnerabilities)](https://sonarcloud.io/project/overview?id=OpenRIAServices_OpenRiaServices)

<!-- Below badges should be reenabled once new scripts for appveyor build is set up

[![Coverity Scan Build Status](https://scan.coverity.com/projects/8802/badge.svg)](https://scan.coverity.com/projects/daniel-svensson-openriaservices)
-->

Open Ria Services is a framework for  helping with the development of rich internet connected native "n-tier" applications. 
It is the evolved Open Source version of *WCF RIA Services*.

The source code and issue list is currently kept at github (https://github.com/OpenRiaServices/OpenRiaServices).

Some of their features are: 
 * Client side entity change tracking similar in concept to Entity Framework
   * Batch save (all or nothing) and undo functionality
 * Excellent support for data binding in with built in support for validation, INotifyPropertyChanged, INotifyCollectionChanged .. 
 * Support for client side queries (where, orderby, skip, take ..)
 * Saves you from having to duplicated lots of code on the server and client
   * Code generation which generates code for client (Model and API) based on server code
   * Automatically handles DTO creation and mapping based on attributes or configuration
   * Allows sharing validation and other logic by using partial classes and automatic linking of files
   
**Release Notes / Changelog**

* A [Change log](https://github.com/OpenRIAServices/OpenRiaServices/blob/main/Changelog.md) is kept keeping track of changes made and write down release notes as features are developed
* [Github releases with changes and Release Notes](https://github.com/OpenRIAServices/OpenRiaServices/releases) for specific versions are created when a new version is released to nuget.

   
**Documentation**:
* General [Documentation](https://openriaservices.gitbook.io/openriaservices/) is based on WCF RIA Services documentation and while not fully updated it is still relevant.
* Changes since WCF RIA Services can be found under https://github.com/OpenRIAServices/OpenRiaServices/releases) and [Change log](https://github.com/OpenRIAServices/OpenRiaServices/blob/main/Changelog.md) 
* The [wiki](https://github.com/OpenRIAServices/OpenRiaServices/wiki) contains various good information
* The [Roadmap / Vision](https://github.com/OpenRIAServices/OpenRiaServices/wiki/Vision---Roadmap) might also be of interest

Contribution Guidelines can be found at https://github.com/OpenRIAServices/OpenRiaServices/wiki/Contribution-Guidelines


# Nuget packages

Here are the most common nuget packages and their current versions.

|Package | Stable | Prerelease |
|------- | ------ | ---------- |
| OpenRiaServices.Client | [![NuGet](https://img.shields.io/nuget/v/OpenRiaServices.Client.svg)](https://www.nuget.org/packages/OpenRiaServices.Client) | [![NuGet](https://img.shields.io/nuget/vpre/OpenRiaServices.Client.svg)](https://www.nuget.org/packages/OpenRiaServices.Client) |
| OpenRiaServices.Client.Core | [![NuGet](https://img.shields.io/nuget/v/OpenRiaServices.Client.Core.svg)](https://www.nuget.org/packages/OpenRiaServices.Client.Core) | [![NuGet](https://img.shields.io/nuget/vpre/OpenRiaServices.Client.Core.svg)](https://www.nuget.org/packages/OpenRiaServices.Client.Core) |
| OpenRiaServices.Client.CodeGen | [![NuGet](https://img.shields.io/nuget/v/OpenRiaServices.Client.CodeGen.svg)](https://www.nuget.org/packages/OpenRiaServices.Client.CodeGen) | [![NuGet](https://img.shields.io/nuget/vpre/OpenRiaServices.Client.CodeGen.svg)](https://www.nuget.org/packages/OpenRiaServices.Client.CodeGen) |
| OpenRiaServices.Hosting.Wcf | [![NuGet](https://img.shields.io/nuget/v/OpenRiaServices.Hosting.Wcf.svg)](https://www.nuget.org/packages/OpenRiaServices.Hosting.Wcf) | [![NuGet](https://img.shields.io/nuget/vpre/OpenRiaServices.Hosting.Wcf.svg)](https://www.nuget.org/packages/OpenRiaServices.Hosting.Wcf) |
| OpenRiaServices.Hosting.AspNetCore | [![NuGet](https://img.shields.io/nuget/v/OpenRiaServices.Hosting.AspNetCore.svg)](https://www.nuget.org/packages/OpenRiaServices.Hosting.AspNetCore) | [![NuGet](https://img.shields.io/nuget/vpre/OpenRiaServices.Hosting.AspNetCore.svg)](https://www.nuget.org/packages/OpenRiaServices.Hosting.AspNetCore) |
| OpenRiaServices.Server | [![NuGet](https://img.shields.io/nuget/v/OpenRiaServices.Server.svg)](https://www.nuget.org/packages/OpenRiaServices.Server) | [![NuGet](https://img.shields.io/nuget/vpre/OpenRiaServices.Server.svg)](https://www.nuget.org/packages/OpenRiaServices.Server) |
| OpenRiaServices.Server.Authentication.AspNetMembership | [![NuGet](https://img.shields.io/nuget/v/OpenRiaServices.Server.Authentication.AspNetMembership.svg)](https://www.nuget.org/packages/OpenRiaServices.Server.Authentication.AspNetMembership) | [![NuGet](https://img.shields.io/nuget/vpre/OpenRiaServices.Server.Authentication.AspNetMembership.svg)](https://www.nuget.org/packages/OpenRiaServices.Server.Authentication.AspNetMembership) |
| OpenRiaServices.Server.EntityFrameworkCore | [![NuGet](https://img.shields.io/nuget/v/OpenRiaServices.Server.EntityFrameworkCore.svg)](https://www.nuget.org/packages/OpenRiaServices.Server.EntityFrameworkCore) | [![NuGet](https://img.shields.io/nuget/vpre/OpenRiaServices.Server.EntityFrameworkCore.svg)](https://www.nuget.org/packages/OpenRiaServices.Server.EntityFrameworkCore) |
| OpenRiaServices.EntityFramework | [![NuGet](https://img.shields.io/nuget/v/OpenRiaServices.EntityFramework.svg)](https://www.nuget.org/packages/OpenRiaServices.EntityFramework) | [![NuGet](https://img.shields.io/nuget/vpre/OpenRiaServices.EntityFramework.svg)](https://www.nuget.org/packages/OpenRiaServices.EntityFramework) |
| OpenRiaServices.EntityFramework.EF4 | [![NuGet](https://img.shields.io/nuget/v/OpenRiaServices.EntityFramework.EF4.svg)](https://www.nuget.org/packages/OpenRiaServices.EntityFramework.EF4) | *depreciated* use EF6 instead|
| OpenRiaServices.T4 | [![NuGet](https://img.shields.io/nuget/v/OpenRiaServices.T4.svg)](https://www.nuget.org/packages/OpenRiaServices.T4) | [![NuGet](https://img.shields.io/nuget/vpre/OpenRiaServices.T4.svg)]() |
| OpenRiaServices.Endpoints | [![NuGet](https://img.shields.io/nuget/v/OpenRiaServices.Endpoints.svg)](https://www.nuget.org/packages/OpenRiaServices.Endpoints) | [![NuGet](https://img.shields.io/nuget/vpre/OpenRiaServices.Endpoints.svg)](https://www.nuget.org/packages/OpenRiaServices.Endpoints) |

# Code of Conduct

This project has adopted the code of conduct defined by the Contributor Covenant to clarify expected behavior in our community.
For more information see the [.NET Foundation Code of Conduct](https://dotnetfoundation.org/code-of-conduct).

# .NET Foundation

This project is supported by the [.NET Foundation](https://dotnetfoundation.org).
