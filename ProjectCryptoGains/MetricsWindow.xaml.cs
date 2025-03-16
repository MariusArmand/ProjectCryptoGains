using FirebirdSql.Data.FirebirdClient;
using ProjectCryptoGains.Common;
using System;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using static ProjectCryptoGains.Common.Utils.DatabaseUtils;
using static ProjectCryptoGains.Common.Utils.LedgersUtils;
using static ProjectCryptoGains.Common.Utils.ReaderUtils;
using static ProjectCryptoGains.Common.Utils.SettingUtils;
using static ProjectCryptoGains.Common.Utils.TradesUtils;
using static ProjectCryptoGains.Common.Utils.Utils;
using static ProjectCryptoGains.Common.Utils.WindowUtils;
using static ProjectCryptoGains.Models;

namespace ProjectCryptoGains
{
    /// <summary>
    /// Interaction logic for MetricsWindow.xaml
    /// </summary>

    public partial class MetricsWindow : SubwindowBase
    {
        private readonly MainWindow _mainWindow;

        private string? lastError = null;

        private string? lastWarning = null;

        public MetricsWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            TitleBarElement = TitleBar;

            _mainWindow = mainWindow;

            BindLabels();
            BindGrid();
        }

        private void BlockUI()
        {
            btnRefresh.IsEnabled = false;

            Cursor = Cursors.Wait;
        }

        private void UnblockUI()
        {
            btnRefresh.IsEnabled = true;

            Cursor = Cursors.Arrow;
        }

        private void BindLabels()
        {
            string fiatCurrency = SettingFiatCurrency;

            using (FbConnection connection = new(connectionString))
            {
                try
                {
                    try
                    {
                        connection.Open();
                    }
                    catch (Exception ex)
                    {
                        CustomMessageBox.Show("Database could not be opened." + Environment.NewLine + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                        // Exit function early
                        return;
                    }

                    // Total Invested
                    using DbCommand selectCommand = connection.CreateCommand();
                    selectCommand.CommandText = @"SELECT ""VALUE"" FROM TB_METRICS
											      WHERE METRIC = 'TOTAL_INVESTED'";

                    using (DbDataReader reader = selectCommand.ExecuteReader())
                    {
                        decimal totalInvested = 0.00m;
                        if (reader.Read())
                        {
                            totalInvested = reader.GetDecimalOrDefault(0, 0.00m);
                        }
                        lblTotalInvestedData.Content = totalInvested.ToString("F2") + " " + fiatCurrency;
                    }

                    // Last Invested
                    selectCommand.CommandText = @"SELECT ""VALUE"" FROM TB_METRICS
											      WHERE METRIC = 'LAST_INVESTED'";

                    using (DbDataReader reader = selectCommand.ExecuteReader())
                    {
                        string lastInvested = "";
                        if (reader.Read())
                        {
                            lastInvested = reader.GetStringOrEmpty(0);
                        }
                        lblLastInvestedData.Content = lastInvested;
                    }
                }
                catch (Exception ex)
                {
                    lastError = "There was a problem getting invest metrics" + Environment.NewLine + ex.Message;
                    CustomMessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    ConsoleLog(_mainWindow.txtLog, $"[Metrics] {lastError}");

                    // Exit function early
                    return;
                }
            }
        }

        private void UnbindLabels()
        {
            string fiatCurrency = SettingFiatCurrency;
            lblTotalInvestedData.Content = "0.00 " + fiatCurrency;
            lblLastInvestedData.Content = "";
        }

        private void BindGrid()
        {
            string fiatCurrency = SettingFiatCurrency;
            dgAvgBuyPrice.Columns[2].Header = "AMOUNT__" + fiatCurrency;
            dgRewardsSummary.Columns[3].Header = "AMOUNT__" + fiatCurrency;

            lblTotalAmountFiatData.Content = "0.00 " + fiatCurrency;

            using (FbConnection connection = new(connectionString))
            {
                try
                {
                    connection.Open();
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show("Database could not be opened." + Environment.NewLine + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                    // Exit function early
                    return;
                }

                int dbLineNumber = 0;
                decimal amnt_fiat;

                // Average buy price
                try
                {
                    // Create a collection of Model objects
                    ObservableCollection<AvgBuyPriceModel> AvgBuyPriceData = [];

                    using DbCommand selectCommand = connection.CreateCommand();
                    selectCommand.CommandText = "SELECT CURRENCY, COALESCE(AMOUNT_FIAT, 0.00) FROM TB_AVG_BUY_PRICE";

                    using (DbDataReader reader = selectCommand.ExecuteReader())
                    {
                        dbLineNumber = 0;
                        while (reader.Read())
                        {
                            dbLineNumber++;

                            amnt_fiat = reader.GetDecimalOrDefault(1);
                            AvgBuyPriceData.Add(new AvgBuyPriceModel
                            {
                                Row_number = dbLineNumber,
                                Currency = reader.GetStringOrEmpty(0),
                                Amount_fiat = amnt_fiat
                            });
                        }
                    }

                    dgAvgBuyPrice.ItemsSource = AvgBuyPriceData;
                }
                catch (Exception ex)
                {
                    lastError = "There was a problem getting average buy prices" + Environment.NewLine + ex.Message;

                    CustomMessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    ConsoleLog(_mainWindow.txtLog, $"[Metrics] {lastError}");

                    // Exit function early
                    return;
                }

                // Rewards
                try
                {
                    // Create a collection of MetricsRewardsSummaryModel objects
                    ObservableCollection<MetricsRewardsSummaryModel> MetricsRewardsSummaryData = [];

                    using DbCommand selectCommand = connection.CreateCommand();
                    selectCommand.CommandText = "SELECT CURRENCY, AMOUNT, AMOUNT_FIAT FROM TB_REWARDS_SUMMARY where AMOUNT > 0";

                    using (DbDataReader reader = selectCommand.ExecuteReader())
                    {
                        decimal tot_amnt_fiat = 0.00m;
                        dbLineNumber = 0;
                        while (reader.Read())
                        {
                            dbLineNumber++;

                            amnt_fiat = reader.GetDecimalOrDefault(2);
                            MetricsRewardsSummaryData.Add(new MetricsRewardsSummaryModel
                            {
                                Row_number = dbLineNumber,
                                Currency = reader.GetStringOrEmpty(0),
                                Amount = reader.GetDecimalOrDefault(1),
                                Amount_fiat = amnt_fiat
                            });
                            tot_amnt_fiat += amnt_fiat;
                        }
                        lblTotalAmountFiatData.Content = tot_amnt_fiat.ToString("F2") + " " + fiatCurrency;
                    }

                    dgRewardsSummary.ItemsSource = MetricsRewardsSummaryData;
                }
                catch (Exception ex)
                {
                    lastError = "There was a problem getting rewards" + Environment.NewLine + ex.Message;

                    CustomMessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    ConsoleLog(_mainWindow.txtLog, $"[Metrics] {lastError}");

                    // Exit function early
                    return;
                }
            }
        }

        private void UnbindGrid()
        {
            string fiatCurrency = SettingFiatCurrency;
            lblTotalAmountFiatData.Content = "0.00 " + fiatCurrency;
            dgAvgBuyPrice.ItemsSource = null;
            dgRewardsSummary.ItemsSource = null;
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            lastWarning = null;
            Refresh();
        }

        private async void Refresh()
        {
            string fiatCurrency = SettingFiatCurrency;
            lastError = null;

            BlockUI();

            try
            {
                ConsoleLog(_mainWindow.txtLog, $"[Metrics] Refreshing metrics");

                bool ledgersRefreshFailed = false;
                string? ledgersRefreshWarning = null;
                bool ledgersRefreshWasBusy = false;
                if (chkRefreshLedgers.IsChecked == true)
                {
                    await Task.Run(() =>
                    {
                        try
                        {
                            ledgersRefreshWarning = RefreshLedgers(_mainWindow, Caller.Metrics);
                        }
                        catch (Exception)
                        {
                            ledgersRefreshFailed = true;
                        }
                        ledgersRefreshWasBusy = LedgersRefreshBusy;
                    });
                }

                string? tradesRefreshError = null;
                string? tradesRefreshWarning = null;
                bool tradesRefreshWasBusy = false;
                if (chkRefreshTrades.IsChecked == true && !ledgersRefreshWasBusy && !ledgersRefreshFailed)
                {
                    await Task.Run(async () =>
                    {
                        try
                        {
                            tradesRefreshWarning = await RefreshTrades(_mainWindow, Caller.Metrics);
                            tradesRefreshWasBusy = TradesRefreshBusy;
                        }
                        catch (Exception ex)
                        {
                            while (ex.InnerException != null)
                            {
                                ex = ex.InnerException;
                            }
                            tradesRefreshError = ex.Message;
                        }
                    });
                }

                if (!ledgersRefreshWasBusy && !tradesRefreshWasBusy && tradesRefreshError == null && !ledgersRefreshFailed)
                {
                    using (FbConnection connection = new(connectionString))
                    {
                        try
                        {
                            connection.Open();
                        }
                        catch (Exception ex)
                        {
                            CustomMessageBox.Show("Database could not be opened." + Environment.NewLine + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                            // Exit function early
                            return;
                        }

                        using DbCommand deleteCommand = connection.CreateCommand();

                        /// Metrics
                        ConsoleLog(_mainWindow.txtLog, $"[Metrics] Calulating metrics");
                        await Task.Run(() =>
                        {
                            // Truncate standard DB table
                            deleteCommand.CommandText = "DELETE FROM TB_METRICS";
                            deleteCommand.ExecuteNonQuery();

                            using DbCommand insertCommand = connection.CreateCommand();
                            // Total Invested
                            insertCommand.CommandText = $@"INSERT INTO TB_METRICS (METRIC, ""VALUE"")
                                                           SELECT 
                                                               'TOTAL_INVESTED' AS METRIC,
                                                               ROUND(SUM(AMOUNT), 2) AS ""VALUE""
                                                           FROM TB_LEDGERS
                                                           WHERE CURRENCY = '{fiatCurrency}'
                                                               AND TYPE = 'DEPOSIT'";

                            insertCommand.ExecuteNonQuery();

                            // Last Invested
                            insertCommand.CommandText = $@"INSERT INTO TB_METRICS (METRIC, ""VALUE"")
													       SELECT 
                                                               'LAST_INVESTED' AS METRIC,
													           SUBSTRING(CAST(MAX(""DATE"") AS VARCHAR(50)) FROM 1 FOR 19) AS ""VALUE""
													       FROM TB_LEDGERS
                                                           WHERE CURRENCY = '{fiatCurrency}'
                                                               AND TYPE = 'DEPOSIT'";

                            insertCommand.ExecuteNonQuery();
                        });

                        if (ledgersRefreshWarning == null)
                        {
                            ConsoleLog(_mainWindow.txtLog, $"[Metrics] Calulating metrics done");
                        }
                        else
                        {
                            ConsoleLog(_mainWindow.txtLog, $"[Metrics] Calulating metrics done with warnings");
                        }

                        /// Average Buy Price
                        ConsoleLog(_mainWindow.txtLog, $"[Metrics] Calulating average buy price");
                        await Task.Run(() =>
                        {
                            // Truncate standard DB table
                            deleteCommand.CommandText = "DELETE FROM TB_AVG_BUY_PRICE";
                            deleteCommand.ExecuteNonQuery();

                            // Insert into standard DB table for each asset
                            using DbCommand selectCommand = connection.CreateCommand();
                            selectCommand.CommandText = $"SELECT CODE, ASSET FROM TB_ASSET_CATALOG WHERE CODE != '{fiatCurrency}' ORDER BY CODE, ASSET";

                            using (DbDataReader reader = selectCommand.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    string code = reader.GetStringOrEmpty(0);
                                    string asset = reader.GetStringOrEmpty(1);

                                    using DbCommand insertCommand = connection.CreateCommand();
                                    insertCommand.CommandText = CreateAvgBuyPriceInsert(asset);
                                    insertCommand.ExecuteNonQuery();
                                }
                            }
                        });
                        ConsoleLog(_mainWindow.txtLog, $"[Metrics] Calulating average buy price done");

                        /// Rewards
                        ConsoleLog(_mainWindow.txtLog, $"[Metrics] Calulating rewards");
                        DateTime date = DateTime.Today;

                        await Task.Run(async () =>
                        {
                            try
                            {
                                // Truncate standard DB table
                                deleteCommand.CommandText = "DELETE FROM TB_REWARDS_SUMMARY";
                                deleteCommand.ExecuteNonQuery();

                                // Insert into standard DB table
                                using DbCommand selectCommand = connection.CreateCommand();
                                selectCommand.CommandText = @"SELECT 
                                                                  catalog.CODE,
                                                                  catalog.ASSET
                                                              FROM
                                                                  (SELECT CODE, ASSET FROM TB_ASSET_CATALOG) catalog
                                                                  INNER JOIN
                                                                      (SELECT DISTINCT CURRENCY FROM TB_LEDGERS) ledgers
                                                                      ON catalog.ASSET = ledgers.CURRENCY
                                                              ORDER BY CODE, ASSET";

                                using (DbDataReader reader = selectCommand.ExecuteReader())
                                {
                                    // For each asset, create rewards summary insert

                                    // Rate limiting mechanism //
                                    DateTime lastCallTime = DateTime.Now;
                                    /////////////////////////////
                                    while (reader.Read())
                                    {
                                        string code = reader.GetStringOrEmpty(0);
                                        string asset = reader.GetStringOrEmpty(1);

                                        using DbCommand insertCommand = connection.CreateCommand();

                                        var (xInFiat, sqlCommand, conversionSource) = CreateRewardsSummaryInsert(asset, code, date, connection);
                                        insertCommand.CommandText = sqlCommand;

                                        if (xInFiat == 0m)
                                        {
                                            lastWarning = $"[Metrics] Could not calculate AMOUNT_{fiatCurrency} for currency: {asset}" + Environment.NewLine + "Retrieved 0.00 exchange rate";
                                            Application.Current.Dispatcher.Invoke(() =>
                                            {
                                                ConsoleLog(_mainWindow.txtLog, lastWarning);
                                            });
                                        }

                                        // Rate limiting mechanism //
                                        if (conversionSource == "API")
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
                                        insertCommand.ExecuteNonQuery();
                                    }
                                    if (lastWarning != null)
                                    {
                                        Application.Current.Dispatcher.Invoke(() =>
                                        {
                                            CustomMessageBox.Show($"There were issues calculating some reward amounts in {fiatCurrency}.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                                        });
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                while (ex.InnerException != null)
                                {
                                    ex = ex.InnerException;
                                }
                                lastError = ex.Message;
                            }
                        });

                        if (lastError == null)
                        {
                            if (ledgersRefreshWarning == null && lastWarning == null)
                            {
                                ConsoleLog(_mainWindow.txtLog, $"[Metrics] Calulating rewards done");
                            }
                            else
                            {
                                ConsoleLog(_mainWindow.txtLog, $"[Metrics] Calulating rewards done with warnings");
                            }
                        }
                        else
                        {
                            ConsoleLog(_mainWindow.txtLog, $"[Metrics] " + lastError);
                            ConsoleLog(_mainWindow.txtLog, $"[Metrics] Calulating rewards unsuccessful");
                        }
                    }

                    BindLabels();
                    BindGrid();

                    if (lastError == null)
                    {
                        if (ledgersRefreshWarning == null && tradesRefreshWarning == null && lastWarning == null)
                        {
                            ConsoleLog(_mainWindow.txtLog, $"[Metrics] Refresh done");
                        }
                        else
                        {
                            ConsoleLog(_mainWindow.txtLog, $"[Metrics] Refresh done with warnings");
                        }
                    }
                    else
                    {
                        UnbindLabels();
                        UnbindGrid();
                        ConsoleLog(_mainWindow.txtLog, $"[Metrics] Refresh unsuccessful");
                    }
                }
                else
                {
                    UnbindLabels();
                    UnbindGrid();
                    if (tradesRefreshError != null)
                    {
                        ConsoleLog(_mainWindow.txtLog, $"[Metrics] " + tradesRefreshError);
                        ConsoleLog(_mainWindow.txtLog, $"[Metrics] Refreshing trades unsuccessful");
                    }
                    ConsoleLog(_mainWindow.txtLog, $"[Metrics] Refresh unsuccessful");
                }
            }
            finally
            {
                UnblockUI();
            }
        }

        private static string CreateAvgBuyPriceInsert(string currency)
        {
            return $@"INSERT INTO TB_AVG_BUY_PRICE (CURRENCY, AMOUNT_FIAT)
                          SELECT 
                              CURRENCY,
                              ROUND(AMOUNT_FIAT, 2) AS AMOUNT_FIAT
                          FROM (
                              SELECT 
                                  '{currency}' AS CURRENCY,
                                  SUM(AMOUNT * UNIT_PRICE_FIAT) / SUM(AMOUNT) AS AMOUNT_FIAT
                              FROM (
                                  SELECT 
                                      BASE_AMOUNT AS AMOUNT,
                                      BASE_UNIT_PRICE_FIAT AS UNIT_PRICE_FIAT
                                  FROM TB_TRADES
                                  WHERE TYPE = 'BUY'
                                      AND BASE_CURRENCY = '{currency}'
                                  UNION ALL
                                  SELECT 
                                      QUOTE_AMOUNT AS AMOUNT,
                                      QUOTE_UNIT_PRICE_FIAT AS UNIT_PRICE_FIAT
                                  FROM TB_TRADES
                                  WHERE TYPE = 'SELL'
                                      AND QUOTE_CURRENCY = '{currency}'
                              )
                          )
                          WHERE AMOUNT_FIAT IS NOT NULL";
        }

        private static (decimal xInFiat, string sqlCommand, string conversionSource) CreateRewardsSummaryInsert(string currency, string currency_code, DateTime date, FbConnection connection)
        {
            try
            {
                var (fiatAmount, source) = ConvertXToFiat(currency_code, date.Date, connection);
                decimal xInFiat = fiatAmount;
                string conversionSource = source;

                string sqlCommand = $@"INSERT INTO TB_REWARDS_SUMMARY (CURRENCY, AMOUNT, AMOUNT_FIAT)
                                       WITH cas AS (
                                           SELECT 
                                               ROUND(SUM(AMOUNT) - SUM(FEE), 10) AS REWARD_SUM
                                           FROM TB_LEDGERS
                                           WHERE TYPE IN ('EARN', 'STAKING', 'AIRDROP')
                                               AND CURRENCY IN ('{currency}')
                                       )
                                       SELECT 
                                           '{currency_code}' AS CURRENCY,
                                           COALESCE(cas.REWARD_SUM, 0.00) AS AMOUNT,
                                           ROUND(COALESCE({xInFiat} * cas.REWARD_SUM, 0.00), 2) AS AMOUNT_FIAT
                                       FROM cas";

                return (xInFiat, sqlCommand, conversionSource);
            }
            catch (Exception ex)
            {
                throw new Exception("CreateRewardsSummaryInsert failed", ex);
            }
        }

        private void BtnHelp_Click(object sender, RoutedEventArgs e)
        {
            OpenHelp("metrics_help.html");
        }
    }
}