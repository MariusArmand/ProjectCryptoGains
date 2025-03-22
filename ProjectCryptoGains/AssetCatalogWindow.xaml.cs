using FirebirdSql.Data.FirebirdClient;
using ProjectCryptoGains.Common;
using System;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using static ProjectCryptoGains.Common.Utils.DatabaseUtils;
using static ProjectCryptoGains.Common.Utils.ReaderUtils;
using static ProjectCryptoGains.Common.Utils.Utils;
using static ProjectCryptoGains.Common.Utils.WindowUtils;
using static ProjectCryptoGains.Models;

namespace ProjectCryptoGains
{
    /// <summary>
    /// Interaction logic for AssetCatalogWindow.xaml
    /// </summary>
    public partial class AssetCatalogWindow : SubwindowBase
    {
        private readonly MainWindow _mainWindow;
        public ObservableCollection<AssetCatalogModel> AssetCatalogData { get; set; }
        public AssetCatalogWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            TitleBarElement = TitleBar;

            _mainWindow = mainWindow;
            AssetCatalogData = [];

            BindGrid();
        }

        private void BlockUI()
        {
            btnSave.IsEnabled = false;
            Cursor = Cursors.Wait;
        }

        private void UnblockUI()
        {
            btnSave.IsEnabled = true;
            Cursor = Cursors.Arrow;
        }

        public void BindGrid()
        {
            // Clear existing data
            AssetCatalogData?.Clear();

            if (AssetCatalogData == null) return;

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
                selectCommand.CommandText = "SELECT * FROM TB_ASSET_CATALOG";

                using (DbDataReader reader = selectCommand.ExecuteReader())
                {
                    int dbLineNumber = 0;
                    while (reader.Read())
                    {
                        dbLineNumber++;

                        AssetCatalogData.Add(new AssetCatalogModel
                        {
                            Asset = reader.GetStringOrEmpty(0),
                            Label = reader.GetStringOrEmpty(1)
                        });
                    }
                }
            }

            dgAssets.ItemsSource = AssetCatalogData;
        }

        private void BtnHelp_Click(object sender, RoutedEventArgs e)
        {
            OpenHelp("asset_catalog_help.html");
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            ConsoleLog(_mainWindow.txtLog, $"[Asset Catalog] Saving assets");

            int errors = 0;
            string? lastInfo = null;
            string? lastError = null;

            if (AssetCatalogData == null || AssetCatalogData.Count == 0)
            {
                lastInfo = "No data to save.";
                ConsoleLog(_mainWindow.txtLog, $"[Asset Catalog] {lastInfo}");
                CustomMessageBox.Show(lastInfo, "Info", MessageBoxButton.OK, MessageBoxImage.Information);

                // Exit function early
                return;
            }

            BlockUI();

            try
            {
                await Task.Run(() =>
                {
                    foreach (var asset in AssetCatalogData)
                    {
                        if (string.IsNullOrWhiteSpace(asset.Asset) || string.IsNullOrWhiteSpace(asset.Label))
                        {
                            errors += 1;
                        }
                    }
                });

                if (errors > 0)
                {
                    lastError = "Asset and label cannot be empty.";
                    ConsoleLog(_mainWindow.txtLog, $"[Asset Catalog] {lastError}");
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
                            deleteCommand.CommandText = "DELETE FROM TB_ASSET_CATALOG";
                            deleteCommand.ExecuteNonQuery();

                            using DbCommand insertCommand = connection.CreateCommand();
                            foreach (var asset in AssetCatalogData)
                            {
                                insertCommand.CommandText = "INSERT INTO TB_ASSET_CATALOG (ASSET, LABEL) VALUES (@ASSET, @LABEL)";
                                insertCommand.Parameters.Clear();

                                AddParameterWithValue(insertCommand, "@ASSET", (object?)asset.Asset ?? DBNull.Value);
                                AddParameterWithValue(insertCommand, "@LABEL", (object?)asset.Label ?? DBNull.Value);

                                try
                                {
                                    insertCommand.ExecuteNonQuery();
                                }
                                catch (Exception ex)
                                {
                                    if (ex.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase))
                                    {
                                        lastError = "Failed to insert data." + Environment.NewLine + "ASSET and LABEL must be unique.";
                                    }
                                    else
                                    {
                                        lastError = "Failed to insert data." + Environment.NewLine + ex.Message;
                                    }
                                }
                            }
                        }
                    });

                    if (lastError != null)
                    {
                        CustomMessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        ConsoleLog(_mainWindow.txtLog, $"[Asset Catalog] {lastError}");
                    }
                }

                if (lastError == null)
                {
                    ConsoleLog(_mainWindow.txtLog, $"[Asset Catalog] Saving successful");
                }
                else
                {
                    ConsoleLog(_mainWindow.txtLog, $"[Asset Catalog] Saving unsuccessful");
                }
            }
            finally
            {
                UnblockUI();
            }
        }
    }
}