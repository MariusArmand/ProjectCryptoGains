﻿using System;
using System.Threading.Tasks;
using System.Windows;
using static ProjectCryptoGains.Utility;

namespace ProjectCryptoGains
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Temporary declare subwindows
        private SettingsWindow? winSettings;
        private AssetCatalogWindow? winAssetCatalog;
        private KrakenAssetsWindow? winKrakenAssets;
        private KrakenPairsWindow? winKrakenPairs;
        private KrakenTradesWindow? winKrakenTrades;
        private KrakenLedgersWindow? winKrakenLedgers;
        private ManualLedgersWindow? winManualLedgers;
        private TradesRawWindow? winTradesRaw;
        private TradesWindow? winTrades;
        private GainsWindow? winGains;
        private LedgersWindow? winLedgers;
        private RewardsWindow? winRewards;
        private MetricsWindow? winMetrics;
        private BalancesWindow? winBalances;

        public MainWindow()
        {
            InitializeComponent();

            // Capture events on titlebar
            this.TitleBar.MouseLeftButtonDown += (sender, e) =>
            {
                if (e.ClickCount == 2) // Detect double-click
                {
                    this.WindowState = (this.WindowState == WindowState.Maximized) ? WindowState.Normal : WindowState.Maximized;
                }
                else
                {
                    this.DragMove();
                }
            };

            ConsoleLog(txtLog, $"[Main] Connecting to database {databasePath}");
            ConsoleLog(txtLog, $"[Main] {TestDatabaseConnection()}");

            // Handle the Closing event of the main window
            this.Closing += async (sender, e) =>
            {
                // Close the subwindows
                foreach (var window in new Window?[] {
                    winAssetCatalog, winKrakenAssets, winKrakenPairs, winKrakenTrades,
                    winKrakenLedgers, winManualLedgers, winTradesRaw, winTrades,
                    winGains, winLedgers, winRewards, winMetrics, winBalances
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

            ////////////////////
            // Load Settings
            ////////////////////
            string? lastError = null;

            try
            {
                LoadSettingFiatCurrencyFromDB();
                if (string.IsNullOrEmpty(SettingFiatCurrency))
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
                MessageBoxResult result = CustomMessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

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
                MessageBoxResult result = CustomMessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

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
                MessageBoxResult result = CustomMessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            SystemCommands.MinimizeWindow(this);
        }

        private void Resize_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
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

        private void MenuSettings_Click(object sender, RoutedEventArgs e)
        {
            // Create window if it doesn't exist yet
            winSettings ??= new SettingsWindow(this);
            ShowAndFocusSubWindow(winSettings, this);
        }

        private void MenuAssetCatalog_Click(object sender, RoutedEventArgs e)
        {
            // Create window if it doesn't exist yet
            winAssetCatalog ??= new AssetCatalogWindow(this);
            if (!winAssetCatalog.IsVisible)
            {
                winAssetCatalog.BindGrid();
            }
            ShowAndFocusSubWindow(winAssetCatalog, this);
        }

        private void MenuKrakenAssets_Click(object sender, RoutedEventArgs e)
        {
            // Create window if it doesn't exist yet
            winKrakenAssets ??= new KrakenAssetsWindow(this);
            ShowAndFocusSubWindow(winKrakenAssets, this);
        }

        private void MenuKrakenPairs_Click(object sender, RoutedEventArgs e)
        {
            // Create window if it doesn't exist yet
            winKrakenPairs ??= new KrakenPairsWindow(this);
            ShowAndFocusSubWindow(winKrakenPairs, this);
        }

        private void MenuKrakenTrades_Click(object sender, RoutedEventArgs e)
        {
            // Create window if it doesn't exist yet
            winKrakenTrades ??= new KrakenTradesWindow(this);
            ShowAndFocusSubWindow(winKrakenTrades, this);
        }

        private void MenuKrakenLedgers_Click(object sender, RoutedEventArgs e)
        {
            // Create window if it doesn't exist yet
            winKrakenLedgers ??= new KrakenLedgersWindow(this);
            ShowAndFocusSubWindow(winKrakenLedgers, this);
        }

        private void MenuManualLedgers_Click(object sender, RoutedEventArgs e)
        {
            // Create window if it doesn't exist yet
            winManualLedgers ??= new ManualLedgersWindow(this);
            ShowAndFocusSubWindow(winManualLedgers, this);
        }

        private void MenuTradesRaw_Click(object sender, RoutedEventArgs e)
        {
            // Create window if it doesn't exist yet
            winTradesRaw ??= new TradesRawWindow(this);
            ShowAndFocusSubWindow(winTradesRaw, this);
        }

        private void MenuTrades_Click(object sender, RoutedEventArgs e)
        {
            // Create window if it doesn't exist yet
            winTrades ??= new TradesWindow(this);
            ShowAndFocusSubWindow(winTrades, this);
        }

        private void MenuGains_Click(object sender, RoutedEventArgs e)
        {
            // Create window if it doesn't exist yet
            winGains ??= new GainsWindow(this);
            ShowAndFocusSubWindow(winGains, this);
        }

        private void MenuLedgers_Click(object sender, RoutedEventArgs e)
        {
            // Create window if it doesn't exist yet
            winLedgers ??= new LedgersWindow(this);
            ShowAndFocusSubWindow(winLedgers, this);
        }

        private void MenuRewards_Click(object sender, RoutedEventArgs e)
        {
            // Create window if it doesn't exist yet
            winRewards ??= new RewardsWindow(this);
            ShowAndFocusSubWindow(winRewards, this);
        }

        private void MenuBalances_Click(object sender, RoutedEventArgs e)
        {
            // Create window if it doesn't exist yet
            winBalances ??= new BalancesWindow(this);
            ShowAndFocusSubWindow(winBalances, this);
        }

        private void MenuMetrics_Click(object sender, RoutedEventArgs e)
        {
            // Create window if it doesn't exist yet
            winMetrics ??= new MetricsWindow(this);
            ShowAndFocusSubWindow(winMetrics, this);
        }

        private void ButtonHelp_Click(object sender, RoutedEventArgs e)
        {
            OpenHelp("help.html");
        }
    }
}