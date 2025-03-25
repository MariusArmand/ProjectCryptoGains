using FirebirdSql.Data.FirebirdClient;
using System;
using System.Data.Common;
using static ProjectCryptoGains.Common.Utils.DatabaseUtils;

namespace ProjectCryptoGains.Common.Utils
{
    public static class SettingsUtils
    {
        // Fiat currency
        private static string _settingFiatCurrency = "NONE";

        public static string SettingFiatCurrency
        {
            get
            {
                return _settingFiatCurrency;
            }
            set
            {
                _settingFiatCurrency = value;
                if (!string.IsNullOrEmpty(_settingFiatCurrency))
                {
                    SaveSettingFiatCurrencyToDB(_settingFiatCurrency);
                }
            }
        }

        private static void SaveSettingFiatCurrencyToDB(string value)
        {
            try
            {
                using (FbConnection connection = new FbConnection(connectionString))
                {
                    connection.Open();

                    // First, delete any existing setting with the same name
                    using DbCommand deleteCommand = connection.CreateCommand();
                    deleteCommand.CommandText = "DELETE FROM TB_SETTINGS WHERE NAME = @NAME";
                    AddParameterWithValue(deleteCommand, "@NAME", "FIAT_CURRENCY");
                    deleteCommand.ExecuteNonQuery();

                    // Then, insert the new setting
                    using DbCommand insertCommand = connection.CreateCommand();
                    insertCommand.CommandText = "INSERT INTO TB_SETTINGS (NAME, \"VALUE\") VALUES (@NAME, @VALUE)";
                    AddParameterWithValue(insertCommand, "@NAME", "FIAT_CURRENCY");
                    AddParameterWithValue(insertCommand, "@VALUE", value);
                    insertCommand.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to save fiat currency to database.", ex);
            }
        }

        public static void LoadSettingFiatCurrencyFromDB()
        {
            try
            {
                using (FbConnection connection = new(connectionString))
                {
                    connection.Open();

                    using DbCommand selectCommand = connection.CreateCommand();
                    selectCommand.CommandText = @"SELECT ""VALUE""
                                                  FROM TB_SETTINGS
										          WHERE NAME = 'FIAT_CURRENCY'";

                    using (DbDataReader reader = selectCommand.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            _settingFiatCurrency = reader.GetStringOrNull(0) ?? "NONE";
                        }
                        else
                        {
                            _settingFiatCurrency = "NONE"; // No setting found in the database
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _settingFiatCurrency = "NONE";
                throw new InvalidOperationException("Failed to load fiat currency from database.", ex);
            }
        }

        // Rewards tax percentage
        private static decimal _settingRewardsTaxPercentage = 0m;

        public static decimal SettingRewardsTaxPercentage
        {
            get
            {
                return _settingRewardsTaxPercentage;
            }
            set
            {
                if (decimal.TryParse(value.ToString(), out decimal tryParsedAmount))
                {
                    _settingRewardsTaxPercentage = tryParsedAmount;
                }
                else
                {
                    _settingRewardsTaxPercentage = 0m;
                }

                SaveSettingRewardsTaxPercentageToDB(_settingRewardsTaxPercentage.ToString());
            }
        }

        private static void SaveSettingRewardsTaxPercentageToDB(string value)
        {
            try
            {
                using (FbConnection connection = new FbConnection(connectionString))
                {
                    connection.Open();

                    // First, delete any existing setting with the same name
                    using DbCommand deleteCommand = connection.CreateCommand();
                    deleteCommand.CommandText = "DELETE FROM TB_SETTINGS WHERE NAME = @NAME";
                    AddParameterWithValue(deleteCommand, "@NAME", "REWARDS_TAX_PERCENTAGE");
                    deleteCommand.ExecuteNonQuery();

                    // Then, insert the new setting
                    using DbCommand insertCommand = connection.CreateCommand();
                    insertCommand.CommandText = "INSERT INTO TB_SETTINGS (NAME, \"VALUE\") VALUES (@NAME, @VALUE)";
                    AddParameterWithValue(insertCommand, "@NAME", "REWARDS_TAX_PERCENTAGE");
                    AddParameterWithValue(insertCommand, "@VALUE", value);
                    insertCommand.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to save rewards tax percentage to database.", ex);
            }
        }

        public static void LoadSettingRewardsTaxPercentageFromDB()
        {
            try
            {
                using (FbConnection connection = new(connectionString))
                {
                    connection.Open();

                    using DbCommand selectCommand = connection.CreateCommand();
                    selectCommand.CommandText = @"SELECT ""VALUE"" 
                                                  FROM TB_SETTINGS
										          WHERE NAME = 'REWARDS_TAX_PERCENTAGE'";

                    using (DbDataReader reader = selectCommand.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            _settingRewardsTaxPercentage = reader.GetDecimalOrDefault(0, 0m);
                        }
                        else
                        {
                            _settingRewardsTaxPercentage = 0m; // No setting found in the database
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _settingRewardsTaxPercentage = 0m;
                throw new InvalidOperationException("Failed to load rewards tax percentage from database.", ex);
            }
        }

        // CoinDesk Data API key
        private static string _settingCoinDeskDataApiKey = "";

        public static string SettingCoinDeskDataApiKey
        {
            get
            {
                return _settingCoinDeskDataApiKey;
            }
            set
            {
                _settingCoinDeskDataApiKey = value;
                SaveSettingCoinDeskDataApiKeyToDB(_settingCoinDeskDataApiKey);
            }
        }

        private static void SaveSettingCoinDeskDataApiKeyToDB(string value)
        {
            try
            {
                using (FbConnection connection = new FbConnection(connectionString))
                {
                    connection.Open();

                    // First, delete any existing setting with the same name
                    using DbCommand deleteCommand = connection.CreateCommand();
                    deleteCommand.CommandText = "DELETE FROM TB_SETTINGS WHERE NAME = @NAME";
                    AddParameterWithValue(deleteCommand, "@NAME", "COINDESKDATA_API_KEY");
                    deleteCommand.ExecuteNonQuery();

                    // Then, insert the new setting
                    using DbCommand insertCommand = connection.CreateCommand();
                    insertCommand.CommandText = "INSERT INTO TB_SETTINGS (NAME, \"VALUE\") VALUES (@NAME, @VALUE)";
                    AddParameterWithValue(insertCommand, "@NAME", "COINDESKDATA_API_KEY");
                    AddParameterWithValue(insertCommand, "@VALUE", value);
                    insertCommand.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to save CoinDesk Data API key to database.", ex);
            }
        }

        public static void LoadSettingCoinDeskDataApiKeyFromDB()
        {
            try
            {
                using (FbConnection connection = new(connectionString))
                {
                    connection.Open();
                    using DbCommand selectCommand = connection.CreateCommand();
                    selectCommand.CommandText = @"SELECT ""VALUE"" 
                                                  FROM TB_SETTINGS
								                  WHERE NAME = 'COINDESKDATA_API_KEY'";

                    using (DbDataReader reader = selectCommand.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            _settingCoinDeskDataApiKey = reader.GetStringOrEmpty(0);
                        }
                        else
                        {
                            _settingCoinDeskDataApiKey = ""; // No setting found in the database
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _settingCoinDeskDataApiKey = "";
                throw new InvalidOperationException("Failed to load CoinDesk Data API key from database.", ex);
            }
        }

        // Printout title prefix
        private static string _settingPrintoutTitlePrefix = "";

        public static string SettingPrintoutTitlePrefix
        {
            get
            {
                return _settingPrintoutTitlePrefix;
            }
            set
            {
                if (value.Length > 50)
                {
                    throw new InvalidOperationException("Printout title prefix cannot exceed 50 characters.");
                }
                _settingPrintoutTitlePrefix = value;
                SaveSettingPrintoutTitlePrefixToDB(_settingPrintoutTitlePrefix);
            }
        }

        private static void SaveSettingPrintoutTitlePrefixToDB(string value)
        {
            try
            {
                using (FbConnection connection = new FbConnection(connectionString))
                {
                    connection.Open();

                    // First, delete any existing setting with the same name
                    using DbCommand deleteCommand = connection.CreateCommand();
                    deleteCommand.CommandText = "DELETE FROM TB_SETTINGS WHERE NAME = @NAME";
                    AddParameterWithValue(deleteCommand, "@NAME", "PRINTOUT_TITLE_PREFIX");
                    deleteCommand.ExecuteNonQuery();

                    // Then, insert the new setting
                    using DbCommand insertCommand = connection.CreateCommand();
                    insertCommand.CommandText = "INSERT INTO TB_SETTINGS (NAME, \"VALUE\") VALUES (@NAME, @VALUE)";
                    AddParameterWithValue(insertCommand, "@NAME", "PRINTOUT_TITLE_PREFIX");
                    AddParameterWithValue(insertCommand, "@VALUE", value);
                    insertCommand.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to save printout title prefix to database.", ex);
            }
        }

        public static void LoadSettingPrintoutTitlePrefixFromDB()
        {
            try
            {
                using (FbConnection connection = new(connectionString))
                {
                    connection.Open();

                    using DbCommand selectCommand = connection.CreateCommand();
                    selectCommand.CommandText = @"SELECT ""VALUE"" 
                                                  FROM TB_SETTINGS
										          WHERE NAME = 'PRINTOUT_TITLE_PREFIX'";

                    using (DbDataReader reader = selectCommand.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            _settingPrintoutTitlePrefix = reader.GetStringOrNull(0) ?? "";
                        }
                        else
                        {
                            _settingPrintoutTitlePrefix = ""; // No setting found in the database
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _settingPrintoutTitlePrefix = "";
                throw new InvalidOperationException("Failed to load printout title prefix from database.", ex);
            }
        }
    }
}