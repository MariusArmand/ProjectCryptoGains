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
using static ProjectCryptoGains.Common.Utils.ParametersWindowsUtils;
using static ProjectCryptoGains.Common.Utils.ReaderUtils;
using static ProjectCryptoGains.Common.Utils.SettingsUtils;
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

        private string _untilDate = "";

        private string? _lastWarning = null;

        public BalancesWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            TitleBarElement = TitleBar;

            _mainWindow = mainWindow;

            lblTotalAmountFiat.Visibility = Visibility.Collapsed;
            lblTotalAmountFiatData.Visibility = Visibility.Collapsed;

            BindGrid();
        }

        protected override void SubwindowBase_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.IsVisible)
            {
                LoadParametersWindows();
                txtUntilDate.Foreground = Brushes.White;
            }
        }

        private void LoadParametersWindows()
        {
            _untilDate = ParWinBalancesUntilDate;
            txtUntilDate.Text = _untilDate;
        }

        public SeriesCollection? SeriesCollection { get; set; }

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
            string fiatCurrency = SettingFiatCurrency;
            dgBalances.Columns[3].Header = $"AMOUNT__{fiatCurrency}";

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
                    CustomMessageBox.Show("Database could not be opened." + Environment.NewLine + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                    // Exit function early
                    return;
                }

                using DbCommand selectCommand = connection.CreateCommand();
                selectCommand.CommandText = @"SELECT 
                                                  balances.ASSET, 
                                                  asset_catalog.LABEL,
                                                  balances.AMOUNT, 
                                                  balances.AMOUNT_FIAT 
                                              FROM TB_BALANCES balances
                                              LEFT OUTER JOIN TB_ASSET_CATALOG asset_catalog
                                                  ON balances.ASSET = asset_catalog.ASSET
                                              ORDER BY balances.ASSET";

                using (DbDataReader reader = selectCommand.ExecuteReader())
                {
                    int dbLineNumber = 0;

                    List<string> assets = [];
                    List<decimal> amounts_fiat = [];

                    string asset = "";
                    string assetWithLabel = "";

                    decimal amnt = 0.00m;
                    decimal amnt_fiat = 0.00m;
                    decimal tot_amnt_fiat = 0.00m;

                    try
                    {
                        while (reader.Read())
                        {
                            dbLineNumber++;

                            asset = reader.GetStringOrEmpty(0);
                            assetWithLabel = $"{asset} ({reader.GetStringOrEmpty(1)})";
                            amnt = reader.GetDecimalOrDefault(2);
                            amnt_fiat = reader.GetDecimalOrDefault(3, 0.00m);

                            BalancesData.Add(new BalancesModel
                            {
                                Row_number = dbLineNumber,
                                Asset = assetWithLabel,
                                Amount = amnt,
                                Amount_fiat = amnt_fiat
                            });

                            tot_amnt_fiat += amnt_fiat;

                            assets.Add(asset); // Add asset to list
                            amounts_fiat.Add(amnt_fiat);
                        }

                        if (chkConvertToFiat.IsChecked == true)
                        {
                            lblTotalAmountFiatData.Content = $"{tot_amnt_fiat} {fiatCurrency}";

                            RefreshPie([.. assets], [.. amounts_fiat]); // Spreads collection items into new arrays for method argument
                            ToggleDataUIVisibility(Visibility.Visible);
                        }
                        else
                        {
                            ToggleDataUIVisibility(Visibility.Hidden);
                        }
                    }
                    catch (Exception ex)
                    {
                        CustomMessageBox.Show("Exception whilst fetching BalancesData." + Environment.NewLine + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

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
                        LabelPoint = chartPoint => $"{(chartPoint.Participation * 100).ToString("F2")}%",
                        Stroke = null,
                        StrokeThickness = 0
                    });
                }
            }

            DataContext = this;
            pcBalances.DataTooltip = null;
        }

        private void ToggleDataUIVisibility(Visibility visibility)
        {
            pcBalances.Visibility = visibility;
            lblTotalAmountFiat.Visibility = visibility;
            lblTotalAmountFiatData.Visibility = visibility;
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
            _untilDate = txtUntilDate.Text;
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            _lastWarning = null;
            Refresh();
        }

        private async void Refresh()
        {
            string fiatCurrency = SettingFiatCurrency;

            if (!IsValidDateFormat(txtUntilDate.Text, "yyyy-MM-dd"))
            {
                CustomMessageBox.Show("Until date does not have a correct YYYY-MM-DD format.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                // Exit function early
                return;
            }

            // Save balances until date parameter
            ParWinBalancesUntilDate = txtUntilDate.Text;

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
                            CustomMessageBox.Show("Database could not be opened." + Environment.NewLine + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

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
                                deleteCommand.CommandText = "DELETE FROM TB_BALANCES";
                                deleteCommand.ExecuteNonQuery();

                                // Insert into db table
                                using DbCommand selectCommand = connection.CreateCommand();
                                selectCommand.CommandText = @"SELECT 
                                                                  asset_catalog.ASSET
                                                              FROM
                                                                  (SELECT ASSET 
                                                                   FROM TB_ASSET_CATALOG) asset_catalog
                                                                  INNER JOIN
                                                                  (SELECT DISTINCT ASSET 
                                                                   FROM TB_LEDGERS) ledgers
                                                                  ON asset_catalog.ASSET = ledgers.ASSET
                                                              ORDER BY 
                                                                  ASSET";

                                using (DbDataReader reader = selectCommand.ExecuteReader())
                                {
                                    // For each asset, create balance insert

                                    // Rate limiting mechanism //
                                    DateTime lastCallTime = DateTime.Now;
                                    /////////////////////////////
                                    while (reader.Read())
                                    {
                                        string asset = reader.GetStringOrEmpty(0);

                                        using DbCommand insertCommand = connection.CreateCommand();
                                        if (asset == fiatCurrency)
                                        {
                                            insertCommand.CommandText = CreateFiatBalancesInsert(asset, _untilDate);
                                        }
                                        else
                                        {
                                            var (xInFiat, sqlCommand, conversionSource) = CreateCryptoBalancesInsert(asset, _untilDate, convertToFiat, connection);
                                            insertCommand.CommandText = sqlCommand;

                                            if (xInFiat == 0m)
                                            {
                                                _lastWarning = $"[Balances] Unable to calculate balance" + Environment.NewLine + $"Retrieved 0.00 exchange rate for asset {asset} on {_untilDate}";
                                                Application.Current.Dispatcher.Invoke(() =>
                                                {
                                                    ConsoleLog(_mainWindow.txtLog, _lastWarning);
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
                                    if (_lastWarning != null)
                                    {
                                        Application.Current.Dispatcher.Invoke(() =>
                                        {
                                            CustomMessageBox.Show("There were issues calculating some balances.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                        if (ledgersRefreshWarning == null && _lastWarning == null)
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
                        ConsoleLog(_mainWindow.txtLog, $"[Balances] {lastError}");
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

        private static (decimal xInFiat, string sqlCommand, string conversionSource) CreateCryptoBalancesInsert(string asset, string untilDate, bool? convertToFiat, FbConnection connection)
        {
            try
            {
                decimal xInFiat = 0.00m;
                string conversionSource = "";

                if (convertToFiat == true)
                {
                    var (fiatAmount, source) = ConvertXToFiat(asset, ConvertStringToIsoDate(untilDate), connection);
                    xInFiat = fiatAmount;
                    conversionSource = source;
                }

                string sqlCommand = $@"INSERT INTO TB_BALANCES (ASSET, AMOUNT, AMOUNT_FIAT)
                                           SELECT 
                                               ASSET, 
                                               AMOUNT, 
                                               AMOUNT_FIAT 
                                           FROM
                                               (
                                                   SELECT 
                                                       '{asset}' AS ASSET,
                                                       ROUND(SUM(AMNT), 10) AS AMOUNT,
                                                       ROUND({xInFiat} * SUM(AMNT), 2) AS AMOUNT_FIAT
                                                   FROM (
                                                       SELECT 
                                                           SUM(AMOUNT) AS AMNT
                                                       FROM TB_LEDGERS
                                                       WHERE ASSET = '{asset}'
                                                           AND TYPE NOT IN ('WITHDRAWAL', 'DEPOSIT')
                                                           AND ""DATE"" < DATEADD(DAY, 1, CAST('{untilDate}' AS TIMESTAMP))
                                                       UNION ALL
                                                       SELECT 
                                                           -SUM(FEE) AS AMNT
                                                       FROM TB_LEDGERS
                                                       WHERE ASSET = '{asset}'
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

        private static string CreateFiatBalancesInsert(string asset, string untilDate)
        {
            string query = $@"INSERT INTO TB_BALANCES (ASSET, AMOUNT, AMOUNT_FIAT)
                                  SELECT 
                                      '{asset}' AS ASSET,
                                      ROUND(SUM(AMNT), 10) AS AMOUNT,
                                      ROUND(SUM(AMNT), 2) AS AMOUNT_FIAT
                                  FROM (
                                      SELECT 
                                          SUM(AMOUNT) AS AMNT
                                      FROM TB_LEDGERS
                                      WHERE ASSET = '{asset}'
                                          AND ""DATE"" < DATEADD(DAY, 1, CAST('{untilDate}' AS TIMESTAMP))
                                      UNION ALL
                                      SELECT 
                                          -SUM(FEE) AS AMNT
                                      FROM TB_LEDGERS
                                      WHERE ASSET = '{asset}'
                                          AND ""DATE"" < DATEADD(DAY, 1, CAST('{untilDate}' AS TIMESTAMP))
                                  )";
            return query;
        }

        private async void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            if (!dgBalances.HasItems)
            {
                CustomMessageBox.Show("Nothing to print.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            BlockUI();

            try
            {
                await PrintBalancesAsync();
            }
            catch (Exception ex)
            {
                ConsoleLog(_mainWindow.txtLog, $"[Balances] Printing failed: {ex.Message}");
                CustomMessageBox.Show($"Printing failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                mainWindow: _mainWindow,
                caller: Caller.Balances,
                title: "Balances",
                subtitle: $"Until\t{_untilDate}",
                columnHeaders: new[] { "ASSET", "AMOUNT", $"AMOUNT_{fiatCurrency}" },
                dataItems: balances,
                dataExtractor: item => new[]
                {
                    (item.Asset ?? "", TextAlignment.Left, 1),
                    ($"{item.Amount,10:F10}", TextAlignment.Right, 1),
                    ($"{item.Amount_fiat,2:F2}", TextAlignment.Right, 1)
                },
                printDlg: printDlg,
                maxColumnsPerRow: 6,
                repeatHeadersPerItem: false
            );
        }

        private void BtnHelp_Click(object sender, RoutedEventArgs e)
        {
            OpenHelp("balances_help.html");
        }
    }
}