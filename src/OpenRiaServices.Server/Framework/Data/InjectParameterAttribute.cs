using System;

namespace OpenRiaServices.Server
{
    /// <summary>
    ///  EXPERIMENTAL: support for injecting dependencies to <see cref="DomainService"/> methods similar to [FromServices] in ASP.NET Core
    ///  <para>
    ///  The services are resolved by calling <see cref="DomainServiceContext.GetService(Type)"/> on <see cref="DomainService.ServiceContext"/>
    ///  </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class InjectParameterAttribute : Attribute
    {
    }
}
