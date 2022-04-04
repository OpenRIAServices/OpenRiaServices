using System;
using System.Data.SqlClient;
using System.IO;
#if !NET6_0
using System.Web.Hosting;
#endif

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

        private static string GetDBFileCore(string fileName)
        {
            if (File.Exists(fileName))
                return Path.Combine(Directory.GetCurrentDirectory(), fileName);

            // If path is not relative to working directory then check if it is a virtual path
#if NET6_0
            string mappedPath = fileName.Replace("~/", "../WebsiteFullTrust/");

#else
            var mappedPath = HostingEnvironment.MapPath(fileName);
#endif
            if (File.Exists(mappedPath))
                return mappedPath;

            return string.Empty;

        }

        #endregion GetSettings

        #region Database Methods

        /// <summary>
        /// Creates a new temporary database and returns Connection String
        /// </summary>
        /// <param name="dbName">Name of the database (e.g. Northwind)</param>     
        /// <returns>connection string used to connect to database</returns>
        public static string CreateNewDatabase(string dbName)
        {
            string DestMDF = CreateDatabaseFile(dbName, overwrite: true);
            return GetConnectionStringForDatabaseFile(DestMDF);
        }

        /// <summary>
        /// Get connectionstring to the temporary created read/write database
        /// </summary>
        /// <param name="dbName"></param>
        /// <returns></returns>
        public static string GetNewDatabaseConnectionString(string dbName)
        {
            string DestMDF = GetTempDbFilePath(dbName, ".mdf");
            return GetConnectionStringForDatabaseFile(DestMDF);
        }

        private static string CreateDatabaseFile(string dbName, bool overwrite)
        {
            string SourceMDF = GetDBFileTemplatePath(dbName + ".mdf");
            string SourceLDF = GetDBFileTemplatePath(dbName + ".ldf");

            string DestMDF = GetTempDbFilePath(dbName, ".mdf");
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


        /// <summary>
        /// Get the path to the temp db file (the copy created) by 
        /// <see cref="CreateDatabaseFile(string, bool)"/>
        /// </summary>
        /// <param name="dbName">database file name without extension</param>
        /// <param name="extension">eg ".mdf" or ".ldf"</param>
        /// <returns>full path to use for temporary db file</returns>
        private static string GetTempDbFilePath(string dbName, string extension)
        {
            return Path.Combine(Environment.GetEnvironmentVariable("TEMP"), dbName + extension);
        }

        // TODO: Allow reading from env and app settings
        internal static string LocalSqlServer => "(localdb)\\MSSQLLocalDB";

        private static string GetConnectionStringForDatabaseFile(string path)
        {
            string catalogName = GetDbCatalogName(Path.GetFileNameWithoutExtension(path));
            return $"Data Source={LocalSqlServer};Initial Catalog={catalogName};AttachDbFilename={path};Integrated Security=True;Connect Timeout=5";
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
            string catalogName = GetDbCatalogName(dbName);

            try
            {
                // Detach database and then delete the file
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

                string DestMDF = GetTempDbFilePath(dbName, ".mdf");
                string DestLDF = DestMDF.Replace(".mdf", ".ldf");
                File.Delete(DestMDF);
                File.Delete(DestLDF);
            }
            catch (Exception ex)
            {
                // should this be an assert?
                Console.WriteLine("Failed to remove database : " + ex.Message);
                throw;
            }
        }


        #endregion Database Methods

    }

}
