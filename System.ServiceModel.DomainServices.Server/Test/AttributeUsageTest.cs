using System.ComponentModel.DataAnnotations;

namespace OpenRiaServices.DomainServices.Server.Test
{
    /// <summary>
    /// Class for testing attribute usage. If this class compiles, it passes.
    /// </summary>
    public class AttributeUsageTest
    {
        // F/P
        [Composition]
        [Exclude]
        [ExternalReference]
        [Include]
        [RoundtripOriginal]
        // F/P/M
        [Delete]
        [Ignore]
        [Insert]
        [Invoke]
        [OutputCache(OutputCacheLocation.None)]
        [Query]
        [Update]
        // F/P/M/C
        [RequiresAuthentication]
        [RequiresRoleAttribute("role")]
        private int field = 1;

        // F/P
        [Composition]
        [Exclude]
        [ExternalReference]
        [Include]
        [RoundtripOriginal]
        // F/P/M
        [Delete]
        [Ignore]
        [Insert]
        [Invoke]
        [OutputCache(OutputCacheLocation.None)]
        [Query]
        [Update]
        // F/P/M/C
        [RequiresAuthentication]
        [RequiresRoleAttribute("role")]
        private int Property { get; set; }

        // F/P/M
        [Delete]
        [Ignore]
        [Insert]
        [Invoke]
        [OutputCache(OutputCacheLocation.None)]
        [Query]
        [Update]
        // F/P/M/C
        [RequiresAuthentication]
        [RequiresRoleAttribute("role")]
        private int Method() { return field; }

        // C
        [DomainIdentifier("name")]
        [DomainServiceDescriptionProvider(typeof(AutDomainServiceDescriptionProvider))]
        // F/P/M/C
        [RequiresAuthentication]
        [RequiresRoleAttribute("role")]
        private class Class { }

        #region DomainServiceDescriptionProvider
        private class AutDomainServiceDescriptionProvider : DomainServiceDescriptionProvider
        {
            private AutDomainServiceDescriptionProvider(Type domainServiceType) : base(domainServiceType, null) { }
        }
        #endregion
    }
}
