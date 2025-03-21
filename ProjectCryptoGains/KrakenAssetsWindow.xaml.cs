using FirebirdSql.Data.FirebirdClient;
using ProjectCryptoGains.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using static ProjectCryptoGains.Common.Utils.DatabaseUtils;
using static ProjectCryptoGains.Common.Utils.ReaderUtils;
using static ProjectCryptoGains.Common.Utils.Utils;
using static ProjectCryptoGains.Common.Utils.ValidationUtils;
using static ProjectCryptoGains.Common.Utils.WindowUtils;
using static ProjectCryptoGains.Models;

namespace ProjectCryptoGains
{
    /// <summary>
    /// Interaction logic for KrakenAssetsWindow.xaml
    /// </summary>
    public partial class KrakenAssetsWindow : SubwindowBase
    {
        private readonly MainWindow _mainWindow;
        public ObservableCollection<AssetsKrakenModel> AssetsKrakenData { get; set; }

        public KrakenAssetsWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            TitleBarElement = TitleBar;

            _mainWindow = mainWindow;
            AssetsKrakenData = [];

            BindGrid();
        }

        private void BlockUI()
        {
            btnRefreshFromSource.IsEnabled = false;
            btnSave.IsEnabled = false;

            Cursor = Cursors.Wait;
        }

        private void UnblockUI()
        {
            btnRefreshFromSource.IsEnabled = true;
            btnSave.IsEnabled = true;

            Cursor = Cursors.Arrow;
        }

        public void BindGrid()
        {
            // Clear existing data
            AssetsKrakenData?.Clear();

            if (AssetsKrakenData == null) return; // Add a null check

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
                                                  ledgers_kraken.ASSET,
                                                  assets_kraken.LABEL
                                              FROM 
                                                  (SELECT DISTINCT ASSET FROM TB_LEDGERS_KRAKEN) ledgers_kraken
                                                  LEFT OUTER JOIN TB_ASSETS_KRAKEN assets_kraken
                                                      ON ledgers_kraken.ASSET = assets_kraken.ASSET";

                using (DbDataReader reader = selectCommand.ExecuteReader())
                {
                    int dbLineNumber = 0;
                    while (reader.Read())
                    {
                        dbLineNumber++;

                        AssetsKrakenData.Add(new AssetsKrakenModel
                        {
                            Asset = reader.GetStringOrEmpty(0),
                            Label = reader.GetStringOrEmpty(1)
                        });
                    }
                }
            }

            dgKrakenAssets.ItemsSource = AssetsKrakenData;
        }

        private void BtnHelp_Click(object sender, RoutedEventArgs e)
        {
            OpenHelp("kraken_assets_help.html");
        }

        private void BtnRefreshFromSource_Click(object sender, RoutedEventArgs e)
        {
            ConsoleLog(_mainWindow.txtLog, $"[Kraken Assets] Refreshing from source");
            BindGrid();
            ConsoleLog(_mainWindow.txtLog, $"[Kraken Assets] Refresh done");
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            ConsoleLog(_mainWindow.txtLog, $"[Kraken Assets] Saving assets");

            int errors = 0;
            string? lastInfo = null;
            string? lastError = null;

            if (AssetsKrakenData == null || AssetsKrakenData.Count == 0)
            {
                lastInfo = "No data to save.";
                ConsoleLog(_mainWindow.txtLog, $"[Kraken Assets] {lastInfo}");
                CustomMessageBox.Show(lastInfo, "Info", MessageBoxButton.OK, MessageBoxImage.Information);

                // Exit function early
                return;
            }

            BlockUI();

            try
            {
                await Task.Run(() =>
                {
                    foreach (var asset in AssetsKrakenData)
                    {
                        if (string.IsNullOrWhiteSpace(asset.Asset) || string.IsNullOrWhiteSpace(asset.Label))
                        {
                            errors += 1;
                        }
                    }
                });

                if (errors > 0)
                {
                    lastError = "ASSET and LABEL cannot be empty.";
                    ConsoleLog(_mainWindow.txtLog, $"[Kraken Assets] {lastError}");
                    CustomMessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    // Save assets to db
                    await Task.Run(() =>
                    {
                        using (FbConnection connection = new(connectionString))
                        {
                            connection.Open();

                            using DbCommand deleteCommand = connection.CreateCommand();
                            deleteCommand.CommandText = "DELETE FROM TB_ASSETS_KRAKEN";
                            deleteCommand.ExecuteNonQuery();

                            using DbCommand insertCommand = connection.CreateCommand();
                            foreach (var krakenAsset in AssetsKrakenData)
                            {
                                insertCommand.CommandText = "INSERT INTO TB_ASSETS_KRAKEN (ASSET, LABEL) VALUES (@ASSET, @LABEL)";
                                insertCommand.Parameters.Clear();

                                AddParameterWithValue(insertCommand, "@ASSET", (object?)krakenAsset.Asset ?? DBNull.Value);
                                AddParameterWithValue(insertCommand, "@LABEL", (object?)krakenAsset.Label ?? DBNull.Value);

                                try
                                {
                                    insertCommand.ExecuteNonQuery();
                                }
                                catch (Exception ex)
                                {
                                    lastError = "Failed to insert data" + Environment.NewLine + ex.Message;
                                }
                            }
                        }
                    });

                    if (lastError != null)
                    {
                        CustomMessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        ConsoleLog(_mainWindow.txtLog, $"[Kraken Assets] {lastError}");
                    }
                }

                // Check for malconfigured assets
                using (FbConnection connection = new(connectionString))
                {
                    connection.Open();

                    List<string> malconfiguredAssets = MalconfiguredAssets(connection);

                    if (malconfiguredAssets.Count > 0)
                    {
                        lastError = "Malconfigured asset(s) detected.";
                        CustomMessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        ConsoleLog(_mainWindow.txtLog, $"[Kraken Assets] {lastError}");

                        // Log each malconfigured asset
                        foreach (string asset in malconfiguredAssets)
                        {
                            ConsoleLog(_mainWindow.txtLog, $"[Kraken Assets] Malconfigured asset: {asset}");
                        }
                    }
                }

                if (lastError == null)
                {
                    ConsoleLog(_mainWindow.txtLog, $"[Kraken Assets] Saving successful");
                }
                else
                {
                    ConsoleLog(_mainWindow.txtLog, $"[Kraken Assets] Saving unsuccessful");
                }
            }
            finally
            {
                UnblockUI();
            }
        }
    }
}