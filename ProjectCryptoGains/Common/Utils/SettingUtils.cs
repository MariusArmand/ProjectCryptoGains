using Microsoft.Data.Sqlite;
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
                using var connection = new SqliteConnection(connectionString);
                connection.Open();

                // First, delete any existing setting with the same name
                using (var deleteCommand = connection.CreateCommand())
                {
                    deleteCommand.CommandText = "DELETE FROM TB_SETTINGS_S WHERE Name = @NAME";
                    deleteCommand.Parameters.AddWithValue("@NAME", "COINDESKDATA_API_KEY");
                    deleteCommand.ExecuteNonQuery();
                }

                // Then, insert the new setting
                using (var insertCommand = connection.CreateCommand())
                {
                    insertCommand.CommandText = "INSERT INTO TB_SETTINGS_S (NAME, VALUE) VALUES (@NAME, @VALUE)";
                    insertCommand.Parameters.AddWithValue("@NAME", "COINDESKDATA_API_KEY");
                    insertCommand.Parameters.AddWithValue("@VALUE", value);
                    insertCommand.ExecuteNonQuery();
                }
                connection.Close();
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
                using SqliteConnection connection = new(connectionString);
                connection.Open();
                DbCommand command = connection.CreateCommand();
                command.CommandText = @"SELECT VALUE 
                                        FROM TB_SETTINGS_S
										WHERE NAME = 'COINDESKDATA_API_KEY'";

                using (DbDataReader reader = command.ExecuteReader())
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
                connection.Close();
            }
            catch (Exception ex)
            {
                _settingCoinDeskDataApiKey = null;
                throw new InvalidOperationException("Failed to load CoinDesk Data API key from database", ex);
            }
        }

        private static string? _settingFiatCurrency;

        public static string? SettingFiatCurrency
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
                using var connection = new SqliteConnection(connectionString);
                connection.Open();

                // First, delete any existing setting with the same name
                using (var deleteCommand = connection.CreateCommand())
                {
                    deleteCommand.CommandText = "DELETE FROM TB_SETTINGS_S WHERE Name = @NAME";
                    deleteCommand.Parameters.AddWithValue("@NAME", "FIAT_CURRENCY");
                    deleteCommand.ExecuteNonQuery();
                }

                // Then, insert the new setting
                using (var insertCommand = connection.CreateCommand())
                {
                    insertCommand.CommandText = "INSERT INTO TB_SETTINGS_S (NAME, VALUE) VALUES (@NAME, @VALUE)";
                    insertCommand.Parameters.AddWithValue("@NAME", "FIAT_CURRENCY");
                    insertCommand.Parameters.AddWithValue("@VALUE", value);
                    insertCommand.ExecuteNonQuery();
                }
                connection.Close();
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
                using SqliteConnection connection = new(connectionString);
                connection.Open();

                DbCommand command = connection.CreateCommand();
                command.CommandText = @"SELECT VALUE 
                                        FROM TB_SETTINGS_S
										WHERE NAME = 'FIAT_CURRENCY'";

                using (DbDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        _settingFiatCurrency = reader.GetStringOrNull(0);
                    }
                    else
                    {
                        _settingFiatCurrency = null; // No setting found in the database
                    }
                }
                connection.Close();
            }
            catch (Exception ex)
            {
                _settingFiatCurrency = null;
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
                using var connection = new SqliteConnection(connectionString);
                connection.Open();

                // First, delete any existing setting with the same name
                using (var deleteCommand = connection.CreateCommand())
                {
                    deleteCommand.CommandText = "DELETE FROM TB_SETTINGS_S WHERE Name = @NAME";
                    deleteCommand.Parameters.AddWithValue("@NAME", "REWARDS_TAX_PERCENTAGE");
                    deleteCommand.ExecuteNonQuery();
                }

                // Then, insert the new setting
                using (var insertCommand = connection.CreateCommand())
                {
                    insertCommand.CommandText = "INSERT INTO TB_SETTINGS_S (NAME, VALUE) VALUES (@NAME, @VALUE)";
                    insertCommand.Parameters.AddWithValue("@NAME", "REWARDS_TAX_PERCENTAGE");
                    insertCommand.Parameters.AddWithValue("@VALUE", value);
                    insertCommand.ExecuteNonQuery();
                }
                connection.Close();
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
                using SqliteConnection connection = new(connectionString);
                connection.Open();

                DbCommand command = connection.CreateCommand();
                command.CommandText = @"SELECT VALUE 
                                        FROM TB_SETTINGS_S
										WHERE NAME = 'REWARDS_TAX_PERCENTAGE'";

                using (DbDataReader reader = command.ExecuteReader())
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
                connection.Close();
            }
            catch (Exception ex)
            {
                _settingRewardsTaxPercentage = 0m;
                throw new InvalidOperationException("Failed to load rewards tax percentage from database", ex);
            }
        }
    }
}