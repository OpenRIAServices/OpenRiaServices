
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

```
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

* look at adding authorization and authentication metadata to endpoiunt
 - this is handled inside DomainServer at the moment
* look at copying/adding attributes applied from query/invoke methods to endpoints

## Reliability / "production" ready


* Better handling of invalid request
   - should probably be http 400
   - avoid exceptions ? (can return null, or specific object[])
   - Ensure better controls 


* Allow setting XmlDictionaryWriter quotas for Read/write
* --max size for requests-- ? (maybe just rely on kestrel)

* Setup test infrastructure
- client tests needs to be run agaisnt aspnet core host 
  * but not all tests can run since Linq To Sql is not usable
  * Either 

* determine if settings for max length etc (timings) for DOS protection is needed (or if it should be set on kestrel etc)

### Perf:

* Reuse XmlDictionaryReader/XmlDictionaryWriter and stream as WCF version does
   * Port BufferManagerStream to use ArrayPool (keep max buffer size limited by ArrayPool.Shared's max size)
* Pool more resources
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


