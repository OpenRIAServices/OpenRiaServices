# 5.0 (Unrealeased)

## Overview

1. Server is now asynronous which allows it to handles burst in load much better and generally better performance (latency/CPU/memory) under load. 
2. Client networking API the *DomainClient* is now based on Task instead of using the APM pattern with Begin/End methods
3. Supported TargetFrameworks has changed
4. AspNetMembership authentication (**AuthenticationBase** class) is moved to a new namespace and nuget package
  * Add a reference to **OpenRIAServices.Server.Authenication.AspNetMembership* if you use it

## Client

Most of the changes are **Brekaing changes**, even if many of them will only require changes in a small percentage of applicaitons.

1  Change DomainClient contract to Task based async methods (#153)
    * Performing multiple loads and waiting for the result is now faster
	* Any custom DomainClient or code which interact with DomainClient will no longer compile.	
2. Remove old target frameworks
* Remove netstandard13 (#160)
* Remove portable class library TargetFramework (#164)
* Remove Silverlight (#174, #175 ..)
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
8. Change from IEnumerable to IReadOnlyCollection in a few places (#183)
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
