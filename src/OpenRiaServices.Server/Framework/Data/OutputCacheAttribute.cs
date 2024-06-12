using System;

namespace OpenRiaServices.Server
{
    /// <summary>
    /// Provides a declarative way to enable output caching.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Field | AttributeTargets.Method | AttributeTargets.Property,
        AllowMultiple = false, Inherited = true)]
#if NET
    [Obsolete("OutputCacheAttribute is not applied by AspNetCore hosting, please se Readme of how caching can be achived.\nThe attribute will probably be removed in future releases")]
#endif
    public sealed class OutputCacheAttribute : Attribute
    {
        /// <summary>
        /// Gets the name of the cache settings
        /// </summary>
        public string CacheProfile
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the duration in seconds for which the response should be cached.
        /// </summary>
        public int Duration
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the location(s) where caching can be applied.
        /// </summary>
        public OutputCacheLocation Location
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets whether the duration should be a sliding expiration or an absolute expiration.
        /// </summary>
        public bool UseSlidingExpiration
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the headers on which a cached response is based.
        /// </summary>
        public string VaryByHeaders
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the SQL cache dependencies.
        /// </summary>
        public string SqlCacheDependencies
        {
            get;
            set;
        }

        /// <summary>
        /// Initializes a new instance of the OutputCacheAttribute class
        /// </summary>
        /// <param name="location">The location(s) where caching can be applied.</param>
        public OutputCacheAttribute(OutputCacheLocation location)
        {
            this.Location = location;
            this.Duration = -1;
        }

        /// <summary>
        /// Initializes a new instance of the OutputCacheAttribute class
        /// </summary>
        /// <param name="location">The location(s) where caching can be applied.</param>
        /// <param name="duration">The duration in seconds for which the response should be cached.</param>
        public OutputCacheAttribute(OutputCacheLocation location, int duration)
        {
            this.Duration = duration;
            this.Location = location;
        }

        /// <summary>
        /// Initializes a new instance of the OutputCacheAttribute class
        /// </summary>
        /// <param name="cacheProfile">The name of the cache settings.</param>
        public OutputCacheAttribute(string cacheProfile)
        {
            this.CacheProfile = cacheProfile;
        }
    }
}
