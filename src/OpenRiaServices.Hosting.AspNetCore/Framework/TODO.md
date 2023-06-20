﻿
## Usability 

* Add readme / release notes
* add page in documentation
* Improve "setup"/Map code (Framework....cs) for 
   * registering DomainServices in DI 
   * mapping domainservices
   * add autodiscovering
   * rename types and change signatures
* Go through public API


Maybe something similar to 

```csharp
services.AddOpenRiaServices/AddDomainServices(x => 
{
    x.AddDomainServices(.... type[]), lifetime;
    x.Scan/DiscoverDomainServices(.... assembly[])

    x.WithBinaryEndpoint(y => 
        y.WithReaderQuota = .. 
    );
});


...

// throws exception if services has not ben added by AddOpenRiaServices ..
app.MapDomainServices/MapOpenRiaServices("/Services", x => 
{
    ??? if mapping of individual domainservicces, check that they can be resolved

    x.WithBinaryEndpoint(y => 
        y.WithReaderQuota = .. 
    );
});
```

## Features 
* Add https checked based on "RequresSecureEndpoint"
* Add caching support - based on WCF implementation's OutputCache
* Add logging support for exceptions returned

* look at adding authorization and authentication metadata to endpoiunt
 - this is handled inside DomainServer at the moment
 - For RequiresAuthentication attribute we should be able to 
   validate authentication early in the pipeline (via metadata?) 
   so we don't need to check in ValidateMethodPermissions


## Reliability / "production" ready

* Setting to show / hide stack traces
   - A good defatul might be to look at IHostEnvironment.EnvironmentName and enable it for development

* Better handling of invalid request (invalid format)
   - should probably be http 400
   - avoid exceptions ? (can return null, or specific object[])
   - Ensure more/better checks in order to validate format received

* Allow setting XmlDictionaryWriter quotas for Read/write
* --max size for requests-- ? (maybe just rely on kestrel)

* Setup test infrastructure
- client tests needs to be run agaisnt aspnet core host 
  * but not all tests can run since Linq To Sql is not usable
  * Either 

* determine if settings for max length etc (timings) for DOS protection is needed (or if it should be set on kestrel etc)

### Perf:

* Pool more resources or use stackalloc
   * object[] for parameters

Extensibility / refactoring:

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


