using FirebirdSql.Data.FirebirdClient;
using System;
using System.Data.Common;
using static ProjectCryptoGains.Common.Utils.DatabaseUtils;
using static ProjectCryptoGains.Common.Utils.DateUtils;

namespace ProjectCryptoGains.Common.Utils
{
    public static class ParametersWindowsUtils
    {
        // Ledgers From Date
        private static string _parWinLedgersFromDate = "";

        public static string ParWinLedgersFromDate
        {
            get
            {
                return _parWinLedgersFromDate;
            }
            set
            {
                _parWinLedgersFromDate = value;
                if (!string.IsNullOrEmpty(_parWinLedgersFromDate))
                {
                    SaveParWinLedgersFromDateToDB(_parWinLedgersFromDate);
                }
            }
        }

        private static void SaveParWinLedgersFromDateToDB(string value)
        {
            try
            {
                using (FbConnection connection = new FbConnection(connectionString))
                {
                    connection.Open();

                    // First, delete any existing parameter with the same window/name
                    using DbCommand deleteCommand = connection.CreateCommand();
                    deleteCommand.CommandText = "DELETE FROM TB_PARAMETERS_WINDOWS WHERE \"WINDOW\" = @WINDOW AND NAME = @NAME";
                    AddParameterWithValue(deleteCommand, "@WINDOW", "LEDGERS");
                    AddParameterWithValue(deleteCommand, "@NAME", "FROM_DATE");
                    deleteCommand.ExecuteNonQuery();

                    // Then, insert the new parameter
                    using DbCommand insertCommand = connection.CreateCommand();
                    insertCommand.CommandText = "INSERT INTO TB_PARAMETERS_WINDOWS (\"WINDOW\", NAME, \"VALUE\") VALUES (@WINDOW, @NAME, @VALUE)";
                    AddParameterWithValue(insertCommand, "@WINDOW", "LEDGERS");
                    AddParameterWithValue(insertCommand, "@NAME", "FROM_DATE");
                    AddParameterWithValue(insertCommand, "@VALUE", value);
                    insertCommand.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to save 'From Date' parameter to database.", ex);
            }
        }

        public static void LoadParWinLedgersFromDateFromDB()
        {
            try
            {
                using (FbConnection connection = new(connectionString))
                {
                    connection.Open();

                    using DbCommand selectCommand = connection.CreateCommand();
                    selectCommand.CommandText = @"SELECT ""VALUE""
                                                  FROM TB_PARAMETERS_WINDOWS
										          WHERE ""WINDOW"" = 'LEDGERS' AND NAME = 'FROM_DATE'";

                    using (DbDataReader reader = selectCommand.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            _parWinLedgersFromDate = reader.GetStringOrNull(0) ?? "2009-01-03";
                        }
                        else
                        {
                            _parWinLedgersFromDate = "2009-01-03"; // No parameter found in the database
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _parWinLedgersFromDate = "2009-01-03";
                throw new InvalidOperationException("Failed to load 'From Date' parameter from database.", ex);
            }
        }

        // Ledgers to date
        private static string _parWinLedgersToDate = "";

        public static string ParWinLedgersToDate
        {
            get
            {
                return _parWinLedgersToDate;
            }
            set
            {
                _parWinLedgersToDate = value;
                if (!string.IsNullOrEmpty(_parWinLedgersToDate))
                {
                    SaveParWinLedgersToDateToDB(_parWinLedgersToDate);
                }
            }
        }

        private static void SaveParWinLedgersToDateToDB(string value)
        {
            try
            {
                using (FbConnection connection = new FbConnection(connectionString))
                {
                    connection.Open();

                    // First, delete any existing parameter with the same window/name
                    using DbCommand deleteCommand = connection.CreateCommand();
                    deleteCommand.CommandText = "DELETE FROM TB_PARAMETERS_WINDOWS WHERE \"WINDOW\" = @WINDOW AND NAME = @NAME";
                    AddParameterWithValue(deleteCommand, "@WINDOW", "LEDGERS");
                    AddParameterWithValue(deleteCommand, "@NAME", "TO_DATE");
                    deleteCommand.ExecuteNonQuery();

                    // Then, insert the new parameter
                    using DbCommand insertCommand = connection.CreateCommand();
                    insertCommand.CommandText = "INSERT INTO TB_PARAMETERS_WINDOWS (\"WINDOW\", NAME, \"VALUE\") VALUES (@WINDOW, @NAME, @VALUE)";
                    AddParameterWithValue(insertCommand, "@WINDOW", "LEDGERS");
                    AddParameterWithValue(insertCommand, "@NAME", "TO_DATE");
                    AddParameterWithValue(insertCommand, "@VALUE", value);
                    insertCommand.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to save 'To Date' parameter to database.", ex);
            }
        }

        public static void LoadParWinLedgersToDateFromDB()
        {
            try
            {
                using (FbConnection connection = new(connectionString))
                {
                    connection.Open();

                    using DbCommand selectCommand = connection.CreateCommand();
                    selectCommand.CommandText = @"SELECT ""VALUE""
                                                  FROM TB_PARAMETERS_WINDOWS
										          WHERE ""WINDOW"" = 'LEDGERS' AND NAME = 'TO_DATE'";

                    using (DbDataReader reader = selectCommand.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            _parWinLedgersToDate = reader.GetStringOrNull(0) ?? GetTodayAsIsoDate();
                        }
                        else
                        {
                            _parWinLedgersToDate = GetTodayAsIsoDate(); // No parameter found in the database
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _parWinLedgersToDate = GetTodayAsIsoDate();
                throw new InvalidOperationException("Failed to load 'To Date' parameter from database.", ex);
            }
        }

        // Trades From Date
        private static string _parWinTradesFromDate = "";

        public static string ParWinTradesFromDate
        {
            get
            {
                return _parWinTradesFromDate;
            }
            set
            {
                _parWinTradesFromDate = value;
                if (!string.IsNullOrEmpty(_parWinTradesFromDate))
                {
                    SaveParWinTradesFromDateToDB(_parWinTradesFromDate);
                }
            }
        }

        private static void SaveParWinTradesFromDateToDB(string value)
        {
            try
            {
                using (FbConnection connection = new FbConnection(connectionString))
                {
                    connection.Open();

                    // First, delete any existing parameter with the same window/name
                    using DbCommand deleteCommand = connection.CreateCommand();
                    deleteCommand.CommandText = "DELETE FROM TB_PARAMETERS_WINDOWS WHERE \"WINDOW\" = @WINDOW AND NAME = @NAME";
                    AddParameterWithValue(deleteCommand, "@WINDOW", "TRADES");
                    AddParameterWithValue(deleteCommand, "@NAME", "FROM_DATE");
                    deleteCommand.ExecuteNonQuery();

                    // Then, insert the new parameter
                    using DbCommand insertCommand = connection.CreateCommand();
                    insertCommand.CommandText = "INSERT INTO TB_PARAMETERS_WINDOWS (\"WINDOW\", NAME, \"VALUE\") VALUES (@WINDOW, @NAME, @VALUE)";
                    AddParameterWithValue(insertCommand, "@WINDOW", "TRADES");
                    AddParameterWithValue(insertCommand, "@NAME", "FROM_DATE");
                    AddParameterWithValue(insertCommand, "@VALUE", value);
                    insertCommand.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to save 'From Date' parameter to database.", ex);
            }
        }

        public static void LoadParWinTradesFromDateFromDB()
        {
            try
            {
                using (FbConnection connection = new(connectionString))
                {
                    connection.Open();

                    using DbCommand selectCommand = connection.CreateCommand();
                    selectCommand.CommandText = @"SELECT ""VALUE""
                                                  FROM TB_PARAMETERS_WINDOWS
										          WHERE ""WINDOW"" = 'TRADES' AND NAME = 'FROM_DATE'";

                    using (DbDataReader reader = selectCommand.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            _parWinTradesFromDate = reader.GetStringOrNull(0) ?? "2009-01-03";
                        }
                        else
                        {
                            _parWinTradesFromDate = "2009-01-03"; // No parameter found in the database
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _parWinTradesFromDate = "2009-01-03";
                throw new InvalidOperationException("Failed to load 'From Date' parameter from database.", ex);
            }
        }

        // Trades to date
        private static string _parWinTradesToDate = "";

        public static string ParWinTradesToDate
        {
            get
            {
                return _parWinTradesToDate;
            }
            set
            {
                _parWinTradesToDate = value;
                if (!string.IsNullOrEmpty(_parWinTradesToDate))
                {
                    SaveParWinTradesToDateToDB(_parWinTradesToDate);
                }
            }
        }

        private static void SaveParWinTradesToDateToDB(string value)
        {
            try
            {
                using (FbConnection connection = new FbConnection(connectionString))
                {
                    connection.Open();

                    // First, delete any existing parameter with the same window/name
                    using DbCommand deleteCommand = connection.CreateCommand();
                    deleteCommand.CommandText = "DELETE FROM TB_PARAMETERS_WINDOWS WHERE \"WINDOW\" = @WINDOW AND NAME = @NAME";
                    AddParameterWithValue(deleteCommand, "@WINDOW", "TRADES");
                    AddParameterWithValue(deleteCommand, "@NAME", "TO_DATE");
                    deleteCommand.ExecuteNonQuery();

                    // Then, insert the new parameter
                    using DbCommand insertCommand = connection.CreateCommand();
                    insertCommand.CommandText = "INSERT INTO TB_PARAMETERS_WINDOWS (\"WINDOW\", NAME, \"VALUE\") VALUES (@WINDOW, @NAME, @VALUE)";
                    AddParameterWithValue(insertCommand, "@WINDOW", "TRADES");
                    AddParameterWithValue(insertCommand, "@NAME", "TO_DATE");
                    AddParameterWithValue(insertCommand, "@VALUE", value);
                    insertCommand.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to save 'To Date' parameter to database.", ex);
            }
        }

        public static void LoadParWinTradesToDateFromDB()
        {
            try
            {
                using (FbConnection connection = new(connectionString))
                {
                    connection.Open();

                    using DbCommand selectCommand = connection.CreateCommand();
                    selectCommand.CommandText = @"SELECT ""VALUE""
                                                  FROM TB_PARAMETERS_WINDOWS
										          WHERE ""WINDOW"" = 'TRADES' AND NAME = 'TO_DATE'";

                    using (DbDataReader reader = selectCommand.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            _parWinTradesToDate = reader.GetStringOrNull(0) ?? GetTodayAsIsoDate();
                        }
                        else
                        {
                            _parWinTradesToDate = GetTodayAsIsoDate(); // No parameter found in the database
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _parWinTradesToDate = GetTodayAsIsoDate();
                throw new InvalidOperationException("Failed to load 'To Date' parameter from database.", ex);
            }
        }

        // Gains From Date
        private static string _parWinGainsFromDate = "";

        public static string ParWinGainsFromDate
        {
            get
            {
                return _parWinGainsFromDate;
            }
            set
            {
                _parWinGainsFromDate = value;
                if (!string.IsNullOrEmpty(_parWinGainsFromDate))
                {
                    SaveParWinGainsFromDateToDB(_parWinGainsFromDate);
                }
            }
        }

        private static void SaveParWinGainsFromDateToDB(string value)
        {
            try
            {
                using (FbConnection connection = new FbConnection(connectionString))
                {
                    connection.Open();

                    // First, delete any existing parameter with the same window/name
                    using DbCommand deleteCommand = connection.CreateCommand();
                    deleteCommand.CommandText = "DELETE FROM TB_PARAMETERS_WINDOWS WHERE \"WINDOW\" = @WINDOW AND NAME = @NAME";
                    AddParameterWithValue(deleteCommand, "@WINDOW", "GAINS");
                    AddParameterWithValue(deleteCommand, "@NAME", "FROM_DATE");
                    deleteCommand.ExecuteNonQuery();

                    // Then, insert the new parameter
                    using DbCommand insertCommand = connection.CreateCommand();
                    insertCommand.CommandText = "INSERT INTO TB_PARAMETERS_WINDOWS (\"WINDOW\", NAME, \"VALUE\") VALUES (@WINDOW, @NAME, @VALUE)";
                    AddParameterWithValue(insertCommand, "@WINDOW", "GAINS");
                    AddParameterWithValue(insertCommand, "@NAME", "FROM_DATE");
                    AddParameterWithValue(insertCommand, "@VALUE", value);
                    insertCommand.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to save 'From Date' parameter to database.", ex);
            }
        }

        public static void LoadParWinGainsFromDateFromDB()
        {
            try
            {
                using (FbConnection connection = new(connectionString))
                {
                    connection.Open();

                    using DbCommand selectCommand = connection.CreateCommand();
                    selectCommand.CommandText = @"SELECT ""VALUE""
                                                  FROM TB_PARAMETERS_WINDOWS
										          WHERE ""WINDOW"" = 'GAINS' AND NAME = 'FROM_DATE'";

                    using (DbDataReader reader = selectCommand.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            _parWinGainsFromDate = reader.GetStringOrNull(0) ?? "2009-01-03";
                        }
                        else
                        {
                            _parWinGainsFromDate = "2009-01-03"; // No parameter found in the database
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _parWinGainsFromDate = "2009-01-03";
                throw new InvalidOperationException("Failed to load 'From Date' parameter from database.", ex);
            }
        }

        // Gains to date
        private static string _parWinGainsToDate = "";

        public static string ParWinGainsToDate
        {
            get
            {
                return _parWinGainsToDate;
            }
            set
            {
                _parWinGainsToDate = value;
                if (!string.IsNullOrEmpty(_parWinGainsToDate))
                {
                    SaveParWinGainsToDateToDB(_parWinGainsToDate);
                }
            }
        }

        private static void SaveParWinGainsToDateToDB(string value)
        {
            try
            {
                using (FbConnection connection = new FbConnection(connectionString))
                {
                    connection.Open();

                    // First, delete any existing parameter with the same window/name
                    using DbCommand deleteCommand = connection.CreateCommand();
                    deleteCommand.CommandText = "DELETE FROM TB_PARAMETERS_WINDOWS WHERE \"WINDOW\" = @WINDOW AND NAME = @NAME";
                    AddParameterWithValue(deleteCommand, "@WINDOW", "GAINS");
                    AddParameterWithValue(deleteCommand, "@NAME", "TO_DATE");
                    deleteCommand.ExecuteNonQuery();

                    // Then, insert the new parameter
                    using DbCommand insertCommand = connection.CreateCommand();
                    insertCommand.CommandText = "INSERT INTO TB_PARAMETERS_WINDOWS (\"WINDOW\", NAME, \"VALUE\") VALUES (@WINDOW, @NAME, @VALUE)";
                    AddParameterWithValue(insertCommand, "@WINDOW", "GAINS");
                    AddParameterWithValue(insertCommand, "@NAME", "TO_DATE");
                    AddParameterWithValue(insertCommand, "@VALUE", value);
                    insertCommand.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to save 'To Date' parameter to database.", ex);
            }
        }

        public static void LoadParWinGainsToDateFromDB()
        {
            try
            {
                using (FbConnection connection = new(connectionString))
                {
                    connection.Open();

                    using DbCommand selectCommand = connection.CreateCommand();
                    selectCommand.CommandText = @"SELECT ""VALUE""
                                                  FROM TB_PARAMETERS_WINDOWS
										          WHERE ""WINDOW"" = 'GAINS' AND NAME = 'TO_DATE'";

                    using (DbDataReader reader = selectCommand.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            _parWinGainsToDate = reader.GetStringOrNull(0) ?? GetTodayAsIsoDate();
                        }
                        else
                        {
                            _parWinGainsToDate = GetTodayAsIsoDate(); // No parameter found in the database
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _parWinGainsToDate = GetTodayAsIsoDate();
                throw new InvalidOperationException("Failed to load 'To Date' parameter from database.", ex);
            }
        }

        // Gains base asset
        private static string _parWinGainsBaseAsset = "";

        public static string ParWinGainsBaseAsset
        {
            get
            {
                return _parWinGainsBaseAsset;
            }
            set
            {
                _parWinGainsBaseAsset = value;
                SaveParWinGainsBaseAssetToDB(_parWinGainsBaseAsset);
            }
        }

        private static void SaveParWinGainsBaseAssetToDB(string value)
        {
            try
            {
                using (FbConnection connection = new FbConnection(connectionString))
                {
                    connection.Open();

                    // First, delete any existing parameter with the same window/name
                    using DbCommand deleteCommand = connection.CreateCommand();
                    deleteCommand.CommandText = "DELETE FROM TB_PARAMETERS_WINDOWS WHERE \"WINDOW\" = @WINDOW AND NAME = @NAME";
                    AddParameterWithValue(deleteCommand, "@WINDOW", "GAINS");
                    AddParameterWithValue(deleteCommand, "@NAME", "BASE_ASSET");
                    deleteCommand.ExecuteNonQuery();

                    // Then, insert the new parameter
                    using DbCommand insertCommand = connection.CreateCommand();
                    insertCommand.CommandText = "INSERT INTO TB_PARAMETERS_WINDOWS (\"WINDOW\", NAME, \"VALUE\") VALUES (@WINDOW, @NAME, @VALUE)";
                    AddParameterWithValue(insertCommand, "@WINDOW", "GAINS");
                    AddParameterWithValue(insertCommand, "@NAME", "BASE_ASSET");
                    AddParameterWithValue(insertCommand, "@VALUE", value);
                    insertCommand.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to save 'Base Asset' parameter to database.", ex);
            }
        }

        public static void LoadParWinGainsBaseAssetFromDB()
        {
            try
            {
                using (FbConnection connection = new(connectionString))
                {
                    connection.Open();

                    using DbCommand selectCommand = connection.CreateCommand();
                    selectCommand.CommandText = @"SELECT ""VALUE""
                                                  FROM TB_PARAMETERS_WINDOWS
										          WHERE ""WINDOW"" = 'GAINS' AND NAME = 'BASE_ASSET'";

                    using (DbDataReader reader = selectCommand.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            _parWinGainsBaseAsset = reader.GetStringOrNull(0) ?? "";
                        }
                        else
                        {
                            _parWinGainsBaseAsset = ""; // No parameter found in the database
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _parWinGainsBaseAsset = "";
                throw new InvalidOperationException("Failed to load 'Base Asset' parameter from database.", ex);
            }
        }

        // Rewards From Date
        private static string _parWinRewardsFromDate = "";

        public static string ParWinRewardsFromDate
        {
            get
            {
                return _parWinRewardsFromDate;
            }
            set
            {
                _parWinRewardsFromDate = value;
                if (!string.IsNullOrEmpty(_parWinRewardsFromDate))
                {
                    SaveParWinRewardsFromDateToDB(_parWinRewardsFromDate);
                }
            }
        }

        private static void SaveParWinRewardsFromDateToDB(string value)
        {
            try
            {
                using (FbConnection connection = new FbConnection(connectionString))
                {
                    connection.Open();

                    // First, delete any existing parameter with the same window/name
                    using DbCommand deleteCommand = connection.CreateCommand();
                    deleteCommand.CommandText = "DELETE FROM TB_PARAMETERS_WINDOWS WHERE \"WINDOW\" = @WINDOW AND NAME = @NAME";
                    AddParameterWithValue(deleteCommand, "@WINDOW", "REWARDS");
                    AddParameterWithValue(deleteCommand, "@NAME", "FROM_DATE");
                    deleteCommand.ExecuteNonQuery();

                    // Then, insert the new parameter
                    using DbCommand insertCommand = connection.CreateCommand();
                    insertCommand.CommandText = "INSERT INTO TB_PARAMETERS_WINDOWS (\"WINDOW\", NAME, \"VALUE\") VALUES (@WINDOW, @NAME, @VALUE)";
                    AddParameterWithValue(insertCommand, "@WINDOW", "REWARDS");
                    AddParameterWithValue(insertCommand, "@NAME", "FROM_DATE");
                    AddParameterWithValue(insertCommand, "@VALUE", value);
                    insertCommand.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to save 'From Date' parameter to database.", ex);
            }
        }

        public static void LoadParWinRewardsFromDateFromDB()
        {
            try
            {
                using (FbConnection connection = new(connectionString))
                {
                    connection.Open();

                    using DbCommand selectCommand = connection.CreateCommand();
                    selectCommand.CommandText = @"SELECT ""VALUE""
                                                  FROM TB_PARAMETERS_WINDOWS
										          WHERE ""WINDOW"" = 'REWARDS' AND NAME = 'FROM_DATE'";

                    using (DbDataReader reader = selectCommand.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            _parWinRewardsFromDate = reader.GetStringOrNull(0) ?? "2009-01-03";
                        }
                        else
                        {
                            _parWinRewardsFromDate = "2009-01-03"; // No parameter found in the database
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _parWinRewardsFromDate = "2009-01-03";
                throw new InvalidOperationException("Failed to load 'From Date' parameter from database.", ex);
            }
        }

        // Rewards to date
        private static string _parWinRewardsToDate = "";

        public static string ParWinRewardsToDate
        {
            get
            {
                return _parWinRewardsToDate;
            }
            set
            {
                _parWinRewardsToDate = value;
                if (!string.IsNullOrEmpty(_parWinRewardsToDate))
                {
                    SaveParWinRewardsToDateToDB(_parWinRewardsToDate);
                }
            }
        }

        private static void SaveParWinRewardsToDateToDB(string value)
        {
            try
            {
                using (FbConnection connection = new FbConnection(connectionString))
                {
                    connection.Open();

                    // First, delete any existing parameter with the same window/name
                    using DbCommand deleteCommand = connection.CreateCommand();
                    deleteCommand.CommandText = "DELETE FROM TB_PARAMETERS_WINDOWS WHERE \"WINDOW\" = @WINDOW AND NAME = @NAME";
                    AddParameterWithValue(deleteCommand, "@WINDOW", "REWARDS");
                    AddParameterWithValue(deleteCommand, "@NAME", "TO_DATE");
                    deleteCommand.ExecuteNonQuery();

                    // Then, insert the new parameter
                    using DbCommand insertCommand = connection.CreateCommand();
                    insertCommand.CommandText = "INSERT INTO TB_PARAMETERS_WINDOWS (\"WINDOW\", NAME, \"VALUE\") VALUES (@WINDOW, @NAME, @VALUE)";
                    AddParameterWithValue(insertCommand, "@WINDOW", "REWARDS");
                    AddParameterWithValue(insertCommand, "@NAME", "TO_DATE");
                    AddParameterWithValue(insertCommand, "@VALUE", value);
                    insertCommand.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to save 'To Date' parameter to database.", ex);
            }
        }

        public static void LoadParWinRewardsToDateFromDB()
        {
            try
            {
                using (FbConnection connection = new(connectionString))
                {
                    connection.Open();

                    using DbCommand selectCommand = connection.CreateCommand();
                    selectCommand.CommandText = @"SELECT ""VALUE""
                                                  FROM TB_PARAMETERS_WINDOWS
										          WHERE ""WINDOW"" = 'REWARDS' AND NAME = 'TO_DATE'";

                    using (DbDataReader reader = selectCommand.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            _parWinRewardsToDate = reader.GetStringOrNull(0) ?? GetTodayAsIsoDate();
                        }
                        else
                        {
                            _parWinRewardsToDate = GetTodayAsIsoDate(); // No parameter found in the database
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _parWinRewardsToDate = GetTodayAsIsoDate();
                throw new InvalidOperationException("Failed to load 'To Date' parameter from database.", ex);
            }
        }

        // Balances until date
        private static string _parWinBalancesUntilDate = "";

        public static string ParWinBalancesUntilDate
        {
            get
            {
                return _parWinBalancesUntilDate;
            }
            set
            {
                _parWinBalancesUntilDate = value;
                if (!string.IsNullOrEmpty(_parWinBalancesUntilDate))
                {
                    SaveParWinBalancesUntilDateToDB(_parWinBalancesUntilDate);
                }
            }
        }

        private static void SaveParWinBalancesUntilDateToDB(string value)
        {
            try
            {
                using (FbConnection connection = new FbConnection(connectionString))
                {
                    connection.Open();

                    // First, delete any existing parameter with the same window/name
                    using DbCommand deleteCommand = connection.CreateCommand();
                    deleteCommand.CommandText = "DELETE FROM TB_PARAMETERS_WINDOWS WHERE \"WINDOW\" = @WINDOW AND NAME = @NAME";
                    AddParameterWithValue(deleteCommand, "@WINDOW", "BALANCES");
                    AddParameterWithValue(deleteCommand, "@NAME", "UNTIL_DATE");
                    deleteCommand.ExecuteNonQuery();

                    // Then, insert the new parameter
                    using DbCommand insertCommand = connection.CreateCommand();
                    insertCommand.CommandText = "INSERT INTO TB_PARAMETERS_WINDOWS (\"WINDOW\", NAME, \"VALUE\") VALUES (@WINDOW, @NAME, @VALUE)";
                    AddParameterWithValue(insertCommand, "@WINDOW", "BALANCES");
                    AddParameterWithValue(insertCommand, "@NAME", "UNTIL_DATE");
                    AddParameterWithValue(insertCommand, "@VALUE", value);
                    insertCommand.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to save 'Until Date' parameter to database.", ex);
            }
        }

        public static void LoadParWinBalancesUntilDateFromDB()
        {
            try
            {
                using (FbConnection connection = new(connectionString))
                {
                    connection.Open();

                    using DbCommand selectCommand = connection.CreateCommand();
                    selectCommand.CommandText = @"SELECT ""VALUE""
                                                  FROM TB_PARAMETERS_WINDOWS
										          WHERE ""WINDOW"" = 'BALANCES' AND NAME = 'UNTIL_DATE'";

                    using (DbDataReader reader = selectCommand.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            _parWinBalancesUntilDate = reader.GetStringOrNull(0) ?? GetTodayAsIsoDate();
                        }
                        else
                        {
                            _parWinBalancesUntilDate = GetTodayAsIsoDate(); // No parameter found in the database
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _parWinBalancesUntilDate = GetTodayAsIsoDate();
                throw new InvalidOperationException("Failed to load 'Until Date' parameter from database.", ex);
            }
        }
    }
}