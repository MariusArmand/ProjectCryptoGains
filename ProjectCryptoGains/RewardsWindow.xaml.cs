using FirebirdSql.Data.FirebirdClient;
using ProjectCryptoGains.Common;
using ProjectCryptoGains.Common.Utils;
using System;
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
using static ProjectCryptoGains.Common.Utils.LedgersUtils;
using static ProjectCryptoGains.Common.Utils.ReaderUtils;
using static ProjectCryptoGains.Common.Utils.SettingUtils;
using static ProjectCryptoGains.Common.Utils.Utils;
using static ProjectCryptoGains.Common.Utils.WindowUtils;
using static ProjectCryptoGains.Models;

namespace ProjectCryptoGains
{
    /// <summary>
    /// Interaction logic for RewardsWindow.xaml
    /// </summary>
    public partial class RewardsWindow : SubwindowBase
    {
        private readonly MainWindow _mainWindow;

        private string fromDate = "2009-01-03";
        private string toDate = GetTodayAsIsoDate();

        private string? lastWarning = null;

        public RewardsWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            TitleBarElement = TitleBar;

            _mainWindow = mainWindow;

            txtFromDate.Text = fromDate;
            txtToDate.Text = toDate;

            BindGrid();
        }

        private void ButtonHelp_Click(object sender, RoutedEventArgs e)
        {
            OpenHelp("rewards_help.html");
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
            decimal rewardsTaxPercentage = SettingRewardsTaxPercentage;

            dgRewards.Columns[7].Header = "AMOUNT__" + fiatCurrency;
            dgRewardsSummary.Columns[3].Header = "AMOUNT__" + fiatCurrency;

            // Create a collections of model objects
            ObservableCollection<RewardsModel> RewardsData = [];
            ObservableCollection<RewardsSummaryModel> RewardsSummaryData = [];

            decimal tot_amnt_fiat = 0.00m;

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
                                                   REFID,
                                                   ""DATE"",
                                                   TYPE,
                                                   EXCHANGE,
                                                   CURRENCY,
                                                   AMOUNT,
                                                   AMOUNT_FIAT,
                                                   TAX,
                                                   UNIT_PRICE,
                                                   UNIT_PRICE_BREAK_EVEN,
                                                   AMOUNT_SELL_BREAK_EVEN
                                               FROM TB_REWARDS_S
                                               ORDER BY ""DATE"" ASC";

                using (DbDataReader reader = selectCommand.ExecuteReader())
                {
                    int dbLineNumber = 0;
                    while (reader.Read())
                    {
                        dbLineNumber++;

                        RewardsData.Add(new RewardsModel
                        {
                            RowNumber = dbLineNumber,
                            Refid = reader.GetStringOrEmpty(0),
                            Date = reader.GetDateTime(1),
                            Type = reader.GetStringOrEmpty(2),
                            Exchange = reader.GetStringOrEmpty(3),
                            Currency = reader.GetStringOrEmpty(4),
                            Amount = reader.GetDecimalOrDefault(5),
                            Amount_fiat = reader.GetDecimalOrDefault(6, 0.00m),
                            Tax = reader.GetDecimalOrDefault(7, 0.00m),
                            Unit_price = reader.GetDecimalOrDefault(8, 0.00m),
                            Unit_price_break_even = reader.GetDecimalOrDefault(9, 0.00m),
                            Amount_sell_break_even = reader.GetDecimalOrDefault(10)
                        });
                    }
                }

                dgRewards.ItemsSource = RewardsData;

                /////////////////////////////////

                selectCommand.CommandText = $@"SELECT 
                                                   CURRENCY,
                                                   ROUND(SUM(AMOUNT), 10) AS AMOUNT,
                                                   ROUND(SUM(AMOUNT_FIAT), 2) AS AMOUNT_FIAT,
                                                   ROUND(SUM(TAX), 2) AS TAX,
                                                   ROUND(AVG(UNIT_PRICE), 2) AS UNIT_PRICE,
                                                   ROUND(AVG(UNIT_PRICE_BREAK_EVEN), 2) AS UNIT_PRICE_BREAK_EVEN,
                                                   ROUND(SUM(AMOUNT_SELL_BREAK_EVEN), 10) AS AMOUNT_SELL_BREAK_EVEN
                                               FROM TB_REWARDS_S
                                               GROUP BY CURRENCY
                                               ORDER BY CURRENCY";

                using (DbDataReader reader = selectCommand.ExecuteReader())
                {
                    int dbLineNumber = 0;

                    decimal amnt_fiat = 0.00m;
                    while (reader.Read())
                    {
                        dbLineNumber++;

                        amnt_fiat = reader.GetDecimalOrDefault(2);
                        RewardsSummaryData.Add(new RewardsSummaryModel
                        {
                            RowNumber = dbLineNumber,
                            Currency = reader.GetStringOrEmpty(0),
                            Amount = reader.GetDecimalOrDefault(1),
                            Amount_fiat = amnt_fiat,
                            Tax = reader.GetDecimalOrDefault(3),
                            Unit_price = reader.GetDecimalOrDefault(4),
                            Unit_price_break_even = reader.GetDecimalOrDefault(5),
                            Amount_sell_break_even = reader.GetDecimalOrDefault(6)
                        });
                        tot_amnt_fiat += amnt_fiat;
                    }
                }
            }

            lblTotalAmountFiatData.Content = tot_amnt_fiat.ToString("F2") + " " + fiatCurrency;
            dgRewardsSummary.ItemsSource = RewardsSummaryData;
        }

        private void UnbindGrid()
        {
            string fiatCurrency = SettingFiatCurrency;
            dgRewards.ItemsSource = null;
            dgRewardsSummary.ItemsSource = null;
            lblTotalAmountFiatData.Content = "0.00 " + fiatCurrency;
        }

        private async void Refresh()
        {
            if (!IsValidDateFormat(txtFromDate.Text, "yyyy-MM-dd"))
            {
                MessageBoxResult result = CustomMessageBox.Show("From date does not have a correct YYYY-MM-DD format.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                // Exit function early
                return;
            }

            if (!IsValidDateFormat(txtToDate.Text, "yyyy-MM-dd"))
            {
                MessageBoxResult result = CustomMessageBox.Show("To date does not have a correct YYYY-MM-DD format.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                // Exit function early
                return;
            }

            BlockUI();

            try
            {

                ConsoleLog(_mainWindow.txtLog, $"[Rewards] Refreshing Rewards");

                bool ledgersRefreshFailed = false;
                string? ledgersRefreshWarning = null;
                bool ledgersRefreshWasBusy = false;
                if (chkRefreshLedgers.IsChecked == true)
                {
                    await Task.Run(() =>
                    {
                        try
                        {
                            ledgersRefreshWarning = RefreshLedgers(_mainWindow, Caller.Rewards);
                        }
                        catch (Exception)
                        {
                            ledgersRefreshFailed = true;
                        }
                        ledgersRefreshWasBusy = LedgersRefreshBusy; // Check if it was busy after the call
                    });
                }

                if (!ledgersRefreshWasBusy && !ledgersRefreshFailed)
                {
                    string? rewardsRefreshError = null;
                    // Refresh Rewards
                    try
                    {
                        await Task.Run(() => RefreshRewards());
                    }
                    catch (Exception ex)
                    {
                        while (ex.InnerException != null)
                        {
                            ex = ex.InnerException;
                        }
                        rewardsRefreshError = ex.Message;
                    }

                    if (rewardsRefreshError == null)
                    {
                        BindGrid();
                        if (ledgersRefreshWarning == null && lastWarning == null)
                        {
                            ConsoleLog(_mainWindow.txtLog, $"[Rewards] Refresh done");
                        }
                        else
                        {
                            ConsoleLog(_mainWindow.txtLog, $"[Rewards] Refresh done with warnings");
                        }
                    }
                    else
                    {
                        UnbindGrid();
                        ConsoleLog(_mainWindow.txtLog, $"[Rewards] {rewardsRefreshError}");
                        ConsoleLog(_mainWindow.txtLog, $"[Rewards] Refresh unsuccessful");
                    }
                }
                else
                {
                    UnbindGrid();
                    ConsoleLog(_mainWindow.txtLog, $"[Rewards] Refresh unsuccessful");
                }
            }
            finally
            {
                UnblockUI();
            }
        }

        private async Task RefreshRewards()
        {
            try
            {
                decimal rewardsTaxPercentage = SettingRewardsTaxPercentage;

                using (FbConnection connection = new(connectionString))
                {
                    connection.Open();

                    using DbCommand deleteCommand = connection.CreateCommand();

                    // Truncate standard DB table
                    deleteCommand.CommandText = "DELETE FROM TB_REWARDS_S";
                    deleteCommand.ExecuteNonQuery();

                    // Read rewards from Kraken ledgers and manual ledgers
                    using DbCommand selectCommand = connection.CreateCommand();
                    selectCommand.CommandText = $@"SELECT
                                                       ledgers.REFID,
                                                       ledgers.""DATE"",
                                                       ledgers.TYPE,
                                                       ledgers.EXCHANGE,
                                                       catalog.CODE AS CURRENCY,
                                                       ROUND(ledgers.AMOUNT - ledgers.FEE, 10) AS AMOUNT
                                                   FROM TB_LEDGERS_S ledgers
                                                       INNER JOIN TB_ASSET_CATALOG_S catalog 
                                                           ON ledgers.CURRENCY = catalog.ASSET
                                                   WHERE ledgers.TYPE IN ('EARN', 'STAKING', 'AIRDROP')
                                                       AND ledgers.""DATE"" BETWEEN @FROM_DATE AND @TO_DATE
                                                   ORDER BY ledgers.""DATE"" ASC";

                    // Convert string dates to DateTime and add parameters
                    AddParameterWithValue(selectCommand, "@FROM_DATE", ConvertStringToIsoDate(fromDate));
                    AddParameterWithValue(selectCommand, "@TO_DATE", ConvertStringToIsoDate(toDate).AddDays(1));

                    // Insert into rewards db table
                    using (DbDataReader reader = selectCommand.ExecuteReader())
                    {
                        // Rate limiting mechanism //
                        DateTime lastCallTime = DateTime.Now;
                        /////////////////////////////
                        while (reader.Read())
                        {
                            string refid = reader.GetStringOrEmpty(0);
                            DateTime date = reader.GetDateTime(1);
                            string type = reader.GetStringOrEmpty(2);
                            string exchange = reader.GetStringOrEmpty(3);
                            string currency = reader.GetStringOrEmpty(4);
                            decimal amount = reader.GetDecimalOrDefault(5);

                            var (fiatAmount, source) = ConvertXToFiat(currency, 1m, date.Date, connection);
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
                                lastWarning = $"[Rewards] Could not perform calculations for refid: {refid}" + Environment.NewLine + "Retrieved 0.00 exchange rate";
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    ConsoleLog(_mainWindow.txtLog, lastWarning);
                                });
                            }

                            using DbCommand insertCommand = connection.CreateCommand();
                            insertCommand.CommandText = $@"INSERT INTO TB_REWARDS_S (
                                                                REFID,
                                                                ""DATE"",
                                                                TYPE,
                                                                EXCHANGE,
                                                                CURRENCY,
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
                                                                @CURRENCY,
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
                            AddParameterWithValue(insertCommand, "@CURRENCY", currency);
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
                            MessageBoxResult result = CustomMessageBox.Show("There were issues with some calculations.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                        });
                    }

                }
            }
            catch (Exception ex)
            {
                throw new Exception("Refreshing rewards failed", ex);
            }
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

        private void ButtonRefresh_Click(object sender, RoutedEventArgs e)
        {
            lastWarning = null;
            Refresh();
        }

        private async void ButtonPrint_Click(object sender, RoutedEventArgs e)
        {
            if (!dgRewards.HasItems)
            {
                MessageBoxResult result = CustomMessageBox.Show("Nothing to print.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            ConsoleLog(_mainWindow.txtLog, $"[Rewards] Printing rewards");

            BlockUI();

            try
            {
                await PrintRewardsAsync();
                ConsoleLog(_mainWindow.txtLog, "[Rewards] Printing done");
            }
            catch (Exception ex)
            {
                ConsoleLog(_mainWindow.txtLog, $"[Rewards] Printing failed: {ex.Message}");
                MessageBoxResult result = CustomMessageBox.Show($"Printing failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                UnblockUI();
            }
        }

        private async Task PrintRewardsAsync()
        {
            string fiatCurrency = SettingFiatCurrency;
            var rewards = dgRewards.ItemsSource.OfType<RewardsModel>();

            PrintDialog printDlg = new();

            await PrintUtils.PrintFlowDocumentAsync(
                columnHeaders: new[] { "DATE", "REFID", "TYPE", "EXCHANGE", "CURRENCY", "AMOUNT", $"AMOUNT_{fiatCurrency}" },
                dataItems: rewards,
                dataExtractor: item => new[]
                {
                    (ConvertDateTimeToString(item.Date) ?? "", TextAlignment.Left, 1),
                    (item.Refid ?? "", TextAlignment.Left, 1),
                    (item.Type ?? "", TextAlignment.Left, 1),
                    (string.IsNullOrEmpty(item.Exchange) ? "N/A" : item.Exchange, TextAlignment.Left, 1),
                    (item.Currency ?? "", TextAlignment.Left, 1),
                    ($"{item.Amount,10:F10}", TextAlignment.Left, 1),
                    ($"{item.Amount_fiat,2:F2}", TextAlignment.Left, 1)
                },
                printDlg: printDlg,
                titlePage: true,
                title: "Project Crypto Gains - Rewards",
                subtitle: $"From\t{fromDate}\nTo\t{toDate}",
                maxColumnsPerRow: 7,
                repeatHeadersPerItem: true,
                itemsPerPage: 22
            );
        }

        private async void ButtonPrintSummary_Click(object sender, RoutedEventArgs e)
        {
            if (!dgRewardsSummary.HasItems)
            {
                MessageBoxResult result = CustomMessageBox.Show("Nothing to print.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            ConsoleLog(_mainWindow.txtLog, $"[Rewards] Printing rewards summary");

            BlockUI();

            try
            {
                await PrintRewardsSummaryAsync(lblTotalAmountFiatData.Content?.ToString() ?? "");
                ConsoleLog(_mainWindow.txtLog, "[Rewards] Printing done");
            }
            catch (Exception ex)
            {
                ConsoleLog(_mainWindow.txtLog, $"[Rewards] Printing failed: {ex.Message}");
                MessageBoxResult result = CustomMessageBox.Show($"Printing failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                UnblockUI();
            }
        }

        private async Task PrintRewardsSummaryAsync(string totalAmountFiat)
        {
            string fiatCurrency = SettingFiatCurrency;
            var rewardsSummary = dgRewardsSummary.ItemsSource.OfType<RewardsSummaryModel>();

            PrintDialog printDlg = new();

            await PrintUtils.PrintFlowDocumentAsync(
                columnHeaders: new[] { "CURRENCY", "AMOUNT", $"AMOUNT_{fiatCurrency}" },
                dataItems: rewardsSummary,
                dataExtractor: item => new[]
                {
                    (item.Currency ?? "", TextAlignment.Left, 1),
                    ($"{item.Amount,10:F10}", TextAlignment.Left, 1),
                    ($"{item.Amount_fiat,2:F2}", TextAlignment.Left, 1)
                },
                printDlg: printDlg,
                title: "Project Crypto Gains - Rewards Summary",
                subtitle: $"From\t{fromDate}\nTo\t{toDate}",
                summaryText: "Total rewards converted " + totalAmountFiat,
                maxColumnsPerRow: 8,
                repeatHeadersPerItem: true
            );
        }
    }
}