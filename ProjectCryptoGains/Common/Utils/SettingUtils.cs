using FirebirdSql.Data.FirebirdClient;
using System;
using System.Data.Common;
using static ProjectCryptoGains.Common.Utils.DatabaseUtils;

namespace ProjectCryptoGains.Common.Utils
{
    public static class SettingUtils
    {
        private static string? _settingCoinDeskDataApiKey;

        public static string? SettingCoinDeskDataApiKey
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

        private static void SaveSettingCoinDeskDataApiKeyToDB(string? value)
        {
            try
            {
                using (FbConnection connection = new FbConnection(connectionString))
                {
                    connection.Open();

                    // First, delete any existing setting with the same name
                    using DbCommand deleteCommand = connection.CreateCommand();
                    deleteCommand.CommandText = "DELETE FROM TB_SETTINGS_S WHERE Name = @NAME";
                    AddParameterWithValue(deleteCommand, "@NAME", "COINDESKDATA_API_KEY");
                    deleteCommand.ExecuteNonQuery();

                    // Then, insert the new setting
                    using DbCommand insertCommand = connection.CreateCommand();
                    insertCommand.CommandText = "INSERT INTO TB_SETTINGS_S (NAME, \"VALUE\") VALUES (@NAME, @VALUE)";
                    AddParameterWithValue(insertCommand, "@NAME", "COINDESKDATA_API_KEY");
                    AddParameterWithValue(insertCommand, "@VALUE", value);
                    insertCommand.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to save CoinDesk Data API key to database", ex);
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
                                              FROM TB_SETTINGS_S
								              WHERE NAME = 'COINDESKDATA_API_KEY'";

                    using (DbDataReader reader = selectCommand.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            _settingCoinDeskDataApiKey = reader.GetStringOrNull(0);
                        }
                        else
                        {
                            _settingCoinDeskDataApiKey = null; // No setting found in the database
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _settingCoinDeskDataApiKey = null;
                throw new InvalidOperationException("Failed to load CoinDesk Data API key from database", ex);
            }
        }

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
                    deleteCommand.CommandText = "DELETE FROM TB_SETTINGS_S WHERE Name = @NAME";
                    AddParameterWithValue(deleteCommand, "@NAME", "FIAT_CURRENCY");
                    deleteCommand.ExecuteNonQuery();

                    // Then, insert the new setting
                    using DbCommand insertCommand = connection.CreateCommand();
                    insertCommand.CommandText = "INSERT INTO TB_SETTINGS_S (NAME, \"VALUE\") VALUES (@NAME, @VALUE)";
                    AddParameterWithValue(insertCommand, "@NAME", "FIAT_CURRENCY");
                    AddParameterWithValue(insertCommand, "@VALUE", value);
                    insertCommand.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to save fiat currency to database", ex);
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
                                              FROM TB_SETTINGS_S
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
                throw new InvalidOperationException("Failed to load fiat currency from database", ex);
            }
        }

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
                    deleteCommand.CommandText = "DELETE FROM TB_SETTINGS_S WHERE Name = @NAME";
                    AddParameterWithValue(deleteCommand, "@NAME", "REWARDS_TAX_PERCENTAGE");
                    deleteCommand.ExecuteNonQuery();

                    // Then, insert the new setting
                    using DbCommand insertCommand = connection.CreateCommand();
                    insertCommand.CommandText = "INSERT INTO TB_SETTINGS_S (NAME, \"VALUE\") VALUES (@NAME, @VALUE)";
                    AddParameterWithValue(insertCommand, "@NAME", "REWARDS_TAX_PERCENTAGE");
                    AddParameterWithValue(insertCommand, "@VALUE", value);
                    insertCommand.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to save rewards tax percentage to database", ex);
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
                                                  FROM TB_SETTINGS_S
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
                throw new InvalidOperationException("Failed to load rewards tax percentage from database", ex);
            }
        }
    }
}