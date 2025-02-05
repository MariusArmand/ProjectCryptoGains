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
    /// Interaction logic for LedgersWindow.xaml
    /// </summary>
    public partial class LedgersWindow : Window
    {
        private readonly MainWindow _mainWindow;

        //public LedgersHelpWindow? winLedgersHelp;

        private string fromDate = "2009-01-03";
        private string toDate = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        public LedgersWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;

            txtFromDate.Foreground = Brushes.Black;
            txtFromDate.Text = fromDate;
            txtToDate.Foreground = Brushes.Black;
            txtToDate.Text = toDate;

            Refresh();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Visibility = Visibility.Hidden;
        }

        private void ButtonHelp_Click(object sender, RoutedEventArgs e)
        {
            OpenHelp("ledgers_help.html");
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
										AMOUNT, 
										CURRENCY, 
										FEE, 
										CASE WHEN REFID LIKE 'MANUAL%' THEN '' ELSE BALANCE END AS BALANCE,
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
                    Refid = reader.IsDBNull(0) ? "" : reader.GetString(0),
                    Date = reader.IsDBNull(1) ? "" : reader.GetString(1),
                    Type = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    Exchange = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    Amount = ConvertStringToDecimal(reader.GetString(4)),
                    Currency = reader.IsDBNull(5) ? "" : reader.GetString(5),
                    Fee = ConvertStringToDecimal(reader.GetString(6)),
                    Balance = reader.IsDBNull(7) ? "" : reader.GetString(7),
                    Source = reader.IsDBNull(8) ? "" : reader.GetString(8),
                    Target = reader.IsDBNull(9) ? "" : reader.GetString(9),
                    Notes = reader.IsDBNull(10) ? "" : reader.GetString(10)
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
                MessageBox.Show("From date does not have a correct YYYY-MM-DD format", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                // Exit function early
                return;
            }

            if (!IsValidDateFormat(txtToDate.Text, "yyyy-MM-dd"))
            {
                // code to handle the exception
                MessageBox.Show("To date does not have a correct YYYY-MM-DD format", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                // Exit function early
                return;
            }

            btnRefresh.IsEnabled = false;
            this.Cursor = Cursors.Wait;

            ConsoleLog(_mainWindow.txtLog, $"[Ledgers] Refreshing ledgers");

            // Load the db table
            bool ledgersRefreshWasBusy = false;
            bool ledgersRefreshFailed = false;
            await Task.Run(() =>
            {
                try
                {
                    RefreshLedgers();
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
                ledgersRefreshWasBusy = LedgersRefreshBusy; // Check if it was busy when called
            });

            if (!ledgersRefreshWasBusy && !ledgersRefreshFailed)
            {
                BindGrid();
                ConsoleLog(_mainWindow.txtLog, $"[Ledgers] Refresh done");
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
            if (!dgLedgers.HasItems)
            {
                MessageBox.Show("Nothing to print", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            ConsoleLog(_mainWindow.txtLog, $"[Ledgers] Printing Ledgers");

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
                printDlg.PrintDocument(idpSource.DocumentPaginator, "Project Crypto Gains - Ledgers");
            });

            ConsoleLog(_mainWindow.txtLog, $"[Ledgers] Printing done");

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
            TableCell? tableCell = new(new Paragraph(new Run("Project Crypto Gains - Ledgers"))
            {
                FontSize = 16
            })
            {
                ColumnSpan = 7,
                TextAlignment = TextAlignment.Center
            };
            tableRow.Cells.Add(tableCell);
            table.RowGroups[0].Rows.Add(tableRow);

            tableRow = new TableRow();
            tableRow.Cells.Add(new TableCell(new Paragraph(new Run("\n"))));
            table.RowGroups[0].Rows.Add(tableRow);

            foreach (var item in dgLedgers.ItemsSource.OfType<LedgersModel>())
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
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run("AMOUNT"))));
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run("CURRENCY"))));
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run("FEE"))));
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run("BALANCE"))));
                table.RowGroups[0].Rows.Add(tableRow);

                tableRow = new TableRow();
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run(item.Refid ?? ""))));
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run(item.Type ?? ""))));
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run(item.Exchange ?? ""))));
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run(item.Amount.ToString() ?? ""))));
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run(item.Currency ?? ""))));
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run(item.Fee.ToString() ?? ""))));
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run(item.Balance?.NullIfEmpty() ?? "N/A"))));
                table.RowGroups[0].Rows.Add(tableRow);

                tableRow = new TableRow
                {
                    FontWeight = FontWeights.Bold
                };
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run("SOURCE"))));
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run("TARGET"))));
                tableCell = new TableCell(new Paragraph(new Run("NOTES")))
                {
                    ColumnSpan = 2
                };
                tableRow.Cells.Add(tableCell);
                table.RowGroups[0].Rows.Add(tableRow);

                tableRow = new TableRow();
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run(item.Source?.NullIfEmpty() ?? "N/A"))));
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run(item.Target?.NullIfEmpty() ?? "N/A"))));
                tableCell = new TableCell(new Paragraph(new Run(item.Notes?.NullIfEmpty() ?? "N/A")))
                {
                    ColumnSpan = 2
                };
                tableRow.Cells.Add(tableCell);
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