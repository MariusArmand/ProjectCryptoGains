using Microsoft.Data.Sqlite;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace ProjectCryptoGains
{
    public static class Utility
    {
        public static readonly string databaseFileName = "pcg.db";
        public static readonly string databasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "db", databaseFileName);
        public static readonly string connectionString = $"Data Source={databasePath}";

        // Parallel run prevention //
        public static bool LedgersRefreshBusy { get; private set; } = false;
        public static bool TradesRawRefreshBusy { get; private set; } = false;
        public static bool TradesRefreshBusy { get; private set; } = false;

        private static readonly object LedgerRefreshlock = new();
        private static readonly object TradesRawRefreshlock = new();
        private static readonly object TradesRefreshlock = new();
        /////////////////////////////

        private static string? _settingCryptoCompareApiKey;

        public static string? SettingCryptoCompareApiKey
        {
            get
            {
                return _settingCryptoCompareApiKey;
            }
            set
            {
                _settingCryptoCompareApiKey = value;
                SaveSettingCryptoCompareApiKeyToDB(_settingCryptoCompareApiKey);
            }
        }

        private static void SaveSettingCryptoCompareApiKeyToDB(string? value)
        {
            try
            {
                using var connection = new SqliteConnection(connectionString);
                connection.Open();

                // First, delete any existing setting with the same name
                using (var deleteCommand = connection.CreateCommand())
                {
                    deleteCommand.CommandText = "DELETE FROM TB_SETTINGS_S WHERE Name = @Name";
                    deleteCommand.Parameters.AddWithValue("@Name", "CRYPTOCOMPARE_API_KEY");
                    deleteCommand.ExecuteNonQuery();
                }

                // Then, insert the new setting
                using (var insertCommand = connection.CreateCommand())
                {
                    insertCommand.CommandText = "INSERT INTO TB_SETTINGS_S (Name, Value) VALUES (@Name, @Value)";
                    insertCommand.Parameters.AddWithValue("@Name", "CRYPTOCOMPARE_API_KEY");
                    insertCommand.Parameters.AddWithValue("@Value", value);
                    insertCommand.ExecuteNonQuery();
                }
                connection.Close();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to save CryptoCompare API key to database", ex);
            }
        }

        public static void LoadSettingCryptoCompareApiKeyFromDB()
        {
            try
            {
                using SqliteConnection connection = new(connectionString);
                connection.Open();
                DbCommand command = connection.CreateCommand();
                command.CommandText = @"SELECT VALUE FROM TB_SETTINGS_S
										WHERE NAME = 'CRYPTOCOMPARE_API_KEY'";

                using (DbDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        _settingCryptoCompareApiKey = reader.GetString(0);
                    }
                    else
                    {
                        _settingCryptoCompareApiKey = null; // No setting found in the database
                    }
                }
                connection.Close();
            }
            catch (Exception ex)
            {
                _settingCryptoCompareApiKey = null;
                throw new InvalidOperationException("Failed to load CryptoCompare API key from database", ex);
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
                    deleteCommand.CommandText = "DELETE FROM TB_SETTINGS_S WHERE Name = @Name";
                    deleteCommand.Parameters.AddWithValue("@Name", "FIAT_CURRENCY");
                    deleteCommand.ExecuteNonQuery();
                }

                // Then, insert the new setting
                using (var insertCommand = connection.CreateCommand())
                {
                    insertCommand.CommandText = "INSERT INTO TB_SETTINGS_S (Name, Value) VALUES (@Name, @Value)";
                    insertCommand.Parameters.AddWithValue("@Name", "FIAT_CURRENCY");
                    insertCommand.Parameters.AddWithValue("@Value", value);
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
                command.CommandText = @"SELECT VALUE FROM TB_SETTINGS_S
										WHERE NAME = 'FIAT_CURRENCY'";

                using (DbDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        _settingFiatCurrency = reader.GetString(0);
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
                    deleteCommand.CommandText = "DELETE FROM TB_SETTINGS_S WHERE Name = @Name";
                    deleteCommand.Parameters.AddWithValue("@Name", "REWARDS_TAX_PERCENTAGE");
                    deleteCommand.ExecuteNonQuery();
                }

                // Then, insert the new setting
                using (var insertCommand = connection.CreateCommand())
                {
                    insertCommand.CommandText = "INSERT INTO TB_SETTINGS_S (Name, Value) VALUES (@Name, @Value)";
                    insertCommand.Parameters.AddWithValue("@Name", "REWARDS_TAX_PERCENTAGE");
                    insertCommand.Parameters.AddWithValue("@Value", value);
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
                command.CommandText = @"SELECT VALUE FROM TB_SETTINGS_S
										WHERE NAME = 'REWARDS_TAX_PERCENTAGE'";

                using (DbDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        string valueFromDB = reader.GetString(0);

                        if (string.IsNullOrEmpty(valueFromDB))
                        {
                            _settingRewardsTaxPercentage = 0m;
                        }
                        else
                        {
                            _settingRewardsTaxPercentage = ConvertStringToDecimal(valueFromDB);
                        }
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

        public class ValidationException(string message) : Exception(message)
        {
        }

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

        public static void RefreshLedgers(MainWindow? _mainWindow = null, string caller = "")
        {
            lock (LedgerRefreshlock) // Only one thread can enter this block at a time
            {
                if (LedgersRefreshBusy)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        string message = "There is already a ledgers refresh in progress. Please Wait";
                        MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        if (_mainWindow != null)
                        {
                            ConsoleLog(_mainWindow.txtLog, $"[{caller}] {message}");
                        }
                    });
                    return; // Exit the method here if refresh is already in progress
                }

                LedgersRefreshBusy = true;
            } // Release the lock here, allowing other threads to check LedgersRefreshBusy

            try
            {
                string? fiatCurrency = SettingFiatCurrency;

                if (_mainWindow != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ConsoleLog(_mainWindow.txtLog, $"[{caller}] Refreshing ledgers");
                    });
                }

                //System.Threading.Thread.Sleep(5000);

                using SqliteConnection connection = new(connectionString);
                connection.Open();

                // Prerequisite validations //
                List<string> missingAssetsManual = MissingAssetsManual(connection);
                if (missingAssetsManual.Count > 0)
                {
                    throw new ValidationException("Manual ledger asset(s) missing in asset catalog." + Environment.NewLine + "[Configure => Asset Catalog]");
                }

                /*
                // OBSOLETE CHECKS
                List<string> missingPairs = MissingPairs(connection);
                List<string> malconfiguredPairs = MalconfiguredPairs(connection);
                if (missingPairs.Count > 0 || malconfiguredPairs.Count > 0)
                {
                    throw new ValidationException("Kraken pair asset(s) missing in asset catalog." + Environment.NewLine + "[Configure => Kraken Pairs]");
                }
                */

                List<string> missingAssets = MissingAssets(connection);
                List<string> malconfiguredAssets = MalconfiguredAssets(connection);
                if (missingAssets.Count > 0 || malconfiguredAssets.Count > 0)
                {
                    throw new ValidationException("Kraken asset(s) missing in asset catalog." + Environment.NewLine + "[Configure => Kraken Assets]");
                }
                //////////////////////////////

                DbCommand commandDelete = connection.CreateCommand();

                // Truncate db table
                commandDelete.CommandText = "DELETE FROM TB_LEDGERS_S";
                commandDelete.ExecuteNonQuery();

                // Insert into db table
                DbCommand commandInsert = connection.CreateCommand();

                commandInsert.CommandText = $@"INSERT INTO TB_LEDGERS_S
                                                SELECT REFID AS REFID,
                                                DATETIME(TIME) AS DATE,
                                                UPPER(TYPE),
                                                'Kraken' AS EXCHANGE,
                                                printf('%.10f',AMOUNT) AS AMOUNT,
                                                assets_kraken.ASSET CURRENCY,
                                                printf('%.10f',FEE) AS FEE,
                                                CASE
                                                WHEN TYPE = 'staking' THEN 'Kraken'
                                                WHEN TYPE = 'earn' THEN 'Kraken'
                                                WHEN TYPE = 'trade' THEN ''
                                                WHEN TYPE = 'deposit' AND assets_kraken.ASSET = '{fiatCurrency}' THEN 'BANK'
                                                WHEN TYPE = 'deposit' AND assets_kraken.ASSET != '{fiatCurrency}' THEN 'WALLET'
                                                WHEN TYPE = 'withdrawal' THEN 'Kraken'
                                                WHEN SUBTYPE = 'spottofutures' THEN 'Kraken spot'
                                                WHEN SUBTYPE = 'futuresfromspot' THEN 'Kraken spot'
                                                WHEN SUBTYPE = 'futurestospot' THEN 'Kraken futures'
                                                WHEN SUBTYPE = 'spotfromfutures' THEN 'Kraken futures'
                                                WHEN SUBTYPE = 'stakingtospot' THEN 'Kraken staking'
                                                WHEN SUBTYPE = 'spotfromstaking' THEN 'Kraken staking'
                                                WHEN SUBTYPE = 'spottostaking' THEN 'Kraken spot'
                                                WHEN SUBTYPE = 'stakingfromspot' THEN 'Kraken spot'
                                                ELSE ''
                                                END AS SOURCE,
                                                CASE
                                                WHEN TYPE = 'staking' THEN 'Kraken'
                                                WHEN TYPE = 'earn' THEN 'Kraken'
                                                WHEN TYPE = 'trade' THEN ''
                                                WHEN TYPE = 'deposit' THEN 'Kraken'
                                                WHEN TYPE = 'withdrawal' AND assets_kraken.ASSET != '{fiatCurrency}' THEN 'WALLET'
                                                WHEN TYPE = 'withdrawal' AND assets_kraken.ASSET = '{fiatCurrency}'THEN 'BANK'
                                                WHEN SUBTYPE = 'spottofutures' THEN 'Kraken futures'
                                                WHEN SUBTYPE = 'futuresfromspot' THEN 'Kraken futures'
                                                WHEN SUBTYPE = 'futurestospot' THEN 'Kraken spot'
                                                WHEN SUBTYPE = 'spotfromfutures' THEN 'Kraken spot'
                                                WHEN SUBTYPE = 'stakingtospot' THEN 'Kraken spot'
                                                WHEN SUBTYPE = 'spotfromstaking' THEN 'Kraken spot'
                                                WHEN SUBTYPE = 'spottostaking' THEN 'Kraken staking'
                                                WHEN SUBTYPE = 'stakingfromspot' THEN 'Kraken staking'
                                                ELSE ''
                                                END AS TARGET,
                                                CASE
                                                WHEN TYPE = 'staking' THEN assets_kraken.ASSET || ' staking reward'
                                                WHEN TYPE = 'earn' AND SUBTYPE = 'reward' THEN assets_kraken.ASSET || ' staking reward'
                                                WHEN TYPE = 'earn' AND SUBTYPE = 'allocation' THEN 'Allocation ' || WALLET
                                                WHEN TYPE = 'earn' AND SUBTYPE = 'deallocation' THEN 'Deallocation ' || WALLET
                                                WHEN TYPE = 'earn' AND SUBTYPE = 'migration' THEN 'Migration ' || WALLET
                                                WHEN TYPE = 'deposit' AND assets_kraken.ASSET = '{fiatCurrency}' THEN 'From Bank to Kraken'
                                                WHEN TYPE = 'deposit' AND assets_kraken.ASSET != '{fiatCurrency}' THEN 'From wallet to Kraken'
                                                WHEN TYPE = 'withdrawal' AND assets_kraken.ASSET != '{fiatCurrency}' THEN 'From Kraken to wallet'
                                                WHEN SUBTYPE = 'spottofutures' THEN 'Start Kraken spot to Kraken futures'
                                                WHEN SUBTYPE = 'futuresfromspot' THEN 'Completed Kraken spot to Kraken futures'
                                                WHEN SUBTYPE = 'futurestospot' THEN 'Start Kraken futures to Kraken spot'
                                                WHEN SUBTYPE = 'spotfromfutures' THEN 'Completed Kraken futures to Kraken spot'
                                                WHEN SUBTYPE = 'stakingtospot' THEN 'Start Kraken staking to Kraken spot'
                                                WHEN SUBTYPE = 'spotfromstaking' THEN 'Completed Kraken staking to Kraken spot'
                                                WHEN SUBTYPE = 'spottostaking' THEN 'Start Kraken spot to Kraken staking'
                                                WHEN SUBTYPE = 'stakingfromspot' THEN 'Completed Kraken spot to Kraken staking'
                                                ELSE ''
                                                END AS NOTES
                                                FROM TB_LEDGERS_KRAKEN_S ledgers_kraken
                                                INNER JOIN TB_ASSET_CODES_KRAKEN_S assets_kraken
                                                ON ledgers_kraken.ASSET = assets_kraken.CODE
                                                WHERE REFID != ''
                                                --added 2025/01/21
                                                AND UPPER(SUBTYPE) NOT IN ('MIGRATION', 'ALLOCATION', 'DEALLOCATION')
                                                --
                                                UNION ALL
                                                SELECT REFID AS REFID,
                                                DATETIME(DATE) AS DATE,
                                                UPPER(TYPE),
                                                EXCHANGE,
                                                printf('%.10f',AMOUNT) AS AMOUNT,
                                                ASSET,
                                                printf('%.10f',FEE) AS FEE,
                                                SOURCE,
                                                TARGET,
                                                NOTES
                                                FROM TB_LEDGERS_MANUAL_S";

                commandInsert.ExecuteNonQuery();

                connection.Close();
                if (_mainWindow != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ConsoleLog(_mainWindow.txtLog, $"[{caller}] Refreshing ledgers done");
                    });
                }
            }
            catch (ValidationException ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                });
                if (_mainWindow != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ConsoleLog(_mainWindow.txtLog, $"[{caller}] Ledgers asset validation error");
                        ConsoleLog(_mainWindow.txtLog, $"[{caller}] Refreshing ledgers unsuccessful");
                    });
                }
                throw new Exception("RefreshLedgers failed", ex);
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("Failed to refresh ledgers." + Environment.NewLine + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                });
                if (_mainWindow != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ConsoleLog(_mainWindow.txtLog, $"[{caller}] " + ex.Message);
                        ConsoleLog(_mainWindow.txtLog, $"[{caller}] Refreshing ledgers unsuccessful");
                    });
                }
                throw new Exception("RefreshLedgers failed", ex);
            }
            finally
            {
                lock (LedgerRefreshlock) // Lock again to safely update LedgersRefreshBusy
                {
                    LedgersRefreshBusy = false;
                }
            }
        }

        public static void RefreshTradesRaw(MainWindow? _mainWindow = null, string caller = "") // OBSOLETE METHOD
        {
            lock (TradesRawRefreshlock) // Only one thread can enter this block at a time
            {
                if (TradesRawRefreshBusy)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show("There is already a raw trades refresh in progress. Please Wait", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                    return; // Exit the method here if refresh is already in progress
                }

                TradesRawRefreshBusy = true;
            } // Release the lock here, allowing other threads to check TradesRawRefreshBusy

            try
            {
                if (_mainWindow != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ConsoleLog(_mainWindow.txtLog, $"[{caller}] Refreshing raw trades");
                    });
                }

                using SqliteConnection connection = new(connectionString);
                connection.Open();

                DbCommand commandDelete = connection.CreateCommand();

                // Truncate db table
                commandDelete.CommandText = "DELETE FROM TB_TRADES_RAW_S";
                commandDelete.ExecuteNonQuery();

                // Insert into db table
                DbCommand commandInsert = connection.CreateCommand();

                commandInsert.CommandText = @"INSERT INTO TB_TRADES_RAW_S 
                                              SELECT DATETIME(TIME) AS DATE, 
                                              UPPER(TYPE) AS TYPE, 
                                              'Kraken' AS EXCHANGE, 
                                              printf('%.10f', VOL) AS BASE_AMOUNT, 
                                              codes.ASSET_LEFT BASE_CURRENCY, 
                                              printf('%.10f', COST) AS QUOTE_AMOUNT, 
                                              codes.ASSET_RIGHT AS QUOTE_CURRENCY,
                                              printf('%.10f', FEE) AS FEE
                                              FROM TB_TRADES_KRAKEN_S trades 
                                              INNER JOIN TB_PAIR_CODES_KRAKEN_S codes 
                                              ON trades.PAIR = codes.CODE";

                commandInsert.ExecuteNonQuery();

                connection.Close();
                if (_mainWindow != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ConsoleLog(_mainWindow.txtLog, $"[{caller}] Refreshing raw trades done");
                    });
                }
            }
            finally
            {
                lock (TradesRawRefreshlock) // Lock again to safely update TradesRawRefreshBusy
                {
                    TradesRawRefreshBusy = false;
                }
            }
        }

        public static async Task RefreshTrades(MainWindow? _mainWindow = null, string caller = "")
        {
            lock (TradesRefreshlock) // Only one thread can enter this block at a time
            {
                if (TradesRefreshBusy)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        string message = "There is already a trades refresh in progress. Please Wait";
                        MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        if (_mainWindow != null)
                        {
                            ConsoleLog(_mainWindow.txtLog, $"[{caller}] {message}");
                        }
                    });
                    return; // Exit the method here if refresh is already in progress
                }

                TradesRefreshBusy = true;
            } // Release the lock here, allowing other threads to check TradesRefreshBusy

            try
            {
                string? fiatCurrency = SettingFiatCurrency;

                if (_mainWindow != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ConsoleLog(_mainWindow.txtLog, $"[{caller}] Refreshing trades");
                    });
                }

                //System.Threading.Thread.Sleep(5000);

                using SqliteConnection connection = new(connectionString);
                connection.Open();

                DbCommand commandDelete = connection.CreateCommand();

                // Truncate db table
                commandDelete.CommandText = "DELETE FROM TB_TRADES_S";
                commandDelete.ExecuteNonQuery();

                // Insert into db table
                DbCommand commandInsert = connection.CreateCommand();

                commandInsert.CommandText = $@"INSERT INTO TB_TRADES_S 
                                                SELECT 
												    REFID, 
												    DATE, 
												    TYPE, 
												    EXCHANGE, 
												    BASE_CURRENCY, 
												    ABS(BASE_AMOUNT) AS BASE_AMOUNT, 
												    BASE_FEE,
												    CASE 
													    WHEN QUOTE_CURRENCY != '{fiatCurrency}' THEN NULL
													    ELSE printf('%.10f', BASE_FEE * BASE_UNIT_PRICE_FIAT)
												    END AS BASE_FEE_FIAT,
												    QUOTE_CURRENCY, 
												    ABS(QUOTE_AMOUNT) AS QUOTE_AMOUNT, 
												    ABS(QUOTE_AMOUNT_FIAT) AS QUOTE_AMOUNT_FIAT,
												    QUOTE_FEE,
												    CASE 
													    WHEN QUOTE_CURRENCY != '{fiatCurrency}' THEN NULL
													    ELSE QUOTE_FEE
												    END AS QUOTE_FEE_FIAT,
												    BASE_UNIT_PRICE, 
												    BASE_UNIT_PRICE_FIAT,
												    QUOTE_UNIT_PRICE, 
												    QUOTE_UNIT_PRICE_FIAT,
                                                    BASE_FEE_FIAT + QUOTE_FEE_FIAT AS TOTAL_FEE_FIAT,
												    CASE 
													    WHEN QUOTE_CURRENCY = '{fiatCurrency}' AND TYPE = 'BUY' THEN ABS(QUOTE_AMOUNT) + BASE_FEE_FIAT + QUOTE_FEE_FIAT
													    WHEN QUOTE_CURRENCY = '{fiatCurrency}' AND TYPE = 'SELL' THEN ABS(QUOTE_AMOUNT) - BASE_FEE_FIAT - QUOTE_FEE_FIAT
													    ELSE NULL
												    END AS COSTS_PROCEEDS											
											    FROM (
												    SELECT 
													    REFID, 
													    DATE, 
													    TYPE, 
													    EXCHANGE, 
													    BASE_CURRENCY, 
													    BASE_AMOUNT, 
													    BASE_FEE,
													    CASE 
														    WHEN QUOTE_CURRENCY != '{fiatCurrency}' THEN NULL
														    ELSE printf('%.10f', BASE_FEE * BASE_UNIT_PRICE_FIAT)
													    END AS BASE_FEE_FIAT,
													    QUOTE_CURRENCY, 
													    QUOTE_AMOUNT, 
													    QUOTE_AMOUNT_FIAT,
													    QUOTE_FEE,
													    CASE 
														    WHEN QUOTE_CURRENCY != '{fiatCurrency}' THEN NULL
														    ELSE QUOTE_FEE
													    END AS QUOTE_FEE_FIAT,
													    BASE_UNIT_PRICE, 
													    BASE_UNIT_PRICE_FIAT,
													    QUOTE_UNIT_PRICE, 
													    QUOTE_UNIT_PRICE_FIAT
												    FROM (
													    SELECT 
														    REFID, 
														    DATE, 
														    TYPE, 
														    EXCHANGE, 
														    BASE_CURRENCY, 
														    BASE_AMOUNT, 
														    BASE_FEE, 
														    QUOTE_CURRENCY, 
														    QUOTE_AMOUNT,
														    CASE 
															    WHEN QUOTE_CURRENCY != '{fiatCurrency}' THEN NULL
															    ELSE QUOTE_AMOUNT
														    END AS QUOTE_AMOUNT_FIAT,												
														    QUOTE_FEE,
														    printf('%.10f', ABS(QUOTE_AMOUNT / BASE_AMOUNT)) AS BASE_UNIT_PRICE,
														    CASE 
															    WHEN QUOTE_CURRENCY != '{fiatCurrency}' THEN NULL
															    ELSE printf('%.10f', ABS(QUOTE_AMOUNT / BASE_AMOUNT))
														    END AS BASE_UNIT_PRICE_FIAT,
														    printf('%.10f', ABS(BASE_AMOUNT / QUOTE_AMOUNT)) AS QUOTE_UNIT_PRICE,
														    CASE 
															    WHEN QUOTE_CURRENCY != '{fiatCurrency}' THEN NULL
															    ELSE printf('%.10f', 1)
														    END AS QUOTE_UNIT_PRICE_FIAT
													    FROM (
														    -- BUY => RIGHT = negative FIAT
														    SELECT 
															    b.REFID, 
															    b.DATE, 
															    'BUY' AS TYPE, 
															    b.EXCHANGE, 
															    b.CURRENCY AS BASE_CURRENCY, 
															    b.AMOUNT AS BASE_AMOUNT, 
															    b.FEE AS BASE_FEE, 
															    q.CURRENCY AS QUOTE_CURRENCY, 
															    q.AMOUNT AS QUOTE_AMOUNT, 
															    q.FEE AS QUOTE_FEE 
														    FROM 
															    (SELECT * FROM TB_LEDGERS_S WHERE TYPE = 'TRADE' AND AMOUNT > 0) b
														    INNER JOIN 
															    (SELECT * FROM TB_LEDGERS_S WHERE TYPE = 'TRADE' AND AMOUNT < 0 AND CURRENCY = '{fiatCurrency}') q
														    ON b.REFID = q.REFID

														    UNION ALL

														    -- SELL => RIGHT = positive FIAT
														    SELECT 
															    b.REFID, 
															    b.DATE, 
															    'SELL' AS TYPE, 
															    b.EXCHANGE, 
															    b.CURRENCY AS BASE_CURRENCY, 
															    b.AMOUNT AS BASE_AMOUNT, 
															    b.FEE AS BASE_FEE, 
															    q.CURRENCY AS QUOTE_CURRENCY, 
															    q.AMOUNT AS QUOTE_AMOUNT, 
															    q.FEE AS QUOTE_FEE 
														    FROM 
															    (SELECT * FROM TB_LEDGERS_S WHERE TYPE = 'TRADE' AND AMOUNT < 0) b
														    INNER JOIN 
															    (SELECT * FROM TB_LEDGERS_S WHERE TYPE = 'TRADE' AND AMOUNT > 0 AND CURRENCY = '{fiatCurrency}') q
														    ON b.REFID = q.REFID

														    UNION ALL

														    -- SELL => LEFT != FIAT, RIGHT != FIAT, LEFT negative amount
														    SELECT 
															    b.REFID, 
															    b.DATE, 
															    'SELL' AS TYPE, 
															    b.EXCHANGE, 
															    b.CURRENCY AS BASE_CURRENCY, 
															    b.AMOUNT AS BASE_AMOUNT, 
															    b.FEE AS BASE_FEE, 
															    q.CURRENCY AS QUOTE_CURRENCY, 
															    q.AMOUNT AS QUOTE_AMOUNT, 
															    q.FEE AS QUOTE_FEE 
														    FROM 
															    (SELECT * FROM TB_LEDGERS_S WHERE TYPE = 'TRADE' AND AMOUNT < 0 AND CURRENCY != '{fiatCurrency}') b
														    INNER JOIN 
															    (SELECT * FROM TB_LEDGERS_S WHERE TYPE = 'TRADE' AND AMOUNT > 0 AND CURRENCY != '{fiatCurrency}') q
														    ON b.REFID = q.REFID
													    )
												    )
											    )";

                commandInsert.ExecuteNonQuery();

                // Update crypto-only entries
                string? exceptionMessage = null;
                Exception? innerExceptionMessage = null;
                try
                {
                    DbCommand command = connection.CreateCommand();
                    command.CommandText = $@"SELECT trades.REFID,
                                             trades.DATE,
									         catalog_base.CODE as BASE_CODE,
                                             trades.BASE_FEE,
									         catalog_quote.CODE as QUOTE_CODE,
                                             trades.QUOTE_AMOUNT,
									         trades.QUOTE_FEE
                                             FROM TB_TRADES_S trades
									         LEFT JOIN TB_ASSET_CATALOG_S catalog_base
									         on trades.BASE_CURRENCY = catalog_base.ASSET
									         LEFT JOIN TB_ASSET_CATALOG_S catalog_quote
									         on trades.QUOTE_CURRENCY = catalog_quote.ASSET
                                             WHERE trades.BASE_CURRENCY != '{fiatCurrency}'
                                             AND trades.QUOTE_CURRENCY != '{fiatCurrency}'";

                    List<(string RefId, Dictionary<string, string> UpdateData)> updates = [];

                    using (DbDataReader reader = command.ExecuteReader())
                    {
                        // Rate limiting mechanism //
                        DateTime lastCallTime = DateTime.MinValue;
                        /////////////////////////////
                        while (reader.Read())
                        {
                            string refid = reader.GetString(0);
                            string date = reader.GetString(1);
                            string base_code = reader.GetString(2);
                            string base_fee = reader.GetString(3);
                            string quote_code = reader.GetString(4);
                            string quote_amount = reader.GetString(5);
                            string quote_fee = reader.GetString(6);

                            DateTime datetime = DateTime.ParseExact(date, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                            // Calculate base_fee_fiat
                            var (base_unit_price_fiat, baseConversionSource) = ConvertXToFiat(base_code, 1m, datetime.Date, connection);
                            string base_fee_fiat = (ConvertStringToDecimal(base_unit_price_fiat) * ConvertStringToDecimal(base_fee)).ToString("F10");

                            // Rate limiting mechanism //
                            if (baseConversionSource == "API")
                            {
                                if ((DateTime.Now - lastCallTime).TotalSeconds < 1)
                                {
                                    // Calculate delay to ensure at least 1 seconds have passed
                                    int delay = Math.Max(0, (int)((lastCallTime.AddSeconds(1) - DateTime.Now).TotalMilliseconds));
                                    await Task.Delay(delay);
                                }
                            }
                            lastCallTime = DateTime.Now;
                            /////////////////////////////

                            var (quote_unit_price_fiat, quoteConversionSource) = ConvertXToFiat(quote_code, 1m, datetime.Date, connection);
                            string quote_fee_fiat = (ConvertStringToDecimal(quote_unit_price_fiat) * ConvertStringToDecimal(quote_fee)).ToString("F10");

                            // Rate limiting mechanism //
                            if (quoteConversionSource == "API")
                            {
                                if ((DateTime.Now - lastCallTime).TotalSeconds < 1)
                                {
                                    // Calculate delay to ensure at least 1 seconds have passed
                                    int delay = Math.Max(0, (int)((lastCallTime.AddSeconds(1) - DateTime.Now).TotalMilliseconds));
                                    await Task.Delay(delay);
                                }
                            }
                            lastCallTime = DateTime.Now;
                            /////////////////////////////

                            string total_fee_fiat = (ConvertStringToDecimal(base_fee_fiat) + ConvertStringToDecimal(quote_fee_fiat)).ToString("F10");

                            string quote_amount_fiat = (ConvertStringToDecimal(quote_unit_price_fiat) * ConvertStringToDecimal(quote_amount)).ToString("F10");
                            string costs_proceeds = (Math.Abs(ConvertStringToDecimal(quote_amount_fiat)) - ConvertStringToDecimal(base_fee_fiat) - ConvertStringToDecimal(quote_fee_fiat)).ToString("F10");

                            var updateData = new Dictionary<string, string>
                            {
                            { "Base_fee_fiat", base_fee_fiat },
                            { "Quote_amount_fiat", quote_amount_fiat },
                            { "Quote_fee_fiat", quote_fee_fiat },
                            { "Base_unit_price_fiat", base_unit_price_fiat },
                            { "Quote_unit_price_fiat", quote_unit_price_fiat },
                            { "Total_fee_fiat", total_fee_fiat },
                            { "Costs_proceeds", costs_proceeds }
                            };

                            updates.Add((refid, updateData));
                        }
                    }

                    // Perform the updates
                    using var transaction = connection.BeginTransaction();
                    try
                    {
                        foreach (var (RefId, UpdateData) in updates)
                        {
                            using var commandUpdate = connection.CreateCommand();
                            commandUpdate.Transaction = transaction; // Assign the transaction to the command
                            commandUpdate.CommandText = @"UPDATE TB_TRADES_S 
                                                            SET BASE_FEE_FIAT = @Base_fee_fiat,
                                                            QUOTE_AMOUNT_FIAT = @Quote_amount_fiat,
                                                            QUOTE_FEE_FIAT = @Quote_fee_fiat,
                                                            BASE_UNIT_PRICE_FIAT = @Base_unit_price_fiat,
                                                            QUOTE_UNIT_PRICE_FIAT = @Quote_unit_price_fiat,
                                                            TOTAL_FEE_FIAT = @Total_fee_fiat,
                                                            COSTS_PROCEEDS = @Costs_proceeds
                                                            WHERE REFID = @RefId";

                            commandUpdate.Parameters.AddWithValue("@RefId", RefId);
                            foreach (var kvp in UpdateData) // kvp = Key-Value Pair
                            {
                                commandUpdate.Parameters.AddWithValue("@" + kvp.Key, kvp.Value);
                            }

                            commandUpdate.ExecuteNonQuery();
                        }
                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    innerExceptionMessage = ex;
                    exceptionMessage = "Updating crypto-only entries failed";
                }

                connection.Close();
                if (_mainWindow != null)
                {
                    if (exceptionMessage != null)
                    {
                        throw new Exception(exceptionMessage, innerExceptionMessage);
                    }
                    else
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            ConsoleLog(_mainWindow.txtLog, $"[{caller}] Refreshing trades done");
                        });
                    }
                }
                else
                {
                    if (exceptionMessage != null)
                    {
                        throw new Exception(exceptionMessage, innerExceptionMessage);
                    }
                }
            }
            finally
            {
                lock (TradesRefreshlock) // Lock again to safely update TradesRefreshBusy
                {
                    TradesRefreshBusy = false;
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

        public static void CloseWindow(Window window)
        {
            window.Close();
        }

        public static void FileDialog(TextBox txtFileName)
        {
            // Create OpenFileDialog
            Microsoft.Win32.OpenFileDialog openFileDlg = new()
            {
                // Set filter for file extension and default file extension
                DefaultExt = ".csv",
                Filter = "CSV Files (*.csv)|*.csv"
            };

            // Launch OpenFileDialog by calling ShowDialog method
            bool? result = openFileDlg.ShowDialog();

            // Get the selected file name and display in a TextBox.
            if (result == true)
            {
                // Store the filename in the textbox and global
                txtFileName.Text = openFileDlg.FileName;
            }
        }

        public static void ShowAndFocusSubWindow(Window window, Window ownerWindow)
        {
            ShowAndFocusWindow(window);
            window.Owner = ownerWindow;
        }

        public static void ShowAndFocusWindow(Window window)
        {
            window.Show();
            window.WindowState = WindowState.Normal;
            window.Activate();
            window.Focus();
        }

        public static void ConsoleLog(TextBox txtLog, String logText)
        {
            // Get the current time in the specified format
            string timestamp = DateTime.Now.ToString("HH:mm:ss");

            // Append text
            if (string.IsNullOrWhiteSpace(txtLog.Text))
            {
                txtLog.AppendText($"{timestamp} {logText}");
            }

            else
            {
                txtLog.AppendText($"\n{timestamp} {logText}");
            }

            // Scroll the TextBox to the bottom
            txtLog.ScrollToEnd();
        }

        public static (string fiatAmount, string source) ConvertXToFiat(string xCurrency, decimal xAmount, DateTime date, SqliteConnection connection)
        {
            try
            {
                string? fiatCurrency = SettingFiatCurrency;

                decimal? exchangeRateApi = null;
                decimal? exchangeRateDb = null;
                ///////////////////////////
                string formattedDate = date.ToString("yyyy-MM-dd HH:mm:ss");
                ///////////////////////////
                DbCommand command = connection.CreateCommand();
                command.CommandText = $@"SELECT EXCHANGE_RATE FROM
									     TB_CONVERT_X_TO_FIAT_A
                                         WHERE CURRENCY = '{xCurrency}'
                                         AND AMOUNT = '{xAmount}'
                                         AND DATE = '{formattedDate}'
                                         AND FIAT_CURRENCY = '{fiatCurrency}'";

                using (DbDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        exchangeRateDb = reader.GetDecimal(0);
                    }
                }
                ///////////////////////////

                if (!exchangeRateDb.HasValue)
                {
                    using var client = new HttpClient();

                    int unixTimestamp = ((int)(date - new DateTime(1970, 1, 1).ToLocalTime()).TotalSeconds);

                    client.BaseAddress = new Uri("https://min-api.cryptocompare.com");
                    string? fiat_currency = SettingFiatCurrency;
                    string? api_key = SettingCryptoCompareApiKey;
                    var response = client.GetAsync($"/data/v2/histoday?limit=1&fsym=" + xCurrency + "&tsym=" + fiat_currency + "&toTs=" + unixTimestamp + "&api_key={" + api_key + "}").Result;
                    var result = response.Content.ReadAsStringAsync().Result;
                    var json = JObject.Parse(result);

                    string? api_response_result = json["Response"]?.ToString();
                    string? api_response_message = json["Message"]?.ToString();

                    if (string.IsNullOrEmpty(api_key) || api_response_result != "Success")
                    {
                        string? error_message = null;
                        if (string.IsNullOrEmpty(api_key))
                        {
                            error_message = "CryptoCompare API call failed: API key is not set";
                        }
                        else
                        {
                            error_message = "CryptoCompare API call failed: " + api_response_message;
                        }
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show(error_message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        });
                        throw new Exception(error_message);
                    }

                    exchangeRateApi = decimal.Parse(json["Data"]?["Data"]?[1]?["close"]?.ToString() ?? "0.00");
                }

                ///////////////////////////
                if (exchangeRateApi > 0)
                {
                    using var commandInsert = connection.CreateCommand();

                    commandInsert.CommandText = @"INSERT INTO TB_CONVERT_X_TO_FIAT_A (CURRENCY, AMOUNT, DATE, FIAT_CURRENCY, EXCHANGE_RATE)
                                              VALUES (@Currency, @Amount, @Date, @Fiat_currency, @Exchange_rate)";

                    commandInsert.Parameters.AddWithValue("@Currency", xCurrency);
                    commandInsert.Parameters.AddWithValue("@Amount", xAmount.ToString());
                    commandInsert.Parameters.AddWithValue("@Date", formattedDate);
                    commandInsert.Parameters.AddWithValue("@Fiat_currency", fiatCurrency);
                    commandInsert.Parameters.AddWithValue("@Exchange_rate", exchangeRateApi);

                    commandInsert.ExecuteNonQuery();
                }
                ///////////////////////////
                decimal fiatAmount = 0;
                string source = "";

                if (exchangeRateDb.HasValue)
                {
                    fiatAmount = (decimal)(xAmount * exchangeRateDb);
                    source = "DB";
                }
                else if (exchangeRateApi.HasValue)
                {
                    fiatAmount = (decimal)(xAmount * exchangeRateApi);
                    source = "API";
                }

                return (fiatAmount.ToString("0.0000000000", NumberFormatInfo.InvariantInfo), source);
            }
            catch (Exception ex)
            {
                throw new Exception("ConvertXToFiat failed", ex);
            }
        }

        public static decimal ConvertStringToDecimal(string input)
        {
            if (decimal.TryParse(input, out decimal tryParsedAmount))
            {
                return tryParsedAmount;
            }
            else
            {
                return 0.0000000000m;
            }
        }

        public static string? NullIfEmpty(this string s)
        {
            return string.IsNullOrEmpty(s) ? null : s;
        }

        public static bool IsValidDateFormat(string date, string format)
        {
            // Try to parse the date using the specified format
            return DateTime.TryParseExact(date, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
        }

        public static string TextBlockContentSplit(TextBlock tbInput)
        {
            string output = "";
            foreach (Inline inline in tbInput.Inlines)
            {
                output += inline.ContentStart.GetTextInRun(LogicalDirection.Forward) + "\n";
            }
            return output;
        }

        public static List<string> MissingPairs(SqliteConnection connection) // OBSOLETE METHOD
        {
            List<string> missingPairs = [];

            DbCommand command = connection.CreateCommand();
            command.CommandText = @"SELECT DISTINCT(trades_kraken.PAIR) AS PAIR
                                    FROM TB_TRADES_KRAKEN_S trades_kraken
								    LEFT OUTER JOIN
								    TB_PAIR_CODES_KRAKEN_S pairs_kraken
								    ON trades_kraken.PAIR = pairs_kraken.CODE
								    WHERE pairs_kraken.ASSET_LEFT IS NULL";

            using (DbDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    if (reader[0] is string asset)
                    {
                        missingPairs.Add(asset);
                    }
                }
            }

            return missingPairs;
        }

        public static List<string> MalconfiguredPairs(SqliteConnection connection) // OBSOLETE METHOD
        {
            List<string> malfconfiguredPair = [];

            DbCommand command = connection.CreateCommand();
            command.CommandText = @"SELECT DISTINCT(CODE) FROM
                                    (SELECT pairs_kraken.code AS CODE FROM
									TB_PAIR_CODES_KRAKEN_S pairs_kraken
									LEFT OUTER JOIN
									TB_ASSET_CATALOG_S catalog
									ON pairs_kraken.ASSET_LEFT = catalog.ASSET
									WHERE catalog.CODE IS NULL
									UNION ALL
									SELECT pairs_kraken.code AS CODE FROM TB_PAIR_CODES_KRAKEN_S pairs_kraken
									LEFT OUTER JOIN
									TB_ASSET_CATALOG_S catalog
									ON pairs_kraken.ASSET_RIGHT = catalog.ASSET
									WHERE catalog.CODE IS NULL)";

            using (DbDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    if (reader[0] is string code)
                    {
                        malfconfiguredPair.Add(code);
                    }
                }
            }

            return malfconfiguredPair;
        }

        public static List<string> MissingAssets(SqliteConnection connection)
        {
            List<string> missingAssets = [];

            DbCommand command = connection.CreateCommand();
            command.CommandText = @"SELECT DISTINCT(ledgers_kraken.ASSET) AS ASSET 
                                    FROM TB_LEDGERS_KRAKEN_S ledgers_kraken
								    LEFT OUTER JOIN
								    TB_ASSET_CODES_KRAKEN_S assets_kraken
								    ON ledgers_kraken.ASSET = assets_kraken.CODE
								    WHERE assets_kraken.ASSET IS NULL";

            using (DbDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    if (reader[0] is string asset)
                    {
                        missingAssets.Add(asset);
                    }
                }
            }

            return missingAssets;
        }

        public static List<string> MalconfiguredAssets(SqliteConnection connection)
        {
            List<string> malfconfiguredAsset = [];

            DbCommand command = connection.CreateCommand();
            command.CommandText = @"SELECT assets_kraken.CODE FROM
									TB_ASSET_CODES_KRAKEN_S assets_kraken
									LEFT OUTER JOIN
									TB_ASSET_CATALOG_S catalog
									ON assets_kraken.ASSET = catalog.ASSET
									WHERE catalog.CODE IS NULL";

            using (DbDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    if (reader[0] is string code)
                    {
                        malfconfiguredAsset.Add(code);
                    }
                }
            }

            return malfconfiguredAsset;
        }

        public static List<string> MissingAssetsManual(SqliteConnection connection)
        {
            List<string> missingAssets = [];

            DbCommand command = connection.CreateCommand();
            command.CommandText = @"SELECT ledgers_manual.ASSET 
                                    FROM TB_LEDGERS_MANUAL_S ledgers_manual
                                    LEFT OUTER JOIN TB_ASSET_CATALOG_S catalog
                                    ON ledgers_manual.ASSET = catalog.ASSET
                                    WHERE catalog.CODE IS NULL";

            using (DbDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    if (reader[0] is string asset)
                    {
                        missingAssets.Add(asset);
                    }
                }
            }

            return missingAssets;
        }

        public static void OpenHelp(string filename)
        {
            string helpfilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "help", filename);

            if (System.IO.File.Exists(helpfilePath))
            {
                try
                {
                    // Opens the file with the default application for .html files, which is usually the default web browser
                    Process.Start(new ProcessStartInfo(helpfilePath) { UseShellExecute = true });
                }
                catch (System.ComponentModel.Win32Exception ex)
                {
                    MessageBox.Show("The help file could not be opened." + Environment.NewLine + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("The help file does not exist", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}