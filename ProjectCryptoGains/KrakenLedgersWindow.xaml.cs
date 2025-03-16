using FirebirdSql.Data.FirebirdClient;
using Microsoft.Win32;
using ProjectCryptoGains.Common;
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
using static ProjectCryptoGains.Common.Utils.DatabaseUtils;
using static ProjectCryptoGains.Common.Utils.DateUtils;
using static ProjectCryptoGains.Common.Utils.ReaderUtils;
using static ProjectCryptoGains.Common.Utils.Utils;
using static ProjectCryptoGains.Common.Utils.ValidationUtils;
using static ProjectCryptoGains.Common.Utils.WindowUtils;
using static ProjectCryptoGains.Models;

namespace ProjectCryptoGains
{
    /// <summary>
    /// Interaction logic for LedgersWindow.xaml
    /// </summary>
    public partial class KrakenLedgersWindow : SubwindowBase
    {
        private readonly MainWindow _mainWindow;

        public KrakenLedgersWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            TitleBarElement = TitleBar;

            _mainWindow = mainWindow;

            BindGrid();
        }

        private void BlockUI()
        {
            btnImport.IsEnabled = false;

            Cursor = Cursors.Wait;
        }

        private void UnblockUI()
        {
            btnImport.IsEnabled = true;

            Cursor = Cursors.Arrow;
        }

        private void BindGrid()
        {
            // Create a collection of LedgersKrakenModel objects
            ObservableCollection<LedgersKrakenModel> ledgersKrakenData = [];

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
                selectCommand.CommandText = "SELECT * FROM TB_LEDGERS_KRAKEN";

                using (DbDataReader reader = selectCommand.ExecuteReader())
                {
                    int dbLineNumber = 0;
                    while (reader.Read())
                    {
                        dbLineNumber++;

                        ledgersKrakenData.Add(new LedgersKrakenModel
                        {
                            Row_number = dbLineNumber,
                            Txid = reader.GetStringOrEmpty(0),
                            Refid = reader.GetStringOrEmpty(1),
                            Time = reader.GetDateTime(2),
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
                }

                dgLedgers.ItemsSource = ledgersKrakenData;
            }
        }

        private void BtnHelp_Click(object sender, RoutedEventArgs e)
        {
            OpenHelp("kraken_ledgers_help.html");
        }

        private void BtnImport_Click(object sender, RoutedEventArgs e)
        {
            string? lastWarning = null;
            string? lastError = null;

            ConsoleLog(_mainWindow.txtLog, $"[Kraken Ledgers] Attempting to import Kraken ledgers");

            // Open file dialog
            OpenFileDialog openFileDlg = new OpenFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                Title = "Import Kraken Ledgers"
            };

            bool? openFiledlgResult = openFileDlg.ShowDialog();
            if (openFiledlgResult != true)
            {
                ConsoleLog(_mainWindow.txtLog, "[Kraken Ledgers] Import cancelled");
                return;
            }

            string filePath = openFileDlg.FileName;

            BlockUI();

            try
            {
                // Create a DataTable
                DataTable dataTable = new();

                // Check if the file exists
                if (!File.Exists(filePath))
                {
                    lastError = "The file does not exist.";
                    CustomMessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    ConsoleLog(_mainWindow.txtLog, $"[Kraken Ledgers] {lastError}");
                    ConsoleLog(_mainWindow.txtLog, $"[Kraken Ledgers] Import unsuccessful");

                    // Exit function early
                    return;
                }

                // Read the CSV file
                using (StreamReader reader = new(filePath))
                {
                    ConsoleLog(_mainWindow.txtLog, $"[Kraken Ledgers] Importing {filePath}");

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
                                    lastError = "Unexpected inputfile header.";
                                    CustomMessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                    ConsoleLog(_mainWindow.txtLog, $"[Kraken Ledgers] {lastError}");

                                    // Exit loop early
                                    break;
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
                        lastError = "File could not be parsed" + Environment.NewLine + ex.Message;
                        CustomMessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        ConsoleLog(_mainWindow.txtLog, $"[Kraken Ledgers] {lastError}");
                    }
                }

                if (lastError == null)
                {
                    // Load the db table with ledgersKrakenData from the csv
                    using (FbConnection connection = new(connectionString))
                    {
                        connection.Open();

                        using DbCommand deleteCommand = connection.CreateCommand();

                        // Truncate DB table
                        deleteCommand.CommandText = "DELETE FROM TB_LEDGERS_KRAKEN";
                        deleteCommand.ExecuteNonQuery();

                        // Initialize the transaction
                        DbTransaction transaction = connection.BeginTransaction();

                        // Counter to keep track of the number of rows inserted
                        int insertCounter = 0;

                        // Create and prepare the command
                        using DbCommand insertCommand = connection.CreateCommand();
                        insertCommand.Transaction = transaction;
                        insertCommand.CommandText = @"INSERT INTO TB_LEDGERS_KRAKEN (
                                                          TXID,
                                                          REFID,
                                                          ""TIME"",
                                                          TYPE,
                                                          SUBTYPE,
                                                          ACLASS,
                                                          ASSET,
                                                          WALLET,
                                                          AMOUNT,
                                                          FEE,
                                                          BALANCE
                                                      ) VALUES (
                                                          @TXID,
                                                          @REFID,
                                                          @TIME,
                                                          @TYPE,
                                                          @SUBTYPE,
                                                          @ACLASS,
                                                          @ASSET,
                                                          @WALLET,
                                                          ROUND(@AMOUNT, 10),
                                                          ROUND(@FEE, 10),
                                                          ROUND(@BALANCE, 10)
                                                      )";

                        insertCommand.Prepare();

                        // Per row in the DataTable insert a row in the DB table
                        foreach (DataRow row in dataTable.Rows)
                        {
                            insertCommand.Parameters.Clear(); // Reset parameters for the next row

                            string value = "";
                            string columnName = "";

                            for (int i = 0; i < dataTable.Columns.Count; i++)
                            {
                                columnName = dataTable.Columns[i].ColumnName.Replace(' ', '_').ToUpper();
                                value = (string)(row.ItemArray[i] ?? "");

                                if (columnName == "TIME")
                                {
                                    AddParameterWithValue(insertCommand, "@" + columnName, ConvertStringToIsoDateTime(value));
                                }
                                else if (columnName == "AMOUNT" || columnName == "FEE" || columnName == "BALANCE")
                                {
                                    AddParameterWithValue(insertCommand, "@" + columnName, ConvertStringToDecimal(value));
                                }
                                else
                                {
                                    AddParameterWithValue(insertCommand, "@" + columnName, value);
                                }
                            }

                            if (lastError != null)
                            {
                                transaction.Rollback();
                                break; // Exit loop, but continue to the final logging
                            }

                            try
                            {
                                insertCommand.ExecuteNonQuery();
                            }
                            catch (Exception ex)
                            {
                                lastError = $"Insert row {insertCounter} failed: {ex.Message}";
                                CustomMessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                ConsoleLog(_mainWindow.txtLog, $"[Kraken Ledgers] {lastError}");
                                transaction.Rollback();
                                break;
                            }

                            insertCounter++;

                            // If the insertCounter is divisible by 10k, commit the transaction and start a new one
                            if (insertCounter % 10000 == 0)
                            {
                                transaction.Commit();
                                transaction = connection.BeginTransaction();
                                insertCommand.Transaction = transaction;
                            }
                        }

                        // Commit any remaining inserts if no errors occurred and perform checks
                        if (lastError == null)
                        {
                            transaction.Commit();

                            // Check for missing assets
                            List<string> missingAssets = MissingAssets(connection);

                            if (missingAssets.Count > 0)
                            {
                                lastWarning = "There are new Kraken assets to be refreshed." + Environment.NewLine + "[Configure => Kraken Assets]";
                                CustomMessageBox.Show(lastWarning, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                                lastWarning = "Unsupported ledger type(s) detected." + Environment.NewLine + "Review csv; Unsupported ledger type(s) will not be taken into account.";
                                CustomMessageBox.Show(lastWarning, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning, TextAlignment.Left);
                                ConsoleLog(_mainWindow.txtLog, $"[Kraken Ledgers] {lastWarning}");

                                // Log each unsupported ledger type
                                foreach ((string RefId, string Type) in unsupportedTypes)
                                {
                                    ConsoleLog(_mainWindow.txtLog, $"[Kraken Ledgers] Unsupported ledger type:" + Environment.NewLine + $"REFID: {RefId}, TYPE: {Type}");
                                }
                            }
                        }
                    }
                }

                BindGrid();

                if (lastError == null)
                {
                    if (lastWarning == null)
                    {
                        ConsoleLog(_mainWindow.txtLog, $"[Kraken Ledgers] Import done");
                    }
                    else
                    {
                        ConsoleLog(_mainWindow.txtLog, $"[Kraken Ledgers] Import done with warnings");
                    }
                }
                else
                {
                    ConsoleLog(_mainWindow.txtLog, $"[Kraken Ledgers] Import unsuccessful");
                }
            }
            finally
            {
                UnblockUI();
            }
        }
    }
}