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
