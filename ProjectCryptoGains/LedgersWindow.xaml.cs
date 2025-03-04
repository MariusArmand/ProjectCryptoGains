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

        private string fromDate = "2009-01-03";
        private string toDate = GetTodayAsIsoDate();

        public LedgersWindow(MainWindow mainWindow)
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
            OpenHelp("ledgers_help.html");
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
            ObservableCollection<LedgersModel> data = [];

            using SqliteConnection connection = new(connectionString);

            try
            {
                // code that may throw an exception
                connection.Open();
            }
            catch (Exception ex)
            {
                // code to handle the exception
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
                                         AMOUNT,
                                         CURRENCY,
                                         FEE,
                                         SOURCE,
                                         TARGET,
                                         NOTES
                                     FROM TB_LEDGERS_S
                                     WHERE strftime('%s', DATE) BETWEEN strftime('%s', '{fromDate}')
                                         AND strftime('%s', date('{toDate}', '+1 day'))
                                     ORDER BY DATE ASC";

            DbDataReader reader = command.ExecuteReader();

            int dbLineNumber = 0;
            while (reader.Read())
            {
                dbLineNumber++;

                data.Add(new LedgersModel
                {
                    RowNumber = dbLineNumber,
                    Refid = reader.GetStringOrEmpty(0),
                    Date = reader.GetStringOrEmpty(1),
                    Type = reader.GetStringOrEmpty(2),
                    Exchange = reader.GetStringOrEmpty(3),
                    Amount = reader.GetDecimalOrDefault(4),
                    Currency = reader.GetStringOrEmpty(5),
                    Fee = reader.GetDecimalOrDefault(6),
                    Source = reader.GetStringOrEmpty(7),
                    Target = reader.GetStringOrEmpty(8),
                    Notes = reader.GetStringOrEmpty(9)
                });
            }
            reader.Close();
            connection.Close();

            dgLedgers.ItemsSource = data;
        }

        private void UnbindGrid()
        {
            dgLedgers.ItemsSource = null;
        }

        private async void Refresh()
        {
            if (!IsValidDateFormat(txtFromDate.Text, "yyyy-MM-dd"))
            {
                // code to handle the exception
                MessageBoxResult result = CustomMessageBox.Show("From date does not have a correct YYYY-MM-DD format", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                // Exit function early
                return;
            }

            if (!IsValidDateFormat(txtToDate.Text, "yyyy-MM-dd"))
            {
                // code to handle the exception
                MessageBoxResult result = CustomMessageBox.Show("To date does not have a correct YYYY-MM-DD format", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                // Exit function early
                return;
            }

            BlockUI();

            ConsoleLog(_mainWindow.txtLog, $"[Ledgers] Refreshing ledgers");

            // Load the db table
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
                            ConsoleLog(_mainWindow.txtLog, $"[Ledgers] " + ex.InnerException.Message);
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

            UnblockUI();
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
            Refresh();
        }

        private async void ButtonPrint_Click(object sender, RoutedEventArgs e)
        {
            if (!dgLedgers.HasItems)
            {
                MessageBoxResult result = CustomMessageBox.Show("Nothing to print", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            ConsoleLog(_mainWindow.txtLog, $"[Ledgers] Printing Ledgers");

            BlockUI();

            try
            {
                await PrintLedgersAsync();
                ConsoleLog(_mainWindow.txtLog, "[Ledgers] Printing done");
            }
            catch (Exception ex)
            {
                ConsoleLog(_mainWindow.txtLog, $"[Ledgers] Printing failed: {ex.Message}");
                MessageBoxResult result = CustomMessageBox.Show($"Printing failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                columnHeaders: new[]
                {
                    "DATE", "REFID", "TYPE", "EXCHANGE", "AMOUNT", "CURRENCY",
                    "FEE", "SOURCE", "TARGET", "NOTES"
                },
                dataItems: ledgers,
                dataExtractor: item => new[]
                {
                    (item.Date ?? "", TextAlignment.Left, 1),
                    (item.Refid ?? "", TextAlignment.Left, 2),
                    (item.Type ?? "", TextAlignment.Left, 1),
                    (string.IsNullOrEmpty(item.Exchange) ? "N/A" : item.Exchange, TextAlignment.Left, 1),
                    ($"{item.Amount,10:F10}", TextAlignment.Left, 1),
                    (item.Currency ?? "", TextAlignment.Left, 1),
                    ($"{item.Fee,10:F10}", TextAlignment.Left, 1),
                    (string.IsNullOrEmpty(item.Source) ? "N/A" : item.Source, TextAlignment.Left, 1),
                    (string.IsNullOrEmpty(item.Target) ? "N/A" : item.Target, TextAlignment.Left, 1),
                    (string.IsNullOrEmpty(item.Notes) ? "N/A" : item.Notes, TextAlignment.Left, 3)
                },
                printDlg: printDlg,
                title: "Project Crypto Gains - Ledgers",
                footerHeight: 20,
                maxColumnsPerRow: 6,
                repeatHeadersPerItem: true,
                itemsPerPage: 15
            );
        }
    }
}