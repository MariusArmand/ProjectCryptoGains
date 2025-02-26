using Microsoft.Data.Sqlite;
using ProjectCryptoGains.Common;
using System;
using System.Collections.Generic;
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
    /// Interaction logic for GainsWindow.xaml
    /// </summary>
    public partial class GainsWindow : SubwindowBase
    {
        private readonly MainWindow _mainWindow;

        private int errors = 0;
        private string fromDate = "2009-01-03";
        private string toDate = GetTodayAsIsoDate();
        private string baseCurrency = "";

        public GainsWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            TitleBarElement = TitleBar;

            _mainWindow = mainWindow;

            txtFromDate.Text = fromDate;
            txtToDate.Text = toDate;
            txtBaseCurrency.Text = baseCurrency;

            BindGrid();
        }

        private void ButtonHelp_Click(object sender, RoutedEventArgs e)
        {
            OpenHelp("gains_help.html");
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
            dgGains.Columns[8].Header = "BASE__UNIT__PRICE__" + fiatCurrency;

            ObservableCollection<GainsModel> dataGains = [];
            ObservableCollection<GainsSummaryModel> dataGainsSummary = [];

            using SqliteConnection connection = new(connectionString);
            try
            {
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
                                         trades.REFID,
                                         trades.DATE,
                                         trades.TYPE,
                                         trades.BASE_CURRENCY,
                                         trades.BASE_AMOUNT,
                                         trades.QUOTE_CURRENCY,
                                         trades.QUOTE_AMOUNT,
                                         trades.BASE_UNIT_PRICE_FIAT,
                                         trades.COSTS_PROCEEDS,
                                         CASE 
                                             WHEN trades.TYPE = 'SELL' THEN NULL
                                             ELSE gains.TX_BALANCE_REMAINING
                                         END AS TX_BALANCE_REMAINING,
                                         CASE 
                                             WHEN trades.TYPE = 'BUY' THEN NULL
                                             ELSE gains.GAIN
                                         END AS GAIN
                                     FROM TB_GAINS_S gains
                                         INNER JOIN TB_TRADES_S trades
                                             ON gains.REFID = trades.REFID
                                     WHERE trades.BASE_CURRENCY LIKE '%{baseCurrency}%'
                                     ORDER BY DATE ASC";

            DbDataReader reader = command.ExecuteReader();

            int dbLineNumber = 0;
            while (reader.Read())
            {
                dbLineNumber++;

                dataGains.Add(new GainsModel
                {
                    RowNumber = dbLineNumber,
                    Refid = reader.GetStringOrEmpty(0),
                    Date = reader.GetStringOrEmpty(1),
                    Type = reader.GetStringOrEmpty(2),
                    Base_currency = reader.GetStringOrEmpty(3),
                    Base_amount = reader.GetDecimalOrDefault(4),
                    Quote_currency = reader.GetStringOrEmpty(5),
                    Quote_amount = reader.GetDecimalOrDefault(6),
                    Base_unit_price_fiat = reader.GetDecimalOrNull(7),
                    Costs_proceeds = reader.GetDecimalOrNull(8),
                    Tx_balance_remaining = reader.GetDecimalOrNull(9),
                    Gain = reader.GetDecimalOrNull(10)
                });
            }
            reader.Close();

            dgGains.ItemsSource = dataGains;

            /////////////////////////////////

            command.CommandText = $@"SELECT 
                                         trades.BASE_CURRENCY AS CURRENCY,
                                         printf('%.10f', SUM(CAST(gains.GAIN AS REAL))) AS GAIN
                                     FROM TB_GAINS_S gains
                                         INNER JOIN TB_TRADES_S trades
                                             ON gains.REFID = trades.REFID
                                     WHERE gains.GAIN != ''
                                         AND trades.BASE_CURRENCY LIKE '%{baseCurrency}%'
                                         AND strftime('%s', trades.DATE) BETWEEN strftime('%s', '{fromDate}') AND strftime('%s', date('{toDate}', '+1 day'))
                                     GROUP BY trades.BASE_CURRENCY
                                     ORDER BY trades.BASE_CURRENCY";

            reader = command.ExecuteReader();

            dbLineNumber = 0;

            decimal tot_gain = 0.00m;
            decimal gain = 0.00m;
            while (reader.Read())
            {
                dbLineNumber++;

                gain = reader.GetDecimalOrDefault(1);
                dataGainsSummary.Add(new GainsSummaryModel
                {
                    RowNumber = dbLineNumber,
                    Currency = reader.GetStringOrEmpty(0),
                    Gain = reader.GetDecimalOrDefault(1)
                });
                tot_gain += gain;
            }
            reader.Close();
            connection.Close();

            lblTotalGainsData.Content = tot_gain.ToString("F2") + " " + fiatCurrency;
            dgGainsSummary.ItemsSource = dataGainsSummary;
        }

        private void UnbindGrid()
        {
            string? fiatCurrency = SettingFiatCurrency;
            lblTotalGainsData.Content = "0.00 " + fiatCurrency;
            dgGains.ItemsSource = null;
            dgGainsSummary.ItemsSource = null;
        }

        private async void Refresh()
        {
            errors = 0;

            if (!IsValidDateFormat(txtFromDate.Text, "yyyy-MM-dd"))
            {
                MessageBoxResult result = CustomMessageBox.Show("From date does not have a correct YYYY-MM-DD format", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!IsValidDateFormat(txtToDate.Text, "yyyy-MM-dd"))
            {
                MessageBoxResult result = CustomMessageBox.Show("To date does not have a correct YYYY-MM-DD format", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            BlockUI();

            ConsoleLog(_mainWindow.txtLog, $"[Gains] Refreshing gains");

            bool ledgersRefreshFailed = false;
            string? ledgersRefreshWarning = null;
            bool ledgersRefreshWasBusy = false;
            if (chkRefreshLedgers.IsChecked == true)
            {
                await Task.Run(() =>
                {
                    try
                    {
                        ledgersRefreshWarning = RefreshLedgers(_mainWindow, Caller.Gains);
                    }
                    catch (Exception)
                    {
                        ledgersRefreshFailed = true;
                    }
                    ledgersRefreshWasBusy = LedgersRefreshBusy;
                });
            }

            string? tradesRefreshError = null;
            string? tradesRefreshWarning = null;
            bool tradesRefreshWasBusy = false;
            if (chkRefreshTrades.IsChecked == true && !ledgersRefreshWasBusy && !ledgersRefreshFailed)
            {
                await Task.Run(async () =>
                {
                    try
                    {
                        tradesRefreshWarning = await RefreshTrades(_mainWindow, Caller.Gains);
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
            }

            // LIFO processing
            if (!ledgersRefreshWasBusy && !tradesRefreshWasBusy && tradesRefreshError == null && !ledgersRefreshFailed)
            {
                await Task.Run(() =>
                {
                    using SqliteConnection connection = new(connectionString);
                    try
                    {
                        connection.Open();
                        // Clear the table before inserting new data
                        using DbCommand commandClear = connection.CreateCommand();
                        commandClear.CommandText = "DELETE FROM TB_GAINS_S";
                        commandClear.ExecuteNonQuery();
                        connection.Close();

                        // Read the assets into a list
                        connection.Open();
                        using DbCommand command = connection.CreateCommand();
                        command.CommandText = $@"SELECT ASSET FROM TB_ASSET_CATALOG_S WHERE ASSET like '%{baseCurrency}%'";

                        List<string> assets = [];

                        using (DbDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string asset = reader.GetStringOrEmpty(0);
                                assets.Add(asset);
                            }
                        }
                        connection.Close();

                        // For each asset do LIFO processing
                        foreach (string asset in assets)
                        {
                            List<TransactionsModel> sellTransactions = ReadTransactionsFromDB(asset, "SELL", "ASC");
                            List<TransactionsModel> buyTransactions = ReadTransactionsFromDB(asset, "BUY");
                            CalculateLIFOGains(asset, sellTransactions, buyTransactions);
                            WriteTransactionsToDB(sellTransactions, buyTransactions, connectionString);
                        }
                    }
                    catch (Exception ex)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            MessageBoxResult result = CustomMessageBox.Show("Database could not be opened." + Environment.NewLine + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        });
                        return;
                    }
                });

                if (errors == 0)
                {
                    BindGrid();
                    if (ledgersRefreshWarning == null && tradesRefreshWarning == null)
                    {
                        ConsoleLog(_mainWindow.txtLog, $"[Gains] Refresh done");
                    }
                    else
                    {
                        ConsoleLog(_mainWindow.txtLog, $"[Gains] Refresh done with warnings");
                    }
                }
                else
                {
                    UnbindGrid();
                    ConsoleLog(_mainWindow.txtLog, $"[Gains] Refresh unsuccessful");
                }
            }
            else
            {
                UnbindGrid();
                if (tradesRefreshError != null)
                {
                    ConsoleLog(_mainWindow.txtLog, $"[Gains] " + tradesRefreshError);
                    ConsoleLog(_mainWindow.txtLog, $"[Gains] Refreshing trades unsuccessful");
                }
                ConsoleLog(_mainWindow.txtLog, $"[Gains] Refresh unsuccessful");
            }

            UnblockUI();
        }

        private static List<TransactionsModel> ReadTransactionsFromDB(String asset, String tx_type, string orderBy = "DESC")
        {
            List<TransactionsModel> transactions = [];

            using (SqliteConnection connection = new(connectionString))
            {
                try
                {
                    connection.Open();
                    using DbCommand command = connection.CreateCommand();
                    command.CommandText = $@"SELECT 
                                                 REFID,
                                                 DATE,
                                                 BASE_AMOUNT AS AMOUNT,
                                                 BASE_UNIT_PRICE_FIAT AS UNIT_PRICE,
                                                 COSTS_PROCEEDS,
                                                 BASE_AMOUNT AS TX_BALANCE_REMAINING
                                             FROM TB_TRADES_S
                                             WHERE BASE_CURRENCY = '{asset}'
                                                 AND TYPE = '{tx_type}'
                                             ORDER BY DATE {orderBy}";

                    using DbDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        transactions.Add(new TransactionsModel
                        {
                            RefId = reader.GetStringOrEmpty(0),
                            Date = reader.GetStringOrNull(1),
                            Amount = reader.GetDecimalOrDefault(2),
                            Unit_price = reader.GetDecimalOrNull(3),
                            Costs_Proceeds = reader.GetDecimalOrNull(4),
                            Tx_Balance_Remaining = reader.GetDecimalOrNull(5)
                        });
                    }
                    reader.Close();
                    connection.Close();
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBoxResult result = CustomMessageBox.Show("Database could not be opened." + Environment.NewLine + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                    return [];
                }
            }

            return transactions;
        }

        private void CalculateLIFOGains(String asset, List<TransactionsModel> sellTransactions, List<TransactionsModel> buyTransactions)
        {
            foreach (var stx in sellTransactions)
            {
                // Initialize the amount we need to sell for this sell transaction
                decimal? amountToSell = stx.Amount;

                decimal? proceeds = stx.Costs_Proceeds;
                decimal? costs = 0;

                // Parse the sell transaction date if it's not null
                DateTime? sellDate = stx.Date != null ? DateTime.ParseExact(stx.Date, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) : null;

                // Filter buy transactions to only those on or before the sell date
                var relevantBuyTransactions = buyTransactions.Where(btx =>
                {
                    if (btx.Date == null || sellDate == null) return true; // Handle cases where date might be missing
                    DateTime buyDate = ConvertStringToIsoDateTime(btx.Date);
                    return buyDate <= sellDate.Value;
                }).ToList();

                // Calculate the sum of amounts in relevant buy transactions
                decimal? totalRelevantAmountBought = relevantBuyTransactions.Sum(btx => btx.Amount);

                if (totalRelevantAmountBought < amountToSell)
                {
                    // Instead of showing directly, schedule MessageBox on the UI thread to not block this thread
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        errors += 1;
                        string lastError = "Not enough buy transactions to cover this sell transaction" +
                                           Environment.NewLine + $"RefId: {stx.RefId}" +
                                           Environment.NewLine + $"Base currency: {asset}" +
                                           Environment.NewLine + $"Amount missing: {amountToSell - totalRelevantAmountBought}";
                        ConsoleLog(_mainWindow.txtLog, $"[Gains] {lastError}");
                        MessageBoxResult result = CustomMessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                    break;
                }

                // Continue selling until we've processed the entire amount to sell
                while (amountToSell > 0 && relevantBuyTransactions.Count != 0)
                {
                    // Iterate through each buy transaction in order (since they're ordered by date descending)
                    foreach (var btx in relevantBuyTransactions)
                    {
                        if (btx.Tx_Balance_Remaining > 0)
                        {
                            decimal? soldAmount = Math.Min((decimal)btx.Tx_Balance_Remaining, (decimal)amountToSell);

                            // Add the cost of the sold amount
                            costs += btx.Unit_price * soldAmount;

                            btx.Tx_Balance_Remaining -= soldAmount;
                            amountToSell -= soldAmount;

                            if (amountToSell == 0) break;
                        }
                    }
                }
                // After processing all relevant buy transactions, set the gain for this sell transaction
                stx.Gain = proceeds - costs;
            }
        }

        private static void WriteTransactionsToDB(List<TransactionsModel> sellTransactions, List<TransactionsModel> buyTransactions, string connectionString)
        {
            using SqliteConnection connection = new(connectionString);
            try
            {
                connection.Open();
                using var commandInsert = connection.CreateCommand();
                commandInsert.CommandText = @"INSERT INTO TB_GAINS_S (
                                                  REFID,
                                                  TX_BALANCE_REMAINING,
                                                  GAIN
                                              ) VALUES (
                                                  @REFID,
                                                  printf('%.10f', @TX_BALANCE_REMAINING),
                                                  CASE 
                                                      WHEN @GAIN != '' 
                                                      THEN printf('%.10f', @GAIN) 
                                                      ELSE '' 
                                                  END
                                              )";

                foreach (var tx in sellTransactions)
                {
                    if (tx.RefId != null) // Ensure REFID isn't null before adding
                    {
                        // Clear parameters from previous iteration
                        commandInsert.Parameters.Clear();

                        commandInsert.Parameters.AddWithValue("@REFID", tx.RefId);
                        commandInsert.Parameters.AddWithValue("@TX_BALANCE_REMAINING", tx.Tx_Balance_Remaining.ToString());
                        commandInsert.Parameters.AddWithValue("@GAIN", tx.Gain.ToString());

                        commandInsert.ExecuteNonQuery();
                    }
                }

                foreach (var tx in buyTransactions)
                {
                    if (tx.RefId != null) // Ensure REFID isn't null before adding
                    {
                        // Clear parameters from previous iteration
                        commandInsert.Parameters.Clear();

                        commandInsert.Parameters.AddWithValue("@REFID", tx.RefId);
                        commandInsert.Parameters.AddWithValue("@TX_BALANCE_REMAINING", tx.Tx_Balance_Remaining.ToString());
                        commandInsert.Parameters.AddWithValue("@GAIN", tx.Gain.ToString());

                        commandInsert.ExecuteNonQuery();
                    }
                }

                connection.Close();
            }
            catch (Exception ex)
            {
                MessageBoxResult result = CustomMessageBox.Show("Error writing to database." + Environment.NewLine + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private void TextBoxBaseCurrency_KeyUp(object sender, KeyboardEventArgs e)
        {
            SetBaseCurrency();
        }

        private void SetBaseCurrency()
        {
            baseCurrency = txtBaseCurrency.Text;
        }

        private void ButtonRefresh_Click(object sender, RoutedEventArgs e)
        {
            Refresh();
        }

        private async void ButtonPrint_Click(object sender, RoutedEventArgs e)
        {
            if (!dgGains.HasItems)
            {
                MessageBoxResult result = CustomMessageBox.Show("Nothing to print", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            ConsoleLog(_mainWindow.txtLog, $"[Gains] Printing Gains");

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
                printDlg.PrintDocument(idpSource.DocumentPaginator, "Project Crypto Gains - Gains");
            });

            ConsoleLog(_mainWindow.txtLog, $"[Gains] Printing done");

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
            TableCell? tableCell = new(new Paragraph(new Run("Project Crypto Gains - Gains"))
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

            foreach (var item in dgGains.ItemsSource.OfType<GainsModel>())
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
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run("BASE_CURRENCY"))));
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run("BASE_AMOUNT"))));
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run("QUOTE_CURRENCY"))));
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run("QUOTE_AMOUNT"))));
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run("BASE_UNIT_PRICE_" + fiatCurrency))));
                table.RowGroups[0].Rows.Add(tableRow);

                tableRow = new TableRow();
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run(item.Refid ?? ""))));
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run(item.Type ?? ""))));
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run(item.Base_currency ?? ""))));
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run($"{item.Base_amount,10:F10}" ?? ""))));
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run(item.Quote_currency ?? ""))));
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run($"{item.Quote_amount,10:F10}" ?? ""))));
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run($"{item.Base_unit_price_fiat,2:F2}" ?? ""))));
                table.RowGroups[0].Rows.Add(tableRow);

                tableRow = new TableRow
                {
                    FontWeight = FontWeights.Bold
                };
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run("COSTS_PROCEEDS"))));
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run("TX_BALANCE_REMAINING"))));
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run("GAIN"))));
                table.RowGroups[0].Rows.Add(tableRow);

                tableRow = new TableRow();
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run($"{item.Costs_proceeds,2:F2}" ?? ""))));
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run(item.Tx_balance_remaining.HasValue ? $"{item.Tx_balance_remaining,10:F10}" : "N/A"))));
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run(item.Gain.HasValue ? $"{item.Gain,2:F2}" : "N/A"))));
                table.RowGroups[0].Rows.Add(tableRow);

                tableRow = new TableRow();
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run("\n"))));
                table.RowGroups[0].Rows.Add(tableRow);
            }
            flowDoc.Blocks.Add(table);
            return flowDoc;
        }

        private async void ButtonPrintSummary_Click(object sender, RoutedEventArgs e)
        {
            if (!dgGainsSummary.HasItems)
            {
                MessageBoxResult result = CustomMessageBox.Show("Nothing to print", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            ConsoleLog(_mainWindow.txtLog, $"[Gains] Printing Gains Summary");

            BlockUI();

            // Create a PrintDialog
            PrintDialog printDlg = new();

            string totalAmountFiat = (String)(lblTotalGainsData.Content ?? "");

            await Task.Run(() =>
            {
                // Create a FlowDocument dynamically.
                FlowDocument doc = CreateFlowDocumentSummary(totalAmountFiat);
                doc.Name = "FlowDoc";
                // Create IDocumentPaginatorSource from FlowDocument
                IDocumentPaginatorSource idpSource = doc;
                // Call PrintDocument method to send document to printer
                printDlg.PrintDocument(idpSource.DocumentPaginator, "Project Crypto Gains - Gains Summary");
            });

            ConsoleLog(_mainWindow.txtLog, $"[Gains] Printing done");

            UnblockUI();
        }

        private FlowDocument CreateFlowDocumentSummary(string totalGains)
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
            TableCell? tableCell = new(new Paragraph(new Run("Project Crypto Gains - Gains Summary"))
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

            tableRow = new TableRow();
            tableCell = new TableCell(new Paragraph(new Run("From\t" + fromDate)))
            {
                ColumnSpan = 7,
                TextAlignment = TextAlignment.Left
            };
            tableRow.Cells.Add(tableCell);
            table.RowGroups[0].Rows.Add(tableRow);

            tableRow = new TableRow();
            tableCell = new TableCell(new Paragraph(new Run("To\t" + toDate)))
            {
                ColumnSpan = 7,
                TextAlignment = TextAlignment.Left
            };
            tableRow.Cells.Add(tableCell);
            table.RowGroups[0].Rows.Add(tableRow);

            tableRow = new TableRow();
            tableRow.Cells.Add(new TableCell(new Paragraph(new Run("\n")))
            {
                ColumnSpan = 7,
                TextAlignment = TextAlignment.Left
            });
            table.RowGroups[0].Rows.Add(tableRow);

            foreach (var item in dgGainsSummary.ItemsSource.OfType<GainsSummaryModel>())
            {
                tableRow = new TableRow
                {
                    FontWeight = FontWeights.Bold
                };
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run("CURRENCY"))));
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run("GAIN"))));
                table.RowGroups[0].Rows.Add(tableRow);

                tableRow = new TableRow();
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run(item.Currency ?? ""))));
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run($"{item.Gain,2:F2}" ?? ""))));
                table.RowGroups[0].Rows.Add(tableRow);

                tableRow = new TableRow();
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run("\n"))));
                table.RowGroups[0].Rows.Add(tableRow);
            }

            tableRow = new TableRow();
            tableCell = new TableCell(new Paragraph(new Run("Total gains " + totalGains)))
            {
                ColumnSpan = 7,
                TextAlignment = TextAlignment.Left
            };
            tableRow.Cells.Add(tableCell);
            table.RowGroups[0].Rows.Add(tableRow);

            flowDoc.Blocks.Add(table);
            return flowDoc;
        }
    }
}