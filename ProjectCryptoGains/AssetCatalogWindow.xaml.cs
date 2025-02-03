using Microsoft.Data.Sqlite;
using System;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using static ProjectCryptoGains.Models;
using static ProjectCryptoGains.Utility;

namespace ProjectCryptoGains
{
    /// <summary>
    /// Interaction logic for AssetCatalogWindow.xaml
    /// </summary>
    public partial class AssetCatalogWindow : Window
    {
        private readonly MainWindow _mainWindow;
        public ObservableCollection<AssetsModel>? Assets { get; set; }
        public AssetCatalogWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
            Assets = [];
            BindGrid();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Visibility = Visibility.Hidden;
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            if (Assets == null || Assets.Count == 0)
            {
                MessageBox.Show("No data to save", "Info", MessageBoxButton.OK, MessageBoxImage.Information);

                // Exit function early
                return;
            }

            ConsoleLog(_mainWindow.txtLog, $"[Assets] Saving assets");

            btnSave.IsEnabled = false;
            this.Cursor = Cursors.Wait;

            int errors = 0;
            string? lastError = null;

            await Task.Run(() =>
            {
                foreach (var asset in Assets)
                {
                    if (string.IsNullOrWhiteSpace(asset.Code))
                    {
                        errors += 1;
                    }
                }
            });

            if (errors > 0)
            {
                btnSave.IsEnabled = true;
                this.Cursor = Cursors.Arrow;

                lastError = "Code cannot be empty";
                ConsoleLog(_mainWindow.txtLog, $"[Assets] {lastError}");
                MessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                // Exit function early
                return;
            }

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
                        lastError = "Failed to insert data." + Environment.NewLine + ex.Message;
                    }
                }
            });

            if (lastError == null)
            {
                ConsoleLog(_mainWindow.txtLog, $"[Assets] Saving successful");
            }
            else
            {
                MessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                ConsoleLog(_mainWindow.txtLog, $"[Assets] {lastError}");
                ConsoleLog(_mainWindow.txtLog, $"[Assets] Saving unsuccessful");
            }

            btnSave.IsEnabled = true;
            this.Cursor = Cursors.Arrow;
        }

        public void BindGrid()
        {
            /// Fill the datagrid with data from the database

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
                MessageBox.Show("Database could not be opened." + Environment.NewLine + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Cursor = Cursors.Arrow;

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
                    Code = reader.IsDBNull(0) ? "" : reader.GetString(0),
                    Asset = reader.IsDBNull(1) ? "" : reader.GetString(1)
                });
            }
            reader.Close();
            connection.Close();

            dgAssets.ItemsSource = Assets;
        }
    }
}