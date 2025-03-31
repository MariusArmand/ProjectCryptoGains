using FirebirdSql.Data.FirebirdClient;
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
using static ProjectCryptoGains.Common.Utils.ExceptionUtils;
using static ProjectCryptoGains.Common.Utils.LedgersUtils;
using static ProjectCryptoGains.Common.Utils.ParametersWindowsUtils;
using static ProjectCryptoGains.Common.Utils.ReaderUtils;
using static ProjectCryptoGains.Common.Utils.RewardsUtils;
using static ProjectCryptoGains.Common.Utils.SettingsUtils;
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

        private int _errors = 0;
        private string _fromDate = "";
        private string _toDate = "";
        private string _baseAsset = "";

        public GainsWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            TitleBarElement = TitleBar;

            _mainWindow = mainWindow;

            ReadParametersWindows();
            BindGrid();
        }

        protected override void SubwindowBase_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.IsVisible)
            {
                ReadParametersWindows();
                txtFromDate.Foreground = Brushes.White;
                txtToDate.Foreground = Brushes.White;
            }
        }

        private void ReadParametersWindows()
        {
            _fromDate = ParWinGainsFromDate;
            txtFromDate.Text = _fromDate;

            _toDate = ParWinGainsToDate;
            txtToDate.Text = _toDate;

            _baseAsset = ParWinGainsBaseAsset;
            txtBaseAsset.Text = _baseAsset;
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
            string fiatCurrency = SettingFiatCurrency;
            decimal rewardsTaxPercentage = SettingRewardsTaxPercentage;
            dgGains.Columns[8].Header = $"BASE__UNIT__PRICE__{fiatCurrency}";

            ObservableCollection<GainsModel> GainsData = [];
            ObservableCollection<GainsSummaryModel> GainsSummaryData = [];

            decimal tot_gain = 0.00m;

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
                selectCommand.CommandText = $@"SELECT
                                                   acquisitions.REFID,
                                                   acquisitions.""DATE"",
                                                   acquisitions.TYPE,
                                                   acquisitions.BASE_ASSET,
                                                   acquisitions.BASE_AMOUNT,
                                                   acquisitions.QUOTE_ASSET,
                                                   acquisitions.QUOTE_AMOUNT,
                                                   acquisitions.BASE_UNIT_PRICE_FIAT,
                                                   acquisitions.COSTS_PROCEEDS,
                                                   CASE
                                                       WHEN acquisitions.TYPE = 'SELL' THEN NULL
                                                       ELSE gains.TX_BALANCE_REMAINING
                                                   END AS TX_BALANCE_REMAINING,
                                                   CASE
                                                       WHEN acquisitions.TYPE IN ('BUY', 'STAKING') THEN NULL
                                                       ELSE gains.GAIN
                                                   END AS GAIN
                                               FROM TB_GAINS gains
                                                   INNER JOIN
                                                       (SELECT
                                                            REFID,
                                                            ""DATE"",
                                                            TYPE,
                                                            BASE_ASSET,
                                                            BASE_AMOUNT,
                                                            QUOTE_ASSET,
                                                            QUOTE_AMOUNT,
                                                            BASE_UNIT_PRICE_FIAT,
                                                            COSTS_PROCEEDS
                                                        FROM TB_TRADES
                                                        UNION ALL
                                                        SELECT
                                                            REFID,
                                                            ""DATE"",
                                                            TYPE,
                                                            ASSET AS BASE_ASSET,
                                                            AMOUNT AS BASE_AMOUNT,
                                                            'EUR' AS QUOTE_ASSET,
                                                            {(rewardsTaxPercentage > 0 ? "AMOUNT * UNIT_PRICE" : "0")} AS QUOTE_AMOUNT,
                                                            {(rewardsTaxPercentage > 0 ? "UNIT_PRICE" : "0")} AS BASE_UNIT_PRICE_FIAT,
                                                            {(rewardsTaxPercentage > 0 ? "AMOUNT * UNIT_PRICE" : "0")} AS COSTS_PROCEEDS
                                                        FROM TB_REWARDS) acquisitions
                                                        ON gains.REFID = acquisitions.REFID
                                               WHERE acquisitions.BASE_ASSET LIKE '%{_baseAsset}%'
                                               ORDER BY ""DATE"" ASC";

                using (DbDataReader reader = selectCommand.ExecuteReader())
                {
                    int dbLineNumber = 0;
                    while (reader.Read())
                    {
                        dbLineNumber++;

                        GainsData.Add(new GainsModel
                        {
                            Row_number = dbLineNumber,
                            Refid = reader.GetStringOrEmpty(0),
                            Date = reader.GetDateTime(1),
                            Type = reader.GetStringOrEmpty(2),
                            Base_asset = reader.GetStringOrEmpty(3),
                            Base_amount = reader.GetDecimalOrDefault(4),
                            Quote_asset = reader.GetStringOrEmpty(5),
                            Quote_amount = reader.GetDecimalOrDefault(6),
                            Base_unit_price_fiat = reader.GetDecimal(7),
                            Costs_proceeds = reader.GetDecimal(8),
                            Tx_balance_remaining = reader.GetDecimalOrNull(9),
                            Gain = reader.GetDecimalOrNull(10)
                        });
                    }
                }

                dgGains.ItemsSource = GainsData;

                /////////////////////////////////

                selectCommand.CommandText = $@"SELECT 
                                                   acquisitions.BASE_ASSET,
                                                   asset_catalog.LABEL,
                                                   ROUND(SUM(gains.GAIN), 10) AS GAIN
                                               FROM TB_GAINS gains
                                                   INNER JOIN
                                                       (SELECT 
                                                            REFID,
                                                            ""DATE"",
                                                            BASE_ASSET
                                                        FROM TB_TRADES
                                                        UNION ALL
                                                        SELECT 
                                                            REFID,
                                                            ""DATE"",
                                                            ASSET AS BASE_ASSET
                                                        FROM TB_REWARDS) acquisitions
                                                        ON gains.REFID = acquisitions.REFID
                                                   LEFT OUTER JOIN TB_ASSET_CATALOG asset_catalog
                                                        ON acquisitions.BASE_ASSET = asset_catalog.ASSET
                                               WHERE gains.GAIN IS NOT NULL
                                                   AND acquisitions.BASE_ASSET LIKE @BASE_ASSET
                                                   AND acquisitions.""DATE"" BETWEEN @FROM_DATE AND @TO_DATE
                                               GROUP BY acquisitions.BASE_ASSET, asset_catalog.LABEL
                                               ORDER BY acquisitions.BASE_ASSET";

                AddParameterWithValue(selectCommand, "@BASE_ASSET", $"%{_baseAsset}%");
                AddParameterWithValue(selectCommand, "@FROM_DATE", ConvertStringToIsoDate(_fromDate));
                AddParameterWithValue(selectCommand, "@TO_DATE", ConvertStringToIsoDate(_toDate).AddDays(1));

                using (DbDataReader reader = selectCommand.ExecuteReader())
                {
                    int dbLineNumber = 0;

                    decimal gain = 0.00m;
                    while (reader.Read())
                    {
                        dbLineNumber++;

                        gain = reader.GetDecimalOrDefault(2);
                        GainsSummaryData.Add(new GainsSummaryModel
                        {
                            Row_number = dbLineNumber,
                            Asset = $"{reader.GetStringOrEmpty(0)} ({reader.GetStringOrEmpty(1)})",
                            Gain = reader.GetDecimalOrDefault(2)
                        });
                        tot_gain += gain;
                    }
                }
            }
            lblTotalGainsData.Content = $"{tot_gain.ToString("F2")} {fiatCurrency}";
            dgGainsSummary.ItemsSource = GainsSummaryData;
        }

        private void UnbindGrid()
        {
            string fiatCurrency = SettingFiatCurrency;
            lblTotalGainsData.Content = $"0.00 {fiatCurrency}";
            dgGains.ItemsSource = null;
            dgGainsSummary.ItemsSource = null;
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
            _fromDate = txtFromDate.Text;
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
            _toDate = txtToDate.Text;
        }

        private void TextBoxBaseAsset_KeyUp(object sender, KeyboardEventArgs e)
        {
            SetBaseAsset();
        }

        private void SetBaseAsset()
        {
            _baseAsset = txtBaseAsset.Text;
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            Refresh();
        }

        private async void Refresh()
        {
            _errors = 0;

            if (!IsValidDateFormat(txtFromDate.Text, "yyyy-MM-dd"))
            {
                CustomMessageBox.Show("From date does not have a correct YYYY-MM-DD format.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!IsValidDateFormat(txtToDate.Text, "yyyy-MM-dd"))
            {
                CustomMessageBox.Show("To date does not have a correct YYYY-MM-DD format.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Save gains parameters
            ParWinGainsFromDate = txtFromDate.Text;
            ParWinGainsToDate = txtToDate.Text;
            ParWinGainsBaseAsset = txtBaseAsset.Text;

            BlockUI();

            try
            {
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

                string? rewardsRefreshError = null;
                string? rewardsRefreshWarning = null;
                bool rewardsRefreshWasBusy = false;
                if (chkRefreshRewards.IsChecked == true && !ledgersRefreshWasBusy && !tradesRefreshWasBusy && !ledgersRefreshFailed && tradesRefreshError == null)
                {
                    await Task.Run(async () =>
                    {
                        try
                        {
                            rewardsRefreshWarning = await RefreshRewards(_mainWindow, Caller.Gains, "2009-01-03", GetTodayAsIsoDate());
                            rewardsRefreshWasBusy = RewardsRefreshBusy;
                        }
                        catch (Exception ex)
                        {
                            while (ex.InnerException != null)
                            {
                                ex = ex.InnerException;
                            }
                            rewardsRefreshError = ex.Message;
                        }
                    });
                }

                // LIFO processing
                if (!ledgersRefreshWasBusy && !tradesRefreshWasBusy && !rewardsRefreshWasBusy && !ledgersRefreshFailed && tradesRefreshError == null && rewardsRefreshError == null)
                {
                    // Clear the table before inserting new data
                    ClearTable();

                    await Task.Run(() =>
                    {
                        using (FbConnection connection = new(connectionString))
                        {
                            try
                            {
                                connection.Open();

                                // Read the assets into a list
                                using DbCommand selectCommand = connection.CreateCommand();
                                selectCommand.CommandText = $@"SELECT ASSET FROM TB_ASSET_CATALOG WHERE ASSET like '%{_baseAsset}%'";

                                List<string> assets = [];

                                using (DbDataReader reader = selectCommand.ExecuteReader())
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
                                    List<TransactionsModel> sellTransactions = ReadSellTransactionsFromDB(asset);
                                    List<TransactionsModel> acquireTransactions = ReadAcquireTransactionsFromDB(asset);
                                    CalculateLIFOGains(asset, sellTransactions, acquireTransactions);
                                    WriteTransactionsToDB(sellTransactions, acquireTransactions, connectionString);
                                }
                            }
                            catch (Exception ex)
                            {
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    string errorMessage;
                                    switch (ex)
                                    {
                                        case DatabaseReadException dbREx:
                                            errorMessage = BuildErrorMessage(dbREx);
                                            break;

                                        case DatabaseWriteException dbWEx:
                                            errorMessage = BuildErrorMessage(dbWEx);
                                            break;

                                        default:
                                            errorMessage = $"Gains could not be calculated.{Environment.NewLine}{ex.Message}";
                                            break;
                                    }

                                    CustomMessageBox.Show(errorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                });
                                return;
                            }
                        }
                    });

                    if (_errors == 0)
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
                        ClearTable();
                        UnbindGrid();
                        ConsoleLog(_mainWindow.txtLog, $"[Gains] Refresh unsuccessful");
                    }
                }
                else
                {
                    ClearTable();
                    UnbindGrid();
                    if (tradesRefreshError != null)
                    {
                        ConsoleLog(_mainWindow.txtLog, $"[Gains] {tradesRefreshError}");
                        ConsoleLog(_mainWindow.txtLog, $"[Gains] Refreshing trades unsuccessful");
                    }
                    ConsoleLog(_mainWindow.txtLog, $"[Gains] Refresh unsuccessful");
                }
            }
            finally
            {
                UnblockUI();
            }
        }

        private static List<TransactionsModel> ReadSellTransactionsFromDB(String asset)
        {
            List<TransactionsModel> transactions = [];

            using (FbConnection connection = new(connectionString))
            {
                try
                {
                    connection.Open();

                    using DbCommand selectCommand = connection.CreateCommand();
                    selectCommand.CommandText = $@"SELECT 
                                                       REFID,
                                                       ""DATE"",
                                                       BASE_AMOUNT AS AMOUNT,
                                                       BASE_UNIT_PRICE_FIAT AS UNIT_PRICE,
                                                       COSTS_PROCEEDS,
                                                       BASE_AMOUNT AS TX_BALANCE_REMAINING
                                                   FROM TB_TRADES
                                                   WHERE BASE_ASSET = '{asset}'
                                                       AND TYPE = 'SELL'
                                                   ORDER BY ""DATE"" ASC";

                    using (DbDataReader reader = selectCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            transactions.Add(new TransactionsModel
                            {
                                RefId = reader.GetStringOrEmpty(0),
                                Date = reader.GetDateTime(1),
                                Amount = reader.GetDecimalOrDefault(2),
                                Unit_price = reader.GetDecimal(3),
                                Costs_Proceeds = reader.GetDecimal(4),
                                Tx_Balance_Remaining = null
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new DatabaseReadException("Transaction could not be read.", ex);
                }
            }

            return transactions;
        }

        private static List<TransactionsModel> ReadAcquireTransactionsFromDB(String asset)
        {
            decimal rewardsTaxPercentage = SettingRewardsTaxPercentage;

            List<TransactionsModel> transactions = [];

            using (FbConnection connection = new(connectionString))
            {
                try
                {
                    connection.Open();

                    using DbCommand selectCommand = connection.CreateCommand();
                    // UNIT_PRICE (Fair Market Value) is used when rewards are taxed as income to set the cost basis,
                    // preventing double taxation by offsetting the already-taxed FMV in the LIFO gain calculation;
                    // Set to 0 when no income tax applies to ensure the full proceeds are taxed as capital gains
                    selectCommand.CommandText = $@"SELECT 
                                                       REFID,
                                                       ""DATE"",
                                                       BASE_AMOUNT AS AMOUNT,
                                                       BASE_UNIT_PRICE_FIAT AS UNIT_PRICE,
                                                       COSTS_PROCEEDS,
                                                       BASE_AMOUNT AS TX_BALANCE_REMAINING
                                                   FROM TB_TRADES
                                                   WHERE BASE_ASSET = '{asset}'
                                                       AND TYPE = 'BUY'
                                                   UNION ALL
                                                   SELECT
                                                       REFID,
                                                       ""DATE"",
                                                       AMOUNT,
                                                       {(rewardsTaxPercentage > 0 ? "UNIT_PRICE" : "0")} AS UNIT_PRICE,
                                                       0 AS COSTS_PROCEEDS,
                                                       AMOUNT AS TX_BALANCE_REMAINING
                                                   FROM TB_REWARDS
                                                   WHERE ASSET = '{asset}'
                                                   ORDER BY 2 DESC";

                    using (DbDataReader reader = selectCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            transactions.Add(new TransactionsModel
                            {
                                RefId = reader.GetStringOrEmpty(0),
                                Date = reader.GetDateTime(1),
                                Amount = reader.GetDecimalOrDefault(2),
                                Unit_price = reader.GetDecimal(3),
                                Costs_Proceeds = reader.GetDecimal(4),
                                Tx_Balance_Remaining = reader.GetDecimal(5)
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new DatabaseReadException("Transaction could not be read.", ex);
                }
            }

            return transactions;
        }

        private void CalculateLIFOGains(String asset, List<TransactionsModel> sellTransactions, List<TransactionsModel> acquireTransactions)
        {
            foreach (var stx in sellTransactions)
            {
                // Initialize the amount we need to sell for this sell transaction
                decimal amountToSell = stx.Amount;

                decimal proceeds = stx.Costs_Proceeds;
                decimal costs = 0;

                // Parse the sell transaction date if it's not null
                DateTime sellDate = stx.Date;

                // Filter acquire transactions to only those on or before the sell date
                var relevantAcquireTransactions = acquireTransactions
                    .Where(atx => atx.Date <= sellDate)
                    .ToList();

                // Calculate the sum of amounts in relevant acquire transactions
                decimal totalRelevantAmountBought = relevantAcquireTransactions.Sum(atx => atx.Amount);

                if (totalRelevantAmountBought < amountToSell)
                {
                    // Instead of showing directly, schedule MessageBox on the UI thread to not block this thread
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _errors += 1;
                        string lastError = "Not enough acquire transactions to cover this sell transaction" +
                                           Environment.NewLine + $"RefId: {stx.RefId}" +
                                           Environment.NewLine + $"Base asset: {asset}" +
                                           Environment.NewLine + $"Amount missing: {amountToSell - totalRelevantAmountBought}";
                        ConsoleLog(_mainWindow.txtLog, $"[Gains] {lastError}");
                        CustomMessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                    break;
                }

                // Continue selling until we've processed the entire amount to sell
                while (amountToSell > 0 && relevantAcquireTransactions.Count != 0)
                {
                    // Iterate through each acquire transaction in order (since they're ordered by date descending)
                    foreach (var atx in relevantAcquireTransactions)
                    {
                        if (atx.Tx_Balance_Remaining > 0)
                        {
                            decimal soldAmount = Math.Min((decimal)atx.Tx_Balance_Remaining, amountToSell);

                            // Add the cost of the sold amount
                            costs += atx.Unit_price * soldAmount;

                            atx.Tx_Balance_Remaining -= soldAmount;
                            amountToSell -= soldAmount;

                            if (amountToSell == 0) break;
                        }
                    }
                }
                // After processing all relevant acquire transactions, set the gain for this sell transaction
                stx.Gain = proceeds - costs;
            }
        }

        private static void WriteTransactionsToDB(List<TransactionsModel> sellTransactions, List<TransactionsModel> acquireTransactions, string connectionString)
        {
            using (FbConnection connection = new(connectionString))
            {
                try
                {
                    connection.Open();

                    using DbCommand insertCommand = connection.CreateCommand();
                    insertCommand.CommandText = @"INSERT INTO TB_GAINS (
                                                      REFID,
                                                      TX_BALANCE_REMAINING,
                                                      GAIN
                                                  ) VALUES (
                                                      @REFID,
                                                      ROUND(@TX_BALANCE_REMAINING, 10),
                                                      CASE 
                                                          WHEN @GAIN IS NOT NULL
                                                          THEN ROUND(@GAIN, 10) 
                                                          ELSE NULL
                                                      END
                                                  )";

                    foreach (var tx in sellTransactions)
                    {
                        if (tx.RefId != null) // Ensure REFID isn't null before adding
                        {
                            // Clear parameters from previous iteration
                            insertCommand.Parameters.Clear();

                            AddParameterWithValue(insertCommand, "@REFID", tx.RefId);
                            AddParameterWithValue(insertCommand, "@TX_BALANCE_REMAINING", tx.Tx_Balance_Remaining);
                            AddParameterWithValue(insertCommand, "@GAIN", tx.Gain);

                            insertCommand.ExecuteNonQuery();
                        }
                    }

                    foreach (var tx in acquireTransactions)
                    {
                        if (tx.RefId != null) // Ensure REFID isn't null before adding
                        {
                            // Clear parameters from previous iteration
                            insertCommand.Parameters.Clear();

                            AddParameterWithValue(insertCommand, "@REFID", tx.RefId);
                            AddParameterWithValue(insertCommand, "@TX_BALANCE_REMAINING", tx.Tx_Balance_Remaining);
                            AddParameterWithValue(insertCommand, "@GAIN", tx.Gain);

                            insertCommand.ExecuteNonQuery();
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new DatabaseWriteException("Error writing to database.", ex);
                }
            }
        }

        private void ClearTable()
        {
            try
            {
                using (FbConnection connection = new(connectionString))
                {
                    connection.Open();
                    // Clear the table before inserting new data
                    using DbCommand deleteCommand = connection.CreateCommand();
                    deleteCommand.CommandText = "DELETE FROM TB_GAINS";
                    deleteCommand.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                ConsoleLog(_mainWindow.txtLog, $"[Gains] Clearing table failed: {ex.Message}");
            }
        }

        private async void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            if (!dgGains.HasItems)
            {
                CustomMessageBox.Show("Nothing to print.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            BlockUI();

            try
            {
                await PrintGainsAsync();
            }
            catch (Exception ex)
            {
                ConsoleLog(_mainWindow.txtLog, $"[Gains] Printing failed: {ex.Message}");
                CustomMessageBox.Show($"Printing failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                UnblockUI();
            }
        }

        private async Task PrintGainsAsync()
        {
            string fiatCurrency = SettingFiatCurrency;
            var gains = dgGains.ItemsSource.OfType<GainsModel>();

            PrintDialog printDlg = new();

            await PrintUtils.PrintFlowDocumentAsync(
                mainWindow: _mainWindow,
                caller: Caller.Gains,
                columnHeaders: new[]
                {
                    "DATE", "REFID", "TYPE", "BASE_ASSET", "BASE_AMOUNT", "QUOTE_ASSET",
                    "QUOTE_AMOUNT", $"BASE_UNIT_PRICE_{fiatCurrency}", "COSTS_PROCEEDS",
                    "TX_BALANCE_REMAINING", "GAIN"
                },
                dataItems: gains,
                dataExtractor: item => new[]
                {
                    (ConvertDateTimeToString(item.Date) ?? "", TextAlignment.Left, 1),
                    (item.Refid ?? "", TextAlignment.Left, 2),
                    (item.Type ?? "", TextAlignment.Left, 1),
                    (item.Base_asset ?? "", TextAlignment.Left, 1),
                    ($"{item.Base_amount,10:F10}", TextAlignment.Left, 1),
                    (item.Quote_asset ?? "", TextAlignment.Left, 1),
                    ($"{item.Quote_amount,10:F10}", TextAlignment.Left, 1),
                    ($"{item.Base_unit_price_fiat,2:F2}", TextAlignment.Left, 1),
                    ($"{item.Costs_proceeds,2:F2}", TextAlignment.Left, 1),
                    (item.Tx_balance_remaining.HasValue ? $"{item.Tx_balance_remaining,10:F10}" : "N/A", TextAlignment.Left, 1),
                    (item.Gain.HasValue ? $"{item.Gain,2:F2}" : "N/A", TextAlignment.Left, 1)
                },
                printDlg: printDlg,
                titlePage: true,
                title: "Gains",
                subtitle: $"From\t{_fromDate}\nTo\t{_toDate}",
                footerHeight: 20,
                maxColumnsPerRow: 7,
                repeatHeadersPerItem: true,
                itemsPerPage: 15
            );
        }

        private void BtnHelp_Click(object sender, RoutedEventArgs e)
        {
            OpenHelp("gains_help.html");
        }

        private async void BtnPrintSummary_Click(object sender, RoutedEventArgs e)
        {
            if (!dgGainsSummary.HasItems)
            {
                CustomMessageBox.Show("Nothing to print.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            BlockUI();

            try
            {
                await PrintGainsSummaryAsync(lblTotalGainsData.Content?.ToString() ?? "");
            }
            catch (Exception ex)
            {
                ConsoleLog(_mainWindow.txtLog, $"[Gains] Printing failed: {ex.Message}");
                CustomMessageBox.Show($"Printing failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                UnblockUI();
            }
        }

        private async Task PrintGainsSummaryAsync(string totalGains)
        {
            string fiatCurrency = SettingFiatCurrency;
            var gainsSummary = dgGainsSummary.ItemsSource.OfType<GainsSummaryModel>();

            PrintDialog printDlg = new();

            await PrintUtils.PrintFlowDocumentAsync(
                mainWindow: _mainWindow,
                caller: Caller.Gains,
                columnHeaders: new[] { "ASSET", "GAIN" },
                dataItems: gainsSummary,
                dataExtractor: item => new[]
                {
                    (item.Asset ?? "", TextAlignment.Left, 1),
                    ($"{item.Gain,2:F2}", TextAlignment.Left, 1)
                },
                printDlg: printDlg,
                title: "Gains Summary",
                subtitle: $"From\t{_fromDate}\nTo\t{_toDate}",
                summaryText: $"Total gains {totalGains}",
                maxColumnsPerRow: 7,
                repeatHeadersPerItem: true
            );
        }
    }
}