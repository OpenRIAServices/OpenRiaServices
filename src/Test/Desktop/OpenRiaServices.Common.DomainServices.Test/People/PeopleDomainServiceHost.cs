using System;
using OpenRiaServices.Hosting.Wcf;

namespace People
{
    public class PeopleDomainServiceHost : DomainServiceHost
    {
        public PeopleDomainServiceHost(Type domainServiceType, params Uri[] baseAddresses)
            : base(domainServiceType, baseAddresses)
        {
        }
    }
}
