
## Usability 

* Add readme / release notes
* add page in documentation
* Go through public API
* Improve "setup"/Map code (Framework....cs) for 
   * registering DomainServices in DI 
   * mapping domainservices
   * add autodiscovering
   * rename types and change signatures


Maybe something similar to 

```csharp
services.AddOpenRiaServices/AddDomainServices(x => 

    // Handles services.SampleDomainService<IHttpContextAccessor>()
    // and optionally keeps a list of registered domainservices for easy mapping of all'
    // Maybe an optional ServiceLifetime parameter to allow scoped registration
    x.AddDomainService<SampleDomainService>();

    // Add domainservices in batch, or by scannin a whole assembly
    x.AddDomainServices(type[], lifetime = Transaction)
    x.AddDomainServices(assembly[], lifetime = Transaction)

    // Allow configuring option and enpoints in the future
    x.WithBinaryEndpoint(y => 
        // y is BinaryEndpointOptions or BinaryEndpointBuilder
        y.ReaderQuota = .. 
    );
});


...

// throws exception if services has not ben added by AddOpenRiaServices ..
app.MapDomainServices/MapOpenRiaServices("/Services", x => 
{
    // As today
    x.MapDomainService<SampleDomainService>();

    // Maybe allow mapping all registered from AddOpenRiaServices call ?
    x.MapAllDomainService();

    // Setup naming or endpoints here or in AddOpenRiaServices 
    x.WithBinaryEndpoint(y => 
        y.WithReaderQuota = .. 
    );
});
```

## Features 
* Add https checked based on "RequresSecureEndpoint"
    * Make sure test "InvokingHttpsServiceOverHttpFails" starts running
    * Can add a check in CreateDomainService method based on httpContext.IsHttps
    * Save boolean in private field on "operation" (don't look at attribute on each invoke)

* Add caching support or obsolete OutputCache attribute ?
    * OutputCache could be moved back to Wcf Hosting assembly
    * If support is added should it integrate with OutputCaching middleware ?
    * If supported with duration the cache string can be computed at startup and stored in the "OperationInvoker" class

* Look at adding a "global" "OnError" functionality which can be configured during setup
  * It should propably work similar to DomainService.OnError 
  * It should work for errors from constructors as well 
  * The same callback should work for multiple DomainServices

* Add logging support for exceptions returned

* For RequiresAuthentication attribute we should be able to 
  validate authentication early in the pipeline (via metadata?) 
  so we don't need to check in ValidateMethodPermissions.
  *  Ideally the DomainService should not even be creat


## Reliability / "production" ready

* Setting (option) to show / hide stack traces
   - Currently IHostEnvironment.EnvironmentName is used and it is for development

* Better handling of invalid request (invalid format or problem with queries)
   - should probably be http 400
   - avoid exceptions ? (can return null, or specific object[])

* Handle exceptions from when creating DomainServices
   - Se test TestProviderConstructorThrows which is currently disabled for ASPNETCORE
   - For WCF hosting these errors are handled and a proper binary response with error details are returned

* Allow setting XmlDictionaryWriter quotas for Read/write


* determine if settings for max length etc (timings) for DOS protection is needed (or if it should be set on kestrel etc)

### Perf:

* Pool more resources or use stackalloc
   * object[] for parameters

### Extensibility / refactoring:

* Move serialization code await from OperationInvoker
- considera a setup with a shared "SerializationFormat" class which creates "Serializers/formatters" based on method signature
 This is somewhat similar to serializationhelper, but it would return a custom class instead of DataContractSerializer.
 The approach should be extensible so it is easy to implement another protocol thant the "binary"

     The shared "SerializationFormat" would have 
     - "ContentType"
     - methods to create "serialiser"
     The "per opertion" serialiser class would have methods 
     - for serialising results similar to "WriterResult", "WriteError" (both with exception and validationerros)
     - reading parameters "ReadParameters", "ReadQueryParameters", "ReadSubmitParameter?" ? (single method ? returning valuetask ?)


