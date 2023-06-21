#  AspNetCore 0.4.0

* AspNetCore
     *   Copies "All" attributes to endpoint metadata      
         * Some attributes sucha as Validation, Authorization and other attributes specific to OpenRiaServices are not copied
     * `AddDomainService()` methods inside `MapOpenRiaServices` now returns `IEndpointConventionBuilder` allowing conventions to be specified per `DomainService`
        ```C#
        app.MapOpenRiaServices(builder =>
        {
            builder.AddDomainService<Cities.CityDomainService>()
                .WithGroupName("Cities");
        });
        ```
     * Updated README.md and added Sample project
     * Make it compatible with more middleware such as OutputCache middleware by "Completing" responses
     * CHANGES:
        * `services.AddOpenRiaServices<T>()` now requires T to derive from DomainServce
        * `services.AddOpenRiaServices<T>()` has different return type

# 5.3.1 with EFCore 2.0.2 and AspNetCore 0.3.0

* Code Generation
  * Switch to using Mono.Cecil to parse pdb files during code generation (#410)
    This should make it possible to use portable and embedded pdb's on the server
  * 
* AspNetCore
    * New extension method to add OpenRiaServices to services from #413 by @ehsangfl.
        ```C#
        services.AddOpenRiaServices<T>()
        ```
    * New extension method to add OpenRiaServices to pipeline from #413 by @ehsangfl.
        ```C#
        endpoints.MapOpenRiaServices(opt => opt.AddDomainService<T>())
        ```
    * Add Net7 build target to support "Finally Conventions" (`IEndpointConventionBuilder.Finally`)
    * Add `OpenRiaServices.Server.DomainOperationEntry` to endpoint metadata
        * This allows end user to easier implement additional conventions (such as Open Api or similar)
    * Copy `AuthorizationAttribute`s to endpoint metadata for queries and invokes to support [AspNetCore Authorization](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/simple?view=aspnetcore-7.0)
        *  Attributes can be set on either method or class level
    * Fixed serialization of sizes larger than 1 GB

*Other*
- Updated nuget packages

# 5.3.0 with EFCore 2.0.1

* Fix shadow property issue in EF Core DB Context extensions (#397):
    * Based on Allianz PR #393
    * Fix bug with shadowproperties being marked as modified when performing AttachAsModified in DbContextEFCoreExtensions
* DomainServiceTestHost improvements 1 (#395):
    * Allow usage of CRUD-methods ending with Async in testhost.
* DomainServiceTestHost improvements 2 (#396):
    * Add support for async IEnumerable queries in test host, i.e. querys returning `async Task<IEnumerable<` 

*Other*
- Fixed github Code QL validation of builds
- Updated nuget packages

# 5.2.0

* Client: Make construktors for Operation classes public to make them more testable (#387)
  * Add new public constructors to LoadOperation, InvokeOperation and SubmitOperation
* DomainServiceTestHost improvements (#386):
  * Add constructor which accept a single user (IPrincipal) 
  * **BREAKING CHANGE**: Constructor taking in Func<> and user does no longer accept null "user"
    * use constructor without user in order to use the "default" user
    * pass in an "unauthenticated" user to use an "unauthenticated" user
* Remove WCF Depdendency from tooling (#377)

*Other*

* Updated nuget packages used in tests (#382)

# EFCore 2.0.0

* Support for EF Core 6.0 (#385) - by @lindellhugo with help from @ehsangfls code
    * Added support for EF Core 6.0 (requires net 6.0)
    * Added support for new EF Core attributes `ValueGenerated.OnUpdate` and `ValueGenerated.OnUpdateSometimes` (only EF Core 6)


# AspNetCore 0.2.0
* Fix data corruption bug in 0.1.2 for large requests (#379)
    * Prevent problem where output buffer becomes incorrect when parsing large requests from the client on x86 systems.
* Improved handling of operation cancelled exceptions (#381) by @SandstromErik 
    * Swallow or return completed task instead of throwing OperationCanceledExceptions

# 5.1.1

* Add net 6.0 as target framework for OpenRiaServices.Server.UnitTesting (#364, #371)
* HttpDomainClient: Buffer requests så HTTP header `content-length` is set #370

# AspNetCore 0.1.1

* AspNetCore: Use route order 0 instead of 1 (#357)
* AspNetCore: Prevention for DOS attacks where attacker specify large content-length #373

# 5.1.0 

## Overview / Major features

* Support for **NET 6**
    * **AspNetCore** based hosting via `OpenRiaServices.Hosting.AspNetCore` #346
        *  See [Readme.md](src/OpenRiaServices.Hosting.AspNetCore/Framework/README.md) and [TODO.md](src/OpenRiaServices.Hosting.AspNetCore/Framework/TODO.md) for usage details and more details about what works.
    * Packages such as `OpenRiaServices.Server` and `OpenRiaServices.EntityFramework` are now compiled against net6.0 as well
* Support for **EntityFrameworkCore** via `OpenRiaServices.Server.EntityFrameworkCore`
    * Se #323 for more details (PR #351, #335)
* New `BinaryHttpDomainClientFactory` which is a `HttpClient` based transport on client (without WCF dependency) which works with **Blazor** (With sample in samples directory).
```C#
DomainContext.DomainClientFactory = new OpenRiaServices.Client.DomainClients.BinaryHttpDomainClientFactory(....)
{
    ServerBaseUri = "https://YOUR_SERVER_URI"
};
```
* Server **Dependency Injection** support
   * Allow injecting services to parameters using attribute `InjectParametersAttribute`
   * ValidationContext And AuthorizationContext will also be able to resolve services using `GetService(Type)` from the configured `IServiceProvider`
   * Add a new `IServiceProvider` setting to allow resolving services via Microsoft DI #340 when using WCF Hosting
 ```C#
// Setup DI
var serviceCollection = new ServiceCollection();
serviceCollection.AddDomainServices(ServiceLifetime.Transient);
var serviceProvider = serviceCollection.BuildServiceProvider();

// Setup OpenRiaServices to use container
var config = OpenRiaServices.Hosting.Wcf.Configuration.Internal.DomainServiceHostingConfiguration.Current;
config.ServiceProvider = serviceProvider;
``` 
  

## What's new since 5.1.0-preview5

* NEW: Package OpenRiaServices.Hosting.AspNetCore, OpenRiaServices.Server.EntityFrameworkCore
* Fix bug #348 System.IndexOutOfRangeException when domain query is executed which can happen if invoking methods concurrently on DomainClient
* *DbDomainService* now has a constructor which takes a DbContext in order to support Dependency Injection
* *DbDomainService* will no longer create a separate DbContext to resolve conflicts (if the method to create DbContext was overridden and returned a fixed instance it could lead to problems with incorrect conflict detection)

**Infreastructure**
* Allow C# 10 #354 
* Support for VS 2022
* Improve Tooling support for projects targeting multiple TargetFrameworks #352 
     * This fix ensures that sdk style implicit included code files are detected for server projects targeting multiple target frameworks.


# 5.1.0-preview5

* Make it possible to cache invoke operations #332
   * Introduce caching for invoke operations by refactoring and using the same logic as for querys
 * Allow method (parameter) injection #339
   * Allow parameters of type CancellationToken for all kinds of service methods
   * Make the generated method return `ValueTask` so that the `UnwrapTaskResult` logic from `ReflectionDomainServiceDescriptionProvider` can be removed (and better performance)
   * Allow injecting services to parameters using attribute `InjectParametersAttribute` 
 * Add a new `IServiceProvider` setting to allow resolving services via Microsoft DI `#340`

    * See example usage in Global Init/Statup below:
     ```C#
    // Setup DI
    var serviceCollection = new ServiceCollection();
    serviceCollection.AddDomainServices(ServiceLifetime.Transient);
    var serviceProvider = serviceCollection.BuildServiceProvider();

    // Setup OpenRiaServices to use container
    var config = OpenRiaServices.Hosting.Wcf.Configuration.Internal.DomainServiceHostingConfiguration.Current;
    config.ServiceProvider = serviceProvider;
    ``` 

# 5.1.0-preview4

* BinaryHttpDomainClientFactory improvements and bug fixes
   * Serialize parameters based on contact's parameterType in methods without sideeffects #322

# 5.1.0-preview3

* Fix bug in 5.1.0-preview2 where server project had wrong assembly name (#320, #321)

# 5.1.0-preview2

* BinaryHttpDomainClientFactory improvements and bug fixes
   * Serialize parameters based on contact's parameterType #318

     This fixes a number of potential issues, such as passing "non-serialisable" enumerables such as result of LINQ Select
     as method parameters
   * Properly handle nullable parameters for EntityActions #311
   * Make fault handling more tolerant to different variants of Faults #310
* Add .netstandard 2.0 build of server project #314
  This is intended to unblock development of newer code generation and hosting layers on .Net 6

New code to create BinaryHttpDomainClientFactory
```C#
DomainContext.DomainClientFactory = new OpenRiaServices.Client.DomainClients.BinaryHttpDomainClientFactory(new Uri("https://YOUR_SERVER_URI"), () => createHttpClient());
```
            
# 5.1.0-preview1

* New HttpClient based DomainClient - BinaryHttpDomainClientFactory (#290)
* **BREAKING CHANGE** Changes to wire format in order to fix serialization of DateTime (nullable and on complex objects) (#289, #75)
  * 5.1 Clients need a 5.1 Server
  * Server is backwards compatible so that it can be upgraded safly first
* **Potential BREAKING change**: Complex Objects/Typs (classes/structs which are not entities) parameters will now always be validated (#303, 
   * Previously they were only validated when calling entity actions and not Invoke methods

## BinaryHttpDomainClientFactory
Add a new cross platform (Including Blazor wasm) more easily extensible DomainClient for the binary protocol.

* Enable binary endpoint to be use on other clients than .Net Framework
* Adds support for blazor wasm where the wcf soap based client has problems
* Enable easier extensibility such as oath, client side caching, compression etc using standard HttpClient middleware (HttpMessageHandler)
* Can support HTTP2 and other more advanced networking features


```C#
HttpMessageHandler httpMessageHandler = new HttpClientHandler() { ... };
DomainContext.DomainClientFactory = new OpenRiaServices.Client.DomainClients.BinaryHttpDomainClientFactory(httpMessageHandler)
            {
                ServerBaseUri = "https://YOUR_SERVER_URI"
            };
```

Or if you want to use factory for your HttpClients you can pass in a function as callback
```C#
DomainContext.DomainClientFactory = new OpenRiaServices.Client.DomainClients.BinaryHttpDomainClientFactory(() => createHttpClient())
            {
                ServerBaseUri = "https://YOUR_SERVER_URI"
            };
```

# 5.0.1

* IMPORTANT: Fix a potential security vulnerability where entity parameters are not validated when calling invoke methods (#292)
  - The issue occurs if there are nothing else to validate so if there were validation attributes on the method, 
  any parameters or it contained any "complex objects" then validation happened as expected.
  Thanks to @ehsangfl for finding and helping to find the cause of the issue
* Allow nullable parameters to GET methods to be sent using same format as non-nullable parameters
  This enables the "HttpClient" based sample DomainClient for Xamarin etc. to work, and prepares for the upcoming official version (#290)
* Make it clear that DomainClientFactory must be set at startup (#301)
  Throws exception instead of trying to create a instance which does not work

**Update dependencies**
  * Mono.Cecil 0.11.3 -> 0.11.4
  * System.ServiceModel.Http 4.8.0 -> 4.8.1

### Infrastructure

* Target C# 9.0 when compiling

# 5.0

## Overview

1. Server is now asynronous which allows it to handles burst in load much better and generally better performance (latency/CPU/memory) under load. 
2. Client networking API the *DomainClient* is now based on Task instead of using the APM pattern with Begin/End methods
3. Supported TargetFrameworks has changed
4. Code generation now works against *netstandard 2.0* and *netcore 2.1+* clients
5. **Major namespace changes** 
	1. *DomainServices* is dropped from namespaces and assemblies
	2. *ApplicationServices* has been renamed to *Authentication*
    3. *Hosting* assembly has been moved to *Hosting.Wcf* namesapce and **moved to a separate nuget package**
5. AspNetMembership authentication (**AuthenticationBase** and related classes) are moved to a new namespace and nuget package
   * Add a reference to *OpenRIAServices.Server.Authenication.AspNetMembership* if you use it
6. Full support for IValidatableObject validation (earlier versions only validated IValidatableObject if ValidationAttribute also was specified)
   
## Upgrade instructions

1. Update both all client anor/or server nuget packages to the new version, dont't mix with v4 in the same project.
2. Search and Replace `OpenRiaServices.DomainServices` with `OpenRiaServices` in all files
3. Search and Replace `.ApplicationServices` with `.Authentication` in all files.
	* `OpenRiaServices.Client.ApplicationServices` has been renamed to `OpenRiaServices.Client.Authentication`
	* `OpenRiaServices.Server.ApplicationServices` has been renamed to `OpenRiaServices.Server.Authentication`
2. If you have been using **AuthenticationBase** or other classes in the `OpenRiaServices.Server.ApplicationServices` namespace in your server project 
   1. Add the *OpenRiaServices.Server.Authentication.AspNetMembership* nuget package to it
   2. Replace *OpenRiaServices.Server.ApplicationServices* with *OpenRiaServices.Server.Authentication*
   3. Add `using OpenRiaServices.Server.Authentication.AspNetMembership;` in file which uses *AuthenicationBase*, *UserBase* or related classes.
3. If you have compilation problems in your DomainServices because it overrides methods which do not exist 
   then try to overridde the method with the same name but with "Async" as postfix, method signatures will be different.
   Eg. replace override of *Invoke* with override of *InvokeAsync*.
4. Fix any additional compilation errors, use changes below for guidance about replacements.
5. If you are using `OpenRiaServices.EntityFramework` the framework will now only call the `SaveChangesAsync` and not `SaveChanges` so if you are overriding `SaveChanges` make sure you do the same for `SaveChangesAsync`   
6.  `OpenRiaServices.Hosting` has been moved to a separate nuget package `OpenRiaServices.Hosting.Wcf` #251
    1. You must add a reference to `OpenRiaServices.Hosting.Wcf` nuget in your web application
    2. Search and Replace `OpenRiaServices.Hosting` with `OpenRiaServices.Hosting.Wcf`
7. Update your web.config file so that it matches the [new format](NuGet/OpenRiaServices.Hosting.WCF/content/web.config.transform)
   **Failure to do this step** will result in the server not responding to client request
   **If you use packages.config for nuget packages**, this step should have been performed automatically.
   1. Search for "`DomainServicesSection`" and **change type** to "`OpenRiaServices.Hosting.Wcf.Configuration.DomainServicesSection, OpenRiaServices.Hosting.Wcf, Version=5.0.0.0, Culture=neutral, PublicKeyToken=2e0b7ccb1ae5b4c8`"
   2. Replace all other places with `OpenRiaServices.Hosting` to `OpenRiaServices.Hosting.Wcf`(if not already replaced)
   3. **If** you reference OpenRiaServices.Hosting with fully qualified assmebly name including **version number**, the version number must be updated from 4 to 5
   
8. Start with building your web project, and only build the client once the server (web project) compiles fine


For better scalability (can be done afterwards):

1. Update your Query and Invoke methods so that they use async/await where relevant.
  E.g if you are using EF6, other ORM frameworks or do network or file access.
  
## Samples

A "new" samples repository is availible at https://github.com/OpenRIAServices/Samples

It is currently quite empty but already demonstrates some of the following scenarios:
* A simple WFP app on .Net framework
   * Includes a number of queries/invokes for manually testing Task returning methods, GET/POST and exception handling 
* A WFP app on .Net Core
    * .net core and netstandard support
    * Using Asp.Net Identity for authentication
    * Writing your own custom Authentication logic
    * Running OpenRiaServices in same project as Asp.Net MVC

# 5.0.0 RC1

* "DomainServices" dropped from all namespaces, filenames as well as nugets and DLLs. #234
  * **IMPORTANT** Search and Replace `OpenRiaServices.DomainServices` with `OpenRiaServices` in all files when uprading
* Namespace `OpenRiaServices.Client.ApplicationServices` has been replaced with  `OpenRiaServices.Client.Authentication`
   **search and replace is needed on upgrading** #248
* `OpenRiaServices.Hosting` has been moved to a separate nuget `OpenRiaServices.Hosting.Wcf` #251
   * **IMPORTANT** You must add a reference to `OpenRiaServices.Hosting.Wcf` nuget in your web application
   * **IMPORTANT** Search and Replace `OpenRiaServices.Hosting` with `OpenRiaServices.Hosting.Wcf` in all files when uprading 
* Updated required version of .Net Framework to 4.7.2 (#241)
* Updated dependencies including EntityFramework to latests availible versions #240

### Client

* `IgnoreDataMemberAttribute` can now be used on the client to prevent client properties from beeing included/overwritten when loading data using `MergeIntoCurrent` or the state manipulating methods `ApplyState`, `ExtractState` etc. #249
	* `MergeAttribute` which has a similar usage area has been moved to `OpenRiaServices.Client.Internal` and might be removed in future releases.

### Code Generation

* Suppress generation of `DebuggerStepThroughAttribute` from async methods
* New handling of shared files #229
  Instead of copying all ".shared" files to the `Generated_Code` folder the server version is referenced instead
  * This should build faster builds and allows find all references, refactoring etc to work for shared files
  * It is possible to opt out of the new behaviour by adding `<OpenRiaSharedFilesMode>Copy</OpenRiaSharedFilesMode>` in the project file
  * The tooling is updated with a new option
* Load Mono.Cecil exclicitly in code generation #255 to fix issue such as #247 without having to do a workaround

### Server 

* Create, Update and Delete methods on server can now return Task #226
* The CancellationToken passed to SubmitAsync and InvokeAsync now supports cancellation on client disconnect #250
* **Security**: Don't include stack traces for errors by default #256
* Dont use HTTP status code "200 OK" for exceptions (will use 500 Internal Server Error by default) #257
  * It is free for library consumers to change the handling of returned HTTP response codes status such as using a "SilverlightFaultBehavior" or similar

### Unit Testing

* `DomainServiceTestHost` has received an number of new methods and overloads to help with unit testing async code. (#245)
	* Added async methods for DomainServiceTestHost
		* UpdateAsync
		* InsertAsync
		* InvokeAsync
		* QueryAsync
		* SubmitAsync
	* Added overloads to `Invoke` for methods returning `Task` to fix the following issues   
		* Fixed bug when TResult was a Task and returned null (could await null)
		* Fixed bug when TResult was a Task<TResult> and returned Task<TResult>

# 5.0.0 Preview 3

### Client

* Complete Load/Invoke/Submit and Authentication operations on the ´SynchronizationContext` of the caller instead of saving the SynchronizationContext (#211, #209 for submit, #216, #212)
  * This is a behavioral change which **might break** applications.
This means that operations should be started on the UI thread if any data of the DomainContext or returned Operaitons are bound to the UI before completion.
* Base SubmitChanges on SubmitChangesAsync (#209)
   * Submit operation will now cancel only if the web request is cancelled (and then *after* cancellation)
     Update cancellation behavior to be the same as for Load and Invoke (Follow up on #203 and #162)
   * Changed extension point for SubmitChangesAsync to a new method called by both SubmitChanges and SubmitChangesAsync
`protected virtual Task<SubmitResult> SubmitChangesAsync(EntityChangeSet changeSet, CancellationToken cancellationToken)`
   * Changed extension point for InvokeOperationAsync to a new protected method.
* Base DomainContext.Invoke on DomainContext.InvokeAsync (#203)
    * Invoke operation will now cancel only if the web request is cancelled (and then *after* cancellation)
* Ensure Completed event is always called when Load/Invoke/SubmitOperation finishes (#206)
* AuthenticationService is rewritten to use TPM (`Task` based methods) instead of `APM` for the methods implementing the actual authentication operations (#212, #214, #216)
* Pass in endpoint name in WcfDomainClientFactory to make it easier to derive from it (#218)
* Hosting - new "PubInternal" types
  * behaviors for easy creation endpoints based on standard wcf (non REST) transports

**Bugfix**
* Handle early cancellation (Cancellation before actual request has been sent) in WebDomainClient (#210)
  * In earlier previews an exception was thrown instead of the operation beeing Cancelled
  
### Server

* Hosting - Endpoint changes (#218)
   * Reuse the same `ContractDescription` for multiple endpoints (endpoint name is not longer added)
   * Dont add SilverlightFaultBehaviour by default to DomainServices
   * new "PubInternal" types
     * behaviors for easy creation endpoints based on standard wcf (non REST) transports
* Trigger cancellation on client disconnect (#222)

### Server

* EntityFramework: Target IDbSet instead of DbSet with AttachAsModified extension methods (#215) 

### Infrastructure

* Improved test execution times by reducing waiting delays (parts in #212. 213)

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
3. * If you are using `OpenRiaServices.EntityFramework` the framework will now only call the `SaveChangesAsync` and not `SaveChanges` so if you are overriding `SaveChanges` make sure you do the same for `SaveChangesAsync`

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
