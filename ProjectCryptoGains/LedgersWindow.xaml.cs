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
using static ProjectCryptoGains.Common.Utility;
using ProjectCryptoGains.Common;

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

            // Capture drag on titlebar
            TitleBar.MouseLeftButtonDown += (sender, e) => DragMove();

            _mainWindow = mainWindow;

            txtFromDate.Text = fromDate;
            txtToDate.Text = toDate;

            BindGrid();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            SystemCommands.MinimizeWindow(this);
        }

        private void Resize_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                SystemCommands.RestoreWindow(this);
            }
            else
            {
                SystemCommands.MaximizeWindow(this);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            SystemCommands.CloseWindow(this);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Visibility = Visibility.Hidden;
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
            if (!dgLedgers.HasItems)
            {
                MessageBoxResult result = CustomMessageBox.Show("Nothing to print", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            ConsoleLog(_mainWindow.txtLog, $"[Ledgers] Printing Ledgers");

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
                printDlg.PrintDocument(idpSource.DocumentPaginator, "Project Crypto Gains - Ledgers");
            });

            ConsoleLog(_mainWindow.txtLog, $"[Ledgers] Printing done");

            UnblockUI();
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
                ColumnSpan = 6,
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
                table.RowGroups[0].Rows.Add(tableRow);

                tableRow = new TableRow();
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run(item.Refid ?? ""))));
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run(item.Type ?? ""))));
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run(item.Exchange ?? ""))));
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run(item.Amount.ToString() ?? ""))));
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run(item.Currency ?? ""))));
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run(item.Fee.ToString() ?? ""))));
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