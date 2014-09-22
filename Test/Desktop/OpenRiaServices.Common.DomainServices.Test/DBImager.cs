using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;

namespace TestDomainServices.Testing
{
    /// <summary>
    /// Class used to create database copies for use in testing
    /// </summary>
    public static class DBImager
    {
        /// <summary>
        /// Readonly -- Return Readonly database
        /// New -- Create a new database 
        /// Static -- Return a static database
        /// </summary>
        public enum AccessMode
        {
            Readonly = 0,
            New = 1,
            NewNoCreate = 5,
            Static = 2,
            Empty = 3,
            EmptyAndCreate = 4
        }

        #region GetSettings
        private static string GetAssemblyDirectory()
        {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetAssembly(typeof(DBImager));
            return new System.IO.FileInfo(assembly.Location).DirectoryName;
        }
        
        private static string GetDBFile(string fileName)
        {
            if (File.Exists(fileName))
                return Path.Combine(Directory.GetCurrentDirectory(), fileName);

            return string.Empty;
        }

        public static string GetServerPath(TestDatabaseProviderInfo provider, string fileName)
        {
            return Path.Combine(provider.DataFilePath, fileName);
        }

        /// <summary>
        /// The default New Database name is %DbName%_%ComputerName% 
        /// DB names don't include process id, only computer name.  This was removed so we don't 
        /// get a bunch of littered DBs on the servers in the case of abnormal termination.  At most, 
        /// we would have one per machine name, since each time a new DB is created for a machine, if 
        /// the DB already exists, it is overwritten.
        /// </summary>
        public static string GetNewDBName(string dbName)
        {
            return dbName + "_" + Environment.MachineName.Replace("-", "");
        }

        /// <summary>
        /// Read the Server Name from the DomainServices.xml file
        /// </summary>
        /// <param name="dbProvider">e.g. SQL2000, SQL2005</param>
        /// <param name="dbLang">e.g. English, Japanese</param>
        /// <returns></returns>
        public static string GetServerLocation(TestDatabaseProviderInfo dbProvider, string dbSrvLang)
        {
            return GetServerLocation(dbProvider.Name, dbSrvLang);
        }

        /// <summary>
        /// Get the Server Name
        /// </summary>
        /// <param name="dbProvider">e.g. SQL2000, SQL2005, filename</param>
        /// <param name="dbLang">e.g. English, Japanese</param>
        /// <returns></returns>
        public static string GetServerLocation(string dbProviderName, string dbSrvLang)
        {
            return GetDefaultServerLocation(dbProviderName);
        }

        public static string GetDefaultServerLocation(TestDatabaseProviderInfo dbProvider)
        {
            return dbProvider.Location;
        }

        public static string GetDefaultServerLocation(string providerName)
        {
            return TestDatabaseProviders.GetProvider(providerName).Location;
        }

        #endregion GetSettings

        #region Database Methods

        public static string GetServerConnectionString(TestDatabaseProviderInfo provider)
        {
            return String.Format(@"Server={0};{1}", provider.Location, provider.Credentials);
        }

        /// <summary>
        /// Get the Server Credentials
        /// </summary>
        /// <param name="dbProvider">e.g. SQL2000, SQL2005</param>
        /// <param name="dbLang">e.g. English, Japanese</param>
        /// <returns></returns>
        public static string GetServerCredentials(TestDatabaseProviderInfo dbProvider, string dbSrvLang)
        {
            return GetServerCredentials(dbProvider.Name, dbSrvLang);
        }

        /// <summary>
        /// Get the Server Credentials
        /// </summary>
        /// <param name="dbProvider">e.g. SQL2000, SQL2005, filename</param>
        /// <param name="dbLang">e.g. English, Japanese</param>
        /// <returns></returns>
        public static string GetServerCredentials(string dbProviderName, string dbSrvLang)
        {
            return GetDefaultServerCredentials(dbProviderName);
        }

        public static string GetDefaultServerCredentials(string providerName)
        {
            return TestDatabaseProviders.GetProvider(providerName).Credentials;
        }

        public static string GetDefaultServerCredentials(TestDatabaseProviderInfo provider)
        {
            return provider.Credentials;
        }

        /// <summary>
        /// Get the Server DataFilePath
        /// </summary>
        /// <param name="dbProvider">e.g. SQL2000, SQL2005</param>
        /// <param name="dbLang">e.g. English, Japanese</param>
        /// <returns></returns>
        public static string GetServerDataFilePath(TestDatabaseProviderInfo dbProvider, string dbSrvLang)
        {
            return GetServerDataFilePath(dbProvider.Name, dbSrvLang);
        }

        /// <summary>
        /// Get the Server DataFilePath
        /// </summary>
        /// <param name="dbProvider">e.g. SQL2000, SQL2005, filename</param>
        /// <param name="dbLang">e.g. English, Japanese</param>
        /// <returns></returns>
        public static string GetServerDataFilePath(string dbProviderName, string dbSrvLang)
        {
            return GetDefaultServerDataFilePath(dbProviderName);
        }

        public static string GetDefaultServerDataFilePath(string providerName)
        {
            return TestDatabaseProviders.GetProvider(providerName).DataFilePath;
        }

        public static string GetDefaultServerDataFilePath(TestDatabaseProviderInfo provider)
        {
            return provider.DataFilePath;
        }

        /// <summary>
        /// Get Connection String (with default dbSrvLang and dbProvider)
        /// </summary>
        /// <param name="DatabaseName">Name of the database (e.g. Northwind)</param>        
        /// <param name="accessMode">Static Readonly database or dynamic created modifable database</param>     
        /// <returns></returns>
        public static string GetConnectionString(string dbName, AccessMode accessMode)
        {
            return GetConnectionString(dbName, GetDefaultDBProvider(), GetDefaultDBSrvLang(), GetDefaultDBSrvType(), GetDefaultDBSQLSrvLang(), GetDefaultDBSrvOS(), accessMode);
        }

        public static string GetConnectionString(string dbName, TestDatabaseProviderInfo dbProvider, AccessMode accessMode)
        {
            return GetConnectionString(dbName, dbProvider, null, null, null, null, accessMode);
        }


        public static string GetConnectionString(string dbName, TestDatabaseProviderInfo dbProvider, string dbSrvLang, string dbSrvType, string dbSQLSrvLang, string dbSrvOS, AccessMode accessMode)
        {
            List<DictionaryEntry> Ls = new List<DictionaryEntry>();
            Ls.Add(new DictionaryEntry("DbProvider", dbProvider.Name));
            Ls.Add(new DictionaryEntry("DbSrvLang", dbSrvLang));
            Ls.Add(new DictionaryEntry("DbSQLSrvLang", dbSQLSrvLang));
            Ls.Add(new DictionaryEntry("DbSrvOS", dbSrvOS));
            Ls.Add(new DictionaryEntry("DbSrvType", dbSrvType));
            return GetConnectionString(dbName, Ls, accessMode);
        }
        /// <summary>
        /// Get Connection string
        /// </summary>
        /// <param name="DatabaseName">Name of the database (e.g. Northwind) </param>
        /// <param name="databaseType">Provider: SQL2000, SQL2005, DatabaseFile (Read from DomainServices.xml)</param>
        /// <param name="accessMode">Static Readonly database or dynamic created modifable database (Read from DomainServices.xml)</param>     
        /// <returns></returns>
        public static string GetConnectionString(string dbName, List<DictionaryEntry> Ls, AccessMode accessMode)
        {
            string dbProvider = GetDBProvider(Ls);

            if (accessMode == AccessMode.Readonly)
            {
                return GetConnectionStringForDatabaseFile(GetConnectionStringForReadOnlyDatabase(dbName, Ls));
            }
            if (accessMode == AccessMode.Static)
            {
                // There are complications on handling DBFile (It is Read-Only after it got sync from Source Depot 
                // For more information -- http://www.sqlteam.com/forums/topic.asp?TOPIC_ID=5894
                // Work around -- copy it to the current directory
                switch (dbProvider)
                {
                    case "DatabaseFile":
                        return GetConnectionString(dbName, Ls, AccessMode.New);
                    default:
                        return GetConnectionStringForDatabaseFile(GetConnectionStringForReadOnlyDatabase(dbName + "_Static", Ls));
                }
            }

            if (accessMode == AccessMode.Empty)
            {
                string Empty_DBName = GetEmptyDBName(dbName);
                DeleteDatabase(Empty_DBName, Ls);

                return DBImager.GetConnectionString(Empty_DBName, Ls, DBImager.AccessMode.Readonly);
            }

            if (accessMode == AccessMode.EmptyAndCreate)
            {
                string Empty_DBName = GetEmptyDBName(dbName);
                DeleteDatabase(Empty_DBName, Ls);
                using (SqlConnection sqlcon = new SqlConnection(GetAdminConnectionString("Master", Ls)))
                {
                    SqlCommand sqlcom = new SqlCommand("CREATE DATABASE [" + Empty_DBName + "]", sqlcon);
                    sqlcom.Connection.Open();
                    sqlcom.ExecuteNonQuery();
                    sqlcom.Connection.Close();
                }

                return DBImager.GetConnectionString(Empty_DBName, Ls, DBImager.AccessMode.Readonly);
            }
            //if (accessMode == AccessMode.New)
            //{

            //    StringBuilder sb = new StringBuilder(@"");
            //    switch (dbProvider)
            //    {
            //        case "DatabaseFile":
            //            string GUID = Guid.NewGuid().ToString();
            //            string SourceMDF = GetDBFile(dbName + ".mdf");
            //            string SourceLDF = GetDBFile(dbName + "_log.ldf");
            //            string DestMDF = Path.Combine(Directory.GetCurrentDirectory(), dbName + GUID + ".mdf");

            //            // If file is not exist, copy it to the local folder
            //            if (!File.Exists(DestMDF))
            //            {
            //                File.Copy(SourceMDF, DestMDF);
            //                File.SetAttributes(DestMDF, FileAttributes.Normal);
            //                File.Copy(SourceLDF, Path.Combine(Directory.GetCurrentDirectory(), dbName + GUID + "_log.ldf"));
            //                File.SetAttributes(Path.Combine(Directory.GetCurrentDirectory(), dbName + GUID + "_log.ldf"), FileAttributes.Normal);
            //            }

            //            return GetConnectionStringForDatabaseFile(DestMDF);
            //        default:
            //            return CreateDB(dbName, Ls);
            //    }


            //}
            if (accessMode == AccessMode.NewNoCreate || accessMode == AccessMode.New)
            {

                StringBuilder sb = new StringBuilder(@"");
                switch (dbProvider)
                {
                    case "DatabaseFile":
                        Guid GUID = Guid.NewGuid();
                        string DestMDF = Path.Combine(Directory.GetCurrentDirectory(), String.Format("{0}{1}.mdf", dbName, GUID));
                        return GetConnectionStringForDatabaseFile(DestMDF);
                    default:
                        return GetDBConnectionStringNoCreate(dbName, Ls);
                }


            }
            throw new Exception("Not a recognized access type");
        }

        internal static string GetEmptyDBName(string dBName)
        {
            return DBImager.GetNewDBName("Empty_" + dBName);
        }

        private static string GetConnectionStringForDatabaseFile(string ConStr)
        {
            if (ConStr.Trim().EndsWith(".mdf"))
            {
                ConStr = "Data Source=.\\SQLEXPRESS;AttachDbFilename=\"" + ConStr + "\";Integrated Security=True;Connect Timeout=30";
            }
            return ConStr;
        }

        public static string GetConnectionStringForReadOnlyDatabase(string dbName)
        {
            return GetConnectionStringForReadOnlyDatabase(dbName, GetDefaultDBProvider(), GetDefaultDBSrvLang(), GetDefaultDBSrvType(), GetDefaultDBSQLSrvLang(), GetDefaultDBSrvOS());
        }

        /// <summary>
        /// Clean up the database
        /// </summary>
        /// <param name="dbName">e.g NorthWind</param>
        /// <param name="dbProvider">e.g. SQL2000, SQL2005 or DatabaseFile (Read from DlinqServers.xml)</param>
        /// <param name="dbSrvLang">e.g. English, Japanese (Read from DlinqServers.xml)</param>
        /// <param name="dbSrvType">e.g. AMD64, x86, IA64, etc</param>
        /// <param name="dbSrvOS">e.g. Win2003, WinXP, etc</param>
        public static string GetConnectionStringForReadOnlyDatabase(string dbName, TestDatabaseProviderInfo dbProvider, string dbSrvLang, string dbSrvType, string dbSQLSrvLang, string dbSrvOS)
        {
            List<DictionaryEntry> Ls = new List<DictionaryEntry>();
            Ls.Add(new DictionaryEntry("DbProvider", dbProvider.Name));
            Ls.Add(new DictionaryEntry("DbSrvLang", dbSrvLang));
            Ls.Add(new DictionaryEntry("DbSQLSrvLang", dbSQLSrvLang));
            Ls.Add(new DictionaryEntry("DbSrvOS", dbSrvOS));
            Ls.Add(new DictionaryEntry("DbSrvType", dbSrvType));
            return GetConnectionStringForReadOnlyDatabase(dbName, Ls);
        }

        private static string GetConnectionStringForReadOnlyDatabase(string dbName, List<DictionaryEntry> Ls)
        {
            string dbProvider = GetDBProvider(Ls);
            StringBuilder sb = new StringBuilder(@"");
            switch (dbProvider)
            {
                case "DatabaseFile":

                    //Return the readonly mdf file 
                    sb.Append(GetDBFile(dbName + ".mdf"));
                    break;
                default:
                    sb.Append(GetAdminConnectionString(dbName, Ls));
                    break;
            }
            return sb.ToString();
        }


        public static void DeleteDatabase(string dbName, List<DictionaryEntry> Ls)
        {
            using (SqlConnection sqlcon = new SqlConnection(GetAdminConnectionString("Master", Ls)))
            {
                SqlCommand sqlcom = new SqlCommand("dbi_DeleteDatabase", sqlcon);
                sqlcom.CommandType = System.Data.CommandType.StoredProcedure;
                sqlcom.Parameters.AddWithValue("@DBName", dbName);
                sqlcom.Connection.Open();
                sqlcom.ExecuteNonQuery();
                sqlcom.Connection.Close();
                SqlConnection.ClearAllPools();
            }
        }

        public static void DeleteDatabase(string dbName, TestDatabaseProviderInfo provider)
        {
            using (SqlConnection sqlcon = new SqlConnection(GetAdminConnectionString("Master", provider)))
            {
                SqlCommand sqlcom = new SqlCommand("dbi_DeleteDatabase", sqlcon);
                sqlcom.CommandType = System.Data.CommandType.StoredProcedure;
                sqlcom.Parameters.AddWithValue("@DBName", dbName);
                sqlcom.Connection.Open();
                sqlcom.ExecuteNonQuery();
                sqlcom.Connection.Close();
                SqlConnection.ClearAllPools();
            }
        }

        private static string CreateDB(string dbName, List<DictionaryEntry> Ls)
        {
            try
            {
                string NewDBName = GetNewDBName(dbName);
                string UserName = "TestUser_" + Environment.MachineName.Replace("-", "");
                using (SqlConnection sqlcon = new SqlConnection(GetAdminConnectionString("Master", Ls)))
                {
                    SqlCommand sqlcom = new SqlCommand("dbi_AddNewDatabase", sqlcon);
                    sqlcom.CommandTimeout = 600;
                    sqlcom.Parameters.AddWithValue("@BaseDBName", dbName);
                    sqlcom.Parameters.AddWithValue("@NewDBName", NewDBName);
                    sqlcom.Parameters.AddWithValue("@UserName", UserName);
                    sqlcom.CommandType = CommandType.StoredProcedure;
                    sqlcom.Connection.Open();
                    sqlcom.ExecuteNonQuery();
                    sqlcom.Connection.Close();
                    SqlConnection.ClearAllPools();
                    return "Server=" + GetServerLocation(Ls) + ";Database=" + NewDBName + ";User=" + UserName + ";Password=" + UserName;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        private static string GetDBConnectionStringNoCreate(string dbName, List<DictionaryEntry> Ls)
        {
            try
            {
                string NewDBName = GetNewDBName(dbName);
                return "Server=" + GetServerLocation(Ls) + ";Database=" + NewDBName + ";" + TestDatabaseProviders.GetProvider(GetDBProvider(Ls)).Credentials;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        private static string GetAdminConnectionString(string dbName, List<DictionaryEntry> Ls)
        {
            return "Server=" + GetServerLocation(Ls) + ";Database=" + dbName + ";User=RiaTest;Password=TestPassword";
        }

        private static string GetAdminConnectionString(string dbName, TestDatabaseProviderInfo provider)
        {
            return "Server=" + provider.Location + ";Database=" + dbName + ";User=RiaTest;Password=TestPassword";
        }

        /// <summary>
        /// Clean up the database (Using default DBProvider and DBSrvLang)
        /// </summary>
        /// <param name="dbName">e.g. NorthWind</param>
        public static void CleanDB(string dbName)
        {
            CleanDB(dbName, GetDefaultDBProvider(), GetDefaultDBSrvLang(), GetDefaultDBSrvType(), GetDefaultDBSQLSrvLang(), GetDefaultDBSrvOS());
        }

        /// <summary>
        /// Clean up the database
        /// </summary>
        /// <param name="dbName">e.g NorthWind</param>
        /// <param name="dbProvider">e.g. TestDatabaseProviders.SQL2000, TestDatabaseProviders.SQL2005</param>
        /// <param name="dbSrvLang">e.g. English, Japanese (Read from DlinqServers.xml)</param>
        /// <param name="dbSrvType">e.g. AMD64, x86, IA64, etc</param>
        /// <param name="dbSrvOS">e.g. Win2003, WinXP, etc</param>
        public static void CleanDB(string dbName, TestDatabaseProviderInfo dbProvider, string dbSrvLang, string dbSrvType, string dbSQLSrvLang, string dbSrvOS)
        {
            List<DictionaryEntry> Ls = new List<DictionaryEntry>();
            Ls.Add(new DictionaryEntry("DbProvider", dbProvider.Name));
            Ls.Add(new DictionaryEntry("DbSrvLang", dbSrvLang));
            Ls.Add(new DictionaryEntry("DbSQLSrvLang", dbSQLSrvLang));
            Ls.Add(new DictionaryEntry("DbSrvOS", dbSrvOS));
            Ls.Add(new DictionaryEntry("DbSrvType", dbSrvType));
            CleanDB(dbName, Ls);
        }

        /// <summary>
        /// Clean up the database
        /// </summary>
        /// <param name="dbName">e.g NorthWind</param>
        /// <param name="dbProvider">e.g. DatabaseFile (Read from DlinqServers.xml)</param>
        /// <param name="dbSrvLang">e.g. English, Japanese (Read from DlinqServers.xml)</param>
        /// <param name="dbSrvType">e.g. AMD64, x86, IA64, etc</param>
        /// <param name="dbSrvOS">e.g. Win2003, WinXP, etc</param>
        public static void CleanDB(string dbName, string file, string dbSrvLang, string dbSrvType, string dbSQLSrvLang, string dbSrvOS)
        {
            List<DictionaryEntry> Ls = new List<DictionaryEntry>();
            Ls.Add(new DictionaryEntry("DbProvider", file));
            Ls.Add(new DictionaryEntry("DbSrvLang", dbSrvLang));
            Ls.Add(new DictionaryEntry("DbSQLSrvLang", dbSQLSrvLang));
            Ls.Add(new DictionaryEntry("DbSrvOS", dbSrvOS));
            Ls.Add(new DictionaryEntry("DbSrvType", dbSrvType));
            CleanDB(dbName, Ls);
        }

        public static void CleanDB(string dbName, List<DictionaryEntry> Ls)
        {
            dbName = GetNewDBName(dbName);
            string dbProvider = GetDBProvider(Ls);

            //If it is Database File, we got nothing to clean. 
            if (dbProvider == "DatabaseFile")
                return;
            try
            {
                DeleteDatabase(dbName, Ls);
            }
            catch (Exception ex)
            {
                // should this be an assert?
                Console.WriteLine("Failed to remove database : " + ex.Message);
            }
        }

        public static void CleanDB(string dbName, TestDatabaseProviderInfo provider)
        {
            dbName = GetNewDBName(dbName);

            try
            {
                DeleteDatabase(dbName, provider);
            }
            catch (Exception ex)
            {
                // should this be an assert?
                Console.WriteLine("Failed to remove database : " + ex.Message);
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

        private static string GetDefaultDBSrvLang()
        {
            // If Environment variable Database Language is defined, default DB Srv Lang == %DatabaseLanguage% 
            // Default DB Srv Lang is English
            return SafeGetEnvironmentVariableOrDefault("DbSrvLang", "English");
        }

        private static TestDatabaseProviderInfo GetDefaultDBProvider()
        {
            // if Environment variable DB provider is defined, default DB provider == %DBProvider%
            // Default DB Provider is SQL2005
            string result = SafeGetEnvironmentVariableOrDefault("DbProvider", "SQL2005");

            return TestDatabaseProviders.GetProvider(result);
        }

        private static string GetDefaultDBSrvType()
        {
            // if Environment variable DB Srv Type is defined, default DB Srv Type == %DBSrvType%
            // Default DB Server Type is x86
            return SafeGetEnvironmentVariableOrDefault("DbSrvType", null);
        }

        private static string GetDefaultDBSQLSrvLang()
        {
            // if Environment variable DB SQLSrvLang is defined, default DB SQLSrvLang == %dbSQLSrvLang%
            // Default DB SQLSrvLang is English
            return SafeGetEnvironmentVariableOrDefault("dbSQLSrvLang", null);
        }

        private static string GetDefaultDBSrvOS()
        {
            // if Environment variable DB SrvOS is defined, default DB SrvOS == %SrvOS%
            // Default DB SrvOS is Win2003
            return SafeGetEnvironmentVariableOrDefault("dbSrvOS", null);
        }

        private static string GetServerLocation(List<DictionaryEntry> Ls)
        {
            string provider = GetDBProvider(Ls);
            string serverLocation = GetDefaultServerLocation(TestDatabaseProviders.GetProvider(provider));
            return serverLocation;
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

    /// <summary>
    /// Central class to provide test database server information.
    /// </summary>   
    public static class TestDatabaseProviders
    {
        public static readonly TestDatabaseProviderInfo SQL2005 = new TestDatabaseProviderInfo
        {
            Name = "SQL2005",
            Location = @".\mssql2012",
            Credentials = @"User=RiaTest;Password=TestPassword",
            DataFilePath = @"D:\Data\"
        };

        public static TestDatabaseProviderInfo GetProvider(string name)
        {
            switch (name)
            {
                case ("SQL2005"):
                    return SQL2005;
                default:
                    throw new Exception("Unknown database provider: " + name);
            }
        }
    }

    public class TestDatabaseProviderInfo
    {
        private string name;

        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                name = value;
            }
        }
        private string location;

        public string Location
        {
            get
            {
                return location;
            }
            set
            {
                location = value;
            }
        }
        private string credentials;

        public string Credentials
        {
            get
            {
                return credentials;
            }
            set
            {
                credentials = value;
            }
        }
        private string dataFilePath;

        public string DataFilePath
        {
            get
            {
                return dataFilePath;
            }
            set
            {
                dataFilePath = value;
            }
        }
    }
}
