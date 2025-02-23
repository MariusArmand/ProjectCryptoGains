using Microsoft.Data.Sqlite;
using System;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using static ProjectCryptoGains.Models;
using static ProjectCryptoGains.Common.Utility;
using ProjectCryptoGains.Common;

namespace ProjectCryptoGains
{
    /// <summary>
    /// Interaction logic for MetricsWindow.xaml
    /// </summary>

    public partial class MetricsWindow : Window
    {
        private readonly MainWindow _mainWindow;

        private SqliteConnection? connection;

        private string? lastError = null;

        private string? lastWarning = null;

        public MetricsWindow(MainWindow mainWindow)
        {
            InitializeComponent();

            // Capture drag on titlebar
            TitleBar.MouseLeftButtonDown += (sender, e) => DragMove();

            _mainWindow = mainWindow;

            if (!OpenDatabaseConnection())
            {
                return;
            }

            BindLabels();
            BindGrid();

            connection?.Close();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            SystemCommands.MinimizeWindow(this);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            SystemCommands.CloseWindow(this);
        }

        private bool OpenDatabaseConnection()
        {
            try
            {
                connection = new(connectionString);
                connection.Open();
                return true;
            }
            catch (Exception ex)
            {
                MessageBoxResult result = CustomMessageBox.Show("Database could not be opened." + Environment.NewLine + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                btnRefresh.IsEnabled = true;
                Cursor = Cursors.Arrow;
                return false;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Visibility = Visibility.Hidden;
        }

        private void ButtonHelp_Click(object sender, RoutedEventArgs e)
        {
            OpenHelp("metrics_help.html");
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
            string? fiatCurrency = SettingFiatCurrency;

            if (connection != null)
            {
                try
                {
                    // Total Invested
                    DbCommand command = connection.CreateCommand();
                    command.CommandText = @"SELECT VALUE FROM TB_METRICS_S
											WHERE METRIC = 'TOTAL_INVESTED'";

                    DbDataReader reader = command.ExecuteReader();

                    decimal totalInvested = 0.00m;
                    if (reader.HasRows)
                    {
                        reader.Read();
                        totalInvested = reader.GetDecimalOrDefault(0, 0.00m);
                    }
                    lblTotalInvestedData.Content = totalInvested.ToString("F2") + " " + fiatCurrency;
                    reader.Close();

                    // Last Invested
                    command.CommandText = @"SELECT VALUE FROM TB_METRICS_S
											WHERE METRIC = 'LAST_INVESTED'";

                    reader = command.ExecuteReader();

                    string lastInvested = "";
                    if (reader.HasRows)
                    {
                        reader.Read();
                        lastInvested = reader.GetStringOrEmpty(0);
                    }
                    lblLastInvestedData.Content = lastInvested;
                    reader.Close();
                }
                catch (Exception ex)
                {
                    lastError = "There was a problem getting invest metrics." + Environment.NewLine + ex.Message;
                    MessageBoxResult result = CustomMessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    ConsoleLog(_mainWindow.txtLog, $"[Metrics] {lastError}");

                    // Exit function early
                    return;
                }
            }
        }

        private void UnbindLabels()
        {
            string? fiatCurrency = SettingFiatCurrency;
            lblTotalInvestedData.Content = "0.00 " + fiatCurrency;
            lblLastInvestedData.Content = "";
        }

        private void BindGrid()
        {
            string? fiatCurrency = SettingFiatCurrency;
            dgAvgBuyPrice.Columns[2].Header = "AMOUNT__" + fiatCurrency;
            dgRewardsSummary.Columns[3].Header = "AMOUNT__" + fiatCurrency;

            lblTotalAmountFiatData.Content = "0.00 " + fiatCurrency;

            if (connection != null)
            {
                int dbLineNumber = 0;
                decimal amnt_fiat;

                // Average buy price
                try
                {
                    // Create a collection of Model objects
                    ObservableCollection<AvgBuyPriceModel> dataAvgBuyPrice = [];

                    DbCommand command = connection.CreateCommand();

                    command.CommandText = "SELECT CURRENCY, COALESCE(AMOUNT_FIAT, 0.00) FROM TB_AVG_BUY_PRICE_S";
                    DbDataReader reader = command.ExecuteReader();

                    dbLineNumber = 0;
                    while (reader.Read())
                    {
                        dbLineNumber++;

                        amnt_fiat = reader.GetDecimalOrDefault(1);
                        dataAvgBuyPrice.Add(new AvgBuyPriceModel
                        {
                            RowNumber = dbLineNumber,
                            Currency = reader.GetStringOrEmpty(0),
                            Amount_fiat = amnt_fiat
                        });
                    }

                    reader.Close();

                    dgAvgBuyPrice.ItemsSource = dataAvgBuyPrice;
                }
                catch (Exception ex)
                {
                    lastError = "There was a problem getting average buy prices." + Environment.NewLine + ex.Message;

                    MessageBoxResult result = CustomMessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    ConsoleLog(_mainWindow.txtLog, $"[Metrics] {lastError}");

                    UnblockUI();

                    // Exit function early
                    return;
                }

                // Rewards
                try
                {
                    // Create a collection of RewardsSummaryModel objects
                    ObservableCollection<RewardsSummaryModel> dataRewards = [];

                    DbCommand command = connection.CreateCommand();

                    command.CommandText = "SELECT CURRENCY, AMOUNT, AMOUNT_FIAT FROM TB_REWARDS_SUMMARY_S where CAST(AMOUNT AS REAL) > 0";
                    DbDataReader reader = command.ExecuteReader();

                    decimal tot_amnt_fiat = 0.00m;
                    dbLineNumber = 0;
                    while (reader.Read())
                    {
                        dbLineNumber++;

                        amnt_fiat = reader.GetDecimalOrDefault(2);
                        dataRewards.Add(new RewardsSummaryModel
                        {
                            RowNumber = dbLineNumber,
                            Currency = reader.GetStringOrEmpty(0),
                            Amount = reader.GetDecimalOrDefault(1),
                            Amount_fiat = amnt_fiat
                        });
                        tot_amnt_fiat += amnt_fiat;
                    }
                    lblTotalAmountFiatData.Content = tot_amnt_fiat.ToString("F2") + " " + fiatCurrency;

                    reader.Close();

                    dgRewardsSummary.ItemsSource = dataRewards;
                }
                catch (Exception ex)
                {
                    lastError = "There was a problem getting rewards." + Environment.NewLine + ex.Message;

                    MessageBoxResult result = CustomMessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    ConsoleLog(_mainWindow.txtLog, $"[Metrics] {lastError}");

                    UnblockUI();

                    // Exit function early
                    return;
                }
            }
        }

        private void UnbindGrid()
        {
            string? fiatCurrency = SettingFiatCurrency;
            lblTotalAmountFiatData.Content = "0.00 " + fiatCurrency;
            dgAvgBuyPrice.ItemsSource = null;
            dgRewardsSummary.ItemsSource = null;
        }

        private async void Refresh()
        {
            string? fiatCurrency = SettingFiatCurrency;
            lastError = null;

            BlockUI();

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
                if (!OpenDatabaseConnection())
                {
                    return;
                }

                if (connection != null)
                {
                    DbCommand commandDelete = connection.CreateCommand();

                    /// Metrics
                    ConsoleLog(_mainWindow.txtLog, $"[Metrics] Calulating metrics");
                    await Task.Run(() =>
                    {
                        // Truncate standard DB table
                        commandDelete.CommandText = "DELETE FROM TB_METRICS_S";
                        commandDelete.ExecuteNonQuery();

                        // Total Invested
                        DbCommand commandInsert = connection.CreateCommand();

                        commandInsert.CommandText = $@"INSERT INTO TB_METRICS_S
                                                    SELECT 'TOTAL_INVESTED' AS METRIC,
                                                    SUM(CAST(AMOUNT AS NUMERIC)) AS VALUE
                                                    FROM TB_LEDGERS_S
                                                    WHERE CURRENCY = '{fiatCurrency}'
                                                    AND TYPE = 'DEPOSIT'";

                        commandInsert.ExecuteNonQuery();

                        // Last Invested
                        commandInsert.CommandText = $@"INSERT INTO TB_METRICS_S
													SELECT 'LAST_INVESTED' AS METRIC,
													MAX(DATE) AS VALUE
													FROM TB_LEDGERS_S
                                                    WHERE CURRENCY = '{fiatCurrency}'
                                                    AND TYPE = 'DEPOSIT'";

                        commandInsert.ExecuteNonQuery();
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
                        commandDelete.CommandText = "DELETE FROM TB_AVG_BUY_PRICE_S";
                        commandDelete.ExecuteNonQuery();

                        // Insert into standard DB table for each asset
                        using DbCommand command = connection.CreateCommand();
                        command.CommandText = $"SELECT CODE, ASSET FROM TB_ASSET_CATALOG_S WHERE CODE != '{fiatCurrency}' ORDER BY CODE, ASSET";
                        using DbDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            string code = reader.GetStringOrEmpty(0);
                            string asset = reader.GetStringOrEmpty(1);

                            using DbCommand commandIns = connection.CreateCommand();
                            commandIns.CommandText = CreateAvgBuyPriceInsert(asset);
                            commandIns.ExecuteNonQuery();
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
                            commandDelete.CommandText = "DELETE FROM TB_REWARDS_SUMMARY_S";
                            commandDelete.ExecuteNonQuery();

                            // Insert into standard DB table
                            using DbCommand command = connection.CreateCommand();
                            command.CommandText = @"SELECT catalog.CODE, catalog.ASSET
                                                    FROM
                                                    (SELECT CODE, ASSET FROM TB_ASSET_CATALOG_S) catalog
                                                    INNER JOIN
                                                    (SELECT DISTINCT CURRENCY FROM TB_LEDGERS_S) ledgers
                                                    ON catalog.ASSET = ledgers.CURRENCY
                                                    ORDER BY CODE, ASSET";
                            using DbDataReader reader = command.ExecuteReader();

                            // For each asset, create rewards summary insert

                            // Rate limiting mechanism //
                            DateTime lastCallTime = DateTime.MinValue;
                            /////////////////////////////
                            while (reader.Read())
                            {
                                string code = reader.GetStringOrEmpty(0);
                                string asset = reader.GetStringOrEmpty(1);

                                using DbCommand commandIns = connection.CreateCommand();

                                var (xInFiat, sqlCommand, conversionSource) = CreateRewardsSummaryInsert(asset, code, date, connection);
                                commandIns.CommandText = sqlCommand;

                                if (ConvertStringToDecimal(xInFiat) == 0m)
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
                                commandIns.ExecuteNonQuery();
                            }
                            if (lastWarning != null)
                            {
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    MessageBoxResult result = CustomMessageBox.Show($"There were issues calculating some reward amounts in {fiatCurrency}", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                                });
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

                connection?.Close();

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

            UnblockUI();
        }

        private static string CreateAvgBuyPriceInsert(string currency)
        {
            return $@"INSERT INTO TB_AVG_BUY_PRICE_S
                      SELECT CURRENCY, AMOUNT_FIAT
					  FROM(
                          SELECT '{currency}' AS CURRENCY,
                          SUM(AMOUNT * UNIT_PRICE_FIAT) / SUM(AMOUNT) AS AMOUNT_FIAT
                          FROM (
	                          SELECT BASE_AMOUNT AS AMOUNT, BASE_UNIT_PRICE_FIAT AS UNIT_PRICE_FIAT
	                          FROM TB_TRADES_S
	                          WHERE TYPE = 'BUY'
	                          AND BASE_CURRENCY = '{currency}'
	                          UNION ALL
	                          SELECT QUOTE_AMOUNT AS AMOUNT, QUOTE_UNIT_PRICE_FIAT AS UNIT_PRICE_FIAT
	                          FROM TB_TRADES_S
	                          WHERE TYPE = 'SELL'
	                          AND QUOTE_CURRENCY = '{currency}'))
					  WHERE AMOUNT_FIAT != ''";
        }

        private static (string xInFiat, string sqlCommand, string conversionSource) CreateRewardsSummaryInsert(string currency, string currency_code, DateTime date, SqliteConnection connection)
        {
            try
            {
                var (fiatAmount, source) = ConvertXToFiat(currency_code, 1m, date.Date, connection);
                string xInFiat = fiatAmount;
                string conversionSource = source;

                string sqlCommand = $@"WITH cas AS (
						                 SELECT printf('%.10f', SUM(CAST(AMOUNT AS NUMERIC)) - SUM(CAST(FEE AS NUMERIC))) AS REWARD_SUM
						                 FROM TB_LEDGERS_S
						                 WHERE TYPE IN ('EARN', 'STAKING', 'AIRDROP') AND CURRENCY IN ('{currency}')
					                     )
					                     INSERT INTO TB_REWARDS_SUMMARY_S
					                     SELECT 
						                     '{currency_code}' AS CURRENCY,
						                     IFNULL(cas.REWARD_SUM, 0.00) AS AMOUNT,
						                     IFNULL({xInFiat} * cas.REWARD_SUM, 0.00) AS AMOUNT_FIAT
					                     FROM cas";

                return (xInFiat, sqlCommand, conversionSource);
            }
            catch (Exception ex)
            {
                throw new Exception("CreateRewardsSummaryInsert failed", ex);
            }
        }

        private void ButtonRefresh_Click(object sender, RoutedEventArgs e)
        {
            lastWarning = null;
            Refresh();
        }
    }
}