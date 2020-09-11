using Microsoft.WindowsAzure.StorageClient;

namespace OpenRiaServices.WindowsAzure
{
    /// <summary>
    /// The base type for all entities that can be made available from the <see cref="TableDomainService{T}"/>.
    /// </summary>
    public abstract class TableEntity : TableServiceEntity
    {
        // The etag is not exposed as a property to keep it from showing up as a column in
        // the Windows Azure table storage schema that is created from the entity type
        private string _etag;

        /// <summary>
        /// Gets the etag used in concurrency checking for the entity
        /// </summary>
        /// <returns>The etag for the entity</returns>
        public string GetETag()
        {
            return this._etag;
        }

        /// <summary>
        /// Sets the etag used in concurrency checking for the entity
        /// </summary>
        /// <param name="etag">The etag for the entity</param>
        public void SetETag(string etag)
        {
            this._etag = etag;
        }
    }
}
