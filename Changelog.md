# 5.0 (In Preview)

## Overview

1. Server is now asynronous which allows it to handles burst in load much better and generally better performance (latency/CPU/memory) under load. 
2. Client networking API the *DomainClient* is now based on Task instead of using the APM pattern with Begin/End methods
3. Supported TargetFrameworks has changed
4. Code generation now works against *netstandard 2.0* and *netcore 2.1+* clients
5. AspNetMembership authentication (**AuthenticationBase** class) is moved to a new namespace and nuget package
   * Add a reference to **OpenRIAServices.Server.Authenication.AspNetMembership* if you use it

## Upgrade instructions

1. Update both all client anor/or server nuget packages to the new version, dont't mix with v4 in the same project.
2. If you have been using **AuthenticationBase** class in your server project 
   1. add the *OpenRiaServices.DomainServices.Server.Authentication.AspNetMembership* nuget package to it
   2. Replace *penRiaServices.DomainServices.Server.ApplicationServices* with *penRiaServices.DomainServices.Server.Authentication*
   3. Add using for *penRiaServices.DomainServices.Server.Authentication.AspNetMembership* in file which uses *AuthenicationBase*
3. If you have compilation problems in your DomainServices because it overrides methods which do not exist 
   then try to overridde the method with the same name but with "Async" as postfix, method signature will be different.
   Eg. replace override of *Invoke* with override of *InvokeAsync*.
4. Fix any additional compilation errors, use changes below for guidance about replacements.
   
   
For better scalability (can be done afterwards):

1. Update your Query and Invoke methods so that they use async/await where relevant.
  E.g if you are using EF6, other ORM frameworks or do network or file access.   

# 5.0.0 Preview 2

This is mostly a performance and feature update.
It does not contain any new breaking changes since preiew 1

### Client

* Cache *ChannelFactories* for large performance improvements (#184)

### Code generation

* Code generation now works for *netstandard* and *netcoreapp* (#199, #201)
* Fix error messages about incompatible Target frameworks when TargetFramework differs between client and server. (#188)

### Server

* *Significant* performance and memory usage improvemens for serialization (#189)
* Improve Perf for Query and Invoke where validation is not required (#186)
* Reduce per request allocations (#197)
* Small improvements in Task To APM wrapper for async completing operations
* Fix potential race condition when configuring endpoint authentication. (#196)

### Other

* Readme updates

# 5.0.0 Preview 1

## Client

Most of the changes are **Brekaing changes**, even if many of them will only require changes in a small percentage of applications.

1  Change DomainClient contract to Task based async methods (#153)
    * Performing multiple loads and waiting for the result is now faster
	* Any custom DomainClient or code which interact with DomainClient will no longer compile.	
2. Remove old target frameworks
  * Remove netstandard13 (#160)
  * Remove portable class library TargetFramework (#164)
  * Remove Silverlight (#174, #175 and more commits)
  * .Net Framework 4.5 requirement is replaced by 4.6 (will be 4.6.1+)
3. Move *EntityList* and *QueryBuilder* from OpenRiaServices...Data namespace to OpenRiaServices...Client namespace (#182)
4. Dont allocate PropertyChangedEventArgs if not needed (#155)   
    * remove sevaral *OnPropertyChanged* methods and only keep RaisePropertyChanged
	   * If your code does not compile override RaisePropertyChanged instead
	* Memory usage during Load etc is much lighter
5. Make DomainClientResult internal so it can be removed in the future
6. Have `EntityContainer.LoadEntities` return `IEnumerable<Entity>` instead of `IEnumerable`
7. Make WebDomainClient non sealed (#166) *non breaking*
   Make CallServiceOperation virtual so that the invoke behaviour can be modified in derived classes.
   This should simplify adding bearer based authentication
8. Cache WCF ChannelFactories per DomainContext type for improved perf (#184)
  * This significantly improves the performance for the first network operation per DominContext insteance.
    The cost is only paid on the first access per DomainService *type* (not per instance).
	Performance of overhead for E2E benchmark of loading some simple entity from a simple DomainService went from 200% to <10% 
	(so the speedup for creating a new instance of this specific DomainContext is above 20 times) and will improve with more complex services.
 9. Change from IEnumerable to IReadOnlyCollection in a few places (#183)
  * Mostly *ValidationErrors* properties and for IEntityCollection

*Behaviour changes*
1. Base DomainContext.Load on DomainContext.LoadAsync instead of other way around
* The generic `Load<TEntity>` can be overridden but it will only be called when any of the "Load" methods are called
2. `DomainContext.IsLoading` is no longer set to false directly on cancellation.
       Instead a load is only considered done until after it has been cancelled (or completed)


## Server

1. DomainServices are now async #159 and many methods have been renamed with Async suffic
1. Move aspnet authentication to separate namespace, assembly and nuget package (#173)
Move Authenication related code from ..Server.ApplicationServices to
* ..Server.Authentication
* and ..Server.Authentication.AspNetMembership (AuthenticationBase, UserBase ..)

## Other

* Fixed a number of flaky tests (#161, #172,  .. and more commits)
* use VS2019 for azure  pipelines (#148)
* Have client test start webserver if not already running (#169)
* Changed folder structure by placing code in src folder (#176)
* net45 dependency replaced with net46 dependency
  * With slightly better less allocations as a result
* Removed code market as obsolete (#170)
* Some modernisation of codebase via refactoring via code analyzers


# 4.6.2

## Client

	
1. Add transport (OpenRiaServices.DomainServices.Client.Web) to netstandard 2.0 nuget.
  This provides the SoapDomainClientFactory for netstandard / netcoreapp assemblies.
   
# 4.6.1

## Client

1. Add new interface *IEntityCollection<TEntity>* (#156)
  This is a combination of ICollection<>, INotifyPropertyChanged, INotifyCollectionChanged as well as typed events for Add/Remove of entitites.
  It helps treating collections of entities EntityCollection and EntitySet in a more uniform way.
  It should help OpenRiaServices.M2M integrate better with the core pacakges.
2. *LoadOperation* performance improvements (Issue #154)
 * Raise Reset action instead of multiple Add notfications for **significantly imroved IU responsiveness** when binding to the 
*Entitites* or *AllEntities* properties before the load operation is completed.
    * The UI now only updates once instead of potentially once per item
  * Don't allocate multiple *NotifyCollectionChangedEventArgs* and *PropertyChangedEventArgs* for each entity loaded.
  * Don't allocate event args if no one is listening
3. *EntityCollection* performance improvements (Issue #154)
   * Don't allocate *NotifyCollectionChangedEventArgs* on each change if no one is listening
   

# 4.6.0

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

* Fix namespace for ObjectContextExtensions for OpenRiaServices.EntityFramework (breaking change)

## Code generation


## Other
 
1. Use VS 2017 for compilation
2. Replaced multiple projects with using built in multitargeting with SDK style projects
1. All assemblies are strong named ("signed") (#145)
  * The "signed" version of all nuget packages will be depreciated since it is only to replace them with the "normal" nuget packages
  * This will significantly make maintainance easier as well as make it easier to "get started" since there are less variations
4. Add VS Extension projects to main solution
* Fix tests for VS Extension project
 * Remove "Interfaces" assembly to reduce complextity and fix some problems

## Tooling

* *New Visual Studio extension* https://marketplace.visualstudio.com/items?itemName=OpenRiaServices.OpenRiaServicesTooling release in preview
  * Updates "manage openriaservices link" dialog with more settings
  * Update add new item for domainservice item tempate so it generates EF6 compatible code
   * Add VS2017 + VS2019 support
   * Bugfix when invoking tooling with unloaded projects

# 4.5.4 and earlier

See: https://github.com/OpenRIAServices/OpenRiaServices/releases for release notes
