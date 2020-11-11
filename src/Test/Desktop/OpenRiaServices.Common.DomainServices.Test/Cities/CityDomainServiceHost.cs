using System;
using OpenRiaServices.Hosting.WCF;

namespace Cities
{
    public class CityDomainServiceHost : DomainServiceHost
    {
        public CityDomainServiceHost(Type domainServiceType, params Uri[] baseAddresses)
            : base(domainServiceType, baseAddresses)
        {
        }
    }
}
