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
    // OBSOLETE WINDOW

    /// <summary>
    /// Interaction logic for KrakenTradesWindow.xaml
    /// </summary>
    public partial class KrakenTradesWindow : Window
    {
        private readonly MainWindow _mainWindow;

        private string filePath = "";

        public KrakenTradesWindow(MainWindow mainWindow)
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

        private void ButtonHelp_Click(object sender, RoutedEventArgs e)
        {
            OpenHelp("kraken_trades_help.html");
        }

        private void BindGrid()
        {
            // Create a collection of KrakenTradesModel objects
            ObservableCollection<KrakenTradesModel> data = [];

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
            command.CommandText = "SELECT * FROM TB_TRADES_KRAKEN_S";
            DbDataReader reader = command.ExecuteReader();

            int dbLineNumber = 0;
            while (reader.Read())
            {
                dbLineNumber++;

                data.Add(new KrakenTradesModel
                {
                    RowNumber = dbLineNumber,
                    Txid = reader.IsDBNull(0) ? "" : reader.GetString(0),
                    Ordertxid = reader.IsDBNull(1) ? "" : reader.GetString(1),
                    Pair = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    Time = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    Type = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    Ordertype = reader.IsDBNull(5) ? "" : reader.GetString(5),
                    Price = ConvertStringToDecimal(reader.GetString(6)),
                    Cost = ConvertStringToDecimal(reader.GetString(7)),
                    Fee = ConvertStringToDecimal(reader.GetString(8)),
                    Vol = ConvertStringToDecimal(reader.GetString(9)),
                    Margin = ConvertStringToDecimal(reader.GetString(10)),
                    Misc = reader.IsDBNull(11) ? "" : reader.GetString(11),
                    Ledgers = reader.IsDBNull(12) ? "" : reader.GetString(12),
                });
            }
            reader.Close();
            connection.Close();

            dgTrades.ItemsSource = data;
        }

        private void ButtonUpload_Click(object sender, RoutedEventArgs e)
        {
            string? lastWarning = null;
            string? lastError = null;

            ConsoleLog(_mainWindow.txtLog, $"[Kraken Trades] Attempting to load {filePath}");

            btnUpload.IsEnabled = false;
            this.Cursor = Cursors.Wait;

            // Create a DataTable
            DataTable dataTable = new();

            // Check if the file exists
            if (!File.Exists(filePath))
            {
                lastError = "The file does not exist";
                MessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                ConsoleLog(_mainWindow.txtLog, $"[Kraken Trades] {lastError}");
                ConsoleLog(_mainWindow.txtLog, $"[Kraken Trades] Load unsuccessful");

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
                            string[] columnNamesExpected = ["txid", "ordertxid", "pair", "time", "type", "ordertype", "price", "cost", "fee", "vol", "margin", "misc", "ledgers"];

                            if (!Enumerable.SequenceEqual(columnNames, columnNamesExpected))
                            {
                                lastError = "Unexpected inputfile header";
                                MessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                ConsoleLog(_mainWindow.txtLog, $"[Kraken Trades] {lastError}");

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
                    ConsoleLog(_mainWindow.txtLog, $"[Kraken Trades] {lastError}");
                    ConsoleLog(_mainWindow.txtLog, $"[Kraken Trades] Load unsuccessful");

                    btnUpload.IsEnabled = true;
                    this.Cursor = Cursors.Arrow;

                    // Exit function early
                    return;
                }
            }

            // Load the DB table with data from the csv
            using (SqliteConnection connection = new(connectionString))
            {
                connection.Open();

                DbCommand commandDelete = connection.CreateCommand();

                // Truncate DB table
                commandDelete.CommandText = "DELETE FROM TB_TRADES_KRAKEN_S";
                commandDelete.ExecuteNonQuery();

                // Initialize the transaction
                DbTransaction transaction = connection.BeginTransaction();

                // Counter to keep track of the number of rows inserted
                int insertCounter = 0;

                // Per row in the DataTable insert a row in the DB table
                foreach (DataRow row in dataTable.Rows)
                {
                    DbCommand commandInsert = connection.CreateCommand();

                    commandInsert.CommandText = "INSERT INTO TB_TRADES_KRAKEN_S (TXID, ORDERTXID, PAIR, TIME, TYPE, ORDERTYPE, PRICE, COST, FEE, VOL, MARGIN, MISC, LEDGERS) VALUES (@TXID, @ORDERTXID, @PAIR, @TIME, @TYPE, @ORDERTYPE, @PRICE, @COST, @FEE, @VOL, @MARGIN, @MISC, @LEDGERS)";
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

                // Check for missing pairs
                List<string> missingPairs = MissingPairs(connection);

                if (missingPairs.Count > 0)
                {
                    lastWarning = "There are new Kraken pair codes to be refreshed." + Environment.NewLine + "[Configure => Kraken Pairs]";
                    MessageBox.Show(lastWarning, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    ConsoleLog(_mainWindow.txtLog, $"[Kraken Trades] {lastWarning}");

                    // Log each missing pair
                    foreach (string pair in missingPairs)
                    {
                        ConsoleLog(_mainWindow.txtLog, $"[Kraken Trades] Missing pair: {pair}");
                    }
                }

                connection.Close();
            }
            BindGrid();

            btnUpload.IsEnabled = true;
            this.Cursor = Cursors.Arrow;

            if (lastError == null)
            {
                ConsoleLog(_mainWindow.txtLog, $"[Kraken Trades] Load successful");
            }
            else
            {
                ConsoleLog(_mainWindow.txtLog, $"[Kraken Trades] Load unsuccessful");
            }
        }

        private void ButtonBrowse_Click(object sender, RoutedEventArgs e)
        {
            FileDialog(txtFileName);
            filePath = txtFileName.Text;
        }
    }
}