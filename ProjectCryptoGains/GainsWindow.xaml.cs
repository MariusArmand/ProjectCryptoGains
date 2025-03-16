using FirebirdSql.Data.FirebirdClient;
using ProjectCryptoGains.Common;
using ProjectCryptoGains.Common.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static ProjectCryptoGains.Common.Utils.DatabaseUtils;
using static ProjectCryptoGains.Common.Utils.DateUtils;
using static ProjectCryptoGains.Common.Utils.ExceptionUtils;
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
    /// Interaction logic for GainsWindow.xaml
    /// </summary>
    public partial class GainsWindow : SubwindowBase
    {
        private readonly MainWindow _mainWindow;

        private int errors = 0;
        private string fromDate = "2009-01-03";
        private string toDate = GetTodayAsIsoDate();
        private string baseCurrency = "";

        public GainsWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            TitleBarElement = TitleBar;

            _mainWindow = mainWindow;

            txtFromDate.Text = fromDate;
            txtToDate.Text = toDate;
            txtBaseCurrency.Text = baseCurrency;

            BindGrid();
        }

        private void BlockUI()
        {
            btnRefresh.IsEnabled = false;

            btnPrint.IsEnabled = false;
            imgBtnPrint.Source = new BitmapImage(new Uri(@"Resources/printer_busy.png", UriKind.Relative));
            btnPrintSummary.IsEnabled = false;
            imgBtnPrintSummary.Source = new BitmapImage(new Uri(@"Resources/printer_busy.png", UriKind.Relative));

            Cursor = Cursors.Wait;
        }

        private void UnblockUI()
        {
            btnRefresh.IsEnabled = true;

            btnPrint.IsEnabled = true;
            imgBtnPrint.Source = new BitmapImage(new Uri(@"Resources/printer.png", UriKind.Relative));
            btnPrintSummary.IsEnabled = true;
            imgBtnPrintSummary.Source = new BitmapImage(new Uri(@"Resources/printer.png", UriKind.Relative));

            Cursor = Cursors.Arrow;
        }

        private void BindGrid()
        {
            string fiatCurrency = SettingFiatCurrency;
            dgGains.Columns[8].Header = "BASE__UNIT__PRICE__" + fiatCurrency;

            ObservableCollection<GainsModel> GainsData = [];
            ObservableCollection<GainsSummaryModel> GainsSummaryData = [];

            decimal tot_gain = 0.00m;

            using (FbConnection connection = new(connectionString))
            {
                try
                {
                    connection.Open();
                }
                catch (Exception ex)
                {
                    MessageBoxResult result = CustomMessageBox.Show("Database could not be opened." + Environment.NewLine + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                    // Exit function early
                    return;
                }

                using DbCommand selectCommand = connection.CreateCommand();
                selectCommand.CommandText = $@"SELECT 
                                                   trades.REFID,
                                                   trades.""DATE"",
                                                   trades.TYPE,
                                                   trades.BASE_CURRENCY,
                                                   trades.BASE_AMOUNT,
                                                   trades.QUOTE_CURRENCY,
                                                   trades.QUOTE_AMOUNT,
                                                   trades.BASE_UNIT_PRICE_FIAT,
                                                   trades.COSTS_PROCEEDS,
                                                   CASE 
                                                       WHEN trades.TYPE = 'SELL' THEN NULL
                                                       ELSE gains.TX_BALANCE_REMAINING
                                                   END AS TX_BALANCE_REMAINING,
                                                   CASE 
                                                       WHEN trades.TYPE = 'BUY' THEN NULL
                                                       ELSE gains.GAIN
                                                   END AS GAIN
                                               FROM TB_GAINS gains
                                                   INNER JOIN TB_TRADES trades
                                                       ON gains.REFID = trades.REFID
                                               WHERE trades.BASE_CURRENCY LIKE '%{baseCurrency}%'
                                               ORDER BY ""DATE"" ASC";

                using (DbDataReader reader = selectCommand.ExecuteReader())
                {
                    int dbLineNumber = 0;
                    while (reader.Read())
                    {
                        dbLineNumber++;

                        GainsData.Add(new GainsModel
                        {
                            Row_number = dbLineNumber,
                            Refid = reader.GetStringOrEmpty(0),
                            Date = reader.GetDateTime(1),
                            Type = reader.GetStringOrEmpty(2),
                            Base_currency = reader.GetStringOrEmpty(3),
                            Base_amount = reader.GetDecimalOrDefault(4),
                            Quote_currency = reader.GetStringOrEmpty(5),
                            Quote_amount = reader.GetDecimalOrDefault(6),
                            Base_unit_price_fiat = reader.GetDecimal(7),
                            Costs_proceeds = reader.GetDecimal(8),
                            Tx_balance_remaining = reader.GetDecimalOrNull(9),
                            Gain = reader.GetDecimalOrNull(10)
                        });
                    }
                }

                dgGains.ItemsSource = GainsData;

                /////////////////////////////////

                selectCommand.CommandText = $@"SELECT 
                                                   trades.BASE_CURRENCY AS CURRENCY,
                                                   ROUND(SUM(gains.GAIN), 10) AS GAIN
                                               FROM TB_GAINS gains
                                                   INNER JOIN TB_TRADES trades
                                                       ON gains.REFID = trades.REFID
                                               WHERE gains.GAIN IS NOT NULL
                                                   AND trades.BASE_CURRENCY LIKE @BASE_CURRENCY
                                                   AND trades.""DATE"" BETWEEN @FROM_DATE AND @TO_DATE
                                               GROUP BY trades.BASE_CURRENCY
                                               ORDER BY trades.BASE_CURRENCY";

                AddParameterWithValue(selectCommand, "@BASE_CURRENCY", $"%{baseCurrency}%");
                AddParameterWithValue(selectCommand, "@FROM_DATE", ConvertStringToIsoDate(fromDate));
                AddParameterWithValue(selectCommand, "@TO_DATE", ConvertStringToIsoDate(toDate).AddDays(1));

                using (DbDataReader reader = selectCommand.ExecuteReader())
                {
                    int dbLineNumber = 0;

                    decimal gain = 0.00m;
                    while (reader.Read())
                    {
                        dbLineNumber++;

                        gain = reader.GetDecimalOrDefault(1);
                        GainsSummaryData.Add(new GainsSummaryModel
                        {
                            Row_number = dbLineNumber,
                            Currency = reader.GetStringOrEmpty(0),
                            Gain = reader.GetDecimalOrDefault(1)
                        });
                        tot_gain += gain;
                    }
                }
            }
            lblTotalGainsData.Content = tot_gain.ToString("F2") + " " + fiatCurrency;
            dgGainsSummary.ItemsSource = GainsSummaryData;
        }

        private void UnbindGrid()
        {
            string fiatCurrency = SettingFiatCurrency;
            lblTotalGainsData.Content = "0.00 " + fiatCurrency;
            dgGains.ItemsSource = null;
            dgGainsSummary.ItemsSource = null;
        }

        private void TxtFromDate_GotFocus(object sender, RoutedEventArgs e)
        {
            if (txtFromDate.Text == "YYYY-MM-DD")
            {
                txtFromDate.Text = string.Empty;
            }
        }

        private void TxtFromDate_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtFromDate.Text))
            {
                txtFromDate.Text = "YYYY-MM-DD";
                txtFromDate.Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102)); // Gray #666666
            }
        }

        private void TxtFromDate_KeyUp(object sender, KeyboardEventArgs e)
        {
            SetFromDate();
            txtFromDate.Foreground = Brushes.White;
        }

        private void SetFromDate()
        {
            fromDate = txtFromDate.Text;
        }

        private void TxtToDate_GotFocus(object sender, RoutedEventArgs e)
        {
            if (txtToDate.Text == "YYYY-MM-DD")
            {
                txtToDate.Text = string.Empty;
            }
        }

        private void TxtToDate_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtToDate.Text))
            {
                txtToDate.Text = "YYYY-MM-DD";
                txtToDate.Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102)); // Gray #666666
            }
        }

        private void TxtToDate_KeyUp(object sender, KeyboardEventArgs e)
        {
            SetToDate();
            txtToDate.Foreground = Brushes.White;
        }

        private void SetToDate()
        {
            toDate = txtToDate.Text;
        }

        private void TextBoxBaseCurrency_KeyUp(object sender, KeyboardEventArgs e)
        {
            SetBaseCurrency();
        }

        private void SetBaseCurrency()
        {
            baseCurrency = txtBaseCurrency.Text;
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            Refresh();
        }

        private async void Refresh()
        {
            errors = 0;

            if (!IsValidDateFormat(txtFromDate.Text, "yyyy-MM-dd"))
            {
                MessageBoxResult result = CustomMessageBox.Show("From date does not have a correct YYYY-MM-DD format.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!IsValidDateFormat(txtToDate.Text, "yyyy-MM-dd"))
            {
                MessageBoxResult result = CustomMessageBox.Show("To date does not have a correct YYYY-MM-DD format.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            BlockUI();

            try
            {

                ConsoleLog(_mainWindow.txtLog, $"[Gains] Refreshing gains");

                bool ledgersRefreshFailed = false;
                string? ledgersRefreshWarning = null;
                bool ledgersRefreshWasBusy = false;
                if (chkRefreshLedgers.IsChecked == true)
                {
                    await Task.Run(() =>
                    {
                        try
                        {
                            ledgersRefreshWarning = RefreshLedgers(_mainWindow, Caller.Gains);
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
                            tradesRefreshWarning = await RefreshTrades(_mainWindow, Caller.Gains);
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

                // LIFO processing
                if (!ledgersRefreshWasBusy && !tradesRefreshWasBusy && tradesRefreshError == null && !ledgersRefreshFailed)
                {
                    await Task.Run(() =>
                    {
                        using (FbConnection connection = new(connectionString))
                        {
                            try
                            {
                                connection.Open();
                                // Clear the table before inserting new data
                                using DbCommand deleteCommand = connection.CreateCommand();
                                deleteCommand.CommandText = "DELETE FROM TB_GAINS";
                                deleteCommand.ExecuteNonQuery();

                                // Read the assets into a list
                                using DbCommand selectCommand = connection.CreateCommand();
                                selectCommand.CommandText = $@"SELECT ASSET FROM TB_ASSET_CATALOG WHERE ASSET like '%{baseCurrency}%'";

                                List<string> assets = [];

                                using (DbDataReader reader = selectCommand.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        string asset = reader.GetStringOrEmpty(0);
                                        assets.Add(asset);
                                    }
                                }
                                connection.Close();

                                // For each asset do LIFO processing
                                foreach (string asset in assets)
                                {
                                    List<TransactionsModel> sellTransactions = ReadTransactionsFromDB(asset, "SELL", "ASC");
                                    List<TransactionsModel> buyTransactions = ReadTransactionsFromDB(asset, "BUY");
                                    CalculateLIFOGains(asset, sellTransactions, buyTransactions);
                                    WriteTransactionsToDB(sellTransactions, buyTransactions, connectionString);
                                }
                            }
                            catch (Exception ex)
                            {
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    string errorMessage;
                                    switch (ex)
                                    {
                                        case DatabaseReadException dbREx:
                                            errorMessage = BuildErrorMessage(dbREx);
                                            break;

                                        case DatabaseWriteException dbWEx:
                                            errorMessage = BuildErrorMessage(dbWEx);
                                            break;

                                        default:
                                            errorMessage = $"Gains could not be calculated.{Environment.NewLine}{ex.Message}";
                                            break;
                                    }

                                    MessageBoxResult result = CustomMessageBox.Show(errorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                });
                                return;
                            }
                        }
                    });

                    if (errors == 0)
                    {
                        BindGrid();
                        if (ledgersRefreshWarning == null && tradesRefreshWarning == null)
                        {
                            ConsoleLog(_mainWindow.txtLog, $"[Gains] Refresh done");
                        }
                        else
                        {
                            ConsoleLog(_mainWindow.txtLog, $"[Gains] Refresh done with warnings");
                        }
                    }
                    else
                    {
                        UnbindGrid();
                        ConsoleLog(_mainWindow.txtLog, $"[Gains] Refresh unsuccessful");
                    }
                }
                else
                {
                    UnbindGrid();
                    if (tradesRefreshError != null)
                    {
                        ConsoleLog(_mainWindow.txtLog, $"[Gains] " + tradesRefreshError);
                        ConsoleLog(_mainWindow.txtLog, $"[Gains] Refreshing trades unsuccessful");
                    }
                    ConsoleLog(_mainWindow.txtLog, $"[Gains] Refresh unsuccessful");
                }
            }
            finally
            {
                UnblockUI();
            }
        }

        private static List<TransactionsModel> ReadTransactionsFromDB(String asset, String tx_type, string orderBy = "DESC")
        {
            List<TransactionsModel> transactions = [];

            using (FbConnection connection = new(connectionString))
            {
                try
                {
                    connection.Open();

                    using DbCommand selectCommand = connection.CreateCommand();
                    selectCommand.CommandText = $@"SELECT 
                                                       REFID,
                                                       ""DATE"",
                                                       BASE_AMOUNT AS AMOUNT,
                                                       BASE_UNIT_PRICE_FIAT AS UNIT_PRICE,
                                                       COSTS_PROCEEDS,
                                                       BASE_AMOUNT AS TX_BALANCE_REMAINING
                                                   FROM TB_TRADES
                                                   WHERE BASE_CURRENCY = '{asset}'
                                                       AND TYPE = '{tx_type}'
                                                   ORDER BY ""DATE"" {orderBy}";

                    using (DbDataReader reader = selectCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            decimal? txBalanceRemaining;
                            if (tx_type == "SELL")
                            {
                                txBalanceRemaining = null; // We don't need to keep track of remaining balances for sell transactions
                            }
                            else
                            {
                                txBalanceRemaining = reader.GetDecimal(5);
                            }

                            transactions.Add(new TransactionsModel
                            {
                                RefId = reader.GetStringOrEmpty(0),
                                Date = reader.GetDateTime(1),
                                Amount = reader.GetDecimalOrDefault(2),
                                Unit_price = reader.GetDecimal(3),
                                Costs_Proceeds = reader.GetDecimal(4),
                                Tx_Balance_Remaining = txBalanceRemaining
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new DatabaseReadException("Transaction could not be read.", ex);
                }
            }

            return transactions;
        }

        private void CalculateLIFOGains(String asset, List<TransactionsModel> sellTransactions, List<TransactionsModel> buyTransactions)
        {
            foreach (var stx in sellTransactions)
            {
                // Initialize the amount we need to sell for this sell transaction
                decimal amountToSell = stx.Amount;

                decimal proceeds = stx.Costs_Proceeds;
                decimal costs = 0;

                // Parse the sell transaction date if it's not null
                DateTime sellDate = stx.Date;

                // Filter buy transactions to only those on or before the sell date
                var relevantBuyTransactions = buyTransactions
                    .Where(btx => btx.Date <= sellDate)
                    .ToList();

                // Calculate the sum of amounts in relevant buy transactions
                decimal totalRelevantAmountBought = relevantBuyTransactions.Sum(btx => btx.Amount);

                if (totalRelevantAmountBought < amountToSell)
                {
                    // Instead of showing directly, schedule MessageBox on the UI thread to not block this thread
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        errors += 1;
                        string lastError = "Not enough buy transactions to cover this sell transaction." +
                                           Environment.NewLine + $"RefId: {stx.RefId}" +
                                           Environment.NewLine + $"Base currency: {asset}" +
                                           Environment.NewLine + $"Amount missing: {amountToSell - totalRelevantAmountBought}";
                        ConsoleLog(_mainWindow.txtLog, $"[Gains] {lastError}");
                        MessageBoxResult result = CustomMessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                    break;
                }

                // Continue selling until we've processed the entire amount to sell
                while (amountToSell > 0 && relevantBuyTransactions.Count != 0)
                {
                    // Iterate through each buy transaction in order (since they're ordered by date descending)
                    foreach (var btx in relevantBuyTransactions)
                    {
                        if (btx.Tx_Balance_Remaining > 0)
                        {
                            decimal soldAmount = Math.Min((decimal)btx.Tx_Balance_Remaining, amountToSell);

                            // Add the cost of the sold amount
                            costs += btx.Unit_price * soldAmount;

                            btx.Tx_Balance_Remaining -= soldAmount;
                            amountToSell -= soldAmount;

                            if (amountToSell == 0) break;
                        }
                    }
                }
                // After processing all relevant buy transactions, set the gain for this sell transaction
                stx.Gain = proceeds - costs;
            }
        }

        private static void WriteTransactionsToDB(List<TransactionsModel> sellTransactions, List<TransactionsModel> buyTransactions, string connectionString)
        {
            using (FbConnection connection = new(connectionString))
            {
                try
                {
                    connection.Open();

                    using DbCommand insertCommand = connection.CreateCommand();
                    insertCommand.CommandText = @"INSERT INTO TB_GAINS (
                                                      REFID,
                                                      TX_BALANCE_REMAINING,
                                                      GAIN
                                                  ) VALUES (
                                                      @REFID,
                                                      ROUND(@TX_BALANCE_REMAINING, 10),
                                                      CASE 
                                                          WHEN @GAIN IS NOT NULL
                                                          THEN ROUND(@GAIN, 10) 
                                                          ELSE NULL
                                                      END
                                                  )";

                    foreach (var tx in sellTransactions)
                    {
                        if (tx.RefId != null) // Ensure REFID isn't null before adding
                        {
                            // Clear parameters from previous iteration
                            insertCommand.Parameters.Clear();

                            AddParameterWithValue(insertCommand, "@REFID", tx.RefId);
                            AddParameterWithValue(insertCommand, "@TX_BALANCE_REMAINING", tx.Tx_Balance_Remaining);
                            AddParameterWithValue(insertCommand, "@GAIN", tx.Gain);

                            insertCommand.ExecuteNonQuery();
                        }
                    }

                    foreach (var tx in buyTransactions)
                    {
                        if (tx.RefId != null) // Ensure REFID isn't null before adding
                        {
                            // Clear parameters from previous iteration
                            insertCommand.Parameters.Clear();

                            AddParameterWithValue(insertCommand, "@REFID", tx.RefId);
                            AddParameterWithValue(insertCommand, "@TX_BALANCE_REMAINING", tx.Tx_Balance_Remaining);
                            AddParameterWithValue(insertCommand, "@GAIN", tx.Gain);

                            insertCommand.ExecuteNonQuery();
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new DatabaseWriteException("Error writing to database.", ex);
                }
            }
        }

        private async void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            if (!dgGains.HasItems)
            {
                MessageBoxResult result = CustomMessageBox.Show("Nothing to print.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            ConsoleLog(_mainWindow.txtLog, $"[Gains] Printing gains");

            BlockUI();

            try
            {
                await PrintGainsAsync();
                ConsoleLog(_mainWindow.txtLog, "[Gains] Printing done");
            }
            catch (Exception ex)
            {
                ConsoleLog(_mainWindow.txtLog, $"[Gains] Printing failed: {ex.Message}");
                MessageBoxResult result = CustomMessageBox.Show($"Printing failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                UnblockUI();
            }
        }

        private async Task PrintGainsAsync()
        {
            string fiatCurrency = SettingFiatCurrency;
            var gains = dgGains.ItemsSource.OfType<GainsModel>();

            PrintDialog printDlg = new();

            await PrintUtils.PrintFlowDocumentAsync(
                columnHeaders: new[]
                {
                    "DATE", "REFID", "TYPE", "BASE_CURRENCY", "BASE_AMOUNT", "QUOTE_CURRENCY",
                    "QUOTE_AMOUNT", $"BASE_UNIT_PRICE_{fiatCurrency}", "COSTS_PROCEEDS",
                    "TX_BALANCE_REMAINING", "GAIN"
                },
                dataItems: gains,
                dataExtractor: item => new[]
                {
                    (ConvertDateTimeToString(item.Date) ?? "", TextAlignment.Left, 1),
                    (item.Refid ?? "", TextAlignment.Left, 2),
                    (item.Type ?? "", TextAlignment.Left, 1),
                    (item.Base_currency ?? "", TextAlignment.Left, 1),
                    ($"{item.Base_amount,10:F10}", TextAlignment.Left, 1),
                    (item.Quote_currency ?? "", TextAlignment.Left, 1),
                    ($"{item.Quote_amount,10:F10}", TextAlignment.Left, 1),
                    ($"{item.Base_unit_price_fiat,2:F2}", TextAlignment.Left, 1),
                    ($"{item.Costs_proceeds,2:F2}", TextAlignment.Left, 1),
                    (item.Tx_balance_remaining.HasValue ? $"{item.Tx_balance_remaining,10:F10}" : "N/A", TextAlignment.Left, 1),
                    (item.Gain.HasValue ? $"{item.Gain,2:F2}" : "N/A", TextAlignment.Left, 1)
                },
                printDlg: printDlg,
                titlePage: true,
                title: "Gains",
                subtitle: $"From\t{fromDate}\nTo\t{toDate}",
                footerHeight: 20,
                maxColumnsPerRow: 7,
                repeatHeadersPerItem: true,
                itemsPerPage: 15
            );
        }

        private void BtnHelp_Click(object sender, RoutedEventArgs e)
        {
            OpenHelp("gains_help.html");
        }

        private async void BtnPrintSummary_Click(object sender, RoutedEventArgs e)
        {
            if (!dgGainsSummary.HasItems)
            {
                MessageBoxResult result = CustomMessageBox.Show("Nothing to print.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            ConsoleLog(_mainWindow.txtLog, $"[Gains] Printing gains summary");

            BlockUI();

            try
            {
                await PrintGainsSummaryAsync(lblTotalGainsData.Content?.ToString() ?? "");
                ConsoleLog(_mainWindow.txtLog, "[Gains] Printing done");
            }
            catch (Exception ex)
            {
                ConsoleLog(_mainWindow.txtLog, $"[Gains] Printing failed: {ex.Message}");
                MessageBoxResult result = CustomMessageBox.Show($"Printing failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                UnblockUI();
            }
        }

        private async Task PrintGainsSummaryAsync(string totalGains)
        {
            string fiatCurrency = SettingFiatCurrency;
            var gainsSummary = dgGainsSummary.ItemsSource.OfType<GainsSummaryModel>();

            PrintDialog printDlg = new();

            await PrintUtils.PrintFlowDocumentAsync(
                columnHeaders: new[] { "CURRENCY", "GAIN" },
                dataItems: gainsSummary,
                dataExtractor: item => new[]
                {
                    (item.Currency ?? "", TextAlignment.Left, 1),
                    ($"{item.Gain,2:F2}", TextAlignment.Left, 1)
                },
                printDlg: printDlg,
                title: "Gains Summary",
                subtitle: $"From\t{fromDate}\nTo\t{toDate}",
                summaryText: "Total gains " + totalGains,
                maxColumnsPerRow: 7,
                repeatHeadersPerItem: true
            );
        }
    }
}