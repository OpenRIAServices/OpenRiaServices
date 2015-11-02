using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Hosting;

namespace TestDomainServices.Testing
{
    /// <summary>
    /// Class used to create database copies for use in testing
    /// </summary>
    public static class DBImager
    {
        #region GetSettings

        /// <summary>
        /// Gets the path to the read-only database template file.
        /// </summary>
        /// <example>
        ///     GetDBFileTemplatePath("Northwind.mdf")
        /// </example>
        /// <param name="fileName">Name of the file.</param>
        /// <returns></returns>
        private static string GetDBFileTemplatePath(string fileName)
        {
            return GetDBFileCore(@"~/App_Data/Templates/" + fileName);
        }

        private static string GetDBFile(string fileName)
        {
            return GetDBFileCore(@"~/App_Data/" + fileName);
        }


        private static string GetDBFileCore(string fileName)
        {
            if (File.Exists(fileName))
                return Path.Combine(Directory.GetCurrentDirectory(), fileName);

            // If path is not relative to working directory then check if it is a virtual path
            var mappedPath = HostingEnvironment.MapPath(fileName);
            if (File.Exists(mappedPath))
                return mappedPath;

            return string.Empty;
        }

        #endregion GetSettings

        #region Database Methods

        /// <summary>
        /// Get Connection String (with default dbSrvLang and dbProvider)
        /// </summary>
        /// <param name="DatabaseName">Name of the database (e.g. Northwind)</param>     
        /// <returns></returns>
        public static string GetConnectionString(string dbName)
        {
            return GetConnectionString(dbName, GetDefaultDBProviderName());
        }

        public static string GetConnectionString(string dbName, string dbProvider)
        {
            List<DictionaryEntry> Ls = new List<DictionaryEntry>();
            Ls.Add(new DictionaryEntry("DbProvider", dbProvider));
            return GetConnectionString(dbName, Ls);
        }
        /// <summary>
        /// Get Connection string
        /// </summary>
        /// <param name="DatabaseName">Name of the database (e.g. Northwind) </param>
        /// <param name="databaseType">Provider: SQL2000, SQL2005, DatabaseFile (Read from DomainServices.xml)</param>
        /// <param name="accessMode">Static Readonly database or dynamic created modifable database (Read from DomainServices.xml)</param>     
        /// <returns></returns>
        public static string GetConnectionString(string dbName, List<DictionaryEntry> Ls)
        {
            string dbProvider = GetDBProvider(Ls);
            StringBuilder sb = new StringBuilder(@"");
            switch (dbProvider)
            {
                case "DatabaseFileD": // DYMMY
                    // TODO: Remove this
                    return GetConnectionStringForDatabaseFile(GetDBFile(dbName + ".mdf"));
                //return @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=Northwind;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=True;";

                case "DatabaseFile":
                    string DestMDF = CreateDatabaseFile(dbName, overwrite: true);
                    return GetConnectionStringForDatabaseFile(DestMDF);

                case "DatabaseFile_ORIG":
                //string GUID = Guid.NewGuid().ToString();
                //string SourceMDF = GetDBFile(dbName + ".mdf");
                //string SourceLDF = GetDBFile(dbName + "_log.ldf");
                //string DestMDF = Path.Combine(Directory.GetCurrentDirectory(), dbName + GUID + ".mdf");

                //// If file is not exist, copy it to the local folder
                //if (!File.Exists(DestMDF))
                //{
                //    File.Copy(SourceMDF, DestMDF);
                //    File.SetAttributes(DestMDF, FileAttributes.Normal);
                //    File.Copy(SourceLDF, Path.Combine(Directory.GetCurrentDirectory(), dbName + GUID + "_log.ldf"));
                //    File.SetAttributes(Path.Combine(Directory.GetCurrentDirectory(), dbName + GUID + "_log.ldf"), FileAttributes.Normal);
                //}

                //return GetConnectionStringForDatabaseFile(DestMDF);
                default:
                    throw new NotImplementedException();
            }
        }


        private static string CreateDatabaseFile(string dbName, bool overwrite)
        {
            string SourceMDF = GetDBFileTemplatePath(dbName + ".mdf");
            string SourceLDF = GetDBFileTemplatePath(dbName + ".ldf");
            string DestMDF = HttpContext.Current.Server.MapPath("~/App_Data/" + dbName + ".mdf");
            string DestLDF = DestMDF.Replace(".mdf", ".ldf");

            // If file is not exist, copy it to the local folder
            if (overwrite || !File.Exists(DestMDF))
            {
                File.Copy(SourceMDF, DestMDF, overwrite);
                File.SetAttributes(DestMDF, FileAttributes.Normal);
                File.Copy(SourceLDF, DestLDF, overwrite);
                File.SetAttributes(DestLDF, FileAttributes.Normal);
            }

            return DestMDF;
        }

        // TODO: Allow reading from env and app settings
        internal static string LocalSqlServer => "(localdb)\\MSSQLLocalDB";

        private static string GetConnectionStringForDatabaseFile(string ConStr)
        {
            //if (ConStr.Trim().EndsWith(".mdf"))
            //{
            string catalogName = GetDbCatalogName(Path.GetFileNameWithoutExtension(ConStr));
            ConStr = $"Data Source={LocalSqlServer};Initial Catalog={catalogName};AttachDbFilename={ConStr};Integrated Security=True;Connect Timeout=30";
            //}

            //ConStr = $"Data Source=.;AttachDbFilename={ConStr};Integrated Security=True;Connect Timeout=30";
            return ConStr;
        }

        private static string GetDbCatalogName(string dbName)
        {
            return $"{dbName}_ATTACHED";
        }

        /// <summary>
        /// Clean up the database (Using default DBProvider and DBSrvLang)
        /// </summary>
        /// <param name="dbName">e.g. NorthWind</param>
        public static void CleanDB(string dbName)
        {
            CleanDB(dbName, GetDefaultDBProviderName());
        }

        private static void CleanDB(string dbName, string dbProvider)
        {
            List<DictionaryEntry> Ls = new List<DictionaryEntry>();
            Ls.Add(new DictionaryEntry("DbProvider", dbProvider));
            CleanDB(dbName, Ls);
        }

        private static void CleanDB(string dbName, List<DictionaryEntry> Ls)
        {
            string catalogName = GetDbCatalogName(dbName);
            string dbProvider = GetDBProvider(Ls);

            try
            {
                // If it is Database File, we don't bother cleaning since we will overwrite it the next time
                if (dbProvider == "DatabaseFile")
                {
                    using (var sqlCLient = new SqlConnection($"Data Source={LocalSqlServer};Initial Catalog=master;Integrated Security=True;Connect Timeout=10"))
                    {
                        sqlCLient.Open();
                        using (var cmd = sqlCLient.CreateCommand())
                        {
                            // Force disconnects for other users
                            cmd.CommandText = $"ALTER DATABASE [{catalogName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE";
                            cmd.ExecuteNonQuery();

                            cmd.CommandText = $"EXEC sp_detach_db '{catalogName}', 'true';";
                            //cmd.CommandType = CommandType.Text;
                            cmd.ExecuteNonQuery();

                        }
                    }

                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            catch (Exception ex)
            {
                // should this be an assert?
                Console.WriteLine("Failed to remove database : " + ex.Message);
                throw;
            }
        }

        private static string GetDBProvider(List<DictionaryEntry> Ls)
        {
            string dbProvider = null;
            foreach (DictionaryEntry DE in Ls)
            {
                switch (DE.Key.ToString())
                {
                    case "DbProvider":
                        dbProvider = DE.Value.ToString();
                        break;
                }
            }
            return dbProvider;
        }

        private static string GetDefaultDBProviderName()
        {
            // if Environment variable DB provider is defined, default DB provider == %DBProvider%
            // Default DB Provider is SQL2005
            return SafeGetEnvironmentVariableOrDefault("DbProvider", "DatabaseFile");
        }

        #endregion Database Methods

        private static string SafeGetEnvironmentVariableOrDefault(string variable, string defaultValue)
        {
            try
            {
                string value = Environment.GetEnvironmentVariable(variable);
                if (!String.IsNullOrEmpty(value))
                {
                    defaultValue = value;
                }
            }
            catch (System.Security.SecurityException)
            {
            }

            return defaultValue;
        }
    }

    /// <summary>
    /// Static cache of active database connection strings. This cache can be used by DomainServices
    /// to provide connection durablility across provider requests.
    /// </summary>
    public static class ActiveConnections
    {
        private static Dictionary<string, string> activeConnections = new Dictionary<string, string>();

        static ActiveConnections()
        {
            Load();
        }

        public static string Get(string databaseName)
        {
            System.Diagnostics.Debugger.Break();
            string connection = null;
            activeConnections.TryGetValue(databaseName, out connection);
            return connection;
        }

        public static void Set(string databaseName, string connnectionString)
        {
            if (connnectionString != null)
            {
                activeConnections[databaseName] = connnectionString;
            }
            else
            {
                activeConnections.Remove(databaseName);
            }
            Save();
        }

        private static void Load()
        {
            string path = HttpContext.Current.Server.MapPath("~/activeconnections.txt");
            if (File.Exists(path))
            {
                foreach (string line in File.ReadAllLines(path).Where(p => !string.IsNullOrEmpty(p)))
                {
                    string[] tokens = line.Split('\0');
                    activeConnections.Add(tokens[0], tokens[1]);
                }
            }
        }

        private static void Save()
        {
            string[] lines = activeConnections.Select(kvp => kvp.Key + "\0" + kvp.Value).ToArray();
            File.WriteAllLines(HttpContext.Current.Server.MapPath("~/activeconnections.txt"), lines);
        }
    }
}
