using System;

namespace System.ServiceModel
{
    //
    // Summary:
    //     Indicates that an interface or a class defines a service contract in a Windows
    //     Communication Foundation (WCF) application.
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
    internal class ServiceContractAttribute : Attribute
    {
        public string Name { get; set; }
    }
}
