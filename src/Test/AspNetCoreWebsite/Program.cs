using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OpenRiaServices.Hosting.AspNetCore;
using OpenRiaServices.Server;
using RootNamespace.TestNamespace;
using TestDomainServices.Testing;

[assembly: DomainServiceEndpointRoutePattern(EndpointRoutePattern.FullName)]

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenRiaServices(o =>
{
    o.IncludeExceptionMessageInFault = (ex) => true;
    o.UnsafeIncludeStackTraceInErrors = true;
});

builder.Services.AddAuthentication();

//builder.Services.AddOpenRiaServices(x =>
//{
//    x.WithOnError((ex, context) =>
//    {
//        Console.WriteLine($"Error: {ex.Message}");
//        context.Response.StatusCode = 500;
//        return Task.CompletedTask;
//    });
//    x.RegisterDomainService<AuthenticationService1>();
//    x.RegisterDomainServicesInAssembly(...);
//    x.RegisterDomainServicesInAssembly(...);
//});


//builder.Services.AddOpenRiaServices(x =>
//{
//    x.WithOnError((ex, context) =>
//    {
//        Console.WriteLine($"Error: {ex.Message}");
//        context.Response.StatusCode = 500;
//        return Task.CompletedTask;
//    });
//})
//    .RegisterDomainService<AuthenticationService1>()
//    .RegisterDomainServicesInAssembly(...)
//    .RegisterDomainServicesInLoadedAssemblies(...);



// Allow injection of HttpContext

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<HttpContext>(s => s.GetRequiredService<IHttpContextAccessor>().HttpContext);

builder.Services.AddDomainServicesFromAssembly(typeof(TestDomainServices.NamedUpdates.NamedUpdate_CustomAndUpdate).Assembly);

// Types in this assembly
builder.Services.AddTransient<AuthenticationService1>();

var app = builder.Build();

//app.UseEndpoints(endpoints =>
//{
app.MapOpenRiaServices(builder =>
   {
       // All all domainservices registered in the container
       builder.AddRegisteredDomainServices(suppressAndLogErrors: true);
 
       // Add services with old endpoint structure to allow unit tests to work
       // REMARKS: The unit tests should be rewritten so this is not needed
       builder.AddDomainService<Cities.CityDomainService>("Cities-CityDomainService.svc/binary");
   });

// TestDatabase
app.MapPost("/Services/TestServices.svc/CreateDatabase", context =>
{
    DBImager.CreateNewDatabase(context.Request.Query["name"]);
    context.Response.StatusCode = 200;    
    return Task.CompletedTask;
});
app.MapPost("/Services/TestServices.svc/DropDatabase", context =>
{
    DBImager.CleanDB(context.Request.Query["name"]);
    context.Response.StatusCode = 200;
    return Task.CompletedTask;
});


app.MapGet("/", httpContext =>
    {
        var dataSource = httpContext.RequestServices.GetRequiredService<EndpointDataSource>();

        var sb = new StringBuilder();
        sb.Append("<html><body>");
        sb.AppendLine("<p>Endpoints:</p>");
        foreach (var endpoint in dataSource.Endpoints.OfType<RouteEndpoint>().OrderBy(e => e.RoutePattern.RawText, StringComparer.OrdinalIgnoreCase))
        {
            sb.AppendLine(FormattableString.Invariant($"- <a href=\"{endpoint.RoutePattern.RawText}\">{endpoint.RoutePattern.RawText}</a><br />"));
            foreach (var metadata in endpoint.Metadata)
            {
                sb.AppendLine("<li>" + metadata + "</li>");
            }
        }

        var response = httpContext.Response;
        response.StatusCode = 200;

        sb.AppendLine("</body></html>");
        response.ContentType = "text/html";
        return response.WriteAsync(sb.ToString());
    });
//});

app.Run();
