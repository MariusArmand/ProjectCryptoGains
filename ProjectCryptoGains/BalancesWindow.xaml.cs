using LiveCharts;
using LiveCharts.Wpf;
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
using static ProjectCryptoGains.Common.Utils.Utils;
using static ProjectCryptoGains.Common.Utils.WindowUtils;
using static ProjectCryptoGains.Models;

namespace ProjectCryptoGains
{
    /// <summary>
    /// Interaction logic for BalancesWindow.xaml
    /// </summary>
    public partial class BalancesWindow : SubwindowBase
    {
        private readonly MainWindow _mainWindow;

        private string untilDate = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        private SqliteConnection? connection;

        public string Amount_fiat_header { get; set; } = "AMOUNT__FIAT";

        private string? lastWarning = null;

        public BalancesWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            TitleBarElement = TitleBar;

            _mainWindow = mainWindow;

            txtUntilDate.Text = untilDate;
            lblTotalAmountFiat.Visibility = Visibility.Collapsed;
            lblTotalAmountFiatData.Visibility = Visibility.Collapsed;

            BindGrid();
        }

        public SeriesCollection? SeriesCollection { get; set; }

        private void ButtonHelp_Click(object sender, RoutedEventArgs e)
        {
            OpenHelp("balances_help.html");
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

        private void ButtonRefresh_Click(object sender, RoutedEventArgs e)
        {
            lastWarning = null;
            Refresh();
        }

        private void DatePickerDate_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Refresh();
        }

        private void BindGrid()
        {
            string? fiatCurrency = SettingFiatCurrency;
            dgBalances.Columns[3].Header = "AMOUNT__" + fiatCurrency;

            // Create a collection of BalancesModel objects
            ObservableCollection<BalancesModel> data = [];

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
            command.CommandText = "SELECT CURRENCY, AMOUNT, AMOUNT_FIAT FROM TB_BALANCES_S";
            DbDataReader reader = command.ExecuteReader();

            int dbLineNumber = 0;

            List<string> currencies = [];
            List<decimal> amounts_fiat = [];

            string curr = "";

            decimal amnt = 0.00m;
            decimal amnt_fiat = 0.00m;
            decimal tot_amnt_fiat = 0.00m;

            try
            {
                while (reader.Read())
                {
                    dbLineNumber++;

                    curr = reader.GetStringOrEmpty(0);
                    amnt = reader.GetDecimalOrDefault(1);
                    amnt_fiat = reader.GetDecimalOrDefault(2, 0.00m);

                    data.Add(new BalancesModel
                    {
                        RowNumber = dbLineNumber,
                        Currency = curr,
                        Amount = amnt,
                        Amount_fiat = amnt_fiat
                    });

                    tot_amnt_fiat += amnt_fiat;

                    currencies.Add(curr); // Add currency to list
                    amounts_fiat.Add(amnt_fiat);
                }

                if (chkConvertToFiat.IsChecked == true)
                {
                    lblTotalAmountFiatData.Content = tot_amnt_fiat + " " + fiatCurrency;

                    RefreshPie([.. currencies], [.. amounts_fiat]); // Spreads collection items into new arrays for method argument
                    pcBalances.Visibility = Visibility.Visible;
                    lblTotalAmountFiat.Visibility = Visibility.Visible;
                    lblTotalAmountFiatData.Visibility = Visibility.Visible;
                }
                else
                {
                    pcBalances.Visibility = Visibility.Hidden;
                    lblTotalAmountFiat.Visibility = Visibility.Hidden;
                    lblTotalAmountFiatData.Visibility = Visibility.Hidden;
                }
            }
            catch (Exception ex)
            {
                MessageBoxResult result = CustomMessageBox.Show("Exception whilst fetching data." + Environment.NewLine + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                // Exit function early
                return;
            }
            reader.Close();
            connection.Close();

            dgBalances.ItemsSource = data;
        }

        private void UnbindGrid()
        {
            dgBalances.ItemsSource = null;
            pcBalances.Visibility = Visibility.Hidden;
            lblTotalAmountFiat.Visibility = Visibility.Hidden;
            lblTotalAmountFiatData.Visibility = Visibility.Hidden;
        }

        private void RefreshPie(string[] currencies, decimal[] amounts_fiat)
        {
            SeriesCollection ??= []; // Initialize if null

            SeriesCollection.Clear(); // Clear the existing series before adding new data

            decimal totalValue = amounts_fiat.Sum();

            for (int i = 0; i < currencies.Length; i++)
            {
                if (amounts_fiat[i] > 0)
                {
                    string title = $"{currencies[i]} [{(amounts_fiat[i] / totalValue) * 100:F2}%]";

                    SeriesCollection.Add(new PieSeries
                    {
                        Title = title,
                        Values = new ChartValues<decimal> { amounts_fiat[i] },
                        DataLabels = true,
                        LabelPoint = chartPoint => (chartPoint.Participation * 100).ToString("F2") + "%",
                        Stroke = null,
                        StrokeThickness = 0
                    });
                }
            }

            DataContext = this;
            pcBalances.DataTooltip = null;
        }

        private async void Refresh()
        {
            string? fiatCurrency = SettingFiatCurrency;

            if (!IsValidDateFormat(txtUntilDate.Text, "yyyy-MM-dd"))
            {
                MessageBoxResult result = CustomMessageBox.Show("Until date does not have a correct YYYY-MM-DD format", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                // Exit function early
                return;
            }

            BlockUI();

            ConsoleLog(_mainWindow.txtLog, $"[Balances] Refreshing balances");

            bool ledgersRefreshFailed = false;
            string? ledgersRefreshWarning = null;
            bool ledgersRefreshWasBusy = false;
            if (chkRefreshLedgers.IsChecked == true)
            {
                await Task.Run(() =>
                {
                    try
                    {
                        ledgersRefreshWarning = RefreshLedgers(_mainWindow, Caller.Balances);
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
                string? lastError = null;

                connection = new(connectionString);

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

                bool? convertToFiat = chkConvertToFiat.IsChecked;

                await Task.Run(async () =>
                {
                    try
                    {
                        DbCommand commandDelete = connection.CreateCommand();

                        // Truncate db table
                        commandDelete.CommandText = "DELETE FROM TB_BALANCES_S";
                        commandDelete.ExecuteNonQuery();

                        // Insert into db table
                        using DbCommand command = connection.CreateCommand();
                        command.CommandText = @"SELECT catalog.CODE, catalog.ASSET
                                                FROM
                                                (SELECT CODE, ASSET FROM TB_ASSET_CATALOG_S) catalog
                                                INNER JOIN
                                                (SELECT DISTINCT CURRENCY FROM TB_LEDGERS_S) ledgers
                                                ON catalog.ASSET = ledgers.CURRENCY
                                                ORDER BY CODE, ASSET";
                        using DbDataReader reader = command.ExecuteReader();

                        // For each asset, create balance insert

                        // Rate limiting mechanism //
                        DateTime lastCallTime = DateTime.MinValue;
                        /////////////////////////////
                        while (reader.Read())
                        {
                            string code = reader.GetStringOrEmpty(0);
                            string asset = reader.GetStringOrEmpty(1);

                            using DbCommand commandInsert = connection.CreateCommand();
                            if (code == fiatCurrency)
                            {
                                commandInsert.CommandText = CreateFiatBalancesInsert(asset, untilDate);
                            }
                            else
                            {
                                var (xInFiat, sqlCommand, conversionSource) = CreateCryptoBalancesInsert(asset, code, untilDate, convertToFiat, connection);
                                commandInsert.CommandText = sqlCommand;

                                if (ConvertStringToDecimal(xInFiat) == 0m)
                                {
                                    lastWarning = $"[Balances] Could not calculate balance for currency: {asset}" + Environment.NewLine + "Retrieved 0.00 exchange rate";
                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        ConsoleLog(_mainWindow.txtLog, lastWarning);
                                    });
                                }

                                // Rate limiting mechanism //
                                if (conversionSource == "API")
                                {
                                    if ((DateTime.Now - lastCallTime).TotalSeconds < 1)
                                    {
                                        // Calculate delay to ensure at least 1 seconds have passed
                                        int delay = Math.Max(0, (int)((lastCallTime.AddSeconds(1) - DateTime.Now).TotalMilliseconds));
                                        await Task.Delay(delay);
                                    }
                                    lastCallTime = DateTime.Now;
                                }
                                /////////////////////////////
                            }
                            commandInsert.ExecuteNonQuery();
                        }
                        if (lastWarning != null)
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                MessageBoxResult result = CustomMessageBox.Show("There were issues calculating some balances", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        while (ex.InnerException != null)
                        {
                            ex = ex.InnerException;
                        }
                        lastError = ex.Message;
                    }
                });

                UnbindGrid();
                BindGrid();

                connection.Close();

                if (lastError == null)
                {
                    if (ledgersRefreshWarning == null && lastWarning == null)
                    {
                        ConsoleLog(_mainWindow.txtLog, $"[Balances] Refresh done");
                    }
                    else
                    {
                        ConsoleLog(_mainWindow.txtLog, $"[Rewards] Refresh done with warnings");
                    }
                }
                else
                {
                    ConsoleLog(_mainWindow.txtLog, $"[Balances] " + lastError);
                    ConsoleLog(_mainWindow.txtLog, $"[Balances] Refresh unsuccessful");
                }
            }
            else
            {
                UnbindGrid();
                ConsoleLog(_mainWindow.txtLog, $"[Balances] Refresh unsuccessful");
            }

            UnblockUI();
        }

        private void TxtUntilDate_GotFocus(object sender, RoutedEventArgs e)
        {
            if (txtUntilDate.Text == "YYYY-MM-DD")
            {
                txtUntilDate.Text = string.Empty;
            }
        }

        private void TxtUntilDate_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtUntilDate.Text))
            {
                txtUntilDate.Text = "YYYY-MM-DD";
                txtUntilDate.Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102)); // Gray #666666
            }
        }

        private void TextBoxUntilDate_KeyUp(object sender, KeyboardEventArgs e)
        {
            SetUntilDate();
            txtUntilDate.Foreground = Brushes.White;
        }

        private void SetUntilDate()
        {
            untilDate = txtUntilDate.Text;
        }

        private async void ButtonPrint_Click(object sender, RoutedEventArgs e)
        {
            if (!dgBalances.HasItems)
            {
                MessageBoxResult result = CustomMessageBox.Show("Nothing to print", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            ConsoleLog(_mainWindow.txtLog, $"[Balances] Printing Balances");

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
                printDlg.PrintDocument(idpSource.DocumentPaginator, "Project Crypto Gains - Balances");
            });

            ConsoleLog(_mainWindow.txtLog, $"[Balances] Printing done");

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
            TableCell? tableCell = new(new Paragraph(new Run("Project Crypto Gains - Balances"))
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
            tableCell = new TableCell(new Paragraph(new Run("Until\t" + untilDate)))
            {
                ColumnSpan = 7,
                TextAlignment = TextAlignment.Left
            };
            tableRow.Cells.Add(tableCell);
            table.RowGroups[0].Rows.Add(tableRow);

            tableRow = new TableRow();
            tableRow.Cells.Add(new TableCell(new Paragraph(new Run("\n"))));
            table.RowGroups[0].Rows.Add(tableRow);

            var firstColumnWidth = 120; // Set the desired width for the first column

            TableColumn column = new()
            {
                Width = new GridLength(firstColumnWidth, GridUnitType.Pixel)
            };
            table.Columns.Add(column);

            tableRow = new TableRow
            {
                FontWeight = FontWeights.Bold
            };
            tableRow.Cells.Add(new TableCell(new Paragraph(new Run("CURRENCY"))));
            tableRow.Cells.Add(new TableCell(new Paragraph(new Run("AMOUNT"))) { TextAlignment = TextAlignment.Right });
            tableRow.Cells.Add(new TableCell(new Paragraph(new Run("AMOUNT " + fiatCurrency))) { TextAlignment = TextAlignment.Right });
            table.RowGroups[0].Rows.Add(tableRow);

            foreach (var item in dgBalances.ItemsSource.OfType<BalancesModel>())
            {
                tableRow = new TableRow();
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run(item.Currency ?? ""))));
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run($"{item.Amount,10:F10}" ?? ""))) { TextAlignment = TextAlignment.Right });
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run($"{item.Amount_fiat,2:F2}" ?? ""))) { TextAlignment = TextAlignment.Right });
                table.RowGroups[0].Rows.Add(tableRow);
            }

            flowDoc.Blocks.Add(table);
            return flowDoc;
        }

        private static (string xInFiat, string sqlCommand, string conversionSource) CreateCryptoBalancesInsert(string currency, string currency_code, string untilDate, bool? convertToFiat, SqliteConnection connection)
        {
            try
            {
                string xInFiat = "0.00";
                string conversionSource = "";

                if (convertToFiat == true)
                {
                    var (fiatAmount, source) = ConvertXToFiat(currency_code, 1m, DateTime.ParseExact(untilDate, "yyyy-MM-dd", CultureInfo.InvariantCulture), connection);
                    xInFiat = fiatAmount;
                    conversionSource = source;
                }

                string sqlCommand = $@"INSERT INTO TB_BALANCES_S
						                  SELECT CURRENCY, AMOUNT, AMOUNT_FIAT 
						                  FROM
						                  (
						                  SELECT 
							                  '{currency}' AS CURRENCY, 
							                  printf(' %.10f', SUM(AMNT)) AS AMOUNT,
							                  ROUND({xInFiat} * SUM(AMNT), 2) AS AMOUNT_FIAT 
						                  FROM (
							                    SELECT SUM(AMOUNT) AS AMNT 
							                    FROM TB_LEDGERS_S
							                    WHERE CURRENCY = '{currency}'
				                                AND TYPE NOT IN ('WITHDRAWAL', 'DEPOSIT')
							                    AND strftime('%s', DATE) < strftime('%s', date('{untilDate}', '+1 day'))
							                    UNION ALL
							                    SELECT -SUM(FEE) AS AMNT 
							                    FROM TB_LEDGERS_S
							                    WHERE CURRENCY = '{currency}'
							                    AND strftime('%s', DATE) < strftime('%s', date('{untilDate}', '+1 day'))
							                   )
						                  )
						                  WHERE CAST(AMOUNT AS REAL) > 0";

                return (xInFiat, sqlCommand, conversionSource);
            }
            catch (Exception ex)
            {
                throw new Exception("CreateCryptoBalancesInsert failed", ex);
            }
        }

        private static string CreateFiatBalancesInsert(string fiat_code, string untilDate)
        {
            string query = $@"INSERT INTO TB_BALANCES_S
							  SELECT 
					              '{fiat_code}' AS CURRENCY, 
								  printf('%.10f', SUM(AMNT)) AS AMOUNT,
								  ROUND(SUM(AMNT), 2) AS AMOUNT_FIAT 
							  FROM (
								    SELECT SUM(AMOUNT) AS AMNT 
								    FROM TB_LEDGERS_S
								    WHERE CURRENCY = '{fiat_code}'
					                AND strftime('%s', DATE) <= strftime('%s', date('{untilDate}', '+1 day'))
								    UNION ALL
								    SELECT -SUM(FEE) AS AMNT 
								    FROM TB_LEDGERS_S
								    WHERE CURRENCY = '{fiat_code}'
									AND strftime('%s', DATE) <= strftime('%s', date('{untilDate}', '+1 day'))
								   )";
            return query;
        }
    }
}