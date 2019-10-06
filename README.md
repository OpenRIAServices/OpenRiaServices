# OpenRiaServiecs

Azure Pipelines: [![Build Status](https://dev.azure.com/OpenRiaServices/OpenRiaServices/_apis/build/status/OpenRIAServices.OpenRiaServices?branchName=master)](https://dev.azure.com/OpenRiaServices/OpenRiaServices/_build/latest?definitionId=1&branchName=master)
[![Tests](https://img.shields.io/azure-devops/tests/OpenRiaServices/OpenRiaServices/1/master.svg)](https://dev.azure.com/OpenRiaServices/OpenRiaServices/_build/latest?definitionId=1&branchName=master)


Sonarqube: [![Sonarqube - technical debpt](https://img.shields.io/sonar/https/sonarcloud.io/OpenRIAServices_OpenRiaServices/tech_debt.svg)](https://sonarcloud.io/dashboard?id=OpenRIAServices_OpenRiaServices)

LGTM [![Total alerts](https://img.shields.io/lgtm/alerts/g/OpenRIAServices/OpenRiaServices.svg?logo=lgtm&logoWidth=18)](https://lgtm.com/projects/g/OpenRIAServices/OpenRiaServices/alerts/)

<!-- Below badges should be reenabled once new scripts for appveyor build is set up

Appveyor: [![Build status](https://img.shields.io/appveyor/ci/OpenRiaServices/openriaservices/master.svg)](https://ci.appveyor.com/project/OpenRiaServices/OpenRiaServices/branch/master)

[![Coverity Scan Build Status](https://scan.coverity.com/projects/8802/badge.svg)](https://scan.coverity.com/projects/daniel-svensson-openriaservices)
-->

Open Ria Services is a framework for  helping with the development of rich internet connected native "n-tier" applications. 
It is the evolved Open Source version of *WCF RIA Services*.

The source code and issue list is currently kept at github (https://github.com/OpenRiaServices/OpenRiaServices).

Some of ther features are: 
 * Client side entity change tracking similar in concept to Entity Framework
   * Batch save (all or nothing) and undo functionality
 * Excellent support for data binding in with built in support for validation, INotifyPropertyChanged, INotifyCollectionChanged .. 
 * Support for client side queries (where, orderby, skip, take ..)
 * Saves you from having to duplicated lots of code on the server and client
   * Code generation which generates code for client (Model and API) based on server code
   * Automatically handles DTO creation and mapping based on attributes or configuration
   * Allows sharing validation and other logic by using partial classes and automatic linking of files
   
**Documentation**:
* The original documentation for WCF RIA Services is still relevant and can be found at https://msdn.microsoft.com/en-us/library/ee707344(v=vs.91).aspx . Namespaces and assembly names are no longer correct since they changed with the release of OpenRiaServices.
* Documentation for changes since WCF RIA Services can be found under https://github.com/OpenRIAServices/OpenRiaServices/releases)
* The [wiki](https://github.com/OpenRIAServices/OpenRiaServices/wiki) contains various good information
* The [Roadmap / Vision](https://github.com/OpenRIAServices/OpenRiaServices/wiki/Vision---Roadmap) might also be of interest

Contribution Guidelines can be found at https://github.com/OpenRIAServices/OpenRiaServices/wiki/Contribution-Guidelines


# Nuget packages

Here are the most common nuget packages and their current versions.

|Package | Stable | Prerelease |
|------- | ------ | ---------- |
| OpenRiaServices.Client | [![NuGet](https://img.shields.io/nuget/v/OpenRiaServices.Client.svg)](https://www.nuget.org/packages/OpenRiaServices.Client) | [![NuGet](https://img.shields.io/nuget/vpre/OpenRiaServices.Client.svg)]() |
| OpenRiaServices.Client.Core | [![NuGet](https://img.shields.io/nuget/v/OpenRiaServices.Client.Core.svg)](https://www.nuget.org/packages/OpenRiaServices.Client.Core) | [![NuGet](https://img.shields.io/nuget/vpre/OpenRiaServices.Client.Core.svg)]() |
| OpenRiaServices.Client.CodeGen | [![NuGet](https://img.shields.io/nuget/v/OpenRiaServices.Client.CodeGen.svg)](https://www.nuget.org/packages/OpenRiaServices.Client.CodeGen) | [![NuGet](https://img.shields.io/nuget/vpre/OpenRiaServices.Client.CodeGen.svg)]() |
| OpenRiaServices.Server | [![NuGet](https://img.shields.io/nuget/v/OpenRiaServices.Server.svg)](https://www.nuget.org/packages/OpenRiaServices.Server) | [![NuGet](https://img.shields.io/nuget/vpre/OpenRiaServices.Server.svg)]() |
| OpenRiaServices.EntityFramework | [![NuGet](https://img.shields.io/nuget/v/OpenRiaServices.EntityFramework.svg)](https://www.nuget.org/packages/OpenRiaServices.EntityFramework) | [![NuGet](https://img.shields.io/nuget/vpre/OpenRiaServices.EntityFramework.svg)]() |
| OpenRiaServices.EntityFramework.EF4 | [![NuGet](https://img.shields.io/nuget/v/OpenRiaServices.EntityFramework.EF4.svg)](https://www.nuget.org/packages/OpenRiaServices.EntityFramework.EF4) | *depreciated* use EF6 instead|
| OpenRiaServices.T4 | [![NuGet](https://img.shields.io/nuget/v/OpenRiaServices.T4.svg)](https://www.nuget.org/packages/OpenRiaServices.T4) | [![NuGet](https://img.shields.io/nuget/vpre/OpenRiaServices.T4.svg)]() |
| OpenRiaServices.Endpoints | [![NuGet](https://img.shields.io/nuget/v/OpenRiaServices.Endpoints.svg)](https://www.nuget.org/packages/OpenRiaServices.Endpoints) | [![NuGet](https://img.shields.io/nuget/vpre/OpenRiaServices.Endpoints.svg)]() |


