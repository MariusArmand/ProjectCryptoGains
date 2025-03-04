using Microsoft.Data.Sqlite;
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
            string? fiatCurrency = SettingFiatCurrency;
            decimal rewardsTaxPercentage = SettingRewardsTaxPercentage;

            dgRewards.Columns[7].Header = "AMOUNT__" + fiatCurrency;
            dgRewardsSummary.Columns[3].Header = "AMOUNT__" + fiatCurrency;

            // Create a collections of model objects
            ObservableCollection<RewardsModel> dataRewards = [];
            ObservableCollection<RewardsSummaryModel> dataRewardsSummary = [];

            using SqliteConnection connection = new(connectionString);

            try
            {
                connection.Open();
            }
            catch (Exception ex)
            {
                MessageBoxResult result = CustomMessageBox.Show("Database could not be opened." + Environment.NewLine + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                UnblockUI();

                // Exit function early
                return;
            }

            DbCommand command = connection.CreateCommand();
            command.CommandText = $@"SELECT 
                                         REFID,
                                         DATE,
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
                                     ORDER BY DATE ASC";

            DbDataReader reader = command.ExecuteReader();

            int dbLineNumber = 0;
            while (reader.Read())
            {
                dbLineNumber++;

                dataRewards.Add(new RewardsModel
                {
                    RowNumber = dbLineNumber,
                    Refid = reader.GetStringOrEmpty(0),
                    Date = reader.GetStringOrEmpty(1),
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
            reader.Close();

            dgRewards.ItemsSource = dataRewards;

            /////////////////////////////////

            command.CommandText = $@"SELECT 
                                         CURRENCY,
                                         printf('%.10f', SUM(CAST(AMOUNT AS REAL))) AS AMOUNT,
                                         printf('%.2f', SUM(CAST(AMOUNT_FIAT AS REAL))) AS AMOUNT_FIAT,
                                         printf('%.2f', SUM(CAST(TAX AS REAL))) AS TAX,
                                         printf('%.2f', AVG(CAST(UNIT_PRICE AS REAL))) AS UNIT_PRICE,
                                         printf('%.2f', AVG(CAST(UNIT_PRICE_BREAK_EVEN AS REAL))) AS UNIT_PRICE_BREAK_EVEN,
                                         printf('%.10f', SUM(CAST(AMOUNT_SELL_BREAK_EVEN AS REAL))) AS AMOUNT_SELL_BREAK_EVEN
                                     FROM TB_REWARDS_S
                                     GROUP BY CURRENCY
                                     ORDER BY CURRENCY";

            reader = command.ExecuteReader();

            dbLineNumber = 0;

            decimal tot_amnt_fiat = 0.00m;
            decimal amnt_fiat = 0.00m;
            while (reader.Read())
            {
                dbLineNumber++;

                amnt_fiat = reader.GetDecimalOrDefault(2);
                dataRewardsSummary.Add(new RewardsSummaryModel
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
            reader.Close();
            connection.Close();

            lblTotalAmountFiatData.Content = tot_amnt_fiat.ToString("F2") + " " + fiatCurrency;
            dgRewardsSummary.ItemsSource = dataRewardsSummary;

        }

        private void UnbindGrid()
        {
            string? fiatCurrency = SettingFiatCurrency;
            dgRewards.ItemsSource = null;
            dgRewardsSummary.ItemsSource = null;
            lblTotalAmountFiatData.Content = "0.00 " + fiatCurrency;
        }

        private async void Refresh()
        {
            if (!IsValidDateFormat(txtFromDate.Text, "yyyy-MM-dd"))
            {
                MessageBoxResult result = CustomMessageBox.Show("From date does not have a correct YYYY-MM-DD format", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                // Exit function early
                return;
            }

            if (!IsValidDateFormat(txtToDate.Text, "yyyy-MM-dd"))
            {
                MessageBoxResult result = CustomMessageBox.Show("To date does not have a correct YYYY-MM-DD format", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                // Exit function early
                return;
            }

            BlockUI();

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

            UnblockUI();
        }

        private async Task RefreshRewards()
        {
            try
            {
                decimal rewardsTaxPercentage = SettingRewardsTaxPercentage;

                using SqliteConnection connection = new(connectionString);
                connection.Open();

                DbCommand commandDelete = connection.CreateCommand();

                // Truncate standard DB table
                commandDelete.CommandText = "DELETE FROM TB_REWARDS_S";
                commandDelete.ExecuteNonQuery();

                // Read rewards from Kraken ledgers and manual ledgers
                using (DbCommand command = connection.CreateCommand())
                {
                    command.CommandText = $@"SELECT
                                                 ledgers.REFID,
                                                 ledgers.DATE,
                                                 ledgers.TYPE,
                                                 ledgers.EXCHANGE,
                                                 catalog.CODE AS CURRENCY,
                                                 printf('%.10f', ledgers.AMOUNT - ledgers.FEE) AS AMOUNT
                                             FROM TB_LEDGERS_S ledgers
                                                 INNER JOIN TB_ASSET_CATALOG_S catalog 
                                                     ON ledgers.CURRENCY = catalog.ASSET
                                             WHERE ledgers.TYPE IN ('EARN', 'STAKING', 'AIRDROP')
                                                 AND strftime('%s', DATE) BETWEEN strftime('%s', '{fromDate}')
                                                     AND strftime('%s', date('{toDate}', '+1 day'))";

                    // Insert into rewards db table
                    using DbDataReader reader = command.ExecuteReader();

                    // Rate limiting mechanism //
                    DateTime lastCallTime = DateTime.Now;
                    /////////////////////////////
                    while (reader.Read())
                    {
                        string refid = reader.GetStringOrEmpty(0);
                        string date = reader.GetStringOrEmpty(1);
                        string type = reader.GetStringOrEmpty(2);
                        string exchange = reader.GetStringOrEmpty(3);
                        string currency = reader.GetStringOrEmpty(4);
                        string amount = reader.GetStringOrEmpty(5);
                        DateTime datetime = ConvertStringToIsoDateTime(date);

                        var (fiatAmount, source) = ConvertXToFiat(currency, 1m, datetime.Date, connection);
                        string exchangeRate = fiatAmount;

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
                        string amount_fiat = "0.00";
                        string tax = "0.00";
                        string unit_price_break_even = "0.00";
                        string amount_sell_break_even = "0.00";

                        if (ConvertStringToDecimal(exchangeRate) != 0m)
                        {
                            amount_fiat = (ConvertStringToDecimal(exchangeRate) * ConvertStringToDecimal(amount)).ToString();
                            tax = (ConvertStringToDecimal(amount_fiat) * (rewardsTaxPercentage / 100m)).ToString("F10");
                            unit_price_break_even = (ConvertStringToDecimal(exchangeRate) * (1 + (rewardsTaxPercentage / 100m))).ToString("F10");
                            amount_sell_break_even = (ConvertStringToDecimal(tax) / ConvertStringToDecimal(unit_price_break_even)).ToString("F10");
                        }
                        else
                        {
                            lastWarning = $"[Rewards] Could not perform calculations for refid: {refid}" + Environment.NewLine + "Retrieved 0.00 exchange rate";
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                ConsoleLog(_mainWindow.txtLog, lastWarning);
                            });
                        }

                        using DbCommand commandIns = connection.CreateCommand();
                        commandIns.CommandText = $@"INSERT INTO TB_REWARDS_S (
                                                        REFID,
                                                        DATE,
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
                                                        printf('%.10f', @AMOUNT_FIAT),
                                                        @TAX,
                                                        @UNIT_PRICE,
                                                        @UNIT_PRICE_BREAK_EVEN,
                                                        @AMOUNT_SELL_BREAK_EVEN
                                                    )";

                        AddParameterWithValue(commandIns, "@REFID", refid);
                        AddParameterWithValue(commandIns, "@DATE", date);
                        AddParameterWithValue(commandIns, "@TYPE", type);
                        AddParameterWithValue(commandIns, "@EXCHANGE", exchange);
                        AddParameterWithValue(commandIns, "@CURRENCY", currency);
                        AddParameterWithValue(commandIns, "@AMOUNT", amount);
                        AddParameterWithValue(commandIns, "@AMOUNT_FIAT", amount_fiat);
                        AddParameterWithValue(commandIns, "@TAX", tax);
                        AddParameterWithValue(commandIns, "@UNIT_PRICE", exchangeRate);
                        AddParameterWithValue(commandIns, "@UNIT_PRICE_BREAK_EVEN", unit_price_break_even);
                        AddParameterWithValue(commandIns, "@AMOUNT_SELL_BREAK_EVEN", amount_sell_break_even);

                        commandIns.ExecuteNonQuery();
                    }
                    if (lastWarning != null)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            MessageBoxResult result = CustomMessageBox.Show("There were issues with some calculations", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                        });
                    }
                }
                connection.Close();
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
                MessageBoxResult result = CustomMessageBox.Show("Nothing to print", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            ConsoleLog(_mainWindow.txtLog, $"[Rewards] Printing Rewards");

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
            string? fiatCurrency = SettingFiatCurrency;
            var rewards = dgRewards.ItemsSource.OfType<RewardsModel>();

            PrintDialog printDlg = new();

            await PrintUtils.PrintFlowDocumentAsync(
                columnHeaders: new[] { "DATE", "REFID", "TYPE", "EXCHANGE", "CURRENCY", "AMOUNT", $"AMOUNT {fiatCurrency}" },
                dataItems: rewards,
                dataExtractor: item => new[]
                {
                    (item.Date ?? "", TextAlignment.Left, 1),
                    (item.Refid ?? "", TextAlignment.Left, 1),
                    (item.Type ?? "", TextAlignment.Left, 1),
                    (item.Exchange ?? "", TextAlignment.Left, 1),
                    (item.Currency ?? "", TextAlignment.Left, 1),
                    ($"{item.Amount,10:F10}", TextAlignment.Left, 1),
                    ($"{item.Amount_fiat,2:F2}", TextAlignment.Left, 1)
                },
                printDlg: printDlg,
                title: "Project Crypto Gains - Rewards",
                maxColumnsPerRow: 7,
                repeatHeadersPerItem: true,
                itemsPerPage: 21
            );
        }

        private async void ButtonPrintSummary_Click(object sender, RoutedEventArgs e)
        {
            if (!dgRewardsSummary.HasItems)
            {
                MessageBoxResult result = CustomMessageBox.Show("Nothing to print", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            ConsoleLog(_mainWindow.txtLog, $"[Rewards] Printing Rewards Summary");

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
            string? fiatCurrency = SettingFiatCurrency;
            var rewardsSummary = dgRewardsSummary.ItemsSource.OfType<RewardsSummaryModel>();

            PrintDialog printDlg = new();

            await PrintUtils.PrintFlowDocumentAsync(
                columnHeaders: new[] { "CURRENCY", "AMOUNT", $"AMOUNT {fiatCurrency}" },
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
                maxColumnsPerRow: 3,
                repeatHeadersPerItem: true,
                virtualColumnCount: 8
            );
        }
    }
}