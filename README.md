# OpenRiaServiecs

[![Build status](https://img.shields.io/appveyor/ci/OpenRiaServices/openriaservices/master.svg)](https://ci.appveyor.com/project/OpenRiaServices/OpenRiaServices/branch/master)
[![Tests status](https://img.shields.io/appveyor/tests/OpenRiaServices/openriaservices/master.svg)](https://ci.appveyor.com/project/OpenRiaServices/OpenRiaServices/branch/master)

<!-- Below badges should be reenabled once new scripts for appveyor build is set up
[![Coverity Scan Build Status](https://scan.coverity.com/projects/8802/badge.svg)](https://scan.coverity.com/projects/daniel-svensson-openriaservices)
[![Sonarqube - technical debpt](https://img.shields.io/sonar/https/sonarqube.com/OpenRiaServices/tech_debt.svg)](https://sonarqube.com/dashboard/index?id=OpenRiaServices)
-->

The Open RIA Services project continues what was previously known as WCF RIA Services.

The source code and issue list is currently kept at github (https://github.com/OpenRiaServices/OpenRiaServices).

Documentation:
* The original documentation for WCF RIA Services is still relevant and can be found at https://msdn.microsoft.com/en-us/library/ee707344(v=vs.91).aspx . Namespaces and assembly names are no longer correct since they changed with the release of OpenRiaServices.
* Documentation for changes since WCF RIA Services can be found under https://openriaservices.codeplex.com/documentation 

Contribution Guidelines can be found at https://github.com/OpenRIAServices/OpenRiaServices/wiki/Contribution-Guidelines



# Nuget packages

Here are the most common nuget packages and their current versions.

### Unsigned nuget packages

|Package | Stable | Prerelease |
|------- | ------ | ---------- |
| OpenRiaServices.Client | [![NuGet](https://img.shields.io/nuget/v/OpenRiaServices.Client.svg)](https://www.nuget.org/packages/OpenRiaServices.Client) | [![NuGet](https://img.shields.io/nuget/vpre/OpenRiaServices.Client.svg)]() |
| OpenRiaServices.Client.Core | [![NuGet](https://img.shields.io/nuget/v/OpenRiaServices.Client.Core.svg)](https://www.nuget.org/packages/OpenRiaServices.Client.Core) | [![NuGet](https://img.shields.io/nuget/vpre/OpenRiaServices.Client.Core.svg)]() |
| OpenRiaServices.Client.CodeGen | [![NuGet](https://img.shields.io/nuget/v/OpenRiaServices.Client.CodeGen.svg)](https://www.nuget.org/packages/OpenRiaServices.Client.CodeGen) | [![NuGet](https://img.shields.io/nuget/vpre/OpenRiaServices.Client.CodeGen.svg)]() |
| OpenRiaServices.Server | [![NuGet](https://img.shields.io/nuget/v/OpenRiaServices.Server.svg)](https://www.nuget.org/packages/OpenRiaServices.Server) | [![NuGet](https://img.shields.io/nuget/vpre/OpenRiaServices.Server.svg)]() |
| OpenRiaServices.EntityFramework | [![NuGet](https://img.shields.io/nuget/v/OpenRiaServices.EntityFramework.svg)](https://www.nuget.org/packages/OpenRiaServices.EntityFramework) | [![NuGet](https://img.shields.io/nuget/vpre/OpenRiaServices.EntityFramework.svg)]() |
| OpenRiaServices.EntityFramework.EF4 | [![NuGet](https://img.shields.io/nuget/v/OpenRiaServices.EntityFramework.EF4.svg)](https://www.nuget.org/packages/OpenRiaServices.EntityFramework.EF4) | [![NuGet](https://img.shields.io/nuget/vpre/OpenRiaServices.EntityFramework.EF4.svg)]() |
| OpenRiaServices.T4 | [![NuGet](https://img.shields.io/nuget/v/OpenRiaServices.T4.svg)](https://www.nuget.org/packages/OpenRiaServices.T4) | [![NuGet](https://img.shields.io/nuget/vpre/OpenRiaServices.T4.svg)]() |
| OpenRiaServices.Endpoints | [![NuGet](https://img.shields.io/nuget/v/OpenRiaServices.Endpoints.svg)](https://www.nuget.org/packages/OpenRiaServices.Endpoints) | [![NuGet](https://img.shields.io/nuget/vpre/OpenRiaServices.Endpoints.svg)]() |


### Signed nuget packages

|Package | Stable | Prerelease |
|------- | ------ | ---------- |
| OpenRiaServices.Signed.Client | [![NuGet](https://img.shields.io/nuget/v/OpenRiaServices.Signed.Client.svg)](https://www.nuget.org/packages/OpenRiaServices.Signed.Client) | [![NuGet](https://img.shields.io/nuget/vpre/OpenRiaServices.Signed.Client.svg)]() |
| OpenRiaServices.Signed.Client.Core | [![NuGet](https://img.shields.io/nuget/v/OpenRiaServices.Signed.Client.Core.svg)](https://www.nuget.org/packages/OpenRiaServices.Signed.Client.Core) | [![NuGet](https://img.shields.io/nuget/vpre/OpenRiaServices.Signed.Client.Core.svg)]() |
| OpenRiaServices.Signed.Client.CodeGen | [![NuGet](https://img.shields.io/nuget/v/OpenRiaServices.Signed.Client.CodeGen.svg)](https://www.nuget.org/packages/OpenRiaServices.Signed.Client.CodeGen) | [![NuGet](https://img.shields.io/nuget/vpre/OpenRiaServices.Signed.Client.CodeGen.svg)]() |
| OpenRiaServices.Signed.Server | [![NuGet](https://img.shields.io/nuget/v/OpenRiaServices.Signed.Server.svg)](https://www.nuget.org/packages/OpenRiaServices.Signed.Server) | [![NuGet](https://img.shields.io/nuget/vpre/OpenRiaServices.Signed.Server.svg)]() |
| OpenRiaServices.Signed.EntityFramework | [![NuGet](https://img.shields.io/nuget/v/OpenRiaServices.Signed.EntityFramework.svg)](https://www.nuget.org/packages/OpenRiaServices.Signed.EntityFramework) | [![NuGet](https://img.shields.io/nuget/vpre/OpenRiaServices.Signed.EntityFramework.svg)]() |
| OpenRiaServices.Signed.EntityFramework.EF4 | [![NuGet](https://img.shields.io/nuget/v/OpenRiaServices.Signed.EntityFramework.EF4.svg)](https://www.nuget.org/packages/OpenRiaServices.Signed.EntityFramework.EF4) | [![NuGet](https://img.shields.io/nuget/vpre/OpenRiaServices.Signed.EntityFramework.EF4.svg)]() |
| OpenRiaServices.Signed.T4 | [![NuGet](https://img.shields.io/nuget/v/OpenRiaServices.Signed.T4.svg)](https://www.nuget.org/packages/OpenRiaServices.Signed.T4) | [![NuGet](https://img.shields.io/nuget/vpre/OpenRiaServices.Signed.T4.svg)]() |
| OpenRiaServices.Signed.Endpoints | [![NuGet](https://img.shields.io/nuget/v/OpenRiaServices.Signed.Endpoints.svg)](https://www.nuget.org/packages/OpenRiaServices.Signed.Endpoints) | [![NuGet](https://img.shields.io/nuget/vpre/OpenRiaServices.Signed.Endpoints.svg)]() |

