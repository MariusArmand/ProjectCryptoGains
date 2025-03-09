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
        public ObservableCollection<AssetCodesKrakenModel> AssetCodesKrakenData { get; set; }

        public KrakenAssetsWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            TitleBarElement = TitleBar;

            _mainWindow = mainWindow;
            AssetCodesKrakenData = [];

            BindGrid();
        }

        private void ButtonHelp_Click(object sender, RoutedEventArgs e)
        {
            OpenHelp("kraken_assets_help.html");
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

        private void RefreshFromSource_Click(object sender, RoutedEventArgs e)
        {
            ConsoleLog(_mainWindow.txtLog, $"[Kraken AssetCatalogData] Refreshing from source");
            BindGrid();
            ConsoleLog(_mainWindow.txtLog, $"[Kraken AssetCatalogData] Refresh done");
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            ConsoleLog(_mainWindow.txtLog, $"[Kraken AssetCatalogData] Saving assets");

            int errors = 0;
            string? lastInfo = null;
            string? lastError = null;

            if (AssetCodesKrakenData == null || AssetCodesKrakenData.Count == 0)
            {
                lastInfo = "No data to save.";
                ConsoleLog(_mainWindow.txtLog, $"[Kraken AssetCatalogData] {lastInfo}");
                MessageBoxResult result = CustomMessageBox.Show(lastInfo, "Info", MessageBoxButton.OK, MessageBoxImage.Information);

                // Exit function early
                return;
            }

            BlockUI();

            try
            {

                await Task.Run(() =>
                {
                    foreach (var asset in AssetCodesKrakenData)
                    {
                        if (string.IsNullOrWhiteSpace(asset.Code) || string.IsNullOrWhiteSpace(asset.Asset))
                        {
                            errors += 1;
                        }
                    }
                });

                if (errors > 0)
                {
                    lastError = "CODE and ASSET cannot be empty.";
                    ConsoleLog(_mainWindow.txtLog, $"[Kraken AssetCatalogData] {lastError}");
                    MessageBoxResult result = CustomMessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                            deleteCommand.CommandText = "DELETE FROM TB_ASSET_CODES_KRAKEN_S";
                            deleteCommand.ExecuteNonQuery();

                            using DbCommand insertCommand = connection.CreateCommand();
                            foreach (var krakenAsset in AssetCodesKrakenData)
                            {
                                insertCommand.CommandText = "INSERT INTO TB_ASSET_CODES_KRAKEN_S (CODE, ASSET) VALUES (@CODE, @ASSET)";
                                insertCommand.Parameters.Clear();

                                AddParameterWithValue(insertCommand, "@CODE", (object?)krakenAsset.Code ?? DBNull.Value);
                                AddParameterWithValue(insertCommand, "@ASSET", (object?)krakenAsset.Asset ?? DBNull.Value);

                                try
                                {
                                    insertCommand.ExecuteNonQuery();
                                }
                                catch (Exception ex)
                                {
                                    lastError = "Failed to insert data." + Environment.NewLine + ex.Message;
                                }
                            }
                        }
                    });

                    if (lastError != null)
                    {
                        MessageBoxResult result = CustomMessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        ConsoleLog(_mainWindow.txtLog, $"[Kraken AssetCatalogData] {lastError}");
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
                        MessageBoxResult result = CustomMessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        ConsoleLog(_mainWindow.txtLog, $"[Kraken AssetCatalogData] {lastError}");

                        // Log each malconfigured asset
                        foreach (string code in malconfiguredAssets)
                        {
                            ConsoleLog(_mainWindow.txtLog, $"[Kraken AssetCatalogData] Malconfigured asset for code: {code}");
                        }
                    }
                }

                if (lastError == null)
                {
                    ConsoleLog(_mainWindow.txtLog, $"[Kraken AssetCatalogData] Saving successful");
                }
                else
                {
                    ConsoleLog(_mainWindow.txtLog, $"[Kraken AssetCatalogData] Saving unsuccessful");
                }
            }
            finally
            {
                UnblockUI();
            }
        }

        public void BindGrid()
        {
            // Clear existing data
            AssetCodesKrakenData?.Clear();

            if (AssetCodesKrakenData == null) return; // Add a null check

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
                selectCommand.CommandText = @"SELECT 
                                                  ledgers_kraken.ASSET AS CODE,
                                                  asset_codes.ASSET
                                              FROM 
                                                  (SELECT DISTINCT ASSET FROM TB_LEDGERS_KRAKEN_S) ledgers_kraken
                                                  LEFT OUTER JOIN TB_ASSET_CODES_KRAKEN_S asset_codes
                                                      ON ledgers_kraken.ASSET = asset_codes.CODE";

                using (DbDataReader reader = selectCommand.ExecuteReader())
                {
                    int dbLineNumber = 0;
                    while (reader.Read())
                    {
                        dbLineNumber++;

                        AssetCodesKrakenData.Add(new AssetCodesKrakenModel
                        {
                            Code = reader.GetStringOrEmpty(0),
                            Asset = reader.GetStringOrEmpty(1)
                        });
                    }
                }
            }

            dgKrakenAssets.ItemsSource = AssetCodesKrakenData;
        }
    }
}