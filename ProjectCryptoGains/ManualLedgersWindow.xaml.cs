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
    /// Interaction logic for ManualLedgersWindow.xaml
    /// </summary>

    public partial class ManualLedgersWindow : Window
    {
        private readonly MainWindow _mainWindow;

        private string filePath = "";

        public ManualLedgersWindow(MainWindow mainWindow)
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
            // Create a collection of ManualTransactionsModel objects
            ObservableCollection<ManualTransactionsModel> data = [];

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
            command.CommandText = "SELECT * FROM TB_LEDGERS_MANUAL_S";
            DbDataReader reader = command.ExecuteReader();

            int dbLineNumber = 0;
            while (reader.Read())
            {
                dbLineNumber++;

                data.Add(new ManualTransactionsModel
                {
                    RowNumber = dbLineNumber,
                    Refid = reader.IsDBNull(0) ? "" : reader.GetString(0),
                    Date = reader.IsDBNull(1) ? "" : reader.GetString(1),
                    Type = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    Exchange = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    Asset = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    Amount = ConvertStringToDecimal(reader.GetString(5)),
                    Fee = ConvertStringToDecimal(reader.GetString(6)),
                    Source = reader.IsDBNull(7) ? "" : reader.GetString(7),
                    Target = reader.IsDBNull(8) ? "" : reader.GetString(8),
                    Notes = reader.IsDBNull(9) ? "" : reader.GetString(9)
                });
            }
            reader.Close();
            connection.Close();

            dgLedgers.ItemsSource = data;
        }

        private void ButtonUpload_Click(object sender, RoutedEventArgs e)
        {
            string? lastError = null;

            ConsoleLog(_mainWindow.txtLog, $"[Manual Ledgers] Attempting to load {filePath}");

            btnUpload.IsEnabled = false;
            this.Cursor = Cursors.Wait;

            // Create a DataTable
            DataTable dataTable = new();

            // Check if the file exists
            if (!File.Exists(filePath))
            {
                lastError = "The file does not exist";
                MessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                ConsoleLog(_mainWindow.txtLog, $"[Manual Ledgers] {lastError}");

                btnUpload.IsEnabled = true;
                this.Cursor = Cursors.Arrow;

                // Exit function early
                return;
            }

            // Read the CSV file
            StreamReader? reader;
            try
            {
                reader = new(filePath);
            }
            catch (Exception ex)
            {
                // code to handle the exception
                lastError = "File could not be opened." + Environment.NewLine + ex.Message;
                MessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                ConsoleLog(_mainWindow.txtLog, $"[Manual Ledgers] {lastError}");
                ConsoleLog(_mainWindow.txtLog, $"[Manual Ledgers] Load unsuccessful");

                btnUpload.IsEnabled = true;
                this.Cursor = Cursors.Arrow;

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
                            lastError = "Unexpected inputfile header";
                            MessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            ConsoleLog(_mainWindow.txtLog, $"[Manual Ledgers] {lastError}");

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
                ConsoleLog(_mainWindow.txtLog, $"[Manual Ledgers] {lastError}");
                ConsoleLog(_mainWindow.txtLog, $"[Manual Ledgers] Load unsuccessful");

                btnUpload.IsEnabled = true;
                this.Cursor = Cursors.Arrow;

                // Exit function early
                return;
            }

            // Load the db table with data from the csv
            using (SqliteConnection connection = new(connectionString))
            {
                connection.Open();

                DbCommand commandDelete = connection.CreateCommand();

                // Truncate DB table
                commandDelete.CommandText = "DELETE FROM TB_LEDGERS_MANUAL_S";
                commandDelete.ExecuteNonQuery();

                // Initialize the transaction
                DbTransaction transaction = connection.BeginTransaction();

                // Counter to keep track of the number of rows inserted
                int insertCounter = 0;

                // Per row in the DataTable insert a row in the DB table
                foreach (DataRow row in dataTable.Rows)
                {
                    DbCommand commandInsert = connection.CreateCommand();

                    commandInsert.CommandText = "INSERT INTO TB_LEDGERS_MANUAL_S (REFID, DATE, TYPE, EXCHANGE, ASSET, AMOUNT, FEE, SOURCE, TARGET, NOTES) VALUES (@REFID, @DATE, @TYPE, @EXCHANGE, @ASSET, @AMOUNT, @FEE, @SOURCE, @TARGET, @NOTES)";
                    commandInsert.Prepare();

                    string value = "";
                    string columnName = "";
                    for (int i = 0; i < dataTable.Columns.Count; i++)
                    {
                        columnName = dataTable.Columns[i].ColumnName.Replace(' ', '_').ToUpper();
                        value = (string)(row.ItemArray[i] ?? "");
                        if (i != 3 && i != 7 && i != 8 && i != 9 && value == "")
                        {
                            lastError = "Insert row " + insertCounter + " failed: " + columnName + " cannot be null";
                            MessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            ConsoleLog(_mainWindow.txtLog, $"[Manual Ledgers] {lastError}");

                            btnUpload.IsEnabled = true;
                            this.Cursor = Cursors.Arrow;

                            // Exit function early
                            return;
                        }
                        if (columnName == "DATE" && !IsValidDateFormat(value, "yyyy-MM-dd HH:mm:ss"))
                        {
                            lastError = "Insert row " + insertCounter + " failed: " + columnName + " should be in yyyy-MM-dd HH:mm:ss format";
                            MessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            ConsoleLog(_mainWindow.txtLog, $"[Manual Ledgers] {lastError}");

                            btnUpload.IsEnabled = true;
                            this.Cursor = Cursors.Arrow;

                            // Exit function early
                            return;
                        }
                        AddParameterWithValue(commandInsert, "@" + columnName, value);
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
                List<string> missingAssets = MissingAssetManual(connection);

                if (missingAssets.Count > 0)
                {
                    lastError = "Missing asset(s) detected." + Environment.NewLine + "[Configure => Asset Catalog]";
                    MessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    ConsoleLog(_mainWindow.txtLog, $"[Manual Ledgers] {lastError}");

                    // Log each missing asset
                    foreach (string asset in missingAssets)
                    {
                        ConsoleLog(_mainWindow.txtLog, $"[Manual Ledgers] Missing asset: {asset}");
                    }
                }

                connection.Close();
            }
            BindGrid();

            btnUpload.IsEnabled = true;
            this.Cursor = Cursors.Arrow;

            if (lastError == null)
            {
                ConsoleLog(_mainWindow.txtLog, $"[Manual Ledgers] Load successful");
            }
            else
            {
                ConsoleLog(_mainWindow.txtLog, $"[Manual Ledgers] Load unsuccessful");
            }
        }

        private void ButtonBrowse_Click(object sender, RoutedEventArgs e)
        {
            FileDialog(txtFileName);
            filePath = txtFileName.Text;
        }
    }
}