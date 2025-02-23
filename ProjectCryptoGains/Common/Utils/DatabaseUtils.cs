using Microsoft.Data.Sqlite;
using System;
using System.Data.Common;
using System.IO;

namespace ProjectCryptoGains.Common.Utils
{
    public static class DatabaseUtils
    {
        public static readonly string databaseFileName = "pcg.db";
        public static readonly string databasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "db", databaseFileName);
        public static readonly string connectionString = $"Data Source={databasePath}";

        public static string TestDatabaseConnection()
        {
            if (!File.Exists(databasePath))
            {
                return "Database file does not exist";
            }
            else
            {
                using SqliteConnection connection = new(connectionString);
                try
                {
                    connection.Open(); // No need to close connection, since it'll get closed automatically because we are relying on "using"
                    return "Database connection successful";
                }
                catch (Exception ex)
                {
                    return $"Error connecting to the database: {ex.Message}";
                }
            }
        }

        public static void AddParameterWithValue(DbCommand command, string parameterName, object parameterValue)
        {
            /// Add parameter and value to a DBCommand

            DbParameter parameter = command.CreateParameter();
            parameter.ParameterName = parameterName;
            parameter.Value = parameterValue;
            command.Parameters.Add(parameter);
        }
    }
}
