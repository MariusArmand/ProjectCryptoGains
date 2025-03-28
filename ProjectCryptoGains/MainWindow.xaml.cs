using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static ProjectCryptoGains.Common.Utils.DatabaseUtils;
using static ProjectCryptoGains.Common.Utils.ParametersWindowsUtils;
using static ProjectCryptoGains.Common.Utils.SettingsUtils;
using static ProjectCryptoGains.Common.Utils.Utils;
using static ProjectCryptoGains.Common.Utils.WindowUtils;

namespace ProjectCryptoGains
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Temporary declare subwindows
        private KrakenLedgersWindow? _winKrakenLedgers;
        private ManualLedgersWindow? _winManualLedgers;
        private ExchangeRatesWindow? _winExchangeRates;
        private SettingsWindow? _winSettings;
        private AssetCatalogWindow? _winAssetCatalog;
        private KrakenAssetsWindow? _winKrakenAssets;
        private LedgersWindow? _winLedgers;
        private TradesWindow? _winTrades;
        private RewardsWindow? _winRewards;
        private GainsWindow? _winGains;
        private BalancesWindow? _winBalances;
        private MetricsWindow? _winMetrics;

        public MainWindow()
        {
            InitializeComponent();

            // Capture events on titlebar
            TitleBar.MouseLeftButtonDown += (sender, e) =>
            {
                if (e.ClickCount == 2) // Detect double-click
                {
                    WindowState = (WindowState == WindowState.Maximized) ? WindowState.Normal : WindowState.Maximized;
                }
                else
                {
                    DragMove();
                }
            };

            ConsoleLog(txtLog, $"[Main] Connecting to database {databasePath}");
            ConsoleLog(txtLog, $"[Main] {TestDatabaseConnection()}");

            // Handle the Closing event of the main window
            Closing += async (sender, e) =>
            {
                // Close the subwindows
                foreach (var window in new Window?[] {
                    _winManualLedgers, _winKrakenLedgers, _winExchangeRates,
                    _winAssetCatalog, _winKrakenAssets, _winSettings,
                    _winLedgers, _winTrades, _winGains, _winRewards, _winBalances, _winMetrics
                })
                {
                    if (window != null)
                    {
                        CloseWindow(window);
                    }
                }
                await Task.Delay(100);
                Application.Current.Shutdown();
            };

            if (DisclaimerWindow.Show() != true)
            {
                // User did not agree, close the application or handle as needed
                Application.Current.Shutdown();
            }

            ////////////////////////////
            // Load settings
            ////////////////////////////
            string? lastError = null;

            // Fiat currency
            try
            {
                LoadSettingFiatCurrencyFromDB();
                if (SettingFiatCurrency == "NONE")
                {
                    SettingFiatCurrency = "EUR";
                }
            }
            catch (InvalidOperationException ex)
            {
                lastError = ex.Message;
                if (ex.InnerException != null)
                {
                    lastError += Environment.NewLine + ex.InnerException.Message;
                }

                ConsoleLog(txtLog, $"[Settings] {lastError}");
                CustomMessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // Rewards tax percentage
            try
            {
                LoadSettingRewardsTaxPercentageFromDB();
            }
            catch (InvalidOperationException ex)
            {
                lastError = ex.Message;
                if (ex.InnerException != null)
                {
                    lastError += Environment.NewLine + ex.InnerException.Message;
                }

                ConsoleLog(txtLog, $"[Settings] {lastError}");
                CustomMessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // CoinDesk Data API key
            try
            {
                LoadSettingCoinDeskDataApiKeyFromDB();
            }
            catch (InvalidOperationException ex)
            {
                lastError = ex.Message;
                if (ex.InnerException != null)
                {
                    lastError += Environment.NewLine + ex.InnerException.Message;
                }

                ConsoleLog(txtLog, $"[Settings] {lastError}");
                CustomMessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // Printout title prefix
            try
            {
                LoadSettingPrintoutTitlePrefixFromDB();
            }
            catch (InvalidOperationException ex)
            {
                lastError = ex.Message;
                if (ex.InnerException != null)
                {
                    lastError += Environment.NewLine + ex.InnerException.Message;
                }

                ConsoleLog(txtLog, $"[Settings] {lastError}");
                CustomMessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            ////////////////////////////

            ////////////////////////////
            // Load parameters windows
            ////////////////////////////

            // Ledgers from date
            try
            {
                LoadParWinLedgersFromDateFromDB();
            }
            catch (InvalidOperationException ex)
            {
                lastError = ex.Message;
                if (ex.InnerException != null)
                {
                    lastError += Environment.NewLine + ex.InnerException.Message;
                }

                ConsoleLog(txtLog, $"[Ledgers] {lastError}");
                CustomMessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // Ledgers to date
            try
            {
                LoadParWinLedgersToDateFromDB();
            }
            catch (InvalidOperationException ex)
            {
                lastError = ex.Message;
                if (ex.InnerException != null)
                {
                    lastError += Environment.NewLine + ex.InnerException.Message;
                }

                ConsoleLog(txtLog, $"[Ledgers] {lastError}");
                CustomMessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // Trades from date
            try
            {
                LoadParWinTradesFromDateFromDB();
            }
            catch (InvalidOperationException ex)
            {
                lastError = ex.Message;
                if (ex.InnerException != null)
                {
                    lastError += Environment.NewLine + ex.InnerException.Message;
                }

                ConsoleLog(txtLog, $"[Trades] {lastError}");
                CustomMessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // Trades to date
            try
            {
                LoadParWinTradesToDateFromDB();
            }
            catch (InvalidOperationException ex)
            {
                lastError = ex.Message;
                if (ex.InnerException != null)
                {
                    lastError += Environment.NewLine + ex.InnerException.Message;
                }

                ConsoleLog(txtLog, $"[Trades] {lastError}");
                CustomMessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // Gains from date
            try
            {
                LoadParWinGainsFromDateFromDB();
            }
            catch (InvalidOperationException ex)
            {
                lastError = ex.Message;
                if (ex.InnerException != null)
                {
                    lastError += Environment.NewLine + ex.InnerException.Message;
                }

                ConsoleLog(txtLog, $"[Gains] {lastError}");
                CustomMessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // Gains to date
            try
            {
                LoadParWinGainsToDateFromDB();
            }
            catch (InvalidOperationException ex)
            {
                lastError = ex.Message;
                if (ex.InnerException != null)
                {
                    lastError += Environment.NewLine + ex.InnerException.Message;
                }

                ConsoleLog(txtLog, $"[Gains] {lastError}");
                CustomMessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // Gains base asset
            try
            {
                LoadParWinGainsBaseAssetFromDB();
            }
            catch (InvalidOperationException ex)
            {
                lastError = ex.Message;
                if (ex.InnerException != null)
                {
                    lastError += Environment.NewLine + ex.InnerException.Message;
                }

                ConsoleLog(txtLog, $"[Gains] {lastError}");
                CustomMessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // Rewards from date
            try
            {
                LoadParWinRewardsFromDateFromDB();
            }
            catch (InvalidOperationException ex)
            {
                lastError = ex.Message;
                if (ex.InnerException != null)
                {
                    lastError += Environment.NewLine + ex.InnerException.Message;
                }

                ConsoleLog(txtLog, $"[Rewards] {lastError}");
                CustomMessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // Rewards to date
            try
            {
                LoadParWinRewardsToDateFromDB();
            }
            catch (InvalidOperationException ex)
            {
                lastError = ex.Message;
                if (ex.InnerException != null)
                {
                    lastError += Environment.NewLine + ex.InnerException.Message;
                }

                ConsoleLog(txtLog, $"[Rewards] {lastError}");
                CustomMessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // Balances until date
            try
            {
                LoadParWinBalancesUntilDateFromDB();
            }
            catch (InvalidOperationException ex)
            {
                lastError = ex.Message;
                if (ex.InnerException != null)
                {
                    lastError += Environment.NewLine + ex.InnerException.Message;
                }

                ConsoleLog(txtLog, $"[Balances] {lastError}");
                CustomMessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            ////////////////////////////
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

        public static void CloseWindow(Window window)
        {
            window.Close();
        }

        private void MenuManualLedgers_Click(object sender, RoutedEventArgs e)
        {
            // Create window if it doesn't exist yet
            _winManualLedgers ??= new ManualLedgersWindow(this);
            ShowAndFocusSubWindow(_winManualLedgers, this);
        }

        private void MenuKrakenLedgers_Click(object sender, RoutedEventArgs e)
        {
            // Create window if it doesn't exist yet
            _winKrakenLedgers ??= new KrakenLedgersWindow(this);
            ShowAndFocusSubWindow(_winKrakenLedgers, this);
        }

        private void MenuExchangeRates_Click(object sender, RoutedEventArgs e)
        {
            // Create window if it doesn't exist yet
            _winExchangeRates ??= new ExchangeRatesWindow(this);
            ShowAndFocusSubWindow(_winExchangeRates, this);
        }

        private void MenuSettings_Click(object sender, RoutedEventArgs e)
        {
            // Create window if it doesn't exist yet
            _winSettings ??= new SettingsWindow(this);
            ShowAndFocusSubWindow(_winSettings, this);
        }

        private void MenuAssetCatalog_Click(object sender, RoutedEventArgs e)
        {
            // Create window if it doesn't exist yet
            _winAssetCatalog ??= new AssetCatalogWindow(this);
            if (!_winAssetCatalog.IsVisible)
            {
                _winAssetCatalog.BindGrid();
            }
            ShowAndFocusSubWindow(_winAssetCatalog, this);
        }

        private void MenuKrakenAssets_Click(object sender, RoutedEventArgs e)
        {
            // Create window if it doesn't exist yet
            _winKrakenAssets ??= new KrakenAssetsWindow(this);
            ShowAndFocusSubWindow(_winKrakenAssets, this);
        }

        private void MenuLedgers_Click(object sender, RoutedEventArgs e)
        {
            // Create window if it doesn't exist yet
            _winLedgers ??= new LedgersWindow(this);
            ShowAndFocusSubWindow(_winLedgers, this);
        }

        private void MenuTrades_Click(object sender, RoutedEventArgs e)
        {
            // Create window if it doesn't exist yet
            _winTrades ??= new TradesWindow(this);
            ShowAndFocusSubWindow(_winTrades, this);
        }

        private void MenuRewards_Click(object sender, RoutedEventArgs e)
        {
            // Create window if it doesn't exist yet
            _winRewards ??= new RewardsWindow(this);
            ShowAndFocusSubWindow(_winRewards, this);
        }

        private void MenuGains_Click(object sender, RoutedEventArgs e)
        {
            // Create window if it doesn't exist yet
            _winGains ??= new GainsWindow(this);
            ShowAndFocusSubWindow(_winGains, this);
        }

        private void MenuBalances_Click(object sender, RoutedEventArgs e)
        {
            // Create window if it doesn't exist yet
            _winBalances ??= new BalancesWindow(this);
            ShowAndFocusSubWindow(_winBalances, this);
        }

        private void MenuMetrics_Click(object sender, RoutedEventArgs e)
        {
            // Create window if it doesn't exist yet
            _winMetrics ??= new MetricsWindow(this);
            ShowAndFocusSubWindow(_winMetrics, this);
        }

        private void Background_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Focus the border instead of letting txtLog take focus
            if (!txtLog.IsMouseOver) // Only if the click wasn't on txtLog
            {
                (sender as Border)?.Focus();
            }
        }

        private void BtnHelp_Click(object sender, RoutedEventArgs e)
        {
            OpenHelp("help.html");
        }
    }
}