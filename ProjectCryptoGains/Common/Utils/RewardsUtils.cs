using FirebirdSql.Data.FirebirdClient;
using System;
using System.Data.Common;
using System.Threading.Tasks;
using System.Windows;
using static ProjectCryptoGains.Common.Utils.DatabaseUtils;
using static ProjectCryptoGains.Common.Utils.DateUtils;
using static ProjectCryptoGains.Common.Utils.SettingsUtils;
using static ProjectCryptoGains.Common.Utils.Utils;

namespace ProjectCryptoGains.Common.Utils
{
    public static class RewardsUtils
    {
        // Parallel run prevention //
        public static bool RewardsRefreshBusy { get; private set; } = false;
        private static readonly object _RewardsRefreshLock = new();
        /////////////////////////////

        public static async Task<string?> RefreshRewards(MainWindow mainWindow, Caller caller, String fromDate, String toDate)
        {
            lock (_RewardsRefreshLock) // Only one thread can enter this block at a time
            {
                if (RewardsRefreshBusy)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        string message = "There is already a rewards refresh in progress. Please Wait.";
                        CustomMessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        if (caller != Caller.Rewards)
                        {
                            ConsoleLog(mainWindow.txtLog, $"[{caller}] {message}");
                        }
                    });
                    return null; // Exit the method here if refresh is already in progress
                }

                RewardsRefreshBusy = true;
            } // Release the lock here, allowing other threads to check RewardsRefreshBusy

            try
            {
                string logPrefix = $"[{caller}]";
                if (caller != Caller.Rewards)
                {
                    logPrefix = $"[{caller}][Rewards]";
                }
                string? lastWarning = null;
                string fiatCurrency = SettingFiatCurrency;

                if (caller != Caller.Rewards)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ConsoleLog(mainWindow.txtLog, $"[{caller}] Refreshing rewards");
                    });
                }

                decimal rewardsTaxPercentage = SettingRewardsTaxPercentage;

                using (FbConnection connection = new(connectionString))
                {
                    connection.Open();

                    using DbCommand deleteCommand = connection.CreateCommand();

                    // Truncate standard DB table
                    deleteCommand.CommandText = "DELETE FROM TB_REWARDS";
                    deleteCommand.ExecuteNonQuery();

                    // Read rewards from Kraken ledgers and manual ledgers
                    using DbCommand selectCommand = connection.CreateCommand();
                    selectCommand.CommandText = $@"SELECT
                                                       ledgers.REFID,
                                                       ledgers.""DATE"",
                                                       ledgers.TYPE,
                                                       ledgers.EXCHANGE,
                                                       asset_catalog.ASSET,
                                                       ROUND(ledgers.AMOUNT - ledgers.FEE, 10) AS AMOUNT
                                                   FROM TB_LEDGERS ledgers
                                                       INNER JOIN TB_ASSET_CATALOG asset_catalog 
                                                           ON ledgers.ASSET = asset_catalog .ASSET
                                                   WHERE ledgers.TYPE IN ('EARN', 'STAKING', 'AIRDROP')
                                                       AND ledgers.""DATE"" BETWEEN @FROM_DATE AND @TO_DATE
                                                   ORDER BY ledgers.""DATE"" ASC";

                    // Convert string dates to DateTime and add parameters
                    AddParameterWithValue(selectCommand, "@FROM_DATE", ConvertStringToIsoDate(fromDate));
                    AddParameterWithValue(selectCommand, "@TO_DATE", ConvertStringToIsoDate(toDate).AddDays(1));

                    // Insert into rewards db table
                    using (DbDataReader reader = selectCommand.ExecuteReader())
                    {
                        // Rate limiting mechanism    //
                        DateTime lastCallTime = DateTime.Now;
                        // Progress logging mechanism //
                        DateTime lastLogTime = DateTime.Now;
                        int recordsProcessed = 0;
                        ////////////////////////////////
                        while (reader.Read())
                        {
                            recordsProcessed++;

                            // Progress logging mechanism //
                            if ((DateTime.Now - lastLogTime).TotalSeconds >= 30)
                            {
                                string progressMessage = $"[Rewards] Processed {recordsProcessed} records...";
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    ConsoleLog(mainWindow.txtLog, progressMessage);
                                });
                                lastLogTime = DateTime.Now;
                            }
                            ////////////////////////////////

                            string refid = reader.GetStringOrEmpty(0);
                            DateTime date = reader.GetDateTime(1);
                            string type = reader.GetStringOrEmpty(2);
                            string exchange = reader.GetStringOrEmpty(3);
                            string asset = reader.GetStringOrEmpty(4);
                            decimal amount = reader.GetDecimalOrDefault(5);

                            var (fiatAmount, source) = ConvertXToFiat(asset, date.Date, connection);
                            decimal exchangeRate = fiatAmount;

                            // Rate limiting mechanism //
                            if (source == "API")
                            {
                                if ((DateTime.Now - lastCallTime).TotalSeconds < 1)
                                {
                                    // Calculate delay to ensure at least 1 seconds have passed
                                    int delay = Math.Max(0, (int)((lastCallTime.AddSeconds(1) - DateTime.Now).TotalMilliseconds));
                                    await Task.Delay(delay);
                                }
                                lastCallTime = DateTime.Now;
                            }
                            /////////////////////////////
                            decimal amount_fiat = 0.00m;
                            decimal tax = 0.00m;
                            decimal unit_price_break_even = 0.00m;
                            decimal amount_sell_break_even = 0.00m;

                            if (exchangeRate != 0m)
                            {
                                amount_fiat = exchangeRate * amount;
                                tax = amount_fiat * (rewardsTaxPercentage / 100m);
                                unit_price_break_even = exchangeRate * (1 + (rewardsTaxPercentage / 100m));
                                amount_sell_break_even = tax / unit_price_break_even;
                            }
                            else
                            {
                                lastWarning = $"[Rewards] Could not perform calculations for refid: {refid}" + Environment.NewLine + $"Retrieved 0.00 exchange rate for asset {asset} on {ConvertDateTimeToString(date, "yyyy-MM-dd")}";
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    ConsoleLog(mainWindow.txtLog, lastWarning);
                                });
                            }

                            using DbCommand insertCommand = connection.CreateCommand();
                            insertCommand.CommandText = $@"INSERT INTO TB_REWARDS (
                                                                REFID,
                                                                ""DATE"",
                                                                TYPE,
                                                                EXCHANGE,
                                                                ASSET,
                                                                AMOUNT,
                                                                AMOUNT_FIAT,
                                                                TAX,
                                                                UNIT_PRICE,
                                                                UNIT_PRICE_BREAK_EVEN,
                                                                AMOUNT_SELL_BREAK_EVEN
                                                            )
                                                            VALUES (
                                                                @REFID,
                                                                @DATE,
                                                                @TYPE,
                                                                @EXCHANGE,
                                                                @ASSET,
                                                                @AMOUNT,
                                                                ROUND(@AMOUNT_FIAT, 10),
                                                                @TAX,
                                                                @UNIT_PRICE,
                                                                @UNIT_PRICE_BREAK_EVEN,
                                                                @AMOUNT_SELL_BREAK_EVEN
                                                            )";

                            AddParameterWithValue(insertCommand, "@REFID", refid);
                            AddParameterWithValue(insertCommand, "@DATE", date);
                            AddParameterWithValue(insertCommand, "@TYPE", type);
                            AddParameterWithValue(insertCommand, "@EXCHANGE", exchange);
                            AddParameterWithValue(insertCommand, "@ASSET", asset);
                            AddParameterWithValue(insertCommand, "@AMOUNT", amount);
                            AddParameterWithValue(insertCommand, "@AMOUNT_FIAT", amount_fiat);
                            AddParameterWithValue(insertCommand, "@TAX", tax);
                            AddParameterWithValue(insertCommand, "@UNIT_PRICE", exchangeRate);
                            AddParameterWithValue(insertCommand, "@UNIT_PRICE_BREAK_EVEN", unit_price_break_even);
                            AddParameterWithValue(insertCommand, "@AMOUNT_SELL_BREAK_EVEN", amount_sell_break_even);

                            insertCommand.ExecuteNonQuery();
                        }
                    }
                    if (lastWarning != null)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            CustomMessageBox.Show("There were issues with some calculations.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                        });
                    }
                }

                if (caller != Caller.Rewards)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (lastWarning == null)
                        {
                            ConsoleLog(mainWindow.txtLog, $"[{caller}] Refreshing rewards done");
                        }
                        else
                        {
                            ConsoleLog(mainWindow.txtLog, $"[{caller}] Refreshing rewards done with warnings");
                        }
                    });
                }

                return lastWarning;
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    CustomMessageBox.Show("Failed to refresh rewards." + Environment.NewLine + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                });
                if (caller != Caller.Rewards)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ConsoleLog(mainWindow.txtLog, $"[{caller}] {ex.Message}");
                        ConsoleLog(mainWindow.txtLog, $"[{caller}] Refreshing rewards unsuccessful");
                    });
                }
                throw new Exception("RefreshRewards failed", ex);
            }
            finally
            {
                lock (_RewardsRefreshLock) // Lock again to safely update RewardsRefreshBusy
                {
                    RewardsRefreshBusy = false;
                }
            }
        }
    }
}