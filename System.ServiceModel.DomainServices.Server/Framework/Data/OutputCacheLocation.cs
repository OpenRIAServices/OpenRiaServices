namespace System.ServiceModel.DomainServices.Server
{
    /// <summary>
    /// Specifies the valid values for controlling the location of the output-cached HTTP response for a resource.
    /// </summary>
    public enum OutputCacheLocation
    {
        /// <summary>
        /// The output cache can be located on the browser client (where the request originated), on a proxy server 
        /// (or any other server) participating in the request, or on the server where the request was processed.
        /// </summary>
        Any = 0,

        /// <summary>
        /// The output cache is located on the browser client where the request originated.
        /// </summary>
        Client = 1,

        /// <summary>
        /// The output cache can be stored in any HTTP 1.1 cache-capable devices other than the origin server. This 
        /// includes proxy servers and the client that made the request.
        /// </summary>
        Downstream = 2,

        /// <summary>
        /// The output cache is located on the Web server where the request was processed.
        /// </summary>
        Server = 3,

        /// <summary>
        /// The output cache is disabled for the requested page.
        /// </summary>
        None = 4,

        /// <summary>
        /// The output cache can be stored only at the origin server or at the requesting client. Proxy servers are not allowed to cache the response.
        /// </summary>
        ServerAndClient = 5
    }
}