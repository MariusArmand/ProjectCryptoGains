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
using static ProjectCryptoGains.Common.Utils.TradesUtils;
using static ProjectCryptoGains.Common.Utils.Utils;
using static ProjectCryptoGains.Common.Utils.WindowUtils;
using static ProjectCryptoGains.Models;

namespace ProjectCryptoGains
{
    /// <summary>
    /// Interaction logic for TradesWindow.xaml
    /// </summary>
    public partial class TradesWindow : SubwindowBase
    {
        private readonly MainWindow _mainWindow;

        private string fromDate = "2009-01-03";
        private string toDate = GetTodayAsIsoDate();

        public TradesWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            TitleBarElement = TitleBar;

            _mainWindow = mainWindow;

            txtFromDate.Text = fromDate;
            txtToDate.Text = toDate;

            BindGrid();
        }

        private void BlockUI()
        {
            btnRefresh.IsEnabled = false;

            btnPrint.IsEnabled = false;
            imgBtnPrint.Source = new BitmapImage(new Uri(@"Resources/printer_busy.png", UriKind.Relative));

            Cursor = Cursors.Wait;
        }

        private void UnblockUI()
        {
            btnRefresh.IsEnabled = true;

            btnPrint.IsEnabled = true;
            imgBtnPrint.Source = new BitmapImage(new Uri(@"Resources/printer.png", UriKind.Relative));

            Cursor = Cursors.Arrow;
        }

        private void BindGrid()
        {
            string fiatCurrency = SettingFiatCurrency;
            dgTrades.Columns[8].Header = $"BASE__FEE__{fiatCurrency}";
            dgTrades.Columns[11].Header = $"QUOTE__AMOUNT__{fiatCurrency}";
            dgTrades.Columns[13].Header = $"QUOTE__FEE__{fiatCurrency}";
            dgTrades.Columns[15].Header = $"BASE__UNIT__PRICE__{fiatCurrency}";
            dgTrades.Columns[17].Header = $"QUOTE__UNIT__PRICE__{fiatCurrency}";
            dgTrades.Columns[18].Header = $"TOTAL__FEE__{fiatCurrency}";

            // Create a collection of TradesModel objects
            ObservableCollection<TradesModel> TradesData = [];

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
                                                   BASE_ASSET,
                                                   BASE_AMOUNT,
                                                   BASE_FEE,
                                                   BASE_FEE_FIAT,
                                                   QUOTE_ASSET,
                                                   QUOTE_AMOUNT,
                                                   QUOTE_AMOUNT_FIAT,
                                                   QUOTE_FEE,
                                                   QUOTE_FEE_FIAT,
                                                   BASE_UNIT_PRICE,
                                                   BASE_UNIT_PRICE_FIAT,
                                                   QUOTE_UNIT_PRICE,
                                                   QUOTE_UNIT_PRICE_FIAT,
                                                   TOTAL_FEE_FIAT,
                                                   COSTS_PROCEEDS
                                               FROM TB_TRADES
                                               WHERE ""DATE"" BETWEEN @FROM_DATE AND @TO_DATE
                                               ORDER BY ""DATE"" ASC";

                // Convert string dates to DateTime and add parameters
                AddParameterWithValue(selectCommand, "@FROM_DATE", ConvertStringToIsoDate(fromDate));
                AddParameterWithValue(selectCommand, "@TO_DATE", ConvertStringToIsoDate(toDate).AddDays(1));

                using (DbDataReader reader = selectCommand.ExecuteReader())
                {
                    int dbLineNumber = 0;
                    while (reader.Read())
                    {
                        dbLineNumber++;

                        TradesData.Add(new TradesModel
                        {
                            Row_number = dbLineNumber,
                            Refid = reader.GetStringOrEmpty(0),
                            Date = reader.GetDateTime(1),
                            Type = reader.GetStringOrEmpty(2),
                            Exchange = reader.GetStringOrEmpty(3),
                            Base_asset = reader.GetStringOrEmpty(4),
                            Base_amount = reader.GetDecimalOrDefault(5),
                            Base_fee = reader.GetDecimalOrDefault(6),
                            Base_fee_fiat = reader.GetDecimal(7),
                            Quote_asset = reader.GetStringOrEmpty(8),
                            Quote_amount = reader.GetDecimalOrDefault(9),
                            Quote_amount_fiat = reader.GetDecimal(10),
                            Quote_fee = reader.GetDecimalOrDefault(11),
                            Quote_fee_fiat = reader.GetDecimal(12),
                            Base_unit_price = reader.GetDecimalOrDefault(13),
                            Base_unit_price_fiat = reader.GetDecimal(14),
                            Quote_unit_price = reader.GetDecimalOrDefault(15),
                            Quote_unit_price_fiat = reader.GetDecimal(16),
                            Total_fee_fiat = reader.GetDecimal(17),
                            Costs_proceeds = reader.GetDecimal(18)
                        });
                    }
                }
            }

            dgTrades.ItemsSource = TradesData;
        }

        private void UnbindGrid()
        {
            dgTrades.ItemsSource = null;
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

            BlockUI();

            try
            {
                ConsoleLog(_mainWindow.txtLog, $"[Trades] Refreshing trades");

                bool ledgersRefreshFailed = false;
                string? ledgersRefreshWarning = null;
                bool ledgersRefreshWasBusy = false;
                if (chkRefreshLedgers.IsChecked == true)
                {
                    await Task.Run(() =>
                    {
                        try
                        {
                            ledgersRefreshWarning = RefreshLedgers(_mainWindow, Caller.Trades);
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
                    // Load the db table
                    string? tradesRefreshError = null;
                    string? tradesRefreshWarning = null;
                    bool tradesRefreshWasBusy = false;
                    await Task.Run(async () =>
                    {
                        try
                        {
                            tradesRefreshWarning = await RefreshTrades(_mainWindow, Caller.Trades);
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

                    if (!tradesRefreshWasBusy)
                    {
                        if (tradesRefreshError == null)
                        {
                            BindGrid();
                            if (ledgersRefreshWarning == null && tradesRefreshWarning == null)
                            {
                                ConsoleLog(_mainWindow.txtLog, $"[Trades] Refresh done");
                            }
                            else
                            {
                                ConsoleLog(_mainWindow.txtLog, $"[Trades] Refresh done with warnings");
                            }
                        }
                        else
                        {
                            ConsoleLog(_mainWindow.txtLog, $"[Trades] {tradesRefreshError}");
                            ConsoleLog(_mainWindow.txtLog, $"[Trades] Refresh unsuccessful");
                        }
                    }
                    else
                    {
                        UnbindGrid();
                        ConsoleLog(_mainWindow.txtLog, $"[Trades] There is already a trades refresh in progress. Please Wait");
                        ConsoleLog(_mainWindow.txtLog, $"[Trades] Refresh unsuccessful");
                    }
                }
                else
                {
                    UnbindGrid();
                    ConsoleLog(_mainWindow.txtLog, $"[Trades] Refresh unsuccessful");
                }
            }
            finally
            {
                UnblockUI();
            }
        }

        private async void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            if (!dgTrades.HasItems)
            {
                CustomMessageBox.Show("Nothing to print.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            ConsoleLog(_mainWindow.txtLog, $"[Trades] Printing trades");

            BlockUI();

            try
            {
                await PrintTradesAsync();
                ConsoleLog(_mainWindow.txtLog, "[Trades] Printing done");
            }
            catch (Exception ex)
            {
                ConsoleLog(_mainWindow.txtLog, $"[Trades] Printing failed: {ex.Message}");
                CustomMessageBox.Show($"Printing failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                UnblockUI();
            }
        }

        private async Task PrintTradesAsync()
        {
            string fiatCurrency = SettingFiatCurrency;
            var trades = dgTrades.ItemsSource.OfType<TradesModel>();

            PrintDialog printDlg = new();

            await PrintUtils.PrintFlowDocumentAsync(
                columnHeaders: new[]
                {
                    "DATE", "REFID", "TYPE", "EXCHANGE", "BASE_AMOUNT", "BASE_ASSET",
                    "QUOTE_AMOUNT", "QUOTE_ASSET", $"QUOTE_AMOUNT_{fiatCurrency}",
                    $"TOTAL_FEE_{fiatCurrency}", "COSTS_PROCEEDS"
                },
                dataItems: trades,
                dataExtractor: item => new[]
                {
                    (ConvertDateTimeToString(item.Date) ?? "", TextAlignment.Left, 1),
                    (item.Refid ?? "", TextAlignment.Left, 2),
                    (item.Type ?? "", TextAlignment.Left, 1),
                    (item.Exchange ?? "", TextAlignment.Left, 1),
                    ($"{item.Base_amount,10:F10}", TextAlignment.Left, 1),
                    (item.Base_asset ?? "", TextAlignment.Left, 1),
                    ($"{item.Quote_amount,10:F10}", TextAlignment.Left, 1),
                    (item.Quote_asset ?? "", TextAlignment.Left, 1),
                    ($"{item.Quote_amount_fiat,2:F2}", TextAlignment.Left, 1),
                    ($"{item.Total_fee_fiat,2:F2}", TextAlignment.Left, 1),
                    ($"{item.Costs_proceeds,2:F2}", TextAlignment.Left, 1)
                },
                printDlg: printDlg,
                titlePage: true,
                title: "Trades",
                subtitle: $"From\t{fromDate}\nTo\t{toDate}",
                footerHeight: 20,
                maxColumnsPerRow: 7,
                repeatHeadersPerItem: true,
                itemsPerPage: 15
            );
        }

        private void BtnHelp_Click(object sender, RoutedEventArgs e)
        {
            OpenHelp("trades_help.html");
        }
    }
}