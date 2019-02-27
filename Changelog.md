# Unreleased (since 4.5.4)

## Client

1. Add netstandard 2.0 target
2. Support query translation for builtin VB operators in projects compiled using VS 2017
3. Support using "soap" endpoint from client (Cross Platform)
  * native support for all platforms supporting netstandard 2
  * Run this code before creating first DomainContext, 
  ``` csharp
  DomainContext.DomainClientFactory = new OpenRiaServices.DomainServices.Client.Web.SoapDomainClientFactory()
{
    ServerBaseUri = "https://my.site.com/",
};
```
  * Limitations / differences from binary endpoint:
    * Standard query size limitations apply (no workaround as for binary endpoint)
    * invoke operations will use POST even when for operations with HasSideEffect=false
    * soap endpoint must be manually enabled on server

## Server

## Code generation


## Other
 
1. Use VS 2017 for compilation
2. Replaced multiple projects with using built in multitargeting with SDK style projects
1. All assemblies are strong named ("signed") (#145)
  * The "signed" version of all nuget packages will be depreciated since it is only to replace them with the "normal" nuget packages
  * This will significantly make maintainance easier as well as make it easier to "get started" since there are less variations


## Tooling

# 4.5.4 and earlier

See: https://github.com/OpenRIAServices/OpenRiaServices/releases for release notes
