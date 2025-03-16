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
    /// Interaction logic for ManualLedgersWindow.xaml
    /// </summary>
    public partial class ManualLedgersWindow : SubwindowBase
    {
        private readonly MainWindow _mainWindow;

        public ManualLedgersWindow(MainWindow mainWindow)
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
            // Create a collection of LedgersManualModel objects
            ObservableCollection<LedgersManualModel> LedgersManualData = [];

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
                selectCommand.CommandText = "SELECT * FROM TB_LEDGERS_MANUAL";

                using (DbDataReader reader = selectCommand.ExecuteReader())
                {
                    int dbLineNumber = 0;
                    while (reader.Read())
                    {
                        dbLineNumber++;

                        LedgersManualData.Add(new LedgersManualModel
                        {
                            Row_number = dbLineNumber,
                            Refid = reader.GetStringOrEmpty(0),
                            Date = reader.GetDateTime(1),
                            Type = reader.GetStringOrEmpty(2),
                            Exchange = reader.GetStringOrEmpty(3),
                            Asset = reader.GetStringOrEmpty(4),
                            Amount = reader.GetDecimalOrDefault(5),
                            Fee = reader.GetDecimalOrDefault(6),
                            Source = reader.GetStringOrEmpty(7),
                            Target = reader.GetStringOrEmpty(8),
                            Notes = reader.GetStringOrEmpty(9)
                        });
                    }
                }

                dgLedgers.ItemsSource = LedgersManualData;
            }
        }

        private void BtnHelp_Click(object sender, RoutedEventArgs e)
        {
            OpenHelp("manual_ledgers_help.html");
        }

        private void BtnImport_Click(object sender, RoutedEventArgs e)
        {
            string? lastWarning = null;
            string? lastError = null;

            ConsoleLog(_mainWindow.txtLog, $"[Manual Ledgers] Attempting to import manual ledgers");

            // Open file dialog
            OpenFileDialog openFileDlg = new OpenFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                Title = "Import Manual Ledgers"
            };

            bool? openFiledlgResult = openFileDlg.ShowDialog();
            if (openFiledlgResult != true)
            {
                ConsoleLog(_mainWindow.txtLog, "[Manual Ledgers] Import cancelled");
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
                    ConsoleLog(_mainWindow.txtLog, $"[Manual Ledgers] {lastError}");
                    ConsoleLog(_mainWindow.txtLog, $"[Manual Ledgers] Import unsuccessful");

                    // Exit function early
                    return;
                }

                // Read the CSV file
                StreamReader? reader;
                try
                {
                    reader = new(filePath);
                    ConsoleLog(_mainWindow.txtLog, $"[Manual Ledgers] Importing {filePath}");
                }
                catch (Exception ex)
                {
                    lastError = "File could not be opened" + Environment.NewLine + ex.Message;
                    CustomMessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    ConsoleLog(_mainWindow.txtLog, $"[Manual Ledgers] {lastError}");
                    ConsoleLog(_mainWindow.txtLog, $"[Manual Ledgers] Import unsuccessful");

                    // Exit function early
                    return;
                }

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
                            string[] columnNamesExpected = ["refid", "date", "type", "exchange", "asset", "amount", "fee", "source", "target", "notes"];

                            if (!Enumerable.SequenceEqual(columnNames, columnNamesExpected))
                            {
                                lastError = "Unexpected inputfile header.";
                                CustomMessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                ConsoleLog(_mainWindow.txtLog, $"[Manual Ledgers] {lastError}");

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
                    ConsoleLog(_mainWindow.txtLog, $"[Manual Ledgers] {lastError}");
                }

                if (lastError == null)
                {
                    // Load the db table with LedgersManualData from the csv
                    using (FbConnection connection = new(connectionString))
                    {
                        connection.Open();

                        using DbCommand deleteCommand = connection.CreateCommand();

                        // Truncate DB table
                        deleteCommand.CommandText = "DELETE FROM TB_LEDGERS_MANUAL";
                        deleteCommand.ExecuteNonQuery();

                        // Initialize the transaction
                        DbTransaction transaction = connection.BeginTransaction();

                        // Counter to keep track of the number of rows inserted
                        int insertCounter = 0;

                        // Create and prepare the command
                        using DbCommand insertCommand = connection.CreateCommand();
                        insertCommand.Transaction = transaction;
                        insertCommand.CommandText = @"INSERT INTO TB_LEDGERS_MANUAL (
                                                      REFID,
                                                      ""DATE"",
                                                      TYPE,
                                                      EXCHANGE,
                                                      ASSET,
                                                      AMOUNT,
                                                      FEE,
                                                      SOURCE,
                                                      TARGET,
                                                      NOTES
                                                  ) VALUES (
                                                      @REFID,
                                                      @DATE,
                                                      @TYPE,
                                                      @EXCHANGE,
                                                      @ASSET,
                                                      ROUND(@AMOUNT, 10),
                                                      ROUND(@FEE, 10),
                                                      @SOURCE,
                                                      @TARGET,
                                                      @NOTES
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

                                if (i != 3 && i != 7 && i != 8 && i != 9 && value == "")
                                {
                                    lastError = "Insert row " + insertCounter + " failed: " + columnName + " cannot be null.";
                                    CustomMessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                    ConsoleLog(_mainWindow.txtLog, $"[Manual Ledgers] {lastError}");

                                    // Exit loop early
                                    break;
                                }

                                if (columnName == "DATE" && !IsValidDateFormat(value, "yyyy-MM-dd HH:mm:ss"))
                                {
                                    lastError = "Insert row " + insertCounter + " failed: " + columnName + " should be in yyyy-MM-dd HH:mm:ss format.";
                                    CustomMessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                    ConsoleLog(_mainWindow.txtLog, $"[Manual Ledgers] {lastError}");

                                    // Exit loop early
                                    break;
                                }

                                if (columnName == "DATE")
                                {
                                    AddParameterWithValue(insertCommand, "@" + columnName, ConvertStringToIsoDateTime(value));
                                }
                                else if (columnName == "AMOUNT" || columnName == "FEE")
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
                                ConsoleLog(_mainWindow.txtLog, $"[Manual Ledgers] {lastError}");
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

                        // Commit any remaining upserts if no errors occurred and perform checks
                        if (lastError == null)
                        {
                            transaction.Commit();

                            // Check for missing assets
                            List<string> missingAssets = MissingAssetsManual(connection);

                            if (missingAssets.Count > 0)
                            {
                                lastWarning = "Missing asset(s) detected." + Environment.NewLine + "[Configure => Asset Catalog]";
                                CustomMessageBox.Show(lastWarning, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                                ConsoleLog(_mainWindow.txtLog, $"[Manual Ledgers] {lastWarning}");

                                // Log each missing asset
                                foreach (string asset in missingAssets)
                                {
                                    ConsoleLog(_mainWindow.txtLog, $"[Manual Ledgers] Missing asset: {asset}");
                                }
                            }

                            // Check for unsupported ledger types
                            List<(string RefId, string Type)> unsupportedTypes = UnsupportedTypes(connection, LedgerSource.Manual);

                            if (unsupportedTypes.Count > 0)
                            {
                                lastWarning = "Unsupported ledger type(s) detected." + Environment.NewLine + "Review csv; Unsupported ledger type(s) will not be taken into account.";
                                CustomMessageBox.Show(lastWarning, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning, TextAlignment.Left);
                                ConsoleLog(_mainWindow.txtLog, $"[Manual Ledgers] {lastWarning}");

                                // Log each unsupported ledger type
                                foreach ((string RefId, string Type) in unsupportedTypes)
                                {
                                    ConsoleLog(_mainWindow.txtLog, $"[Manual Ledgers] Unsupported ledger type:" + Environment.NewLine + $"REFID: {RefId}, TYPE: {Type}");
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
                        ConsoleLog(_mainWindow.txtLog, $"[Manual Ledgers] Import done");
                    }
                    else
                    {
                        ConsoleLog(_mainWindow.txtLog, $"[Manual Ledgers] Import done with warnings");
                    }
                }
                else
                {
                    ConsoleLog(_mainWindow.txtLog, $"[Manual Ledgers] Import unsuccessful");
                }
            }
            finally
            {
                UnblockUI();
            }
        }
    }
}