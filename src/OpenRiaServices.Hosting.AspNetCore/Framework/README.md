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


## Production Ready - "preview"

The package is production ready, but does not yet contain all features planened for 1.0.
Please look at TODO in project's folder for more details.
    
**Public API will change before 1.0.0 release**
    
There is no documentation yet, please see AspNetCoreWebsite project in repository for usage.

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

Minimal program:

```
using OpenRiaServices.Hosting.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenRiaServices();
builder.Services.AddTransient<CityDomainService>();


var app = builder.Build();
app.MapOpenRiaServices(builder =>
{
    builder.AddDomainService<CityDomainService>();
});


app.Run();
```

## Asp.Net Core integration

Since 0.4.0 any attivbute applied to Invoke/Query are added to the corresponding AspNetCore-Endpoint allowing the use 
of any AspNetCore endpoint middleware (not MVC specific filters, they must work with "minimal api's").

This means you can use standard [aspnetcore Authentication and Authorization](https://learn.microsoft.com/en-us/aspnet/core/security/?view=aspnetcore-7.0)
to validate most requests (but **NOT** indivudual Insert,Update,Delete methods, you can apply atttributes to your DomainService class for those).
The validation will happen before the DomainService is even created, making them more powerful than built in attributes.

You can still (and probably should) use the OpenRiaServices specific attributes such as `[RequiresAuthentication]` or [your own Authorization attributes](https://openriaservices.gitbook.io/openriaservices/ee707361/ee707357)
[Simple authorization](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/simple?view=aspnetcore-7.0), which works on all methods.




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

builder.Services.AddTransient<CacheTestDomainService>();

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
