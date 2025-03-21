using FirebirdSql.Data.FirebirdClient;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using System.Windows;
using static ProjectCryptoGains.Common.Utils.DatabaseUtils;
using static ProjectCryptoGains.Common.Utils.DateUtils;
using static ProjectCryptoGains.Common.Utils.SettingUtils;
using static ProjectCryptoGains.Common.Utils.Utils;

namespace ProjectCryptoGains.Common.Utils
{
    public static class TradesUtils
    {
        // Parallel run prevention //
        public static bool TradesRefreshBusy { get; private set; } = false;
        private static readonly object TradesRefreshlock = new();
        /////////////////////////////

        public static async Task<string?> RefreshTrades(MainWindow _mainWindow, Caller caller)
        {
            lock (TradesRefreshlock) // Only one thread can enter this block at a time
            {
                if (TradesRefreshBusy)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        string message = "There is already a trades refresh in progress. Please Wait.";
                        CustomMessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        if (caller != Caller.Trades)
                        {
                            ConsoleLog(_mainWindow.txtLog, $"[{caller}] {message}");
                        }
                    });
                    return null; // Exit the method here if refresh is already in progress
                }

                TradesRefreshBusy = true;
            } // Release the lock here, allowing other threads to check TradesRefreshBusy

            try
            {
                string? lastWarning = null;
                string fiatCurrency = SettingFiatCurrency;
                string? exceptionMessage = null;
                Exception? innerExceptionMessage = null;

                if (caller != Caller.Trades)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ConsoleLog(_mainWindow.txtLog, $"[{caller}] Refreshing trades");
                    });
                }

                using (FbConnection connection = new(connectionString))
                {
                    connection.Open();

                    using DbCommand deleteCommand = connection.CreateCommand();

                    // Truncate db table
                    deleteCommand.CommandText = "DELETE FROM TB_TRADES";
                    deleteCommand.ExecuteNonQuery();

                    // Insert into db table
                    using DbCommand insertCommand = connection.CreateCommand();
                    insertCommand.CommandText = $@"INSERT INTO TB_TRADES (REFID, ""DATE"", TYPE, EXCHANGE, BASE_ASSET, BASE_AMOUNT, BASE_FEE, BASE_FEE_FIAT, QUOTE_ASSET, QUOTE_AMOUNT, QUOTE_AMOUNT_FIAT, QUOTE_FEE, QUOTE_FEE_FIAT, BASE_UNIT_PRICE, BASE_UNIT_PRICE_FIAT, QUOTE_UNIT_PRICE, QUOTE_UNIT_PRICE_FIAT, TOTAL_FEE_FIAT, COSTS_PROCEEDS)
                                                   SELECT 
                                                        REFID,
                                                        ""DATE"",
                                                        TYPE,
                                                        EXCHANGE,
                                                        BASE_ASSET,
                                                        ROUND(ABS(BASE_AMOUNT), 10) AS BASE_AMOUNT,
                                                        BASE_FEE,
                                                        CASE 
                                                            WHEN QUOTE_ASSET != '{fiatCurrency}' THEN NULL
                                                            ELSE ROUND(BASE_FEE * BASE_UNIT_PRICE_FIAT, 10)
                                                        END AS BASE_FEE_FIAT,
                                                        QUOTE_ASSET,
                                                        ROUND(ABS(QUOTE_AMOUNT), 10) AS QUOTE_AMOUNT,
                                                        ROUND(ABS(QUOTE_AMOUNT_FIAT), 10) AS QUOTE_AMOUNT_FIAT,
                                                        QUOTE_FEE,
                                                        CASE 
                                                            WHEN QUOTE_ASSET != '{fiatCurrency}' THEN NULL
                                                            ELSE QUOTE_FEE
                                                        END AS QUOTE_FEE_FIAT,
                                                        BASE_UNIT_PRICE,
                                                        BASE_UNIT_PRICE_FIAT,
                                                        QUOTE_UNIT_PRICE,
                                                        QUOTE_UNIT_PRICE_FIAT,
                                                        ROUND(BASE_FEE_FIAT + QUOTE_FEE_FIAT, 10) AS TOTAL_FEE_FIAT,
                                                        CASE 
                                                            WHEN QUOTE_ASSET = '{fiatCurrency}' AND TYPE = 'BUY' THEN ROUND(ABS(QUOTE_AMOUNT) + BASE_FEE_FIAT + QUOTE_FEE_FIAT, 10)
                                                            WHEN QUOTE_ASSET = '{fiatCurrency}' AND TYPE = 'SELL' THEN ROUND(ABS(QUOTE_AMOUNT) - BASE_FEE_FIAT - QUOTE_FEE_FIAT, 10)
                                                            ELSE NULL
                                                        END AS COSTS_PROCEEDS
                                                    FROM (
                                                        SELECT 
                                                            REFID,
                                                            ""DATE"",
                                                            TYPE,
                                                            EXCHANGE,
                                                            BASE_ASSET,
                                                            BASE_AMOUNT,
                                                            BASE_FEE,
                                                            CASE 
                                                                WHEN QUOTE_ASSET != '{fiatCurrency}' THEN NULL
                                                                ELSE ROUND(BASE_FEE * BASE_UNIT_PRICE_FIAT, 10)
                                                            END AS BASE_FEE_FIAT,
                                                            QUOTE_ASSET,
                                                            QUOTE_AMOUNT,
                                                            QUOTE_AMOUNT_FIAT,
                                                            QUOTE_FEE,
                                                            CASE 
                                                                WHEN QUOTE_ASSET != '{fiatCurrency}' THEN NULL
                                                                ELSE QUOTE_FEE
                                                            END AS QUOTE_FEE_FIAT,
                                                            BASE_UNIT_PRICE,
                                                            BASE_UNIT_PRICE_FIAT,
                                                            QUOTE_UNIT_PRICE,
                                                            QUOTE_UNIT_PRICE_FIAT
                                                        FROM (
                                                            SELECT 
                                                                REFID,
                                                                ""DATE"",
                                                                TYPE,
                                                                EXCHANGE,
                                                                BASE_ASSET,
                                                                BASE_AMOUNT,
                                                                BASE_FEE,
                                                                QUOTE_ASSET,
                                                                QUOTE_AMOUNT,
                                                                CASE 
                                                                    WHEN QUOTE_ASSET != '{fiatCurrency}' THEN NULL
                                                                    ELSE QUOTE_AMOUNT
                                                                END AS QUOTE_AMOUNT_FIAT,
                                                                QUOTE_FEE,
                                                                ROUND(ABS(QUOTE_AMOUNT / BASE_AMOUNT), 10) AS BASE_UNIT_PRICE,
                                                                CASE 
                                                                    WHEN QUOTE_ASSET != '{fiatCurrency}' THEN NULL
                                                                    ELSE ROUND(ABS(QUOTE_AMOUNT / BASE_AMOUNT), 10)
                                                                END AS BASE_UNIT_PRICE_FIAT,
                                                                ROUND(ABS(BASE_AMOUNT / QUOTE_AMOUNT), 10) AS QUOTE_UNIT_PRICE,
                                                                CASE 
                                                                    WHEN QUOTE_ASSET != '{fiatCurrency}' THEN NULL
                                                                    ELSE 1
                                                                END AS QUOTE_UNIT_PRICE_FIAT
                                                            FROM (
                                                                -- BUY => RIGHT = negative FIAT
                                                                SELECT 
                                                                    b.REFID,
                                                                    b.""DATE"",
                                                                    'BUY' AS TYPE,
                                                                    b.EXCHANGE,
                                                                    b.ASSET AS BASE_ASSET,
                                                                    b.AMOUNT AS BASE_AMOUNT,
                                                                    b.FEE AS BASE_FEE,
                                                                    q.ASSET AS QUOTE_ASSET,
                                                                    q.AMOUNT AS QUOTE_AMOUNT,
                                                                    q.FEE AS QUOTE_FEE
                                                                FROM 
                                                                    (SELECT * FROM TB_LEDGERS WHERE TYPE = 'TRADE' AND AMOUNT > 0) b
                                                                    INNER JOIN 
                                                                    (SELECT * FROM TB_LEDGERS WHERE TYPE = 'TRADE' AND AMOUNT < 0 AND ASSET = '{fiatCurrency}') q
                                                                    ON b.REFID = q.REFID

                                                                UNION ALL

                                                                -- SELL => RIGHT = positive FIAT
                                                                SELECT 
                                                                    b.REFID,
                                                                    b.""DATE"",
                                                                    'SELL' AS TYPE,
                                                                    b.EXCHANGE,
                                                                    b.ASSET AS BASE_ASSET,
                                                                    b.AMOUNT AS BASE_AMOUNT,
                                                                    b.FEE AS BASE_FEE,
                                                                    q.ASSET AS QUOTE_ASSET,
                                                                    q.AMOUNT AS QUOTE_AMOUNT,
                                                                    q.FEE AS QUOTE_FEE
                                                                FROM 
                                                                    (SELECT * FROM TB_LEDGERS WHERE TYPE = 'TRADE' AND AMOUNT < 0) b
                                                                    INNER JOIN 
                                                                    (SELECT * FROM TB_LEDGERS WHERE TYPE = 'TRADE' AND AMOUNT > 0 AND ASSET = '{fiatCurrency}') q
                                                                    ON b.REFID = q.REFID

                                                                UNION ALL

                                                                -- SELL => LEFT != FIAT, RIGHT != FIAT, LEFT negative amount
                                                                SELECT 
                                                                    b.REFID,
                                                                    b.""DATE"",
                                                                    'SELL' AS TYPE,
                                                                    b.EXCHANGE,
                                                                    b.ASSET AS BASE_ASSET,
                                                                    b.AMOUNT AS BASE_AMOUNT,
                                                                    b.FEE AS BASE_FEE,
                                                                    q.ASSET AS QUOTE_ASSET,
                                                                    q.AMOUNT AS QUOTE_AMOUNT,
                                                                    q.FEE AS QUOTE_FEE
                                                                FROM 
                                                                    (SELECT * FROM TB_LEDGERS WHERE TYPE = 'TRADE' AND AMOUNT < 0 AND ASSET != '{fiatCurrency}') b
                                                                    INNER JOIN 
                                                                    (SELECT * FROM TB_LEDGERS WHERE TYPE = 'TRADE' AND AMOUNT > 0 AND ASSET != '{fiatCurrency}') q
                                                                    ON b.REFID = q.REFID
                                                            )
                                                        )
                                                    )";

                    insertCommand.ExecuteNonQuery();

                    // Update crypto-only entries
                    try
                    {
                        using DbCommand selectCommand = connection.CreateCommand();
                        selectCommand.CommandText = $@"SELECT 
                                                           trades.REFID,
                                                           trades.""DATE"",
                                                           asset_catalog_base.ASSET AS BASE_ASSET,
                                                           trades.BASE_FEE,
                                                           asset_catalog_quote.ASSET AS QUOTE_ASSET,
                                                           trades.QUOTE_AMOUNT,
                                                           trades.QUOTE_FEE
                                                       FROM TB_TRADES trades
                                                           LEFT JOIN TB_ASSET_CATALOG asset_catalog_base
                                                               ON trades.BASE_ASSET = asset_catalog_base.ASSET
                                                           LEFT JOIN TB_ASSET_CATALOG asset_catalog_quote
                                                               ON trades.QUOTE_ASSET = asset_catalog_quote.ASSET
                                                       WHERE trades.BASE_ASSET != '{fiatCurrency}'
                                                           AND trades.QUOTE_ASSET != '{fiatCurrency}'";

                        List<(string RefId, Dictionary<string, decimal> UpdateData)> updates = [];

                        using (DbDataReader reader = selectCommand.ExecuteReader())
                        {
                            // Rate limiting mechanism //
                            DateTime lastCallTime = DateTime.Now;
                            /////////////////////////////
                            while (reader.Read())
                            {
                                string refid = reader.GetStringOrEmpty(0);
                                DateTime date = reader.GetDateTime(1);
                                string base_asset = reader.GetStringOrEmpty(2);
                                decimal base_fee = reader.GetDecimalOrDefault(3);
                                string quote_asset = reader.GetStringOrEmpty(4);
                                decimal quote_amount = reader.GetDecimalOrDefault(5);
                                decimal quote_fee = reader.GetDecimalOrDefault(6);

                                // Calculate base fiat
                                var (base_unit_price_fiat, baseConversionSource) = ConvertXToFiat(base_asset, date.Date, connection);

                                string lastWarningPrefix = $"[{caller}]";
                                if (caller != Caller.Trades)
                                {
                                    lastWarningPrefix = $"[{caller}][Trades]";
                                }

                                if (base_unit_price_fiat == 0m)
                                {
                                    lastWarning = $"{lastWarningPrefix} Unable to calculate BASE_UNIT_PRICE_{fiatCurrency}" + Environment.NewLine + $"Retrieved 0.00 exchange rate for asset {base_asset} on {ConvertDateTimeToString(date.Date, "yyyy-MM-dd")}";
                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        ConsoleLog(_mainWindow.txtLog, lastWarning);
                                    });
                                }

                                decimal base_fee_fiat = base_unit_price_fiat * base_fee;

                                // Rate limiting mechanism //
                                if (baseConversionSource == "API")
                                {
                                    if ((DateTime.Now - lastCallTime).TotalSeconds < 1)
                                    {
                                        // Calculate delay to ensure at least 1 seconds have passed
                                        int delay = Math.Max(0, (int)(lastCallTime.AddSeconds(1) - DateTime.Now).TotalMilliseconds);
                                        await Task.Delay(delay);
                                    }
                                }
                                lastCallTime = DateTime.Now;
                                /////////////////////////////

                                // Calculate quote fiat
                                var (quote_unit_price_fiat, quoteConversionSource) = ConvertXToFiat(quote_asset, date.Date, connection);

                                if (quote_unit_price_fiat == 0m)
                                {
                                    lastWarning = $"{lastWarningPrefix} Unable to calculate QUOTE_UNIT_PRICE_{fiatCurrency}" + Environment.NewLine + $"Retrieved 0.00 exchange rate for asset {base_asset} on {ConvertDateTimeToString(date.Date, "yyyy-MM-dd")}";
                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        ConsoleLog(_mainWindow.txtLog, lastWarning);
                                    });
                                }

                                decimal quote_fee_fiat = quote_unit_price_fiat * quote_fee;

                                // Rate limiting mechanism //
                                if (quoteConversionSource == "API")
                                {
                                    if ((DateTime.Now - lastCallTime).TotalSeconds < 1)
                                    {
                                        // Calculate delay to ensure at least 1 seconds have passed
                                        int delay = Math.Max(0, (int)(lastCallTime.AddSeconds(1) - DateTime.Now).TotalMilliseconds);
                                        await Task.Delay(delay);
                                    }
                                }
                                lastCallTime = DateTime.Now;
                                /////////////////////////////

                                decimal total_fee_fiat = base_fee_fiat + quote_fee_fiat;

                                decimal quote_amount_fiat = quote_unit_price_fiat * quote_amount;
                                decimal costs_proceeds = Math.Abs(quote_amount_fiat - base_fee_fiat - quote_fee_fiat);

                                var updateData = new Dictionary<string, decimal>
                                {
                                    { "BASE_FEE_FIAT", base_fee_fiat },
                                    { "QUOTE_AMOUNT_FIAT", quote_amount_fiat },
                                    { "QUOTE_FEE_FIAT", quote_fee_fiat },
                                    { "BASE_UNIT_PRICE_FIAT", base_unit_price_fiat },
                                    { "QUOTE_UNIT_PRICE_FIAT", quote_unit_price_fiat },
                                    { "TOTAL_FEE_FIAT", total_fee_fiat },
                                    { "COSTS_PROCEEDS", costs_proceeds }
                                };

                                updates.Add((refid, updateData));
                            }
                            if (lastWarning != null)
                            {
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    CustomMessageBox.Show($"There were issues calculating some unit prices in {fiatCurrency}.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                                });
                            }
                        }

                        // Perform the updates
                        using var transaction = connection.BeginTransaction(); // Start a database transaction
                        try
                        {
                            // Create a single command object for updating records, reused across all iterations
                            using DbCommand updateCommand = connection.CreateCommand();
                            updateCommand.Transaction = transaction; // Assign the transaction to the command
                            updateCommand.CommandText = @"UPDATE TB_TRADES 
                                                          SET 
                                                              BASE_FEE_FIAT = @BASE_FEE_FIAT,
                                                              QUOTE_AMOUNT_FIAT = @QUOTE_AMOUNT_FIAT,
                                                              QUOTE_FEE_FIAT = @QUOTE_FEE_FIAT,
                                                              BASE_UNIT_PRICE_FIAT = @BASE_UNIT_PRICE_FIAT,
                                                              QUOTE_UNIT_PRICE_FIAT = @QUOTE_UNIT_PRICE_FIAT,
                                                              TOTAL_FEE_FIAT = @TOTAL_FEE_FIAT,
                                                              COSTS_PROCEEDS = @COSTS_PROCEEDS
                                                          WHERE 
                                                              REFID = @REFID";

                            // Define all parameters once before the loop to avoid repeated creation, initialized with placeholders
                            AddParameterWithValue(updateCommand, "@REFID", DBNull.Value);
                            AddParameterWithValue(updateCommand, "@BASE_FEE_FIAT", DBNull.Value);
                            AddParameterWithValue(updateCommand, "@QUOTE_AMOUNT_FIAT", DBNull.Value);
                            AddParameterWithValue(updateCommand, "@QUOTE_FEE_FIAT", DBNull.Value);
                            AddParameterWithValue(updateCommand, "@BASE_UNIT_PRICE_FIAT", DBNull.Value);
                            AddParameterWithValue(updateCommand, "@QUOTE_UNIT_PRICE_FIAT", DBNull.Value);
                            AddParameterWithValue(updateCommand, "@TOTAL_FEE_FIAT", DBNull.Value);
                            AddParameterWithValue(updateCommand, "@COSTS_PROCEEDS", DBNull.Value);

                            // Iterate over the list of updates, where each item contains a RefId and a dictionary of column-value pairs
                            foreach (var (RefId, UpdateData) in updates)
                            {
                                updateCommand.Parameters["@REFID"].Value = RefId;
                                // Update parameter values based on the key-value pairs in UpdateData
                                foreach (var kvp in UpdateData) // kvp = Key-Value Pair from the dictionary
                                {
                                    updateCommand.Parameters[$"@{kvp.Key}"].Value = kvp.Value;
                                }
                                updateCommand.ExecuteNonQuery();
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
                        exceptionMessage = "Updating crypto-only entries failed.";
                    }
                }

                if (caller != Caller.Trades)
                {
                    if (exceptionMessage != null)
                    {
                        throw new Exception(exceptionMessage, innerExceptionMessage);
                    }
                    else
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            if (lastWarning == null)
                            {
                                ConsoleLog(_mainWindow.txtLog, $"[{caller}] Refreshing trades done");
                            }
                            else
                            {
                                ConsoleLog(_mainWindow.txtLog, $"[{caller}] Refreshing trades done with warnings");
                            }
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

                return lastWarning;
            }
            finally
            {
                lock (TradesRefreshlock) // Lock again to safely update TradesRefreshBusy
                {
                    TradesRefreshBusy = false;
                }
            }
        }
    }
}