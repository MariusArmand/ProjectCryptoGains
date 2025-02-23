using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using static ProjectCryptoGains.Common.Utils.DatabaseUtils;
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
                        string message = "There is already a trades refresh in progress. Please Wait";
                        MessageBoxResult result = CustomMessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

                string? fiatCurrency = SettingFiatCurrency;

                if (caller != Caller.Trades)
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
                            string refid = reader.GetStringOrEmpty(0);
                            string date = reader.GetStringOrEmpty(1);
                            string base_code = reader.GetStringOrEmpty(2);
                            string base_fee = reader.GetStringOrEmpty(3);
                            string quote_code = reader.GetStringOrEmpty(4);
                            string quote_amount = reader.GetStringOrEmpty(5);
                            string quote_fee = reader.GetStringOrEmpty(6);

                            DateTime datetime = DateTime.ParseExact(date, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                            // Calculate base_fee_fiat
                            var (base_unit_price_fiat, baseConversionSource) = ConvertXToFiat(base_code, 1m, datetime.Date, connection);

                            string lastWarningPrefix = $"[{caller}]";
                            if (caller != Caller.Trades)
                            {
                                lastWarningPrefix = $"[{caller}][Trades]";
                            }

                            if (ConvertStringToDecimal(base_unit_price_fiat) == 0m)
                            {
                                lastWarning = $"{lastWarningPrefix} Could not calculate BASE_UNIT_PRICE_{fiatCurrency} for asset: {base_code}" + Environment.NewLine + "Retrieved 0.00 exchange rate";
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    ConsoleLog(_mainWindow.txtLog, lastWarning);
                                });
                            }

                            string base_fee_fiat = (ConvertStringToDecimal(base_unit_price_fiat) * ConvertStringToDecimal(base_fee)).ToString("F10");

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

                            var (quote_unit_price_fiat, quoteConversionSource) = ConvertXToFiat(quote_code, 1m, datetime.Date, connection);

                            if (ConvertStringToDecimal(quote_unit_price_fiat) == 0m)
                            {
                                lastWarning = $"{lastWarningPrefix} Could not calculate QUOTE_UNIT_PRICE_{fiatCurrency} for asset: {quote_code}" + Environment.NewLine + "Retrieved 0.00 exchange rate";
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    ConsoleLog(_mainWindow.txtLog, lastWarning);
                                });
                            }

                            string quote_fee_fiat = (ConvertStringToDecimal(quote_unit_price_fiat) * ConvertStringToDecimal(quote_fee)).ToString("F10");

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
                        if (lastWarning != null)
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                MessageBoxResult result = CustomMessageBox.Show($"There were issues calculating some unit prices in {fiatCurrency}", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                            });
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