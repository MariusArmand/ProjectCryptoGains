﻿using Microsoft.Data.Sqlite;
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
        public ObservableCollection<KrakenAssetsModel>? KrakenAssets { get; set; }

        public KrakenAssetsWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            TitleBarElement = TitleBar;

            _mainWindow = mainWindow;

            KrakenAssets = [];

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
            ConsoleLog(_mainWindow.txtLog, $"[Kraken Assets] Refreshing from source");
            BindGrid();
            ConsoleLog(_mainWindow.txtLog, $"[Kraken Assets] Refresh done");
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            ConsoleLog(_mainWindow.txtLog, $"[Kraken Assets] Saving assets");

            int errors = 0;
            string? lastInfo = null;
            string? lastError = null;

            if (KrakenAssets == null || KrakenAssets.Count == 0)
            {
                lastInfo = "No data to save";
                ConsoleLog(_mainWindow.txtLog, $"[Kraken Assets] {lastInfo}");
                MessageBoxResult result = CustomMessageBox.Show(lastInfo, "Info", MessageBoxButton.OK, MessageBoxImage.Information);

                // Exit function early
                return;
            }

            BlockUI();

            await Task.Run(() =>
            {
                foreach (var asset in KrakenAssets)
                {
                    if (string.IsNullOrWhiteSpace(asset.Code) || string.IsNullOrWhiteSpace(asset.Asset))
                    {
                        errors += 1;
                    }
                }
            });

            if (errors > 0)
            {
                lastError = "CODE and ASSET cannot be empty";
                ConsoleLog(_mainWindow.txtLog, $"[Kraken Assets] {lastError}");
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
                    command.CommandText = "DELETE FROM TB_ASSET_CODES_KRAKEN_S";
                    command.ExecuteNonQuery();

                    foreach (var krakenAsset in KrakenAssets)
                    {
                        command.CommandText = "INSERT INTO TB_ASSET_CODES_KRAKEN_S (CODE, ASSET) VALUES (@CODE, @ASSET)";
                        command.Parameters.Clear();

                        command.Parameters.AddWithValue("@CODE", (object?)krakenAsset.Code ?? DBNull.Value);
                        command.Parameters.AddWithValue("@ASSET", (object?)krakenAsset.Asset ?? DBNull.Value);

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
                    MessageBoxResult result = CustomMessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    ConsoleLog(_mainWindow.txtLog, $"[Kraken Assets] {lastError}");
                }
            }

            // Check for malconfigured assets
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                List<string> malconfiguredAssets = MalconfiguredAssets(connection);

                if (malconfiguredAssets.Count > 0)
                {
                    lastError = "Malconfigured asset(s) detected";
                    MessageBoxResult result = CustomMessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    ConsoleLog(_mainWindow.txtLog, $"[Kraken Assets] {lastError}");

                    // Log each malconfigured asset
                    foreach (string code in malconfiguredAssets)
                    {
                        ConsoleLog(_mainWindow.txtLog, $"[Kraken Assets] Malconfigured asset for code: {code}");
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

            UnblockUI();
        }

        public void BindGrid()
        {
            // Clear existing data
            KrakenAssets?.Clear();

            if (KrakenAssets == null) return; // Add a null check

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
            command.CommandText = @"SELECT 
                                        ledgers_kraken.ASSET AS CODE,
                                        asset_codes.ASSET
                                    FROM 
                                        (SELECT DISTINCT ASSET FROM TB_LEDGERS_KRAKEN_S) ledgers_kraken
                                        LEFT OUTER JOIN TB_ASSET_CODES_KRAKEN_S asset_codes
                                            ON ledgers_kraken.ASSET = asset_codes.CODE";

            DbDataReader reader = command.ExecuteReader();

            int dbLineNumber = 0;
            while (reader.Read())
            {
                dbLineNumber++;

                KrakenAssets.Add(new KrakenAssetsModel
                {
                    Code = reader.GetStringOrEmpty(0),
                    Asset = reader.GetStringOrEmpty(1)
                });
            }
            reader.Close();
            connection.Close();

            dgKrakenAssets.ItemsSource = KrakenAssets;
        }
    }
}