[![Stand With Ukraine](https://raw.githubusercontent.com/vshymanskyy/StandWithUkraine/main/banner2-direct.svg)](https://vshymanskyy.github.io/StandWithUkraine)

# ⚡ **Blazing-fast** ASP.NET Core hosting for OpenRiaServices

This software allows existing applications written for OpenRiaServices or WCF RIA Services to run on .NET with Kestrel.

**Hopefully it will allow you as a consumer to make large savings in development time**, weeks or even man years,
by not having to rewrite your application as well as allowing rapid development. <br/>
The software is provided free of charge, but **I urge you to use some of the money saved by using it [to support Ukraine](https://stand-with-ukraine.pp.ua/)**.

## Terms of Use

By using this project or its source code for any purpose, you agree to all of the following:

- You **condemn Russia and its military aggression against Ukraine**
- You **recognize that Russia is an occupant that unlawfully invaded a sovereign state**
- You **support Ukraine's territorial integrity, including its claims over temporarily occupied territories of Crimea and Donbas**
- You **do not support the Russian invasion or contribute to its propaganda'**

This excludes usage by the Russian state, Russian state-owned companies, Russian education who spread propaganda instead of truth, anyone who work with the *filtration camps*, or finance the war by importing Russian oil or gas.

- You allow anonymized telemetry to be collected and sent during preview releases to gather feedback about usage.


## Getting Started

1. Create a new .NET web application, for example, by running `dotnet new web`.
2. Add the `OpenRiaServices.Hosting.AspNetCore` package:

   ```sh
   dotnet add package OpenRiaServices.Hosting.AspNetCore
   ```
3. Add a reference to `OpenRiaServices.Server`.

4. Add one or more domain services:

```csharp
[EnableClientAccess]
public class CityDomainService : DomainService
{
    /* .....  */
}
```
For more information, see the [OpenRiaServices documentation](https://openriaservices.gitbook.io/openriaservices/ee707348/ee707373) and the [domain service sample](https://github.com/OpenRIAServices/OpenRiaServices/blob/086ea8c8fcb115000749be6b2b01cd43bb95bf80/docs/gg602754.md#add-the-poco-class).

5. Set up hosting integration.

Sample program:

```csharp
using OpenRiaServices.Hosting.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenRiaServices();

// Register DomainServices in DI
builder.Services.AddDomainService<CityDomainService>();
// OR builder.Services.AddDomainServices(AppDomain.CurrentDomain.GetAssemblies());


var app = builder.Build();

// Map OpenRiaServices routes, optionally with a prefix such as "/Services".
// This maps all DomainServices registered in builder.Services, using routes
// similar to $"{DomainServiceName}/{MethodName}".
app.MapOpenRiaServices();
// Alternatively, specify the DomainServices to map explicitly. This works better with trimming:
// app.MapOpenRiaServices(builder => { builder.AddDomainService<CityDomainService>(); });

app.Run();
```

## Advanced

### Configuring Hosting Options

You can configure the hosting options by passing a callback to the `AddOpenRiaServices` method.

Options include:

- `ExceptionHandler` - A delegate that can be used to handle exceptions that occur during the execution of a DomainService method.
    - It lets you customize the error message and error code sent to the client, as well as the HTTP status code.
- `IncludeExceptionMessageInErrors` - A boolean that determines if the exception message should be included in the error response.
   - **WARNING**: Raw exception messages can disclose sensitive data. Keep this `false` in production.
     If you use `ExceptionHandler`, return only messages that are safe for clients and log the full exception on the server.
- `IncludeExceptionStackTraceInErrors` - A boolean that determines if the exception stack trace should be included in the error response. 
   - **Warning:** This is INSECURE and *NOT* recommended for production because stack traces can reveal detailed information about your system.
- `QueryLimits` - allows configuring resource limits for parsing queries passed from the client. Se section "Client query resource limits" for details.

Production-safe error configuration (both disclosure options default to `false`):
```csharp
builder.Services.AddOpenRiaServices(o => {
    o.ExceptionHandler = (context, response) =>
    {
        response.ErrorMessage = "The request could not be completed.";
    };
 
    o.IncludeExceptionMessageInErrors = false;
    o.IncludeExceptionStackTraceInErrors = false;
});
```

Only enable exception detail or stack traces in a trusted development environment.
`DomainException` messages are intended for clients, so they must not contain sensitive details either.

### Client query resource limits

| Property | Default | Purpose |
| --- | --- | --- |
| `MaxExpressionDepth` | 200 | Limits parser recursion and expression-tree depth. Stack safety checks may reject shallower expressions. |
| `MaxNumericFormatPrecision` | 100 | Limits the precision in standard numeric formats such as `D10` and `F2`. Zero is allowed. |
| `MaxNumericFormatLength` | 64 | Limits the number of characters in standard and custom numeric format strings. Must be positive. |

For example, a host can tighten these limits:

```csharp
using OpenRiaServices.Hosting;

builder.Services.AddOpenRiaServices(options =>
{
    options.QueryLimits = new QueryLimits
    {
        MaxExpressionDepth = 100,
        MaxNumericFormatPrecision = 28,
        MaxNumericFormatLength = 10
    };
});
```

Numeric `ToString(string)` formats in filters and ordering must be constants.
Ordinary formats such as `"D10"`, `"F2"` and `"0.00"` remain supported within the limits;
formats read from entity properties or computed by other expressions are rejected, even if their result would be short.
Parameterless  `ToString()` calls are unchanged.
Invalid query expressions return HTTP 400 before the query method executes.

**Be careful with raising these limits just to accept untrusted queries.** Larger numeric precision or custom formats
permit larger allocations during execution; greater depth increases stack risk.
These are per-expression/per-format safeguards, not a total memory, row-count or execution-time budget.

### Client query member visibility

Client query filters and ordering can only access entity and complex-type members exposed by client serialization metadata. Use serializable properties for values that clients need to query; public fields and custom indexers are not exposed by this property-based metadata, even when a field has `[DataMember]`.

The existing property visibility rules, including `[Exclude]`, `[DataContract]`/`[DataMember]`, and `[IgnoreDataMember]`, also apply when a property is inherited and its metadata is registered on the derived entity. Rejected members produce the same query error as unknown members, without executing custom indexer getters.

Standard string, array, list, and dictionary indexing and predefined static constants remain supported. These checks are shared with WCF hosting.

### Supporting different serialization formats (Text Xml)

The default serialization format is binary XML, which is compatible with WCF RIA Services.
You can remove the built-in serialization providers by calling `ClearSerializationProviders`.

* Version 1.6.0 added support for high performance MessagePack serialization.
* Version 1.4.0 added text XML serialization

The server selects the serialization format based on the request content type and the configured providers.

### Supporting MessagePack wire format

MessagePack can be enabled as an additional format using MIME type `application/vnd.msgpack`. <br />
Just add a call to `AddMessagePackSerialization` when configuring OpenRiaServices:

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenRiaServices()
    .AddMessagePackSerialization();
```

#### Text XML serialization

XML serialization was added in version 1.4.0 as an alternative to binary serialization (#546).

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenRiaServices()
    .AddXmlSerialization();
```

#### Configuring XML serializer security quotas

**The default XML settings are compatibility defaults, not production resource limits.**
Both binary XML and text XML use `XmlDictionaryReaderQuotas.Max` unless configured.
Set finite quotas for **every enabled XML provider** before accepting untrusted requests.
Enabling MessagePack or text XML does not remove the default binary XML provider.
Use `ClearSerializationProviders` if you intend to disable formats rather than secure all of them.

Reader quotas help limit individual strings, arrays, nesting and XML names.
They do **not** cap the complete deserialized object graph, total allocation or total request size.
Small binary XML requests can expand during deserialization. The initial read buffer size is not a request-body limit.
The examples below are starting points only: adjust them to your service contracts and largest legitimate requests.
`MaxStringContentLength` counts characters; `MaxArrayLength` counts array elements, not bytes.

**Configure binary XML serialization quotas (for the default binary provider):**

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenRiaServices()
    .ConfigureBinarySerialization(options =>
    {
        options.ReaderQuotas = new System.Xml.XmlDictionaryReaderQuotas
        {
            MaxStringContentLength = 1024 * 1024,
            MaxArrayLength = 65536,
            MaxDepth = 32,
            MaxBytesPerRead = 4096,
            MaxNameTableCharCount = 16384,
        };
    });
```

**Configure Text XML serialization quotas:**

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenRiaServices()
    .AddXmlSerialization(options =>
    {
        options.ReaderQuotas = new System.Xml.XmlDictionaryReaderQuotas
        {
            MaxStringContentLength = 1024 * 1024,
            MaxArrayLength = 65536,
            MaxDepth = 32,
            MaxBytesPerRead = 4096,
            MaxNameTableCharCount = 16384,
        };
    });
```

Apply a separate HTTP request-body limit in Kestrel and in any IIS or reverse-proxy configuration.
For example, this Kestrel limit allows at most 4 MiB per request body:

```csharp
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 4 * 1024 * 1024;
});
```

If request decompression is enabled, also bound the decompressed input.
Body-size limits and reader quotas complement each other; neither is a total deserialization memory budget.
Keep rate/concurrency limits and timeouts appropriate for the maximum permitted request.

For services that require authentication, configure ASP.NET Core authentication/authorization middleware
and endpoint authorization (for example, `.RequireAuthorization()` on the relevant service endpoints).
This can reject unauthorized requests **before body deserialization**.
Domain-service attributes such as `[RequiresAuthentication]` remain important, but operation authorization runs
after body deserialization and therefore does not protect that earlier work.
Query parsing occurs after operation authorization; its `QueryLimits` do not limit XML deserialization.

On HTTP clients, also configure `BinaryHttpDomainClientFactory.ReaderQuotas` and, when used,
`XmlHttpDomainClientFactory.ReaderQuotas` before creating clients. Their defaults are also maximum quotas.
Server-side quotas do not protect a client reading a response.


### Performance tuning

Kestrel limits can be tuned to improve performance for a particular application scenario.
For HTTP/2 applications, the flow-control window sizes may be worth adjusting when transferring large responses.
See the [ASP.NET Core gRPC performance guidance on flow control](https://learn.microsoft.com/aspnet/core/grpc/performance#flow-control) for details;
the same HTTP/2 flow-control concepts apply to OpenRiaServices RPC calls.

For example, Kestrel's HTTP/2 stream and connection window sizes can be configured as follows:

```csharp
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.Http2.InitialStreamWindowSize = 2 * 1024 * 1024;
    options.Limits.Http2.InitialConnectionWindowSize = 4 * 1024 * 1024;
});
```

Measure with representative workloads before changing limits, since the best values depend on the application, latency and available resources.

#### MessagePack client responses

The MessagePack client's `ResponsePipeReaderOptions` can be configured to adjust buffering.
For large responses, increasing the buffer size may improve performance.
See the [Changelog](https://github.com/OpenRIAServices/OpenRiaServices/blob/main/Changelog.md#client-openriaservicesclientdomainclientshttp) for configuration details and an example.

#### Known limitations

The following types are **not** supported by the default serializer. To use them, implement custom `MessagePackConverter<T>` converters and register them in `MessagePackSerializationOptions.Serializer.Converters`:

- **`System.Xml.Linq.XElement`** — See the [XElementConverter sample](https://github.com/OpenRIAServices/OpenRiaServices/blob/main/src/Test/AspNetCoreWebsite/MessagePack/XElementConverter.cs) for a reference implementation that serializes the element as a plain XML string.
- **`System.Data.Linq.Binary`** — See the [BinaryConverter sample](https://github.com/OpenRIAServices/OpenRiaServices/blob/main/src/Test/AspNetCoreWebsite/MessagePack/BinaryConverter.cs) for a reference implementation.

Register the converters by passing a configuration callback:

```csharp
builder.Services.AddOpenRiaServices()
    .AddMessagePackSerialization(opt =>
    {
        opt.Serializer = new Nerdbank.MessagePack.MessagePackSerializer()
        {
            Converters = [new XElementConverter(), new BinaryConverter()],
        };
    });
```

### Simple struct parameters and keys

Simple structs can be used in query/custom/invoke operation signatures when they have one or more public readable properties of predefined simple types.

When a simple struct is used as an entity key member, the struct must be shared with the client and implement `IEquatable<T>`.
Its public properties also cannot be marked with `[Exclude]`.
The struct must also be compatible with the configured transport serializer; with the default DataContract serialization, use `[DataContract]` and writable `[DataMember]` properties.

### Specifying endpoint routes

You can choose among three endpoint route patterns by adding the `DomainServiceEndpointRoutePattern` attribute to an assembly:

- If the attribute is defined in the DomainService's assembly, that pattern is used.
- Otherwise, the pattern from the startup assembly is used. Because code generation cannot identify the startup project, it treats the `LinkedServerProject` as the startup project.
   
The options are `WCF`, `FullName`, and `Name`:

- `WCF` generates routes compatible with WCF RIA Services, such as `Some-Namespace-TypeName.svc/binary/Method`. This is the only option supported by the obsolete WCF-based `DomainClient`.
- `FullName` generates routes using the full DomainService name, such as `Some-Namespace-TypeName/Method`.
- `Name` generates routes using the short DomainService name, such as `TypeName/Method`.

The current default is `WCF`; the default is planned to change to `FullName`.
```csharp
[assembly: DomainServiceEndpointRoutePattern(EndpointRoutePattern.WCF)]
// or 
[assembly: DomainServiceEndpointRoutePattern(EndpointRoutePattern.FullName)]
// or 
[assembly: DomainServiceEndpointRoutePattern(EndpointRoutePattern.Name)]
```

If you want to change the route for a specific DomainService or need to map a DomainService to multiple routes you can specify 
a route directly when adding domainservices during the `MapOpenRiaServices` call.

```csharp
app.MapOpenRiaServices(builder =>
{
    builder.AddDomainService<Cities.CityDomainService>("Cities-CityDomainService.svc/binary");
});
```

### Client query complexity limits

Client `where` and `orderby` expressions have these limits:

- 200 levels of nesting (ASP.NET Core configurable; WCF uses 200)
- 4,096 expression nodes and 32,768 characters per expression
- 64 query parts and 131,072 total expression characters per request

Requests that exceed a limit return an error or fault; they are not truncated. Hosts with small thread stacks may reject deeply nested expressions sooner.

See [Client query resource limits](#client-query-resource-limits) to configure depth
and numeric-format limits through `OpenRiaServicesOptions.QueryLimits`.

String replacements are also limited: an expression can contain at most three growing `Replace(string, string)` calls, with a combined growth factor no greater than 100. Each call's factor is `ceil(replacement.Length / search.Length)`; nonconstant or empty search strings use a length of 1.

Non-growing replacements, deletions (`null` or empty replacement), and `Replace(char, char)` do not count. Calls across branches and ordering selectors are counted together. For example, three `Replace("a", "aaa")` calls are allowed (factor 27); a fourth is rejected.

These limits do not replace transport request-size limits or provider execution timeouts.

## ASP.NET Core integration

Since version 0.4.0, attributes applied to `Invoke` and `Query` operations are added to the corresponding ASP.NET Core endpoints.
This lets you use ASP.NET Core endpoint middleware.

This means you can use standard [ASP.NET Core authentication and authorization](https://learn.microsoft.com/en-us/aspnet/core/security/) to validate most requests.
This does not apply to individual `Insert`, `Update`, or `Delete` operations; apply the attributes to the DomainService class to protect those operations.
Authorization middleware runs before the DomainService is created, avoiding the cost of creating the service and its dependencies.

You can still (and probably should) use the OpenRiaServices specific attributes such as `[RequiresAuthentication]` or [your own Authorization attributes](https://openriaservices.gitbook.io/openriaservices/ee707361/ee707357)
[Simple authorization](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/simple), which works on all methods.




### ASP.NET Core authentication and authorization

The **[AspNetCore Sample](https://github.com/OpenRIAServices/Samples/tree/main/WpfCore_AspNetCore)** shows how cookie based login similar to ASP.NET Membership provider *can* be handled.
The change was added in [#16: Add AspNetCore AuthenticationService](https://github.com/OpenRIAServices/Samples/pull/16)
You will need to tweak it so it validates credentials, assign correct Claims (Roles) to users and fits your choosen scheme for authentication.


#### Authentication: Client setup
On the client, ensure that all `HttpClient` instances share the same `CookieContainer` and that the handler is configured to use cookies ([`HttpClientHandler.UseCookies`](https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpclienthandler.usecookies#system-net-http-httpclienthandler-usecookies)).

#### Authentication: ASP.NET Core setup

In Program.cs the following code is needed

Service registrations, adds cookie based authentication and enable Authorization
```csharp
// Additional dependencies for cookie based AuthenticationService
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie();
builder.Services.AddHttpContextAccessor();

builder.Services.AddAuthorization();
```

After configuring services, add authentication (and authorization, if used) before mapping OpenRiaServices endpoints:

```csharp
// Add authentication
app.UseAuthentication();
app.UseAuthorization();

// Enable OpenRiaServices
app.MapOpenRiaServices(...);
```

#### Authorization: Protecting DomainServices using Authorization middleware

Protect all DomainServices or individual services by requiring authorization.
The [authorization middleware](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/introduction) should run before the DomainService is created, avoiding the cost of creating the service and its dependencies and helping protect against denial-of-service (DoS) attacks.

**IMPORANT**: error message on the client will be different and not as user friendly as if the `[RequiresAuthentication]` attribute is used.
You might want to consider changing the HTTP status code to match 401 or similar instead of 404 (when it tries to redirect to missing "/Account/Login" endpoint) 
**Note**: If you need to selectivly only require authentication for some Submit operations (Insert, Update, Delete or Entity Action) then remember to use `[RequiresAuthentication]`

**Example: Require authorization for requests to all mapped DomainServices using the default policy:**

```csharp
app.MapOpenRiaServices(builder =>
{
    builder.AddDomainService<SampleDomainService>();
    builder.AddDomainService<MyAuthenticationService>();
}).RequireAuthorization();
```

You can also configure authorization for an individual DomainService in code:
```csharp
app.MapOpenRiaServices(builder =>
{
    builder.AddDomainService<SampleDomainService>()
        .RequireAuthorization();
});
```

You can also configure authorization for an individual DomainService using attributes:
```csharp
[Authorize]
public class MyAuthenticationService : DomainService, IAuthentication<MyUser>
{
    [AllowAnonymous]
    public MyUser GetUser() {...}
```


### Output cache integration

This example shows how to integrate the [output cache middleware](https://learn.microsoft.com/en-us/aspnet/core/performance/caching/output).
**Warning:** Read the caching documentation and ensure that cached responses are not served to the wrong user.

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenRiaServices();
builder.Services.AddOutputCache(options =>
{
    options.AddBasePolicy(policy => policy.NoCache());
});

builder.Services.AddDomainService<CacheTestDomainService>();

var app = builder.Build();

app.UseOutputCache();


[EnableClientAccess]
public class CacheTestDomainService : DomainService
{
    [Invoke(HasSideEffects = false)]
    public string NoCache()
        => DateTime.Now.ToString();

    [Invoke(HasSideEffects = false)]
    [Microsoft.AspNetCore.OutputCaching.OutputCache(Duration = 5)]
    public string OutputCache()
        => DateTime.Now.ToString();
}

```

## Sample

For additional usage examples, see the `AspNetCoreWebsite` project in this repository and the [WpfCore_AspNetCore sample](https://github.com/OpenRIAServices/Samples/tree/main/WpfCore_AspNetCore) in the Samples repository.
