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
using static ProjectCryptoGains.Common.Utils.Utils;
using static ProjectCryptoGains.Common.Utils.WindowUtils;
using static ProjectCryptoGains.Models;

namespace ProjectCryptoGains
{
    /// <summary>
    /// Interaction logic for LedgersWindow.xaml
    /// </summary>
    public partial class LedgersWindow : SubwindowBase
    {
        private readonly MainWindow _mainWindow;

        private string _fromDate = "";
        private string _toDate = "";

        public LedgersWindow(MainWindow mainWindow)
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
            _fromDate = ParWinLedgersFromDate;
            txtFromDate.Text = _fromDate;

            _toDate = ParWinLedgersToDate;
            txtToDate.Text = _toDate;
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

        public void BindGrid()
        {
            // Create a collection of LedgersModel objects
            ObservableCollection<LedgersModel> LedgersData = [];

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
                                                   AMOUNT,
                                                   ASSET,
                                                   FEE,
                                                   SOURCE,
                                                   TARGET,
                                                   NOTES
                                               FROM TB_LEDGERS
                                               WHERE ""DATE"" BETWEEN @FROM_DATE AND @TO_DATE
                                               ORDER BY ""DATE"", REFID, AMOUNT ASC";

                // Convert string dates to DateTime and add parameters
                AddParameterWithValue(selectCommand, "@FROM_DATE", ConvertStringToIsoDate(_fromDate));
                AddParameterWithValue(selectCommand, "@TO_DATE", ConvertStringToIsoDate(_toDate).AddDays(1));

                using (DbDataReader reader = selectCommand.ExecuteReader())
                {
                    int dbLineNumber = 0;
                    while (reader.Read())
                    {
                        dbLineNumber++;

                        LedgersData.Add(new LedgersModel
                        {
                            Row_number = dbLineNumber,
                            Refid = reader.GetStringOrEmpty(0),
                            Date = reader.GetDateTime(1),
                            Type = reader.GetStringOrEmpty(2),
                            Exchange = reader.GetStringOrEmpty(3),
                            Amount = reader.GetDecimalOrDefault(4),
                            Asset = reader.GetStringOrEmpty(5),
                            Fee = reader.GetDecimalOrDefault(6),
                            Source = reader.GetStringOrEmpty(7),
                            Target = reader.GetStringOrEmpty(8),
                            Notes = reader.GetStringOrEmpty(9)
                        });
                    }
                }
            }

            dgLedgers.ItemsSource = LedgersData;
        }

        private void UnbindGrid()
        {
            dgLedgers.ItemsSource = null;
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

            // Save ledgers parameters
            ParWinLedgersFromDate = txtFromDate.Text;
            ParWinLedgersToDate = txtToDate.Text;

            BlockUI();

            try
            {
                ConsoleLog(_mainWindow.txtLog, $"[Ledgers] Refreshing ledgers");

                // Refresh ledgers
                bool ledgersRefreshFailed = false;
                string? ledgersRefreshWarning = null;
                bool ledgersRefreshWasBusy = false;
                await Task.Run(() =>
                {
                    try
                    {
                        ledgersRefreshWarning = RefreshLedgers(_mainWindow, Caller.Ledgers);
                        ledgersRefreshWasBusy = LedgersRefreshBusy; // Check if it was busy when called
                    }
                    catch (Exception ex)
                    {
                        ledgersRefreshFailed = true;
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            if (ex.InnerException != null)
                            {
                                ConsoleLog(_mainWindow.txtLog, $"[Ledgers] {ex.InnerException.Message}");
                            }
                        });
                    }

                });

                if (!ledgersRefreshWasBusy && !ledgersRefreshFailed)
                {
                    BindGrid();
                    if (ledgersRefreshWarning == null)
                    {
                        ConsoleLog(_mainWindow.txtLog, $"[Ledgers] Refresh done");
                    }
                    else
                    {
                        ConsoleLog(_mainWindow.txtLog, $"[Ledgers] Refresh done with warnings");
                    }
                }
                else
                {
                    UnbindGrid();
                    if (ledgersRefreshWasBusy)
                    {
                        ConsoleLog(_mainWindow.txtLog, $"[Ledgers] There is already a ledgers refresh in progress. Please Wait");
                    }
                    ConsoleLog(_mainWindow.txtLog, $"[Ledgers] Refresh unsuccessful");
                }
            }
            finally
            {
                UnblockUI();
            }
        }

        private async void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            if (!dgLedgers.HasItems)
            {
                CustomMessageBox.Show("Nothing to print.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            BlockUI();

            try
            {
                await PrintLedgersAsync();
            }
            catch (Exception ex)
            {
                ConsoleLog(_mainWindow.txtLog, $"[Ledgers] Printing failed: {ex.Message}");
                CustomMessageBox.Show($"Printing failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                UnblockUI();
            }
        }

        private async Task PrintLedgersAsync()
        {
            var ledgers = dgLedgers.ItemsSource.OfType<LedgersModel>();

            PrintDialog printDlg = new();

            await PrintUtils.PrintFlowDocumentAsync(
                mainWindow: _mainWindow,
                caller: Caller.Ledgers,
                columnHeaders: new[]
                {
                    "DATE", "REFID", "TYPE", "EXCHANGE", "AMOUNT", "ASSET",
                    "FEE", "SOURCE", "TARGET", "NOTES"
                },
                dataItems: ledgers,
                dataExtractor: item => new[]
                {
                    (ConvertDateTimeToString(item.Date) ?? "", TextAlignment.Left, 1),
                    (item.Refid ?? "", TextAlignment.Left, 2),
                    (item.Type ?? "", TextAlignment.Left, 1),
                    (string.IsNullOrEmpty(item.Exchange) ? "N/A" : item.Exchange, TextAlignment.Left, 1),
                    ($"{item.Amount,10:F10}", TextAlignment.Left, 1),
                    (item.Asset ?? "", TextAlignment.Left, 1),
                    ($"{item.Fee,10:F10}", TextAlignment.Left, 1),
                    (string.IsNullOrEmpty(item.Source) ? "N/A" : item.Source, TextAlignment.Left, 1),
                    (string.IsNullOrEmpty(item.Target) ? "N/A" : item.Target, TextAlignment.Left, 1),
                    (string.IsNullOrEmpty(item.Notes) ? "N/A" : item.Notes, TextAlignment.Left, 3)
                },
                printDlg: printDlg,
                titlePage: true,
                title: "Ledgers",
                subtitle: $"From\t{_fromDate}\nTo\t{_toDate}",
                footerHeight: 20,
                maxColumnsPerRow: 7,
                repeatHeadersPerItem: true,
                itemsPerPage: 15
            );
        }

        private void BtnHelp_Click(object sender, RoutedEventArgs e)
        {
            OpenHelp("ledgers_help.html");
        }
    }
}