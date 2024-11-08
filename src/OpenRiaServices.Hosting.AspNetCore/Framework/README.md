[![Stand With Ukraine](https://raw.githubusercontent.com/vshymanskyy/StandWithUkraine/main/banner2-direct.svg)](https://vshymanskyy.github.io/StandWithUkraine)

This software will allow existing applications written for OpenRiaServices or WCF RIA Services to run on net6 and kestrel,
making them future proof and improving their performance. 

**Hopefully it will allow you as a consumer to make large savings in development time**, weeks or even man years,
by not having to rewrite your application as well as allowing rapid development.


The software is provided free of charge, but **I urge you to use some of the money saved by using this software [to support Ukraine](https://stand-with-ukraine.pp.ua/)**
The civilian suffering due to the Russian invasion, the attacks on hospitals and other war crimes are enormous.

## TERM OF USE

By using this project or its source code, for any purpose and in any shape or form, you grant your **agreement** to all the following statements:

- You **condemn Russia and its military aggression against Ukraine**
- You **recognize that Russia is an occupant that unlawfully invaded a sovereign state**
- You **support Ukraine's territorial integrity, including its claims over temporarily occupied territories of Crimea and Donbas**
- You **do not support the Russian invasion or contribute to its propaganda'**

This excludes usage by the Russian state, Russian state-owned companies, Russian education who spread propaganda instead of truth, anyone who work with the *filtration camps*, or finance the war by importing Russian oil or gas.

- You allow anonymized telemetry to collected and sent during the preview releases to gather feedback about usage


## Sample

There is no documentation except for this yet readme, please see AspNetCoreWebsite project in repository for usage.

* For a sample see [WpfCore_AspNetCore in Samples repository](https://github.com/OpenRIAServices/Samples/tree/main/WpfCore_AspNetCore)


## Getting Started

1. Create a new dotnet 6 web application `dotnet new web` or similar
2. Add a reference to *OpenRiaServices.Hosting.AspNetCore*
    `dotnet add package OpenRiaServices.Hosting.AspNetCore`
3.   Add a reference to *OpenRiaServices.Server*

4. Add one or more domainservices


```csharp
[EnableClientAccess]
public class CityDomainService : DomainService
{
    /* .....  */
}
```
For more documentation see https://openriaservices.gitbook.io/openriaservices/ee707348/ee707373 or samples
https://github.com/OpenRIAServices/OpenRiaServices/blob/086ea8c8fcb115000749be6b2b01cd43bb95bf80/docs/gg602754.md#add-the-poco-class

5. Setup hosting integration

Sample program:

```csharp
using OpenRiaServices.Hosting.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenRiaServices();

// Register DomainServices in DI
builder.Services.AddDomainService<CityDomainService>();
// OR builder.Services.AddDomainServices(AppDomain.CurrentDomain.GetAssemblies());


var app = builder.Build();

// Map OpenRiaServices routes , you can optionally add a prefix such as "/Services"
// This will automatically map all DomainServices that are registered in builder.Services
// Using routes similar to $"{DomainServiceName}/{MethodName}"
app.MapOpenRiaServices();
// OR app.MapOpenRiaServices(builder => { builder.AddDomainService<CityDomainService>(); }); to specify exactly the DomainServices to map which works better with Trimming

app.Run();
```

## Advanced

### Configuring Hosting Options

You can configure the hosting options by passing a callback to `AddOpenRiaServices` method.

Options include:
- `ExceptionHandler` - A delegate that can be used to handle exceptions that occur during the execution of a DomainService method.
    - It allows customizing the error message, and error code that is sent to the client as well as the HTTP status code.
- `IncludeExceptionMessageInErrors` - A boolean that determines if the exception message should be included in the error response.
   - **WARNING**: Exposing this information might help a hacker. It is generally better to use the `ExceptionHandler` and 
    ensuring that the message is not passed on to the client is "safe" and does not give to much information about the system to a potential hacker.
- `IncludeExceptionStackTraceInErrors` - A boolean that determines if the exception stack trace should be included in the error response. 
   - **WARNING**: This is considered INSECURE and is *NOT* *recommended for production use as it would gives an attacker detailed information about your system

Example setup:
```csharp
builder.Services.AddOpenRiaServices(o => {
    o.ExceptionHandler = (context, response) =>
    {
        // Pass all exceptions to client
        response.ErrorMessage ??= context.Exception.Message;
    };
 
    // There is at least one test which test that checks that calls stacks of normal exceptions are passed to the client
    // To get StackTrack of "normal" exceptions we need to pass them on to the user, as well as include stack traces
    o.IncludeExceptionMessageInErrors = true;
    o.IncludeExceptionStackTraceInErrors = true;
});
```

### Specifying endpoint routes

You can choose between 3 different approaches to how the endpoint routes are generated.
You do this by adding the `DomainServiceEndpointRoutePattern` attribute to your assembly.
   - If the attribute is defined in the assembly of a specific DomainService, then that will be used
   - Otherwise if there is an attribute in the "startup" assembly then that will be used 
     (since the code generation cannot know what project is the startup project, 
    it will always treat the "LinkedServerProject" as the startup project)
   
The options are `WCF`, `FullName` and `ShortName`.
 * `WCF` will generate the same routes as WCF RIA Services `Some-Namespace-TypeName.svc/binary/Method`
    * This is the only option that works with the (obsolete) WCF based DomainClient
 * `FullName` will generate routes with the full name of the DomainService `Some-Namespace-TypeName/Method`
 * `Name` will generate routes with the short name of the DomainService `TypeName/Method`

The default will be changed to `FullName` which is the same as in WCF RIA Services.
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

## Asp.Net Core integration

Since 0.4.0 any attivbute applied to Invoke/Query are added to the corresponding AspNetCore-Endpoint allowing the use 
of any AspNetCore endpoint middleware (not MVC specific filters, they must work with "minimal api's").

This means you can use standard [aspnetcore Authentication and Authorization](https://learn.microsoft.com/en-us/aspnet/core/security/?view=aspnetcore-7.0)
to validate most requests (but **NOT** indivudual Insert,Update,Delete methods, you can apply atttributes to your DomainService class for those).
The validation will happen before the DomainService is even created, making them more powerful than built in attributes.

You can still (and probably should) use the OpenRiaServices specific attributes such as `[RequiresAuthentication]` or [your own Authorization attributes](https://openriaservices.gitbook.io/openriaservices/ee707361/ee707357)
[Simple authorization](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/simple?view=aspnetcore-7.0), which works on all methods.



### AspNetCore Authentication and Authorization

You can use standard [aspnetcore Authentication and Authorization](https://learn.microsoft.com/en-us/aspnet/core/security/?view=aspnetcore-7.0)
to validate most requests (but **NOT** indivudual Insert, Update, Delete methods, you can apply atttributes to your DomainService class for those).
The validation will happen before the DomainService is even created, making them more powerful than built in attributes.

The **[AspNetCore Sample](https://github.com/OpenRIAServices/Samples/tree/main/WpfCore_AspNetCore)** shows how cookie based login similar to ASP.NET Membership provider *can* be handled.
The change was added in [#16: Add AspNetCore AuthenticationService](https://github.com/OpenRIAServices/Samples/pull/16)
You will need to tweak it so it validates credentials, assign correct Claims (Roles) to users and fits your choosen scheme for authentication.


#### Authentication: Client setup
For the client, **you need to ensure that all HttpClients share the same CookieContainer** and that the it is set to use to cookies (https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpclienthandler.usecookies?view=net-7.0#system-net-http-httpclienthandler-usecookies)

#### Authentication: Asp.Net Core Setup Authentication and Authorization setup

In Program.cs the following code is needed

Service registrations, adds cookie based authentication and enable Authorization
```csharp
// Additional dependencies for cookie based AuthenticationService
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie();
builder.Services.AddHttpContextAccessor();

builder.Services.AddAuthorization();
```

After the "application has been built" and the middleware is configured you should add Authentication (and Authorization if used).
They should be added **before** *OpenRiaServices*

```csharp
// Add authentication
app.UseAuthentication();
app.UseAuthorization();

// Enable OpenRiaServices
app.MapOpenRiaServices(....
```

#### Authorization: Protecting DomainServices using Authorization middleware

A good idea is to protect your DomainServices, all of them or individual, by requiring authorization.
The [Authorization middleware](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/introduction) should run before the DomainService is created so you will avoid any cost with creating the DomainService and it's dependencies, providing a much better protection against DOS.

**IMPORANT**: error message on the client will be different and not as user friendly as if the `[RequiresAuthentication]` attribute is used.
You might want to consider changing the HTTP status code to match 401 or similar instead of 404 (when it tries to redirect to missing "/Account/Login" endpoint) 
**Note**: If you need to selectivly only require authentication for some Submit operations (Insert, Update, Delete or Entity Action) then remember to use `[RequiresAuthentication]`

**Require request to all DomainServices be authorized using default authorization policy**:

```csharp
app.MapOpenRiaServices(builder =>
{
    builder.AddDomainService<SampleDomainService>();
    builder.AddDomainService<MyAuthenticationService>();
}).RequireAuthorization();
```

You can also control the setting per DomainService using code
```csharp
app.MapOpenRiaServices(builder =>
{
    builder.AddDomainService<SampleDomainService>()
        .RequireAuthorization();
});
```

You can also control the setting per DomainService using attributes
```csharp
[Authorize]
public class MyAuthenticationService : DomainService, IAuthentication<MyUser>
{
    [AllowAnonymous]
    public MyUser GetUser() {...}
```


###  Output Cache Integration

Sample showing how to integrate the [OutputCache middleware](https://learn.microsoft.com/en-us/aspnet/core/performance/caching/output?view=aspnetcore-7.0) 
**WARNING:** Se caching documentation and ensure that any usage of output cache is not sent to the wrong user.

```
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenRiaServices();
builder.Services.AddOutputCache(options =>
{
    options.AddBasePolicy(builder => builder.NoCache());
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

```` 
