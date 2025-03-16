using FirebirdSql.Data.FirebirdClient;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Windows;
using static ProjectCryptoGains.Common.Utils.DatabaseUtils;
using static ProjectCryptoGains.Common.Utils.ExceptionUtils;
using static ProjectCryptoGains.Common.Utils.SettingUtils;
using static ProjectCryptoGains.Common.Utils.Utils;
using static ProjectCryptoGains.Common.Utils.ValidationUtils;

namespace ProjectCryptoGains.Common.Utils
{
    public static class LedgersUtils
    {
        // Parallel run prevention //
        public static bool LedgersRefreshBusy { get; private set; } = false;
        private static readonly object LedgerRefreshlock = new();
        /////////////////////////////

        public static string? RefreshLedgers(MainWindow _mainWindow, Caller caller)
        {
            lock (LedgerRefreshlock) // Only one thread can enter this block at a time
            {
                if (LedgersRefreshBusy)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        string message = "There is already a ledgers refresh in progress. Please Wait.";
                        CustomMessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        if (caller != Caller.Ledgers)
                        {
                            ConsoleLog(_mainWindow.txtLog, $"[{caller}] {message}");
                        }
                    });
                    return null; // Exit the method here if refresh is already in progress
                }

                LedgersRefreshBusy = true;
            } // Release the lock here, allowing other threads to check LedgersRefreshBusy

            try
            {
                string? lastWarning = null;
                string fiatCurrency = SettingFiatCurrency;

                if (caller != Caller.Ledgers)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ConsoleLog(_mainWindow.txtLog, $"[{caller}] Refreshing ledgers");
                    });
                }

                using (FbConnection connection = new(connectionString))
                {
                    connection.Open();

                    // Prerequisite validations //
                    List<string> missingAssetsManual = MissingAssetsManual(connection);
                    if (missingAssetsManual.Count > 0)
                    {
                        throw new ValidationException("Manual ledger asset(s) missing in asset catalog." + Environment.NewLine + "[Configure => Asset Catalog]");
                    }

                    List<string> missingAssets = MissingAssets(connection);
                    List<string> malconfiguredAssets = MalconfiguredAssets(connection);
                    if (missingAssets.Count > 0 || malconfiguredAssets.Count > 0)
                    {
                        throw new ValidationException("Kraken asset(s) missing in asset catalog." + Environment.NewLine + "[Configure => Kraken Assets]");
                    }

                    // Check for unsupported ledger types
                    // Manual Ledgers
                    List<(string RefId, string Type)> unsupportedTypes = UnsupportedTypes(connection, LedgerSource.Manual);
                    if (unsupportedTypes.Count > 0)
                    {
                        string lastWarningPrefix = $"[{caller}]";
                        if (caller != Caller.Ledgers)
                        {
                            lastWarningPrefix = $"[{caller}][Ledgers]";
                        }

                        lastWarning = "Unsupported manual ledger type(s) detected." + Environment.NewLine + "Review csv; Unsupported ledger type(s) will not be taken into account.";
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            CustomMessageBox.Show(lastWarning, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning, TextAlignment.Left);
                            ConsoleLog(_mainWindow.txtLog, $"{lastWarningPrefix} {lastWarning}");

                            // Log each unsupported ledger type
                            foreach ((string RefId, string Type) in unsupportedTypes)
                            {
                                ConsoleLog(_mainWindow.txtLog, $"{lastWarningPrefix} Unsupported manual ledger type:" + Environment.NewLine + $"REFID: {RefId}, TYPE: {Type}");
                            }
                        });
                    }

                    // Kraken Ledgers
                    unsupportedTypes = UnsupportedTypes(connection, LedgerSource.Kraken);
                    if (unsupportedTypes.Count > 0)
                    {
                        string lastWarningPrefix = $"[{caller}]";
                        if (caller != Caller.Ledgers)
                        {
                            lastWarningPrefix = $"[{caller}][Ledgers]";
                        }

                        lastWarning = "Unsupported kraken ledger type(s) detected." + Environment.NewLine + "Review csv; Unsupported ledger type(s) will not be taken into account.";
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            CustomMessageBox.Show(lastWarning, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning, TextAlignment.Left);
                            ConsoleLog(_mainWindow.txtLog, $"{lastWarningPrefix} {lastWarning}");

                            // Log each unsupported ledger type
                            foreach ((string RefId, string Type) in unsupportedTypes)
                            {
                                ConsoleLog(_mainWindow.txtLog, $"{lastWarningPrefix} Unsupported kraken ledger type:" + Environment.NewLine + $"REFID: {RefId}, TYPE: {Type}");
                            }
                        });
                    }
                    //////////////////////////////

                    using DbCommand deleteCommand = connection.CreateCommand();

                    // Truncate db table
                    deleteCommand.CommandText = "DELETE FROM TB_LEDGERS";
                    deleteCommand.ExecuteNonQuery();

                    // Insert into db table
                    using DbCommand insertCommand = connection.CreateCommand();
                    insertCommand.CommandText = $@"INSERT INTO TB_LEDGERS (REFID, ""DATE"", TYPE_SOURCE, TYPE, EXCHANGE, AMOUNT, CURRENCY, FEE, SOURCE, TARGET, NOTES)
                                                   SELECT 
                                                       REFID AS REFID,
                                                       ""TIME"" AS ""DATE"",
                                                       UPPER(TYPE) AS TYPE_SOURCE,
                                                       CASE
                                                           WHEN UPPER(TYPE) = 'SPEND' THEN 'TRADE'
                                                           WHEN UPPER(TYPE) = 'RECEIVE' THEN 'TRADE'
                                                           WHEN UPPER(TYPE) = 'EARN' THEN 'STAKING'
                                                           ELSE UPPER(TYPE)
                                                       END AS TYPE,
                                                       'Kraken' AS EXCHANGE,
                                                       ROUND(AMOUNT, 10) AS AMOUNT,
                                                       assets_kraken.ASSET AS CURRENCY,
                                                       ROUND(FEE, 10) AS FEE,
                                                       CASE
                                                           WHEN UPPER(TYPE) IN ('STAKING', 'EARN') THEN 'Kraken'
                                                           WHEN UPPER(TYPE) = 'DEPOSIT' AND assets_kraken.ASSET = '{fiatCurrency}' THEN 'BANK'
                                                           WHEN UPPER(TYPE) = 'DEPOSIT' AND assets_kraken.ASSET != '{fiatCurrency}' THEN 'WALLET'
                                                           WHEN UPPER(TYPE) = 'WITHDRAWAL' THEN 'Kraken'
                                                           ELSE TRIM('')
                                                       END AS SOURCE,
                                                       CASE
                                                           WHEN UPPER(TYPE) IN ('STAKING', 'EARN') THEN 'Kraken'
                                                           WHEN UPPER(TYPE) = 'DEPOSIT' THEN 'Kraken'
                                                           WHEN UPPER(TYPE) = 'WITHDRAWAL' AND assets_kraken.ASSET != '{fiatCurrency}' THEN 'WALLET'
                                                           WHEN UPPER(TYPE) = 'WITHDRAWAL' AND assets_kraken.ASSET = '{fiatCurrency}' THEN 'BANK'
                                                           ELSE TRIM('')
                                                       END AS TARGET,
                                                       CASE
                                                           WHEN UPPER(TYPE) IN ('STAKING', 'EARN') THEN assets_kraken.ASSET || ' staking reward'
                                                           WHEN UPPER(TYPE) = 'DEPOSIT' AND assets_kraken.ASSET = '{fiatCurrency}' THEN 'From Bank to Kraken'
                                                           WHEN UPPER(TYPE) = 'DEPOSIT' AND assets_kraken.ASSET != '{fiatCurrency}' THEN 'From wallet to Kraken'
                                                           WHEN UPPER(TYPE) = 'WITHDRAWAL' AND assets_kraken.ASSET != '{fiatCurrency}' THEN 'From Kraken to wallet'
                                                           WHEN UPPER(TYPE) = 'WITHDRAWAL' AND assets_kraken.ASSET = '{fiatCurrency}' THEN 'From Kraken to Bank'
                                                           ELSE TRIM('')
                                                       END AS NOTES
                                                   FROM TB_LEDGERS_KRAKEN ledgers_kraken
                                                   INNER JOIN TB_ASSET_CODES_KRAKEN assets_kraken
                                                       ON ledgers_kraken.ASSET = assets_kraken.CODE
                                                   WHERE REFID != ''
                                                       AND NOT (UPPER(TYPE) = 'TRANSFER' AND UPPER(SUBTYPE) IN ('STAKINGFROMSPOT', 'SPOTTOSTAKING', 'SPOTFROMSTAKING', 'STAKINGTOSPOT', 'SPOTFROMFUTURES'))
                                                       AND NOT (UPPER(TYPE) = 'EARN' AND UPPER(SUBTYPE) IN ('MIGRATION', 'ALLOCATION', 'DEALLOCATION'))
                                                   UNION ALL
                                                   SELECT 
                                                       REFID AS REFID,
                                                       ""DATE"",
                                                       UPPER(TYPE) AS TYPE_SOURCE,
                                                       UPPER(TYPE) AS TYPE,
                                                       EXCHANGE,
                                                       ROUND(AMOUNT, 10) AS AMOUNT,
                                                       ASSET AS CURRENCY,
                                                       ROUND(FEE, 10) AS FEE,
                                                       SOURCE,
                                                       TARGET,
                                                       NOTES
                                                   FROM TB_LEDGERS_MANUAL";

                    insertCommand.ExecuteNonQuery();
                }

                if (caller != Caller.Ledgers)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (lastWarning == null)
                        {
                            ConsoleLog(_mainWindow.txtLog, $"[{caller}] Refreshing ledgers done");
                        }
                        else
                        {
                            ConsoleLog(_mainWindow.txtLog, $"[{caller}] Refreshing ledgers done with warnings");
                        }
                    });
                }

                return lastWarning;
            }
            catch (ValidationException ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    CustomMessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                });
                if (caller != Caller.Ledgers)
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
                    CustomMessageBox.Show("Failed to refresh ledgers." + Environment.NewLine + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                });
                if (caller != Caller.Ledgers)
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
    }
}