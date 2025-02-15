﻿using Microsoft.Data.Sqlite;
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
    // OBSOLETE WINDOW

    /// <summary>
    /// Interaction logic for TradesRawWindow.xaml
    /// </summary>
    public partial class TradesRawWindow : Window
    {
        private readonly MainWindow _mainWindow;

        private string fromDate = "2009-01-03";
        private string toDate = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        public TradesRawWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;

            //txtFromDate.Foreground = Brushes.Black;
            txtFromDate.Text = fromDate;
            //txtToDate.Foreground = Brushes.Black;
            txtToDate.Text = toDate;

            Refresh();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Visibility = Visibility.Hidden;
        }

        private void BindGrid()
        {
            // Create a collection of TradesRawModel objects
            ObservableCollection<TradesRawModel> data = [];

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
                                        DATE,
                                        TYPE,
                                        EXCHANGE,
                                        BASE_AMOUNT,
                                        BASE_CURRENCY,
                                        QUOTE_AMOUNT,
                                        QUOTE_CURRENCY,
                                        FEE
                                        --FEE_CURRENCY
                                    FROM TB_TRADES_RAW_S
									WHERE strftime('%s', DATE) BETWEEN strftime('%s', '{fromDate}')
									  AND strftime('%s', date('{toDate}', '+1 day'))
									ORDER BY DATE ASC";

            DbDataReader reader = command.ExecuteReader();

            int dbLineNumber = 0;
            while (reader.Read())
            {
                dbLineNumber++;

                data.Add(new TradesRawModel
                {
                    RowNumber = dbLineNumber,
                    Date = reader.IsDBNull(0) ? "" : reader.GetString(0),
                    Type = reader.IsDBNull(1) ? "" : reader.GetString(1),
                    Exchange = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    Base_amount = ConvertStringToDecimal(reader.GetString(3)),
                    Base_currency = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    Quote_amount = ConvertStringToDecimal(reader.GetString(5)),
                    Quote_currency = reader.IsDBNull(6) ? "" : reader.GetString(6),
                    Fee = ConvertStringToDecimal(reader.GetString(7))
                });
            }
            reader.Close();
            connection.Close();

            dgTradesRaw.ItemsSource = data;
        }

        private void UnbindGrid()
        {
            dgTradesRaw.ItemsSource = null;
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

            ConsoleLog(_mainWindow.txtLog, $"[Raw Trades] Refreshing raw trades");

            // Load the db table with data from the Kraken trades table
            bool tradesRawRefreshWasBusy = false;

            await Task.Run(() =>
            {
                RefreshTradesRaw();
                tradesRawRefreshWasBusy = TradesRawRefreshBusy;
            });

            if (!tradesRawRefreshWasBusy)
            {
                BindGrid();

                ConsoleLog(_mainWindow.txtLog, $"[Raw Trades] Refresh done");
            }
            else
            {
                UnbindGrid();
                ConsoleLog(_mainWindow.txtLog, $"[Raw Trades] There is already a raw trades refresh in progress. Please Wait");
                ConsoleLog(_mainWindow.txtLog, $"[Raw Trades] Refresh unsuccessful");
            }

            btnRefresh.IsEnabled = true;
            this.Cursor = Cursors.Arrow;
        }

        private void TxtToDate_GotFocus(object sender, RoutedEventArgs e)
        {
            if (txtToDate.Text == "YYYY-MM-DD")
            {
                txtToDate.Text = string.Empty;
                //txtToDate.Foreground = Brushes.Black;
            }
        }

        private void TxtToDate_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtToDate.Text))
            {
                txtToDate.Text = "YYYY-MM-DD";
                //txtToDate.Foreground = Brushes.Gray;
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
                //txtFromDate.Foreground = Brushes.Black;
            }
        }

        private void TxtFromDate_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtFromDate.Text))
            {
                txtFromDate.Text = "YYYY-MM-DD";
                //txtFromDate.Foreground = Brushes.Gray;
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
            if (!dgTradesRaw.HasItems)
            {
                MessageBox.Show("Nothing to print", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
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
                printDlg.PrintDocument(idpSource.DocumentPaginator, "Project Crypto Gains - Raw Trades");
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
            TableCell? tableCell = new(new Paragraph(new Run("Project Crypto Gains - Raw Trades"))
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

            foreach (var item in dgTradesRaw.ItemsSource.OfType<TradesRawModel>())
            {
                tableRow = new TableRow();
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run(item.Date ?? ""))));
                table.RowGroups[0].Rows.Add(tableRow);

                tableRow = new TableRow
                {
                    FontWeight = FontWeights.Bold
                };
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run("TYPE"))));
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run("EXCHANGE"))));
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run("BASE_AMOUNT"))));
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run("BASE_CURRENCY"))));
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run("QUOTE_AMOUNT"))));
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run("QUOTE_CURRENCY"))));
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run("FEE"))));
                table.RowGroups[0].Rows.Add(tableRow);

                tableRow = new TableRow();
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run(item.Type ?? ""))));
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run(item.Exchange ?? ""))));
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run(item.Base_amount.ToString() ?? ""))));
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run(item.Base_currency ?? ""))));
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run(item.Quote_amount.ToString() ?? ""))));
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run(item.Quote_currency ?? ""))));
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run(item.Fee.ToString() ?? ""))));
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