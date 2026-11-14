using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace OpenRiaServices.Client.Test
{
    /// <summary>
    /// Wrapper class for creating new isolated test database instances.
    /// </summary>
    public class TestDatabase : IDisposable
    {
        private readonly string databaseName;
        private readonly HttpClient _httpClient;
        private Task _initializeTask;

        public TestDatabase(string databaseName)
        {
            this.databaseName = databaseName;
            _httpClient = new HttpClient()
            {
                BaseAddress = new Uri(TestURIs.RootURI, "Services/TestServices.svc/"),
            };
        }

        /// <summary>
        /// Idempotent initialization method. Once the database has been initialized
        /// subsequent calls are ignored
        /// </summary>
        public void Initialize()
        {
            if (_initializeTask is null)
            {
                // call the TestServices web service method to create the database 
                _initializeTask = _httpClient.PostAsync($"CreateDatabase?name={databaseName}", new StringContent(""))
                    .ContinueWith(res =>
                    {
                        // Force throw exception
                        res.Result.EnsureSuccessStatusCode();
                    });
            }
        }


        /// <summary>
        /// Returns true once the async initialization has completed
        /// </summary>
        public bool IsInitialized
        {
            get
            {
                if (_initializeTask is null)
                    return false;

                // Fail early if there is an error
                if (_initializeTask.IsFaulted || _initializeTask.IsCanceled)
                    _initializeTask.GetAwaiter().GetResult();

                return _initializeTask.Status == TaskStatus.RanToCompletion;
            }
        }

        public Task InitializeTask => _initializeTask;

        #region IDisposable Members

        public void Dispose()
        {
            if (_initializeTask is not null)
            {
                _initializeTask = null;
                // call the TestServices web service method to create the database 
                var res = _httpClient.PostAsync($"DropDatabase?name={databaseName}", new StringContent(""))
                    .GetAwaiter().GetResult();
            }
        }
        #endregion
    }
}
