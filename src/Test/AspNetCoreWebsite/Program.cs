using System;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OpenRiaServices.Server;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenRiaServices();

var domainServices = typeof(Program).Assembly.ExportedTypes
    .Where(t => typeof(DomainService).IsAssignableFrom(t) && !t.IsAbstract && t.GetCustomAttribute(typeof(EnableClientAccessAttribute), inherit: true) != null)
    .ToArray();

foreach (var type in domainServices)
    builder.Services.Add(new ServiceDescriptor(type, type, ServiceLifetime.Transient));

var app = builder.Build();

//app.UseEndpoints(endpoints =>
//{
app.MapOpenRiaServices(builder =>
   {
       foreach(var type in domainServices)
       {
           try
           {
               builder.AddDomainService(type);
           }
           catch (global::System.Exception ex)
           {
               Console.WriteLine($"Ignnoreing {type} due to exception: {ex.Message}");
           }
       }
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