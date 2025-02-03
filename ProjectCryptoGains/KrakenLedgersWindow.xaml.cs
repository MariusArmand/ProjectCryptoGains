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
using static ProjectCryptoGains.Utility;

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
            BindGrid();
            _mainWindow = mainWindow;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Visibility = Visibility.Hidden;
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
                MessageBox.Show("Database could not be opened." + Environment.NewLine + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                btnUpload.IsEnabled = true;
                this.Cursor = Cursors.Arrow;

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
                    Txid = reader.IsDBNull(0) ? "" : reader.GetString(0),
                    Refid = reader.IsDBNull(1) ? "" : reader.GetString(1),
                    Time = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    Type = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    Subtype = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    Aclass = reader.IsDBNull(5) ? "" : reader.GetString(5),
                    Asset = reader.IsDBNull(6) ? "" : reader.GetString(6),
                    Wallet = reader.IsDBNull(7) ? "" : reader.GetString(7),
                    Amount = ConvertStringToDecimal(reader.GetString(8)),
                    Fee = ConvertStringToDecimal(reader.GetString(9)),
                    Balance = ConvertStringToDecimal(reader.GetString(10))
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

            btnUpload.IsEnabled = false;
            this.Cursor = Cursors.Wait;

            // Create a DataTable
            DataTable dataTable = new();

            // Check if the file exists
            if (!File.Exists(filePath))
            {
                lastError = "The file does not exist";
                MessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                ConsoleLog(_mainWindow.txtLog, $"[Kraken Ledgers] {lastError}");
                ConsoleLog(_mainWindow.txtLog, $"[Kraken Ledgers] Load unsuccessful");

                btnUpload.IsEnabled = true;
                this.Cursor = Cursors.Arrow;

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
                                MessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                ConsoleLog(_mainWindow.txtLog, $"[Kraken Ledgers] {lastError}");

                                btnUpload.IsEnabled = true;
                                this.Cursor = Cursors.Arrow;

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
                    MessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    ConsoleLog(_mainWindow.txtLog, $"[Kraken Ledgers] {lastError}");
                    ConsoleLog(_mainWindow.txtLog, $"[Kraken Ledgers] Load unsuccessful");

                    btnUpload.IsEnabled = true;
                    this.Cursor = Cursors.Arrow;

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
                List<string> missingAssets = MissingAsset(connection);

                if (missingAssets.Count > 0)
                {
                    lastWarning = "There are new kraken assets to be refreshed." + Environment.NewLine + "[Configure => Kraken Assets]";
                    MessageBox.Show(lastWarning, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    ConsoleLog(_mainWindow.txtLog, $"[Kraken Ledgers] {lastWarning}");

                    // Log each missing asset
                    foreach (string asset in missingAssets)
                    {
                        ConsoleLog(_mainWindow.txtLog, $"[Kraken Ledgers] Missing asset: {asset}");
                    }
                }

                connection.Close();
            }
            BindGrid();

            btnUpload.IsEnabled = true;
            this.Cursor = Cursors.Arrow;

            if (lastError == null)
            {
                ConsoleLog(_mainWindow.txtLog, $"[Kraken Ledgers] Load successful");
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