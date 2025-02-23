using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using static ProjectCryptoGains.Models;
using static ProjectCryptoGains.Common.Utility;
using ProjectCryptoGains.Common;

namespace ProjectCryptoGains
{
    /// <summary>
    /// Interaction logic for LedgersWindow.xaml
    /// </summary>
    public partial class KrakenLedgersWindow : Window
    {
        private readonly MainWindow _mainWindow;

        private string filePath = "";

        public KrakenLedgersWindow(MainWindow mainWindow)
        {
            InitializeComponent();

            // Capture drag on titlebar
            TitleBar.MouseLeftButtonDown += (sender, e) => DragMove();

            _mainWindow = mainWindow;

            BindGrid();
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

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Visibility = Visibility.Hidden;
        }

        private void ButtonHelp_Click(object sender, RoutedEventArgs e)
        {
            OpenHelp("kraken_ledgers_help.html");
        }

        private void BlockUI()
        {
            btnBrowse.IsEnabled = false;
            btnUpload.IsEnabled = false;

            Cursor = Cursors.Wait;
        }

        private void UnblockUI()
        {
            btnBrowse.IsEnabled = true;
            btnUpload.IsEnabled = true;

            Cursor = Cursors.Arrow;
        }

        private void BindGrid()
        {
            // Create a collection of KrakenLedgersModel objects
            ObservableCollection<KrakenLedgersModel> data = [];

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
            command.CommandText = "SELECT * FROM TB_LEDGERS_KRAKEN_S";
            DbDataReader reader = command.ExecuteReader();

            int dbLineNumber = 0;
            while (reader.Read())
            {
                dbLineNumber++;

                data.Add(new KrakenLedgersModel
                {
                    RowNumber = dbLineNumber,
                    Txid = reader.GetStringOrEmpty(0),
                    Refid = reader.GetStringOrEmpty(1),
                    Time = reader.GetStringOrEmpty(2),
                    Type = reader.GetStringOrEmpty(3),
                    Subtype = reader.GetStringOrEmpty(4),
                    Aclass = reader.GetStringOrEmpty(5),
                    Asset = reader.GetStringOrEmpty(6),
                    Wallet = reader.GetStringOrEmpty(7),
                    Amount = reader.GetDecimalOrDefault(8),
                    Fee = reader.GetDecimalOrDefault(9),
                    Balance = reader.GetDecimalOrDefault(10)
                });
            }
            reader.Close();
            connection.Close();

            dgLedgers.ItemsSource = data;
        }

        private void ButtonUpload_Click(object sender, RoutedEventArgs e)
        {
            string? lastWarning = null;
            string? lastError = null;

            ConsoleLog(_mainWindow.txtLog, $"[Kraken Ledgers] Attempting to load {filePath}");

            BlockUI();

            // Create a DataTable
            DataTable dataTable = new();

            // Check if the file exists
            if (!File.Exists(filePath))
            {
                lastError = "The file does not exist";
                MessageBoxResult result = CustomMessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                ConsoleLog(_mainWindow.txtLog, $"[Kraken Ledgers] {lastError}");
                ConsoleLog(_mainWindow.txtLog, $"[Kraken Ledgers] Load unsuccessful");

                UnblockUI();

                // Exit function early
                return;
            }

            // Read the CSV file
            using (StreamReader reader = new(filePath))
            {
                string csvLine;
                string pattern = ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)";

                int csvLineNumber = 0;
                try
                {
                    while (!reader.EndOfStream)
                    {
                        csvLine = reader.ReadLine() ?? "";

                        // Add rows to the DataTable
                        if (csvLineNumber == 0) // Add column headers to the DataTable
                        {
                            // Get the column names from the first line
                            string[] columnNames = Regex.Split(csvLine, pattern).Select(s => s.Trim('"')).ToArray();
                            string[] columnNamesExpected = ["txid", "refid", "time", "type", "subtype", "aclass", "asset", "wallet", "amount", "fee", "balance"];

                            if (!Enumerable.SequenceEqual(columnNames, columnNamesExpected))
                            {
                                lastError = "Unexpected inputfile header";
                                MessageBoxResult result = CustomMessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                ConsoleLog(_mainWindow.txtLog, $"[Kraken Ledgers] {lastError}");
                                ConsoleLog(_mainWindow.txtLog, $"[Kraken Ledgers] Load unsuccessful");

                                UnblockUI();

                                // Exit function early
                                return;
                            }

                            // Add the column names to the DataTable
                            foreach (string columnName in columnNames)
                            {
                                dataTable.Columns.Add(columnName);
                            }
                        }
                        else // Add the rest of the rows to the DataTable
                        {
                            string[] dataValues = Regex.Split(csvLine, pattern).Select(s => s.Trim('"')).ToArray();

                            dataTable.Rows.Add(dataValues);
                        }
                        csvLineNumber++;
                    }
                }
                catch (Exception ex)
                {
                    lastError = "File could not be parsed." + Environment.NewLine + ex.Message;
                    MessageBoxResult result = CustomMessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    ConsoleLog(_mainWindow.txtLog, $"[Kraken Ledgers] {lastError}");
                    ConsoleLog(_mainWindow.txtLog, $"[Kraken Ledgers] Load unsuccessful");

                    UnblockUI();

                    // Exit function early
                    return;
                }
            }

            // Load the db table with data from the csv
            using (SqliteConnection connection = new(connectionString))
            {
                connection.Open();

                DbCommand commandDelete = connection.CreateCommand();

                // Truncate db table
                commandDelete.CommandText = "DELETE FROM TB_LEDGERS_KRAKEN_S";
                commandDelete.ExecuteNonQuery();

                // Initialize the transaction
                DbTransaction transaction = connection.BeginTransaction();

                // Counter to keep track of the number of rows inserted
                int insertCounter = 0;

                // Per row in the DataTable, insert a row in the db table
                foreach (DataRow row in dataTable.Rows)
                {
                    DbCommand commandInsert = connection.CreateCommand();

                    commandInsert.CommandText = "INSERT INTO TB_LEDGERS_KRAKEN_S (TXID, REFID, TIME, TYPE, SUBTYPE, ACLASS, ASSET, WALLET, AMOUNT, FEE, BALANCE) VALUES (@TXID, @REFID, @TIME, @TYPE, @SUBTYPE, @ACLASS, @ASSET, @WALLET, @AMOUNT, @FEE, @BALANCE)";
                    commandInsert.Prepare();

                    for (int i = 0; i < dataTable.Columns.Count; i++)
                    {
                        AddParameterWithValue(commandInsert, "@" + dataTable.Columns[i].ColumnName.ToUpper(), row.ItemArray[i] ?? "");
                    }

                    commandInsert.ExecuteNonQuery();

                    insertCounter++;

                    // If the insertCounter is divisible by 10k, commit the transaction and start a new one
                    if (insertCounter % 10000 == 0)
                    {
                        transaction.Commit();
                        transaction = connection.BeginTransaction();
                    }
                }

                // Commit the transaction
                transaction.Commit();

                // Check for missing assets
                List<string> missingAssets = MissingAssets(connection);

                if (missingAssets.Count > 0)
                {
                    lastWarning = "There are new Kraken assets to be refreshed." + Environment.NewLine + "[Configure => Kraken Assets]";
                    MessageBoxResult result = CustomMessageBox.Show(lastWarning, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    ConsoleLog(_mainWindow.txtLog, $"[Kraken Ledgers] {lastWarning}");

                    // Log each missing asset
                    foreach (string code in missingAssets)
                    {
                        ConsoleLog(_mainWindow.txtLog, $"[Kraken Ledgers] Missing asset for code: {code}");
                    }
                }

                // Check for unsupported ledger types
                List<(string RefId, string Type)> unsupportedTypes = UnsupportedTypes(connection, LedgerSource.Kraken);

                if (unsupportedTypes.Count > 0)
                {
                    lastWarning = "Unsupported ledger type(s) detected." + Environment.NewLine + "Review csv; Unsupported ledger type(s) will not be taken into account";
                    MessageBoxResult result = CustomMessageBox.Show(lastWarning, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning, TextAlignment.Left);
                    ConsoleLog(_mainWindow.txtLog, $"[Manual Ledgers] {lastWarning}");

                    // Log each unsupported ledger type
                    foreach ((string RefId, string Type) in unsupportedTypes)
                    {
                        ConsoleLog(_mainWindow.txtLog, $"[Manual Ledgers] Unsupported ledger type:" + Environment.NewLine + $"REFID: {RefId}, TYPE: {Type}");
                    }
                }

                connection.Close();
            }
            BindGrid();

            UnblockUI();

            if (lastError == null)
            {
                if (lastWarning == null)
                {
                    ConsoleLog(_mainWindow.txtLog, $"[Kraken Ledgers] Load done");
                }
                else
                {
                    ConsoleLog(_mainWindow.txtLog, $"[Kraken Ledgers] Load done with warnings");
                }
            }
            else
            {
                ConsoleLog(_mainWindow.txtLog, $"[Kraken Ledgers] Load unsuccessful");
            }
        }

        private void ButtonBrowse_Click(object sender, RoutedEventArgs e)
        {
            FileDialog(txtFileName);
            filePath = txtFileName.Text;
        }
    }
}