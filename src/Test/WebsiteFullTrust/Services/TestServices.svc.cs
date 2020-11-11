using System.ServiceModel;
using System.ServiceModel.Activation;
using System.Web;
using TestDomainServices.Testing;

namespace Website.Services
{
    [ServiceContract(Namespace = "")]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    public class TestServices 
    {
        /// <summary>
        /// Creates a new unique instance of the specified database (Northwind, AdventureWorks, etc.)
        /// The connection for the new instance is then cached statically and can be accessed to provide
        /// connection durability across DomainService instances (since DPs only serve a single request)
        /// </summary>
        /// <param name="databaseName">The database name</param>
        [OperationContract]
        public void CreateNewDatabase(string databaseName)
        {
            DBImager.CreateNewDatabase(databaseName);
        }

        /// <summary>
        /// Deletes the specified database for the current user and nulls out the
        /// active connection record
        /// </summary>
        /// <param name="databaseName">The database name</param>
        [OperationContract]
        public void ReleaseNewDatabase(string databaseName)
        {
            DBImager.CleanDB(databaseName);
        }

        [OperationContract]
        public void RestartApp()
        {
            HttpRuntime.UnloadAppDomain();
        }
    }
}
