using Microsoft.Data.Sqlite;
using ProjectCryptoGains.Common;
using System;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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
    /// Interaction logic for TradesWindow.xaml
    /// </summary>
    public partial class TradesWindow : SubwindowBase
    {
        private readonly MainWindow _mainWindow;

        private string fromDate = "2009-01-03";
        private string toDate = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        public TradesWindow(MainWindow mainWindow)
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
            OpenHelp("trades_help.html");
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
            string? fiatCurrency = SettingFiatCurrency;
            dgTrades.Columns[8].Header = "BASE__FEE__" + fiatCurrency;
            dgTrades.Columns[11].Header = "QUOTE__AMOUNT__" + fiatCurrency;
            dgTrades.Columns[13].Header = "QUOTE__FEE__" + fiatCurrency;
            dgTrades.Columns[15].Header = "BASE__UNIT__PRICE__" + fiatCurrency;
            dgTrades.Columns[17].Header = "QUOTE__UNIT__PRICE__" + fiatCurrency;
            dgTrades.Columns[18].Header = "TOTAL__FEE__" + fiatCurrency;

            // Create a collection of TradesModel objects
            ObservableCollection<TradesModel> data = [];

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
                                        BASE_CURRENCY,
                                        BASE_AMOUNT,
                                        BASE_FEE,
                                        BASE_FEE_FIAT,
                                        QUOTE_CURRENCY,
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
                                    FROM TB_TRADES_S
									WHERE strftime('%s', DATE) BETWEEN strftime('%s', '{fromDate}')
									  AND strftime('%s', date('{toDate}', '+1 day'))
									ORDER BY DATE ASC";

            DbDataReader reader = command.ExecuteReader();

            int dbLineNumber = 0;
            while (reader.Read())
            {
                dbLineNumber++;

                data.Add(new TradesModel
                {
                    RowNumber = dbLineNumber,
                    Refid = reader.GetStringOrEmpty(0),
                    Date = reader.GetStringOrEmpty(1),
                    Type = reader.GetStringOrEmpty(2),
                    Exchange = reader.GetStringOrEmpty(3),
                    Base_currency = reader.GetStringOrEmpty(4),
                    Base_amount = reader.GetDecimalOrDefault(5),
                    Base_fee = reader.GetDecimalOrDefault(6),
                    Base_fee_fiat = reader.GetDecimalOrNull(7),
                    Quote_currency = reader.GetStringOrEmpty(8),
                    Quote_amount = reader.GetDecimalOrDefault(9),
                    Quote_amount_fiat = reader.GetDecimalOrNull(10),
                    Quote_fee = reader.GetDecimalOrDefault(11),
                    Quote_fee_fiat = reader.GetDecimalOrNull(12),
                    Base_unit_price = reader.GetDecimalOrDefault(13),
                    Base_unit_price_fiat = reader.GetDecimalOrNull(14),
                    Quote_unit_price = reader.GetDecimalOrDefault(15),
                    Quote_unit_price_fiat = reader.GetDecimalOrNull(16),
                    Total_fee_fiat = reader.GetDecimalOrNull(17),
                    Costs_proceeds = reader.GetDecimalOrNull(18)
                });
            }
            reader.Close();
            connection.Close();

            dgTrades.ItemsSource = data;
        }

        private void UnbindGrid()
        {
            dgTrades.ItemsSource = null;
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
                        ConsoleLog(_mainWindow.txtLog, $"[Trades] " + tradesRefreshError);
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

        private void TextBoxToDate_KeyUp(object sender, KeyboardEventArgs e)
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

        private void TextBoxFromDate_KeyUp(object sender, KeyboardEventArgs e)
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
            if (!dgTrades.HasItems)
            {
                MessageBoxResult result = CustomMessageBox.Show("Nothing to print", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            ConsoleLog(_mainWindow.txtLog, $"[Trades] Printing Trades");

            BlockUI();

            // Create a PrintDialog
            PrintDialog printDlg = new();

            await Task.Run(() =>
            {
                // Create a FlowDocument dynamically.
                FlowDocument doc = CreateFlowDocument();
                doc.Name = "FlowDoc";
                // Create IDocumentPaginatorSource from FlowDocument
                IDocumentPaginatorSource idpSource = doc;
                // Call PrintDocument method to send document to printer
                printDlg.PrintDocument(idpSource.DocumentPaginator, "Project Crypto Gains - Trades");
            });

            ConsoleLog(_mainWindow.txtLog, $"[Trades] Printing done");

            UnblockUI();
        }

        /// <summary>
        /// This method creates a dynamic FlowDocument. You can add anything to this
        /// FlowDocument that you would like to send to the printer
        /// </summary>
        private FlowDocument CreateFlowDocument()
        {
            string? fiatCurrency = SettingFiatCurrency;
            // Create a FlowDocument
            FlowDocument flowDoc = new()
            {
                // Set the page width of the flow document to the width of an A4 page
                PageWidth = 793,
                ColumnWidth = 793,

                PagePadding = new Thickness(20),

                FontFamily = new FontFamily("Fixedsys"),
                FontSize = 8
            };

            Table table = new();
            table.RowGroups.Add(new TableRowGroup());
            TableRow? tableRow = new()
            {
                FontWeight = FontWeights.Bold
            };
            TableCell? tableCell = new(new Paragraph(new Run("Project Crypto Gains - Trades"))
            {
                FontSize = 16
            })
            {
                ColumnSpan = 8,
                TextAlignment = TextAlignment.Center
            };
            tableRow.Cells.Add(tableCell);
            table.RowGroups[0].Rows.Add(tableRow);

            tableRow = new TableRow();
            tableRow.Cells.Add(new TableCell(new Paragraph(new Run("\n"))));
            table.RowGroups[0].Rows.Add(tableRow);

            foreach (var item in dgTrades.ItemsSource.OfType<TradesModel>())
            {
                tableRow = new TableRow();
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run(item.Date ?? ""))));
                table.RowGroups[0].Rows.Add(tableRow);

                tableRow = new TableRow
                {
                    FontWeight = FontWeights.Bold
                };
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run("REFID"))));
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run("TYPE"))));
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run("EXCHANGE"))));
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run("BASE_AMOUNT"))));
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run("BASE_CURRENCY"))));
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run("QUOTE_AMOUNT"))));
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run("QUOTE_CURRENCY"))));
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run("QUOTE_AMOUNT_" + fiatCurrency))));
                table.RowGroups[0].Rows.Add(tableRow);

                tableRow = new TableRow();
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run(item.Refid ?? ""))));
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run(item.Type ?? ""))));
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run(item.Exchange ?? ""))));
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run($"{item.Base_amount,10:F10}" ?? ""))));
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run(item.Base_currency ?? ""))));
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run($"{item.Quote_amount,10:F10}" ?? ""))));
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run(item.Quote_currency ?? ""))));
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run($"{item.Quote_amount_fiat,2:F2}" ?? ""))));
                table.RowGroups[0].Rows.Add(tableRow);

                tableRow = new TableRow
                {
                    FontWeight = FontWeights.Bold
                };
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run("TOTAL_FEE_" + fiatCurrency))));
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run("COSTS_PROCEEDS"))));
                table.RowGroups[0].Rows.Add(tableRow);

                tableRow = new TableRow();
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run($"{item.Total_fee_fiat,2:F2}" ?? ""))));
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run($"{item.Costs_proceeds,2:F2}" ?? ""))));
                table.RowGroups[0].Rows.Add(tableRow);

                tableRow = new TableRow();
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run("\n"))));
                table.RowGroups[0].Rows.Add(tableRow);
            }
            flowDoc.Blocks.Add(table);
            return flowDoc;
        }
    }
}