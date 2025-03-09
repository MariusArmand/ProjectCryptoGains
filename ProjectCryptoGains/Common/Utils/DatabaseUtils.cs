using FirebirdSql.Data.FirebirdClient;
using System;
using System.Data.Common;
using System.IO;

namespace ProjectCryptoGains.Common.Utils
{
    public static class DatabaseUtils
    {
        public static readonly string databaseFileName = "pcg.fdb";
        public static readonly string databasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "db", databaseFileName);
        public static readonly string fbClientLibraryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "db", "fbclient.dll");
        public static readonly string connectionString = GetConnectionString();

        public static string GetConnectionString()
        {
            var csb = new FbConnectionStringBuilder
            {
                ServerType = FbServerType.Embedded,
                ClientLibrary = fbClientLibraryPath,
                Database = databasePath,
                UserID = "SYSDBA",
                Password = "masterkey"
            };
            return csb.ToString();
        }

        public static string TestDatabaseConnection()
        {
            // Check if fbclient.dll exists (critical for Firebird Embedded)
            if (!File.Exists(fbClientLibraryPath))
            {
                return $"Firebird client library not found at {fbClientLibraryPath}";
            }

            // Check if the database file exists
            if (!File.Exists(databasePath))
            {
                return "Database file does not exist";
            }

            // Attempt to connect
            using (FbConnection connection = new FbConnection(connectionString))
            {
                try
                {
                    connection.Open(); // No need to close connection, since it'll get closed automatically because we are relying on "using"
                    return "Database connection successful";
                }
                catch (FbException fbEx)
                {
                    return $"Firebird error connecting to the database: {fbEx.Message} (Error Code: {fbEx.ErrorCode})";
                }
                catch (Exception ex)
                {
                    return $"Error connecting to the database: {ex.Message}";
                }
            }
        }

        public static void AddParameterWithValue(DbCommand command, string parameterName, object? parameterValue)
        {
            /// Add parameter and value to a DBCommand

            DbParameter parameter = command.CreateParameter();
            parameter.ParameterName = parameterName;
            parameter.Value = parameterValue;
            command.Parameters.Add(parameter);
        }
    }
}
