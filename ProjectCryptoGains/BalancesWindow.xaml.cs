using FirebirdSql.Data.FirebirdClient;
using LiveCharts;
using LiveCharts.Wpf;
using ProjectCryptoGains.Common;
using ProjectCryptoGains.Common.Utils;
using System;
using System.Collections.Generic;
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

        private string untilDate = GetTodayAsIsoDate();

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

        private void ToggleDataUIVisibility(Visibility visibility)
        {
            pcBalances.Visibility = visibility;
            lblTotalAmountFiat.Visibility = visibility;
            lblTotalAmountFiatData.Visibility = visibility;
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
            string fiatCurrency = SettingFiatCurrency;
            dgBalances.Columns[3].Header = "AMOUNT__" + fiatCurrency;

            // Create a collection of BalancesModel objects
            ObservableCollection<BalancesModel> BalancesData = [];

            using (FbConnection connection = new(connectionString))
            {
                try
                {
                    connection.Open();
                }
                catch (Exception ex)
                {
                    MessageBoxResult result = CustomMessageBox.Show("Database could not be opened." + Environment.NewLine + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                    // Exit function early
                    return;
                }

                using DbCommand selectCommand = connection.CreateCommand();
                selectCommand.CommandText = "SELECT CURRENCY, AMOUNT, AMOUNT_FIAT FROM TB_BALANCES_S";

                using (DbDataReader reader = selectCommand.ExecuteReader())
                {
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

                            BalancesData.Add(new BalancesModel
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
                            ToggleDataUIVisibility(Visibility.Visible);
                        }
                        else
                        {
                            ToggleDataUIVisibility(Visibility.Hidden);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBoxResult result = CustomMessageBox.Show("Exception whilst fetching BalancesData." + Environment.NewLine + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                        // Exit function early
                        return;
                    }
                }
            }

            dgBalances.ItemsSource = BalancesData;
        }

        private void UnbindGrid()
        {
            dgBalances.ItemsSource = null;
            ToggleDataUIVisibility(Visibility.Hidden);
        }

        private void RefreshPie(string[] currencies, decimal[] amounts_fiat)
        {
            SeriesCollection ??= []; // Initialize if null

            SeriesCollection.Clear(); // Clear the existing series before adding new BalancesData

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
            string fiatCurrency = SettingFiatCurrency;

            if (!IsValidDateFormat(txtUntilDate.Text, "yyyy-MM-dd"))
            {
                MessageBoxResult result = CustomMessageBox.Show("Until date does not have a correct YYYY-MM-DD format.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                // Exit function early
                return;
            }

            BlockUI();

            try
            {
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

                    using (FbConnection connection = new(connectionString))
                    {
                        try
                        {
                            connection.Open();
                        }
                        catch (Exception ex)
                        {
                            MessageBoxResult result = CustomMessageBox.Show("Database could not be opened." + Environment.NewLine + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                            // Exit function early
                            return;
                        }

                        bool? convertToFiat = chkConvertToFiat.IsChecked;

                        await Task.Run(async () =>
                        {
                            try
                            {
                                using DbCommand deleteCommand = connection.CreateCommand();

                                // Truncate db table
                                deleteCommand.CommandText = "DELETE FROM TB_BALANCES_S";
                                deleteCommand.ExecuteNonQuery();

                                // Insert into db table
                                using DbCommand selectCommand = connection.CreateCommand();
                                selectCommand.CommandText = @"SELECT 
                                                                  catalog.CODE, 
                                                                  catalog.ASSET
                                                              FROM
                                                                  (SELECT CODE, ASSET 
                                                                   FROM TB_ASSET_CATALOG_S) catalog
                                                                  INNER JOIN
                                                                  (SELECT DISTINCT CURRENCY 
                                                                   FROM TB_LEDGERS_S) ledgers
                                                                  ON catalog.ASSET = ledgers.CURRENCY
                                                              ORDER BY 
                                                                  CODE, 
                                                                  ASSET";

                                using (DbDataReader reader = selectCommand.ExecuteReader())
                                {
                                    // For each asset, create balance insert

                                    // Rate limiting mechanism //
                                    DateTime lastCallTime = DateTime.Now;
                                    /////////////////////////////
                                    while (reader.Read())
                                    {
                                        string code = reader.GetStringOrEmpty(0);
                                        string asset = reader.GetStringOrEmpty(1);

                                        using DbCommand insertCommand = connection.CreateCommand();
                                        if (code == fiatCurrency)
                                        {
                                            insertCommand.CommandText = CreateFiatBalancesInsert(asset, untilDate);
                                        }
                                        else
                                        {
                                            var (xInFiat, sqlCommand, conversionSource) = CreateCryptoBalancesInsert(asset, code, untilDate, convertToFiat, connection);
                                            insertCommand.CommandText = sqlCommand;

                                            if (xInFiat == 0m)
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
                                        insertCommand.ExecuteNonQuery();
                                    }
                                    if (lastWarning != null)
                                    {
                                        Application.Current.Dispatcher.Invoke(() =>
                                        {
                                            MessageBoxResult result = CustomMessageBox.Show("There were issues calculating some balances.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                                        });
                                    }
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
                    }

                    UnbindGrid();
                    BindGrid();

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
            }
            finally
            {
                UnblockUI();
            }
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
                MessageBoxResult result = CustomMessageBox.Show("Nothing to print.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            ConsoleLog(_mainWindow.txtLog, $"[Balances] Printing balances");

            BlockUI();

            try
            {
                await PrintBalancesAsync();
                ConsoleLog(_mainWindow.txtLog, $"[Balances] Printing done");
            }
            catch (Exception ex)
            {
                ConsoleLog(_mainWindow.txtLog, $"[Balances] Printing failed: {ex.Message}");
                MessageBoxResult result = CustomMessageBox.Show($"Printing failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                UnblockUI();
            }
        }

        private async Task PrintBalancesAsync()
        {
            string fiatCurrency = SettingFiatCurrency;
            var balances = dgBalances.ItemsSource.OfType<BalancesModel>();

            PrintDialog printDlg = new();

            await PrintUtils.PrintFlowDocumentAsync(
                title: "Balances",
                subtitle: "Until\t" + untilDate,
                columnHeaders: new[] { "CURRENCY", "AMOUNT", $"AMOUNT_{fiatCurrency}" },
                dataItems: balances,
                dataExtractor: item => new[]
                {
                    (item.Currency ?? "", TextAlignment.Left, 1),
                    ($"{item.Amount,10:F10}", TextAlignment.Right, 1),
                    ($"{item.Amount_fiat,2:F2}", TextAlignment.Right, 1)
                },
                printDlg: printDlg,
                maxColumnsPerRow: 6,
                repeatHeadersPerItem: false
            );
        }

        private static (decimal xInFiat, string sqlCommand, string conversionSource) CreateCryptoBalancesInsert(string currency, string currency_code, string untilDate, bool? convertToFiat, FbConnection connection)
        {
            try
            {
                decimal xInFiat = 0.00m;
                string conversionSource = "";

                if (convertToFiat == true)
                {
                    var (fiatAmount, source) = ConvertXToFiat(currency_code, ConvertStringToIsoDate(untilDate), connection);
                    xInFiat = fiatAmount;
                    conversionSource = source;
                }

                string sqlCommand = $@"INSERT INTO TB_BALANCES_S (CURRENCY, AMOUNT, AMOUNT_FIAT)
                                           SELECT 
                                               CURRENCY, 
                                               AMOUNT, 
                                               AMOUNT_FIAT 
                                           FROM
                                               (
                                                   SELECT 
                                                       '{currency}' AS CURRENCY,
                                                       ROUND(SUM(AMNT), 10) AS AMOUNT,
                                                       ROUND({xInFiat} * SUM(AMNT), 2) AS AMOUNT_FIAT
                                                   FROM (
                                                       SELECT 
                                                           SUM(AMOUNT) AS AMNT
                                                       FROM TB_LEDGERS_S
                                                       WHERE CURRENCY = '{currency}'
                                                           AND TYPE NOT IN ('WITHDRAWAL', 'DEPOSIT')
                                                           AND ""DATE"" < DATEADD(DAY, 1, CAST('{untilDate}' AS TIMESTAMP))
                                                       UNION ALL
                                                       SELECT 
                                                           -SUM(FEE) AS AMNT
                                                       FROM TB_LEDGERS_S
                                                       WHERE CURRENCY = '{currency}'
                                                           AND ""DATE"" < DATEADD(DAY, 1, CAST('{untilDate}' AS TIMESTAMP))
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
            string query = $@"INSERT INTO TB_BALANCES_S (CURRENCY, AMOUNT, AMOUNT_FIAT)
                                  SELECT 
                                      '{fiat_code}' AS CURRENCY,
                                      ROUND(SUM(AMNT), 10) AS AMOUNT,
                                      ROUND(SUM(AMNT), 2) AS AMOUNT_FIAT
                                  FROM (
                                      SELECT 
                                          SUM(AMOUNT) AS AMNT
                                      FROM TB_LEDGERS_S
                                      WHERE CURRENCY = '{fiat_code}'
                                          AND ""DATE"" < DATEADD(DAY, 1, CAST('{untilDate}' AS TIMESTAMP))
                                      UNION ALL
                                      SELECT 
                                          -SUM(FEE) AS AMNT
                                      FROM TB_LEDGERS_S
                                      WHERE CURRENCY = '{fiat_code}'
                                          AND ""DATE"" < DATEADD(DAY, 1, CAST('{untilDate}' AS TIMESTAMP))
                                  )";
            return query;
        }
    }
}