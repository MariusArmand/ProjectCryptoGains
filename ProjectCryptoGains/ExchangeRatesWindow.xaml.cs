using FirebirdSql.Data.FirebirdClient;
using Microsoft.Win32;
using ProjectCryptoGains.Common;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using static ProjectCryptoGains.Common.Utils.CsvUtils;
using static ProjectCryptoGains.Common.Utils.DatabaseUtils;
using static ProjectCryptoGains.Common.Utils.DateUtils;
using static ProjectCryptoGains.Common.Utils.ReaderUtils;
using static ProjectCryptoGains.Common.Utils.Utils;
using static ProjectCryptoGains.Common.Utils.WindowUtils;
using static ProjectCryptoGains.Models;

namespace ProjectCryptoGains
{
    /// <summary>
    /// Interaction logic for ExchangeRatesWindow.xaml
    /// </summary>
    public partial class ExchangeRatesWindow : SubwindowBase
    {
        private readonly MainWindow _mainWindow;

        public ExchangeRatesWindow(MainWindow mainWindow)
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
            // Create a collection of ExchangeRatesModel objects
            ObservableCollection<ExchangeRatesModel> ExchangeRatesData = [];

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
                selectCommand.CommandText = $@"SELECT 
                                                   ""DATE"",
                                                   ASSET,                                                   
                                                   FIAT_CURRENCY,
                                                   EXCHANGE_RATE
                                               FROM TB_EXCHANGE_RATES
                                               ORDER BY ""DATE"", ASSET, FIAT_CURRENCY ASC";

                using (DbDataReader reader = selectCommand.ExecuteReader())
                {
                    int dbLineNumber = 0;
                    while (reader.Read())
                    {
                        dbLineNumber++;

                        ExchangeRatesData.Add(new ExchangeRatesModel
                        {
                            Row_number = dbLineNumber,
                            Date = reader.GetDateTime(0),
                            Asset = reader.GetStringOrEmpty(1),
                            Fiat_currency = reader.GetStringOrEmpty(2),
                            Exchange_rate = reader.GetDecimal(3)
                        });
                    }
                }

                dgExchangeRates.ItemsSource = ExchangeRatesData;
            }
        }

        private void BtnHelp_Click(object sender, RoutedEventArgs e)
        {
            OpenHelp("exchange_rates_help.html");
        }

        private void BtnImport_Click(object sender, RoutedEventArgs e)
        {
            string? lastError = null;

            ConsoleLog(_mainWindow.txtLog, $"[Exchange Rates] Attempting to import exchange rates");

            // Open file dialog
            OpenFileDialog openFileDlg = new OpenFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                Title = "Import Exchange Rates"
            };

            bool? openFiledlgResult = openFileDlg.ShowDialog();
            if (openFiledlgResult != true)
            {
                ConsoleLog(_mainWindow.txtLog, "[Exchange Rates] Import cancelled");
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
                    ConsoleLog(_mainWindow.txtLog, $"[Exchange Rates] {lastError}");
                    ConsoleLog(_mainWindow.txtLog, $"[Exchange Rates] Import unsuccessful");

                    // Exit function early
                    return;
                }

                // Read the CSV file
                StreamReader? reader;
                try
                {
                    reader = new(filePath);
                    ConsoleLog(_mainWindow.txtLog, $"[Exchange Rates] Importing {filePath}");
                }
                catch (Exception ex)
                {
                    lastError = "File could not be opened" + Environment.NewLine + ex.Message;
                    CustomMessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    ConsoleLog(_mainWindow.txtLog, $"[Exchange Rates] {lastError}");
                    ConsoleLog(_mainWindow.txtLog, $"[Exchange Rates] Import unsuccessful");

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
                            string[] columnNames = Regex.Split(csvLine, pattern).Select(s => CsvStripValue(s)).ToArray();
                            string[] columnNamesExpected = ["date", "asset", "fiat_currency", "exchange_rate"];

                            if (!Enumerable.SequenceEqual(columnNames, columnNamesExpected))
                            {
                                lastError = "Unexpected inputfile header.";
                                CustomMessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                ConsoleLog(_mainWindow.txtLog, $"[Exchange Rates] {lastError}");

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
                    ConsoleLog(_mainWindow.txtLog, $"[Exchange Rates] {lastError}");
                }

                if (lastError == null)
                {
                    // Load the db table with ExchangeRatesData from the csv
                    using (FbConnection connection = new(connectionString))
                    {
                        connection.Open();

                        // Initialize the transaction
                        DbTransaction transaction = connection.BeginTransaction();

                        // Counter to keep track of the number of rows upserted
                        int upsertCounter = 0;

                        // Create and prepare the command
                        using DbCommand upsertCommand = connection.CreateCommand();
                        upsertCommand.Transaction = transaction;
                        upsertCommand.CommandText = @"UPDATE OR INSERT INTO TB_EXCHANGE_RATES (
                                                      ""DATE"",
                                                      ASSET,
                                                      FIAT_CURRENCY,
                                                      EXCHANGE_RATE
                                                  ) VALUES (
                                                      @DATE,
                                                      @ASSET,
                                                      @FIAT_CURRENCY,
                                                      ROUND(@EXCHANGE_RATE, 10)
                                                  )
                                                  MATCHING (""DATE"", ASSET, FIAT_CURRENCY)";

                        upsertCommand.Prepare();

                        // Per row in the DataTable upsert a row in the DB table
                        foreach (DataRow row in dataTable.Rows)
                        {
                            upsertCommand.Parameters.Clear(); // Reset parameters for the next row

                            string value = "";
                            string columnName = "";

                            for (int i = 0; i < dataTable.Columns.Count; i++)
                            {
                                columnName = dataTable.Columns[i].ColumnName.Replace(' ', '_').ToUpper();
                                value = (string)(row.ItemArray[i] ?? "");

                                if (value == "")
                                {
                                    lastError = $"Update or insert row {upsertCounter} failed: {columnName} cannot be null.";
                                    CustomMessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                    ConsoleLog(_mainWindow.txtLog, $"[Exchange Rates] {lastError}");

                                    // Exit loop early
                                    break;
                                }

                                if (columnName == "DATE" && !IsValidDateFormat(value, "yyyy-MM-dd"))
                                {
                                    lastError = $"Update or insert row {upsertCounter} failed: {columnName} should be in yyyy-MM-dd format.";
                                    CustomMessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                    ConsoleLog(_mainWindow.txtLog, $"[Exchange Rates] {lastError}");

                                    // Exit loop early
                                    break;
                                }

                                if (columnName == "DATE")
                                {
                                    AddParameterWithValue(upsertCommand, $"@{columnName}", ConvertStringToIsoDateTime($"{value} 00:00:00"));
                                }
                                else if (columnName == "EXCHANGE_RATE")
                                {
                                    AddParameterWithValue(upsertCommand, $"@{columnName}", ConvertStringToDecimal(value));
                                }
                                else
                                {
                                    AddParameterWithValue(upsertCommand, $"@{columnName}", value);
                                }
                            }

                            if (lastError != null)
                            {
                                transaction.Rollback();
                                break; // Exit loop, but continue to the final logging
                            }

                            try
                            {
                                upsertCommand.ExecuteNonQuery();
                            }
                            catch (Exception ex)
                            {
                                lastError = $"Update or insert row {upsertCounter} failed: {ex.Message}";
                                CustomMessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                ConsoleLog(_mainWindow.txtLog, $"[Exchange Rates] {lastError}");
                                transaction.Rollback();
                                break;
                            }

                            upsertCounter++;

                            // If the upsertCounter is divisible by 10k, commit the transaction and start a new one
                            if (upsertCounter % 10000 == 0)
                            {
                                transaction.Commit();
                                transaction = connection.BeginTransaction();
                                upsertCommand.Transaction = transaction;
                            }
                        }

                        // Commit any remaining upserts if no errors occurred
                        if (lastError == null)
                        {
                            transaction.Commit();
                        }
                    }
                }

                BindGrid();

                if (lastError == null)
                {
                    ConsoleLog(_mainWindow.txtLog, $"[Exchange Rates] Import done");
                }
                else
                {
                    ConsoleLog(_mainWindow.txtLog, $"[Exchange Rates] Import unsuccessful");
                }
            }
            finally
            {
                UnblockUI();
            }
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            string? lastError = null;

            ConsoleLog(_mainWindow.txtLog, "[Exchange Rates] Attempting to export exchange rates");
            BlockUI();

            try
            {
                // Create and configure SaveFileDialog
                SaveFileDialog saveFileDlg = new SaveFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                    Title = "Export Exchange Rates",
                    FileName = $"exchange_rates_{DateTime.Now:yyyyMMdd}.csv"
                };

                // Show dialog and get result
                bool? saveFiledlgResult = saveFileDlg.ShowDialog();
                if (saveFiledlgResult != true)
                {
                    ConsoleLog(_mainWindow.txtLog, "[Exchange Rates] Export cancelled");
                    return;
                }

                string filePath = saveFileDlg.FileName;

                // Create StringBuilder for CSV content
                StringBuilder csvContent = new StringBuilder();

                // Add header row
                csvContent.AppendLine("\"date\",\"asset\",\"fiat_currency\",\"exchange_rate\"");

                // Connect to database and retrieve data
                using (FbConnection connection = new FbConnection(connectionString))
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
                    selectCommand.CommandText = @"SELECT 
                                                      ""DATE"", 
                                                      ASSET, 
                                                      FIAT_CURRENCY, 
                                                      EXCHANGE_RATE 
                                                  FROM TB_EXCHANGE_RATES";

                    using (DbDataReader reader = selectCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // Format date as yyyy-MM-dd
                            string date = reader.GetDateTime(0).ToString("yyyy-MM-dd");
                            string asset = reader.GetString(1);
                            string fiatCurrency = reader.GetString(2);
                            decimal exchangeRate = reader.GetDecimal(3);

                            // Escape values and create CSV row
                            string[] values = new[]
                            {
                                CsvEscapeValue(date),
                                CsvEscapeValue(asset),
                                CsvEscapeValue(fiatCurrency),
                                exchangeRate.ToString(CultureInfo.InvariantCulture)
                            };

                            csvContent.AppendLine(string.Join(",", values));
                        }
                    }
                }

                // Write to file
                try
                {
                    File.WriteAllText(filePath, csvContent.ToString());
                    ConsoleLog(_mainWindow.txtLog, $"[Exchange Rates] Exported to {filePath}");
                }
                catch (Exception ex)
                {
                    lastError = "Failed to write CSV file" + Environment.NewLine + ex.Message;
                    CustomMessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    ConsoleLog(_mainWindow.txtLog, $"[Exchange Rates] {lastError}");
                }
            }
            catch (Exception ex)
            {
                lastError = "Export failed" + Environment.NewLine + ex.Message;
                CustomMessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                ConsoleLog(_mainWindow.txtLog, $"[Exchange Rates] {lastError}");
            }
            finally
            {
                UnblockUI();
            }

            if (lastError == null)
            {
                ConsoleLog(_mainWindow.txtLog, "[Exchange Rates] Export done");
            }
            else
            {
                ConsoleLog(_mainWindow.txtLog, "[Exchange Rates] Export unsuccessful");
            }
        }

        private void BtnDeleteAll_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult msgBoxResultYesNo = CustomMessageBox.Show("Are you sure you want to delete all exchange rates?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (msgBoxResultYesNo == MessageBoxResult.Yes)
            {
                ConsoleLog(_mainWindow.txtLog, $"[Exchange Rates] Deleting exchange rates");

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

                    using DbCommand deleteCommand = connection.CreateCommand();

                    // Truncate DB table
                    deleteCommand.CommandText = "DELETE FROM TB_EXCHANGE_RATES";
                    deleteCommand.ExecuteNonQuery();
                }

                BindGrid();

                ConsoleLog(_mainWindow.txtLog, $"[Exchange Rates] Delete done");
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            ConsoleLog(_mainWindow.txtLog, $"[Exchange Rates] Refreshing exchange rates");
            BindGrid();
            ConsoleLog(_mainWindow.txtLog, $"[Exchange Rates] Refresh done");
        }
    }
}