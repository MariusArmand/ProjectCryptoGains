using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
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
    /// Interaction logic for KrakenPairsWindow.xaml
    /// </summary>
    public partial class KrakenPairsWindow : Window
    {
        private readonly MainWindow _mainWindow;
        public ObservableCollection<KrakenPairsModel>? KrakenPairs { get; set; }
        public KrakenPairsWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
            KrakenPairs = [];

            BindGrid();
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Visibility = Visibility.Hidden;
        }

        private void RefreshFromSource_Click(object sender, RoutedEventArgs e)
        {
            ConsoleLog(_mainWindow.txtLog, $"[Kraken Pairs] Refreshing from source");
            BindGrid();
            ConsoleLog(_mainWindow.txtLog, $"[Kraken Pairs] Refresh done");
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            ConsoleLog(_mainWindow.txtLog, $"[Kraken Pairs] Saving pairs");

            int errors = 0;
            string? lastInfo = null;
            string? lastError = null;

            if (KrakenPairs == null || KrakenPairs.Count == 0)
            {
                lastInfo = "No data to save";
                ConsoleLog(_mainWindow.txtLog, $"[Kraken Pairs] {lastInfo}");
                MessageBox.Show(lastInfo, "Info", MessageBoxButton.OK, MessageBoxImage.Information);

                // Exit function early
                return;
            }

            btnSave.IsEnabled = false;
            this.Cursor = Cursors.Wait;

            await Task.Run(() =>
            {
                foreach (var pair in KrakenPairs)
                {
                    if (string.IsNullOrWhiteSpace(pair.Code) || string.IsNullOrWhiteSpace(pair.Asset_left) || string.IsNullOrWhiteSpace(pair.Asset_right))
                    {
                        errors += 1;
                    }
                }
            });

            if (errors > 0)
            {
                lastError = "CODE, ASSET_LEFT and ASSET_RIGHT cannot be empty";
                ConsoleLog(_mainWindow.txtLog, $"[Kraken Pairs] {lastError}");
                MessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                // Save assets to db
                await Task.Run(() =>
                {
                    using var connection = new SqliteConnection(connectionString);
                    connection.Open();
                    using var command = connection.CreateCommand();
                    command.CommandText = "DELETE FROM TB_PAIR_CODES_KRAKEN_S";
                    command.ExecuteNonQuery();

                    foreach (var krakenPair in KrakenPairs)
                    {
                        command.CommandText = "INSERT INTO TB_PAIR_CODES_KRAKEN_S (CODE, ASSET_LEFT, ASSET_RIGHT) VALUES (@Code, @Left, @Right)";
                        command.Parameters.Clear();

                        command.Parameters.AddWithValue("@Code", (object?)krakenPair.Code ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Left", (object?)krakenPair.Asset_left ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Right", (object?)krakenPair.Asset_right ?? DBNull.Value);

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

                if (lastError != null)
                {
                    MessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    ConsoleLog(_mainWindow.txtLog, $"[Kraken Pairs] {lastError}");
                }
            }

            // Check for malconfigured pairs
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                List<string> malfconfiguredPairs = MalconfiguredPair(connection);

                if (malfconfiguredPairs.Count > 0)
                {
                    lastError = "Malconfigured pair(s) detected";
                    MessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    ConsoleLog(_mainWindow.txtLog, $"[Kraken Pairs] {lastError}");

                    // Log each malconfigured pair
                    foreach (string pair in malfconfiguredPairs)
                    {
                        ConsoleLog(_mainWindow.txtLog, $"[Kraken Pairs] Malconfigured Pair: {pair}");
                    }
                }
            }

            if (lastError == null)
            {
                ConsoleLog(_mainWindow.txtLog, $"[Kraken Pairs] Saving successful");
            }
            else
            {
                ConsoleLog(_mainWindow.txtLog, $"[Kraken Pairs] Saving unsuccessful");
            }

            btnSave.IsEnabled = true;
            this.Cursor = Cursors.Arrow;
        }

        public void BindGrid()
        {
            // Clear existing data
            KrakenPairs?.Clear();

            if (KrakenPairs == null) return; // Add a null check

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
            command.CommandText = @"SELECT 
                                        trades.PAIR AS CODE, 
                                        pair_codes.ASSET_LEFT, 
                                        pair_codes.ASSET_RIGHT 
                                    FROM
                                        (SELECT DISTINCT PAIR FROM TB_TRADES_KRAKEN_S) trades
                                    LEFT OUTER JOIN 
                                        TB_PAIR_CODES_KRAKEN_S pair_codes
                                    ON 
                                        trades.PAIR = pair_codes.CODE
                                    ";
            DbDataReader reader = command.ExecuteReader();

            int dbLineNumber = 0;
            while (reader.Read())
            {
                dbLineNumber++;

                KrakenPairs.Add(new KrakenPairsModel
                {
                    Code = reader.IsDBNull(0) ? "" : reader.GetString(0),
                    Asset_left = reader.IsDBNull(1) ? "" : reader.GetString(1),
                    Asset_right = reader.IsDBNull(1) ? "" : reader.GetString(2)
                });
            }
            reader.Close();
            connection.Close();

            dgKrakenPairs.ItemsSource = KrakenPairs;
        }
    }
}