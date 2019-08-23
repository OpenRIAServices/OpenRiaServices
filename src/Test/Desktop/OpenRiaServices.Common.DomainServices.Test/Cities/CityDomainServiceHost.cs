﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OpenRiaServices.DomainServices.Hosting;

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
