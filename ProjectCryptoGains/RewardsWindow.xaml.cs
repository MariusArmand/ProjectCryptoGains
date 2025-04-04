﻿using FirebirdSql.Data.FirebirdClient;
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
using static ProjectCryptoGains.Common.Utils.ParametersWindowsUtils;
using static ProjectCryptoGains.Common.Utils.ReaderUtils;
using static ProjectCryptoGains.Common.Utils.RewardsUtils;
using static ProjectCryptoGains.Common.Utils.SettingsUtils;
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

        private string _fromDate = "";
        private string _toDate = "";

        public RewardsWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            TitleBarElement = TitleBar;

            _mainWindow = mainWindow;

            ReadParametersWindows();
            BindGrid();
        }

        protected override void SubwindowBase_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.IsVisible)
            {
                ReadParametersWindows();
                txtFromDate.Foreground = Brushes.White;
                txtToDate.Foreground = Brushes.White;
            }
        }

        private void ReadParametersWindows()
        {
            _fromDate = ParWinRewardsFromDate;
            txtFromDate.Text = _fromDate;

            _toDate = ParWinRewardsToDate;
            txtToDate.Text = _toDate;
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

            dgRewards.Columns[7].Header = $"AMOUNT__{fiatCurrency}";
            dgRewardsSummary.Columns[3].Header = $"AMOUNT__{fiatCurrency}";

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
                    CustomMessageBox.Show("Database could not be opened." + Environment.NewLine + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                    // Exit function early
                    return;
                }

                using DbCommand selectCommand = connection.CreateCommand();
                selectCommand.CommandText = $@"SELECT 
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
                                               FROM TB_REWARDS
                                               ORDER BY ""DATE"" ASC";

                using (DbDataReader reader = selectCommand.ExecuteReader())
                {
                    int dbLineNumber = 0;
                    while (reader.Read())
                    {
                        dbLineNumber++;

                        RewardsData.Add(new RewardsModel
                        {
                            Row_number = dbLineNumber,
                            Refid = reader.GetStringOrEmpty(0),
                            Date = reader.GetDateTime(1),
                            Type = reader.GetStringOrEmpty(2),
                            Exchange = reader.GetStringOrEmpty(3),
                            Asset = reader.GetStringOrEmpty(4),
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
                                                   rewards.ASSET,
                                                   asset_catalog.LABEL,
                                                   ROUND(SUM(rewards.AMOUNT), 10) AS AMOUNT,
                                                   ROUND(SUM(rewards.AMOUNT_FIAT), 2) AS AMOUNT_FIAT,
                                                   ROUND(SUM(rewards.TAX), 2) AS TAX,
                                                   ROUND(AVG(rewards.UNIT_PRICE), 2) AS UNIT_PRICE,
                                                   ROUND(AVG(rewards.UNIT_PRICE_BREAK_EVEN), 2) AS UNIT_PRICE_BREAK_EVEN,
                                                   ROUND(SUM(rewards.AMOUNT_SELL_BREAK_EVEN), 10) AS AMOUNT_SELL_BREAK_EVEN
                                               FROM TB_REWARDS rewards
                                               LEFT OUTER JOIN TB_ASSET_CATALOG asset_catalog
                                                   ON rewards.ASSET = asset_catalog.ASSET
                                               GROUP BY rewards.ASSET, asset_catalog.LABEL
                                               ORDER BY rewards.ASSET";

                using (DbDataReader reader = selectCommand.ExecuteReader())
                {
                    int dbLineNumber = 0;

                    decimal amnt_fiat = 0.00m;
                    while (reader.Read())
                    {
                        dbLineNumber++;

                        amnt_fiat = reader.GetDecimalOrDefault(3);
                        RewardsSummaryData.Add(new RewardsSummaryModel
                        {
                            Row_number = dbLineNumber,
                            Asset = $"{reader.GetStringOrEmpty(0)} ({reader.GetStringOrEmpty(1)})",
                            Amount = reader.GetDecimalOrDefault(2),
                            Amount_fiat = amnt_fiat,
                            Tax = reader.GetDecimalOrDefault(4),
                            Unit_price = reader.GetDecimalOrDefault(5),
                            Unit_price_break_even = reader.GetDecimalOrDefault(6),
                            Amount_sell_break_even = reader.GetDecimalOrDefault(7)
                        });
                        tot_amnt_fiat += amnt_fiat;
                    }
                }
            }

            lblTotalAmountFiatData.Content = $"{tot_amnt_fiat.ToString("F2")} {fiatCurrency}";
            dgRewardsSummary.ItemsSource = RewardsSummaryData;
        }

        private void UnbindGrid()
        {
            string fiatCurrency = SettingFiatCurrency;
            dgRewards.ItemsSource = null;
            dgRewardsSummary.ItemsSource = null;
            lblTotalAmountFiatData.Content = $"0.00 {fiatCurrency}";
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
            _fromDate = txtFromDate.Text;
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
            _toDate = txtToDate.Text;
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            Refresh();
        }

        private async void Refresh()
        {
            if (!IsValidDateFormat(txtFromDate.Text, "yyyy-MM-dd"))
            {
                CustomMessageBox.Show("From date does not have a correct YYYY-MM-DD format.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                // Exit function early
                return;
            }

            if (!IsValidDateFormat(txtToDate.Text, "yyyy-MM-dd"))
            {
                CustomMessageBox.Show("To date does not have a correct YYYY-MM-DD format.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                // Exit function early
                return;
            }

            // Save rewards parameters
            ParWinRewardsFromDate = txtFromDate.Text;
            ParWinRewardsToDate = txtToDate.Text;

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
                    // Refresh rewards
                    string? rewardsRefreshError = null;
                    string? rewardsRefreshWarning = null;
                    bool rewardsRefreshWasBusy = false;
                    await Task.Run(async () =>
                    {
                        try
                        {
                            rewardsRefreshWarning = await RefreshRewards(_mainWindow, Caller.Rewards, _fromDate, _toDate);
                            rewardsRefreshWasBusy = RewardsRefreshBusy;
                        }
                        catch (Exception ex)
                        {
                            while (ex.InnerException != null)
                            {
                                ex = ex.InnerException;
                            }
                            rewardsRefreshError = ex.Message;
                        }
                    });

                    if (!rewardsRefreshWasBusy)
                    {
                        if (rewardsRefreshError == null)
                        {
                            BindGrid();
                            if (ledgersRefreshWarning == null && rewardsRefreshWarning == null)
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
                            ConsoleLog(_mainWindow.txtLog, $"[Rewards] {rewardsRefreshError}");
                            ConsoleLog(_mainWindow.txtLog, $"[Rewards] Refresh unsuccessful");
                        }
                    }
                    else
                    {
                        UnbindGrid();
                        ConsoleLog(_mainWindow.txtLog, $"[Rewards] There is already a rewards refresh in progress. Please Wait");
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

        private async void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            if (!dgRewards.HasItems)
            {
                CustomMessageBox.Show("Nothing to print.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            BlockUI();

            try
            {
                await PrintRewardsAsync();
            }
            catch (Exception ex)
            {
                ConsoleLog(_mainWindow.txtLog, $"[Rewards] Printing failed: {ex.Message}");
                CustomMessageBox.Show($"Printing failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                mainWindow: _mainWindow,
                caller: Caller.Rewards,
                columnHeaders: new[] { "DATE", "REFID", "TYPE", "EXCHANGE", "ASSET", "AMOUNT", $"AMOUNT_{fiatCurrency}" },
                dataItems: rewards,
                dataExtractor: item => new[]
                {
                    (ConvertDateTimeToString(item.Date) ?? "", TextAlignment.Left, 1),
                    (item.Refid ?? "", TextAlignment.Left, 1),
                    (item.Type ?? "", TextAlignment.Left, 1),
                    (string.IsNullOrEmpty(item.Exchange) ? "N/A" : item.Exchange, TextAlignment.Left, 1),
                    (item.Asset ?? "", TextAlignment.Left, 1),
                    ($"{item.Amount,10:F10}", TextAlignment.Left, 1),
                    ($"{item.Amount_fiat,2:F2}", TextAlignment.Left, 1)
                },
                printDlg: printDlg,
                titlePage: true,
                title: "Rewards",
                subtitle: $"From\t{_fromDate}\nTo\t{_toDate}",
                maxColumnsPerRow: 7,
                repeatHeadersPerItem: true,
                itemsPerPage: 22
            );
        }

        private void BtnHelp_Click(object sender, RoutedEventArgs e)
        {
            OpenHelp("rewards_help.html");
        }

        private async void BtnPrintSummary_Click(object sender, RoutedEventArgs e)
        {
            if (!dgRewardsSummary.HasItems)
            {
                CustomMessageBox.Show("Nothing to print.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            BlockUI();

            try
            {
                await PrintRewardsSummaryAsync(lblTotalAmountFiatData.Content?.ToString() ?? "");
            }
            catch (Exception ex)
            {
                ConsoleLog(_mainWindow.txtLog, $"[Rewards] Printing failed: {ex.Message}");
                CustomMessageBox.Show($"Printing failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                mainWindow: _mainWindow,
                caller: Caller.Rewards,
                columnHeaders: new[] { "ASSET", "AMOUNT", $"AMOUNT_{fiatCurrency}" },
                dataItems: rewardsSummary,
                dataExtractor: item => new[]
                {
                    (item.Asset ?? "", TextAlignment.Left, 1),
                    ($"{item.Amount,10:F10}", TextAlignment.Left, 1),
                    ($"{item.Amount_fiat,2:F2}", TextAlignment.Left, 1)
                },
                printDlg: printDlg,
                title: "Rewards Summary",
                subtitle: $"From\t{_fromDate}\nTo\t{_toDate}",
                summaryText: $"Total rewards converted {totalAmountFiat}",
                maxColumnsPerRow: 8,
                repeatHeadersPerItem: true
            );
        }
    }
}