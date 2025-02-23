using Microsoft.Data.Sqlite;
using ProjectCryptoGains.Common;
using System;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using static ProjectCryptoGains.Models;
using static ProjectCryptoGains.Common.Utility;

namespace ProjectCryptoGains
{
    /// <summary>
    /// Interaction logic for AssetCatalogWindow.xaml
    /// </summary>
    public partial class AssetCatalogWindow : SubwindowBase
    {
        private readonly MainWindow _mainWindow;
        public ObservableCollection<AssetsModel>? Assets { get; set; }
        public AssetCatalogWindow(MainWindow mainWindow)
        {
            InitializeComponent();

            // Capture drag on titlebar
            TitleBar.MouseLeftButtonDown += (sender, e) => DragMove();

            _mainWindow = mainWindow;
            Assets = [];

            BindGrid();
        }

        private void ButtonHelp_Click(object sender, RoutedEventArgs e)
        {
            OpenHelp("asset_catalog_help.html");
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

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            ConsoleLog(_mainWindow.txtLog, $"[Assets] Saving assets");

            int errors = 0;
            string? lastInfo = null;
            string? lastError = null;

            if (Assets == null || Assets.Count == 0)
            {
                lastInfo = "No data to save";
                ConsoleLog(_mainWindow.txtLog, $"[Assets] {lastInfo}");
                MessageBoxResult result = CustomMessageBox.Show(lastInfo, "Info", MessageBoxButton.OK, MessageBoxImage.Information);

                // Exit function early
                return;
            }

            BlockUI();

            await Task.Run(() =>
            {
                foreach (var asset in Assets)
                {
                    if (string.IsNullOrWhiteSpace(asset.Code) || string.IsNullOrWhiteSpace(asset.Asset))
                    {
                        errors += 1;
                    }
                }
            });

            if (errors > 0)
            {
                lastError = "Code and Asset cannot be empty";
                ConsoleLog(_mainWindow.txtLog, $"[Assets] {lastError}");
                MessageBoxResult result = CustomMessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                // Save assets to db
                await Task.Run(() =>
                {
                    using var connection = new SqliteConnection(connectionString);
                    connection.Open();
                    using var command = connection.CreateCommand();
                    command.CommandText = "DELETE FROM TB_ASSET_CATALOG_S";
                    command.ExecuteNonQuery();

                    foreach (var asset in Assets)
                    {
                        command.CommandText = "INSERT INTO TB_ASSET_CATALOG_S (Code, Asset) VALUES (@Code, @Asset)";
                        command.Parameters.Clear();

                        command.Parameters.AddWithValue("@Code", (object?)asset.Code ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Asset", (object?)asset.Asset ?? DBNull.Value);

                        try
                        {
                            command.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            if (ex.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase))
                            {
                                lastError = "Failed to insert data." + Environment.NewLine + "ASSET and CODE must be unique.";
                            }
                            else
                            {
                                lastError = "Failed to insert data." + Environment.NewLine + ex.Message;
                            }
                        }
                    }
                });

                if (lastError != null)
                {
                    MessageBoxResult result = CustomMessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    ConsoleLog(_mainWindow.txtLog, $"[Assets] {lastError}");
                }
            }

            if (lastError == null)
            {
                ConsoleLog(_mainWindow.txtLog, $"[Assets] Saving successful");
            }
            else
            {
                ConsoleLog(_mainWindow.txtLog, $"[Assets] Saving unsuccessful");
            }

            UnblockUI();
        }

        public void BindGrid()
        {
            // Clear existing data
            Assets?.Clear();

            if (Assets == null) return;

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
            command.CommandText = "SELECT * FROM TB_ASSET_CATALOG_S";
            DbDataReader reader = command.ExecuteReader();

            int dbLineNumber = 0;
            while (reader.Read())
            {
                dbLineNumber++;

                Assets.Add(new AssetsModel
                {
                    Code = reader.GetStringOrEmpty(0),
                    Asset = reader.GetStringOrEmpty(1)
                });
            }
            reader.Close();
            connection.Close();

            dgAssets.ItemsSource = Assets;
        }
    }
}