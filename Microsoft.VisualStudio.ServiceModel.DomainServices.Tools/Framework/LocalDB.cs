#if VS10
#else
// ported from $/DevDiv/PU/WPT/venus/mvw/data/LocalDB/LocalDB.cs
namespace Microsoft.VisualStudio.ServiceModel.DomainServices.Tools
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;

    using Registry = global::Microsoft.Win32.Registry;
    using Configuration = global::System.Configuration.Configuration;
    using ConfigurationManager = global::System.Configuration.ConfigurationManager;
    using ExeConfigurationFileMap = global::System.Configuration.ExeConfigurationFileMap;
    using ConfigurationUserLevel = global::System.Configuration.ConfigurationUserLevel;
    using ConnectionStringSettingsList = global::System.Collections.Generic.List<global::System.Configuration.ConnectionStringSettings>;
    using ConnectionStringSettings = global::System.Configuration.ConnectionStringSettings;
    using ConnectionStringSettingsCollection = global::System.Configuration.ConnectionStringSettingsCollection;
    using ConnectionStringsSection           = global::System.Configuration.ConnectionStringsSection;
    using EntityConnectionStringBuilder      = global::System.Data.EntityClient.EntityConnectionStringBuilder;
    using SqlConnectionStringBuilder         = global::System.Data.SqlClient.SqlConnectionStringBuilder;
    using IServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
    using IVsShell = Microsoft.VisualStudio.Shell.Interop.IVsShell;
    using __VSSPROPID = Microsoft.VisualStudio.Shell.Interop.__VSSPROPID;

    internal class LocalDBUtil : IDisposable
    {
        private IServiceProvider _serviceProvider;
        private string _webConfigPath;

        //------------------------------------------------------------------------------
        // Create the instance using a managed service provider
        //------------------------------------------------------------------------------
        public LocalDBUtil(IServiceProvider serviceProvider, string webConfigPath)
        {
            _serviceProvider = serviceProvider;
            _webConfigPath = webConfigPath;
        }

        //------------------------------------------------------------------------------
        // Ensure resources are released if not disposed
        //------------------------------------------------------------------------------
        ~LocalDBUtil()
        {
            Dispose();
        }

        //------------------------------------------------------------------------------
        // Ensure resources are released
        //------------------------------------------------------------------------------
        public void Dispose()
        {
            _serviceProvider = null;
            GC.SuppressFinalize(this);
        }

        //------------------------------------------------------------------------------
        // Get the service provider for the web project context we were initlialized with
        //------------------------------------------------------------------------------
        private IServiceProvider ServiceProvider
        {
            get
            {
                return _serviceProvider;
            }
        }

        //------------------------------------------------------------------------------
        // Opens web configuration for read or write access
        //------------------------------------------------------------------------------
        private Configuration OpenWebConfiguration(bool readOnly)
        {
            ExeConfigurationFileMap webConfig = new ExeConfigurationFileMap { ExeConfigFilename = _webConfigPath };
            return ConfigurationManager.OpenMappedExeConfiguration(webConfig, ConfigurationUserLevel.None);
        }

        //------------------------------------------------------------------------------
        // Gets a list of all the web specific connection string settings, filtering out parent settings
        //------------------------------------------------------------------------------
        private ConnectionStringSettingsList GetConnectionStringSettingsList(Configuration configuration)
        {
            if (configuration == null)
                return null;

            ConnectionStringSettingsList connectionStringSettingsList = new ConnectionStringSettingsList();
            try
            {
                // Get the connection string settings from config
                ConnectionStringsSection connectionStringsSection = (ConnectionStringsSection)configuration.GetSection("connectionStrings");
                ConnectionStringSettingsCollection connectionStringSettingsCollection = connectionStringsSection.ConnectionStrings;

                // Get the parent connection string settings from config
                ConnectionStringsSection parentConnectionStringsSection = (ConnectionStringsSection)connectionStringsSection.SectionInformation.GetParentSection();
                ConnectionStringSettingsCollection parentConnectionStringSettingsCollection = parentConnectionStringsSection.ConnectionStrings;

                // Filter out parent connection string settings
                foreach (ConnectionStringSettings connectionStringSettings in connectionStringSettingsCollection)
                {
                    if (string.IsNullOrEmpty(connectionStringSettings.Name)
                        || parentConnectionStringSettingsCollection[connectionStringSettings.Name] == null)
                    {
                        connectionStringSettingsList.Add(connectionStringSettings);
                    }
                }
            }
            catch (Exception)
            {
                //If web.config file is in bad shape, configuration API will throw an exception. We should silently eat the exception and display the message
                connectionStringSettingsList = null;
            }
            return connectionStringSettingsList;
        }

        //------------------------------------------------------------------------------
        // Get writable collection of connections from web.config
        //------------------------------------------------------------------------------
        private DBConnectionList GetConfigConnections(out Configuration configuration)
        {
            configuration = OpenWebConfiguration(false/*readOnly*/);
            return GetConfigConnections(configuration);
        }

        //------------------------------------------------------------------------------
        // Get collection of connections from provided configuration
        //------------------------------------------------------------------------------
        private DBConnectionList GetConfigConnections(Configuration configuration)
        {
            DBConnectionList configConnections = null;
            if (configuration != null)
            {
                ConnectionStringSettingsList connectionStringSettingsList = GetConnectionStringSettingsList(configuration);
                if (connectionStringSettingsList != null)
                {
                    foreach (ConnectionStringSettings connectionStringSettings in connectionStringSettingsList)
                    {
                        DBConnection configConnection = new ConfigConnection(ServiceProvider, connectionStringSettings);
                        if (configConnections == null)
                        {
                            configConnections = new DBConnectionList();
                        }
                        configConnections.Add(configConnection);
                    }
                }
            }
            return configConnections;
        }

        //--------------------------------------------------------------------------------------------------------------------------
        // Finds all the Connection Strings in web.config, converts them to LocalDB if updateDataSourceToLocalDB is set, and saves
        //-------------------------------------------------------------------------------------------------------------------------
        private bool UpdateConfigConnections(bool updateDataSourceToLocalDB, string projectName)
        {
            Configuration configuration;

            DBConnectionList configConnections = GetConfigConnections(out configuration);

            if (configuration != null && configConnections != null && configConnections.Count > 0)
            {
                foreach (DBConnection configConnection in configConnections)
                {
                    if (configConnection != null && configConnection.IsSQLExpress)
                    {
                        if (updateDataSourceToLocalDB)
                        {
                            configConnection.ConvertToLocalDB();
                        }
                        // For ALL Projects  - Use InitialCatalog instead of AttachDBFileName 
                        // We are using NugetPackages to add connection strings in web.config file and by default it had "MultipleActiveResultSets=True" set
                        configConnection.UpdateConnStrToUseInitialCatalog(projectName);
                    }
                }
                configuration.Save();
                return true;
            }
            return false;
        }

        //------------------------------------------------------------------------------
        // Determines if LocalDB should be the default in new projects
        //------------------------------------------------------------------------------
        private bool LocalDBIsDefault
        {
            get
            {
                return MVWUtilities.GetLocalDBIsDefault(_serviceProvider);
            }
        }

        //------------------------------------------------------------------------------
        // Silently converts the SQL Express connections in web.config to LocalDB
        //------------------------------------------------------------------------------
        public bool UpdateDBConnectionStringsForNewProject(bool updateDataSourceToLocalDB, string projectName)
        {
            return UpdateConfigConnections(LocalDBIsDefault && updateDataSourceToLocalDB, projectName);
        }

        private class DBConnection
        {
            private string _connectionName;
            private string _providerName;
            private string _connectionString;
            private bool _initializing;
            private SqlConnectionStringBuilder _sqlConnectionStringBuilder;
            private EntityConnectionStringBuilder _entityConnectionStringBuilder;

            //------------------------------------------------------------------------------
            // Called by derived classes to initialize the connection
            //------------------------------------------------------------------------------
            public void Initialize()
            {
                _initializing = true;
                try
                {
                    OnInitialize();
                }
                finally
                {
                    _initializing = false;
                }
            }

            //------------------------------------------------------------------------------
            // Overriden to initialize properties in derived classes
            //------------------------------------------------------------------------------
            protected virtual void OnInitialize()
            {
            }

            //------------------------------------------------------------------------------
            // True while initializing
            //------------------------------------------------------------------------------
            public bool Initializing
            {
                get
                {
                    return _initializing;
                }
            }

            //------------------------------------------------------------------------------
            // The name of the connection
            //------------------------------------------------------------------------------
            public string ConnectionName
            {
                get
                {
                    return _connectionName;
                }
                set
                {
                    if (!String.Equals(_connectionName, value, StringComparison.Ordinal))
                    {
                        _connectionName = value;

                        if (!Initializing)
                        {
                            OnConnectionNameChanged();
                        }
                    }
                }
            }

            //------------------------------------------------------------------------------
            // The name of the connection
            //------------------------------------------------------------------------------
            protected virtual void OnConnectionNameChanged()
            {
            }

            //------------------------------------------------------------------------------
            // 
            //------------------------------------------------------------------------------
            public string ProviderName
            {
                get
                {
                    return _providerName;
                }
                set
                {
                    if (!String.Equals(_providerName, value, StringComparison.Ordinal))
                    {
                        _providerName = value;

                        if (!Initializing)
                        {
                            OnProviderNameChanged();
                        }
                    }
                }
            }

            //------------------------------------------------------------------------------
            // 
            //------------------------------------------------------------------------------
            protected virtual void OnProviderNameChanged()
            {
            }

            //------------------------------------------------------------------------------
            // 
            //------------------------------------------------------------------------------
            public string ConnectionString
            {
                get
                {
                    return _connectionString;
                }
                set
                {
                    if (!String.Equals(_connectionString, value, StringComparison.Ordinal))
                    {
                        _connectionString = value;

                        // Set up connection string builders
                        _entityConnectionStringBuilder = null;
                        _sqlConnectionStringBuilder = null;
                        string provider = ProviderName;
                        string connectionString = value;
                        if (String.Equals(provider, "System.Data.EntityClient", StringComparison.OrdinalIgnoreCase))
                        {
                            _entityConnectionStringBuilder = new EntityConnectionStringBuilder(connectionString);
                            provider = _entityConnectionStringBuilder.Provider;
                            connectionString = _entityConnectionStringBuilder.ProviderConnectionString;
                        }
                        if (string.IsNullOrEmpty(provider) // per spec we try sql for unspecied provider
                            || String.Equals(provider, "System.Data.SqlClient", StringComparison.OrdinalIgnoreCase))
                        {
                            _sqlConnectionStringBuilder = new SqlConnectionStringBuilder(connectionString);
                        }

                        if (!Initializing)
                        {
                            OnConnectionStringChanged();
                        }
                    }
                }
            }

            //------------------------------------------------------------------------------
            // 
            //------------------------------------------------------------------------------
            protected virtual void OnConnectionStringChanged()
            {
            }

            //------------------------------------------------------------------------------
            // 
            //------------------------------------------------------------------------------
            public string DataSource
            {
                get
                {
                    if (_sqlConnectionStringBuilder != null)
                    {
                        return _sqlConnectionStringBuilder.DataSource;
                    }
                    return null;
                }
                set
                {
                    if (_sqlConnectionStringBuilder != null)
                    {
                        _sqlConnectionStringBuilder.DataSource = value;
                        UpdateConnectionString();
                    }
                }
            }

            //------------------------------------------------------------------------------
            // 
            //------------------------------------------------------------------------------
            public string AttachDBFilename
            {
                get
                {
                    if (_sqlConnectionStringBuilder != null)
                    {
                        return _sqlConnectionStringBuilder.AttachDBFilename;
                    }
                    return null;
                }
                set
                {
                    if (_sqlConnectionStringBuilder != null)
                    {
                        if (value != null)
                        {
                            _sqlConnectionStringBuilder.AttachDBFilename = value;
                        }
                        else
                        {
                            _sqlConnectionStringBuilder.Remove("AttachDBFilename");
                        }
                        // Update ConnectionString
                        UpdateConnectionString();
                    }
                }
            }

            //------------------------------------------------------------------------------
            // 
            //------------------------------------------------------------------------------
            public string InitialCatalog
            {
                get
                {
                    if (_sqlConnectionStringBuilder != null)
                    {
                        return _sqlConnectionStringBuilder.InitialCatalog;
                    }
                    return null;
                }

                set
                {
                    if (_sqlConnectionStringBuilder != null)
                    {
                        _sqlConnectionStringBuilder.InitialCatalog = value;

                    }
                    else
                    {
                        _sqlConnectionStringBuilder.Remove("Initial Catalog");
                    }
                    UpdateConnectionString();
                }
            }

            //------------------------------------------------------------------------------
            // 
            //------------------------------------------------------------------------------
            public bool UserInstance
            {
                get
                {
                    if (_sqlConnectionStringBuilder != null)
                    {
                        return _sqlConnectionStringBuilder.UserInstance;
                    }
                    return false;
                }
                set
                {
                    if (_sqlConnectionStringBuilder != null)
                    {
                        if (value == true)
                        {
                            _sqlConnectionStringBuilder.UserInstance = value;
                        }
                        else
                        {
                            _sqlConnectionStringBuilder.Remove("User Instance");
                        }
                        UpdateConnectionString();
                    }
                }
            }

            //------------------------------------------------------------------------------
            // 
            //------------------------------------------------------------------------------
            private void UpdateConnectionString()
            {
                if (_sqlConnectionStringBuilder != null)
                {
                    if (_entityConnectionStringBuilder != null)
                    {
                        _entityConnectionStringBuilder.ProviderConnectionString = _sqlConnectionStringBuilder.ConnectionString;
                        ConnectionString = _entityConnectionStringBuilder.ConnectionString;
                    }
                    else
                    {
                        ConnectionString = _sqlConnectionStringBuilder.ConnectionString;
                    }
                }
            }

            //------------------------------------------------------------------------------
            // 
            //------------------------------------------------------------------------------
            public bool IsSQLExpress
            {
                get
                {
                    string dataSource = DataSource;
                    if (!string.IsNullOrEmpty(dataSource)
                        && dataSource.StartsWith(@".\SQLEXPRESS", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                    return false;
                }
            }

            //------------------------------------------------------------------------------
            // 
            //------------------------------------------------------------------------------
            public bool ConvertToLocalDB()
            {
                if (IsSQLExpress)
                {
                    DataSource = @"(LocalDB)\v11.0";
                    UserInstance = false;
                    return true;
                }
                return false;
            }

            public bool UpdateConnStrToUseInitialCatalog(string projectName)
            {
                if (string.IsNullOrEmpty(InitialCatalog))
                {
                    AttachDBFilename = null;
                    InitialCatalog = MVWUtilities.GetUniqueInitialCatalogName(projectName);
                    //Remove UserIntance since it is Initial Catalog
                    UserInstance = false;
                    return true;
                }
                return false;
            }

        }

        private class DBConnectionList : List<DBConnection>
        {
        }

        private class ConfigConnection : DBConnection
        {
            private ConnectionStringSettings _connectionStringSettings;


            //------------------------------------------------------------------------------
            // Construct the config connection
            //------------------------------------------------------------------------------
            public ConfigConnection(IServiceProvider serviceProvider, ConnectionStringSettings connectionStringSettings)
            {
                _connectionStringSettings = connectionStringSettings;
                Initialize();
            }

            //------------------------------------------------------------------------------
            // Initialize the config connection
            //------------------------------------------------------------------------------
            protected override void OnInitialize()
            {
                ConnectionName = _connectionStringSettings.Name;
                ProviderName = _connectionStringSettings.ProviderName;
                ConnectionString = _connectionStringSettings.ConnectionString;
            }

            //------------------------------------------------------------------------------
            // Notification that the provider name changed
            //------------------------------------------------------------------------------
            protected override void OnProviderNameChanged()
            {
                // Update the provider name in config
                if (!String.Equals(ProviderName, _connectionStringSettings.ProviderName, StringComparison.Ordinal))
                {
                    _connectionStringSettings.ProviderName = ProviderName;
                }
            }

            //------------------------------------------------------------------------------
            // Notification that the connection string changed
            //------------------------------------------------------------------------------
            protected override void OnConnectionStringChanged()
            {
                // Update the connection string in config
                if (!String.Equals(ConnectionString, _connectionStringSettings.ConnectionString, StringComparison.Ordinal))
                {
                    _connectionStringSettings.ConnectionString = ConnectionString;
                }
            }
        }
    }

    // ported from $/DevDiv/PU/WPT/venus/mvw/Util/MVWUtilities.cs
    static class MVWUtilities
    {
        internal static ValueType GetProjectProperty<ValueType>(EnvDTE.Project project, string propertyName, ValueType defaultValue)
        {
            ValueType returnValue = defaultValue;
            try
            {
                if (project != null)
                {
                    EnvDTE.Properties properties = project.Properties;
                    if (properties != null)
                    {
                        EnvDTE.Property property = properties.Item(propertyName);
                        if (property != null)
                        {
                            object objValue = property.Value;
                            if (objValue != null)
                            {
                                returnValue = (ValueType)objValue;
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
            return returnValue;
        }

        internal static string GetUniqueInitialCatalogName(string projectName)
        {
            //Initial Catalog=aspnet-{ProjectName}-{Timestamp}
            //Where {Timestamp} is yyyyMMddhhmmss
            //Where  if ProjectName is of the form http://localhost/WebSite1/ we will use "WebSite1" as ProjectName
            // Similarly for FTP Projects

            string siteName = projectName;
            if (projectName.Contains("://"))
            {
                string[] urlSplit = projectName.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                if (urlSplit[urlSplit.Length - 1] != null)
                {
                    siteName = urlSplit[urlSplit.Length - 1];
                }
            }

            string catalogName = "aspnet-" + siteName + "-" + DateTime.Now.ToString("yyyyMMddhhmmss");
            return catalogName;
        }

        internal static bool GetLocalDBIsDefault(IServiceProvider serviceProvider)
        {
            bool defaultValue = true;
            string localreg = GetLocalRegRoot(serviceProvider);
            if (!string.IsNullOrEmpty(localreg))
            {
                string regkey = "HKEY_CURRENT_USER\\" + EnsureTrailingBackSlash(localreg) + "WebProjects";

                return GetRegValue<bool>(serviceProvider, regkey, "LocalDBIsDefault", defaultValue);
            }
            return defaultValue;
        }

        private static string GetLocalRegRoot(IServiceProvider serviceProvider)
        {
            IVsShell vsShell = TemplateUtilities.GetService<IVsShell, IVsShell>(serviceProvider);
            Object obj;
            vsShell.GetProperty((int)__VSSPROPID.VSSPROPID_VirtualRegistryRoot, out obj);
            if (obj != null && obj is string)
            {
                return (string)obj;
            }
            return null;
        }

        private static string EnsureTrailingBackSlash(string str)
        {
            if (str != null && !str.EndsWith("\\", StringComparison.Ordinal))
            {
                str += "\\";
            }
            return str;
        }

        private static ValueType GetRegValue<ValueType>(IServiceProvider serviceProvider, string key, string valueName, ValueType defaultValue)
        {
            ValueType returnValue = defaultValue;
            if (!string.IsNullOrEmpty(key))
            {
                try
                {
                    object objValue = Registry.GetValue(key, valueName, (object)defaultValue);
                    if (objValue != null)
                    {
                        object objConvert = Convert.ChangeType(objValue, typeof(ValueType), CultureInfo.InvariantCulture);
                        if (objConvert != null)
                        {
                            returnValue = (ValueType)objConvert;
                        }
                    }
                }
                catch
                {
                }
            }
            return returnValue;
        }
    }
}
#endif