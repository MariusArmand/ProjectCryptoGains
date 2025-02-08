using Microsoft.Data.Sqlite;
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
using static ProjectCryptoGains.Models;
using static ProjectCryptoGains.Utility;

namespace ProjectCryptoGains
{
    /// <summary>
    /// Interaction logic for TradesWindow.xaml
    /// </summary>
    public partial class TradesWindow : Window
    {
        private readonly MainWindow _mainWindow;

        private string fromDate = "2009-01-03";
        private string toDate = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        public TradesWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;

            txtFromDate.Foreground = Brushes.Black;
            txtFromDate.Text = fromDate;
            txtToDate.Foreground = Brushes.Black;
            txtToDate.Text = toDate;

            BindGrid();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Visibility = Visibility.Hidden;
        }

        private void ButtonHelp_Click(object sender, RoutedEventArgs e)
        {
            OpenHelp("trades_help.html");
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
                MessageBox.Show("Database could not be opened." + Environment.NewLine + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                btnRefresh.IsEnabled = true;
                this.Cursor = Cursors.Arrow;

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
                    Refid = reader.IsDBNull(0) ? "" : reader.GetString(0),
                    Date = reader.IsDBNull(1) ? "" : reader.GetString(1),
                    Type = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    Exchange = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    Base_currency = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    Base_amount = ConvertStringToDecimal(reader.GetString(5)),
                    Base_fee = ConvertStringToDecimal(reader.GetString(6)),
                    Base_fee_fiat = reader.IsDBNull(7) ? null : ConvertStringToDecimal(reader.GetString(7)),
                    Quote_currency = reader.IsDBNull(8) ? "" : reader.GetString(8),
                    Quote_amount = ConvertStringToDecimal(reader.GetString(9)),
                    Quote_amount_fiat = reader.IsDBNull(10) ? null : ConvertStringToDecimal(reader.GetString(10)),
                    Quote_fee = ConvertStringToDecimal(reader.GetString(11)),
                    Quote_fee_fiat = reader.IsDBNull(12) ? null : ConvertStringToDecimal(reader.GetString(12)),
                    Base_unit_price = ConvertStringToDecimal(reader.GetString(13)),
                    Base_unit_price_fiat = reader.IsDBNull(14) ? null : ConvertStringToDecimal(reader.GetString(14)),
                    Quote_unit_price = ConvertStringToDecimal(reader.GetString(15)),
                    Quote_unit_price_fiat = reader.IsDBNull(16) ? null : ConvertStringToDecimal(reader.GetString(16)),
                    Total_fee_fiat = reader.IsDBNull(17) ? null : ConvertStringToDecimal(reader.GetString(17)),
                    Costs_proceeds = reader.IsDBNull(18) ? null : ConvertStringToDecimal(reader.GetString(18))
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
                MessageBox.Show("From date does not have a correct YYYY-MM-DD format", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                // Exit function early
                return;
            }

            if (!IsValidDateFormat(txtToDate.Text, "yyyy-MM-dd"))
            {
                MessageBox.Show("To date does not have a correct YYYY-MM-DD format", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                // Exit function early
                return;
            }

            btnRefresh.IsEnabled = false;
            this.Cursor = Cursors.Wait;

            ConsoleLog(_mainWindow.txtLog, $"[Trades] Refreshing trades");

            bool ledgersRefreshWasBusy = false;
            bool ledgersRefreshFailed = false;
            if (chkRefreshLedgers.IsChecked == true)
            {
                await Task.Run(() =>
                {
                    try
                    {
                        RefreshLedgers(_mainWindow, "Trades");
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
                bool tradesRefreshWasBusy = false;
                await Task.Run(async () =>
                {
                    try
                    {
                        await RefreshTrades();
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
                        ConsoleLog(_mainWindow.txtLog, $"[Trades] Refresh done");
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

            btnRefresh.IsEnabled = true;
            this.Cursor = Cursors.Arrow;
        }

        private void TxtToDate_GotFocus(object sender, RoutedEventArgs e)
        {
            if (txtToDate.Text == "YYYY-MM-DD")
            {
                txtToDate.Text = string.Empty;
                txtToDate.Foreground = Brushes.Black;
            }
        }

        private void TxtToDate_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtToDate.Text))
            {
                txtToDate.Text = "YYYY-MM-DD";
                txtToDate.Foreground = Brushes.Gray;
            }
        }

        private void TextBoxToDate_KeyUp(object sender, KeyboardEventArgs e)
        {
            SetToDate();
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
                txtFromDate.Foreground = Brushes.Black;
            }
        }

        private void TxtFromDate_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtFromDate.Text))
            {
                txtFromDate.Text = "YYYY-MM-DD";
                txtFromDate.Foreground = Brushes.Gray;
            }
        }

        private void TextBoxFromDate_KeyUp(object sender, KeyboardEventArgs e)
        {
            SetFromDate();
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
                MessageBox.Show("Nothing to print", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            ConsoleLog(_mainWindow.txtLog, $"[Trades] Printing Trades");

            btnPrint.IsEnabled = false;
            imgBtnPrint.Source = new BitmapImage(new Uri(@"Resources/printer_busy.png", UriKind.Relative));
            this.Cursor = Cursors.Wait;

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

            btnPrint.IsEnabled = true;
            imgBtnPrint.Source = new BitmapImage(new Uri(@"Resources/printer.png", UriKind.Relative));
            this.Cursor = Cursors.Arrow;
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