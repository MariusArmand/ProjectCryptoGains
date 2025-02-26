using Microsoft.Data.Sqlite;
using Newtonsoft.Json.Linq;
using System;
using System.Data.Common;
using System.Globalization;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using static ProjectCryptoGains.Common.Utils.SettingUtils;

namespace ProjectCryptoGains.Common.Utils
{
    public static class Utils
    {
        public static string AssemblyVersion
        {
            get
            {
                System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
                return $"v{assembly?.GetName()?.Version?.ToString()}" ?? "Unknown Version";
            }
        }

        // Enums //
        public enum LedgerSource
        {
            Kraken,
            Manual
        }

        public enum Caller
        {
            Ledgers,
            Trades,
            Gains,
            Rewards,
            Balances,
            Metrics
        }
        /////////////////////////////

        // Custom exceptions //
        public class ValidationException(string message) : Exception(message)
        {
        }
        /////////////////////////////

        public static void ConsoleLog(TextBox txtLog, string logText)
        {
            // Get the current time in the specified format
            string timestamp = DateTime.Now.ToString("HH:mm:ss");

            // Append text
            if (string.IsNullOrWhiteSpace(txtLog.Text))
            {
                txtLog.AppendText($"{timestamp} {logText}");
            }

            else
            {
                txtLog.AppendText($"\n{timestamp} {logText}");
            }

            // Scroll the TextBox to the bottom
            txtLog.ScrollToEnd();
        }

        public static (string fiatAmount, string source) ConvertXToFiat(string xCurrency, decimal xAmount, DateTime date, SqliteConnection connection)
        {
            try
            {
                string? fiatCurrency = SettingFiatCurrency;

                decimal? exchangeRateApi = null;
                decimal? exchangeRateDb = null;
                ///////////////////////////
                string formattedDate = date.ToString("yyyy-MM-dd HH:mm:ss");
                ///////////////////////////
                DbCommand command = connection.CreateCommand();
                command.CommandText = $@"SELECT EXCHANGE_RATE
                                         FROM TB_CONVERT_X_TO_FIAT_A
                                         WHERE CURRENCY = '{xCurrency}'
                                            AND AMOUNT = '{xAmount}'
                                            AND DATE = '{formattedDate}'
                                            AND FIAT_CURRENCY = '{fiatCurrency}'";

                using (DbDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        exchangeRateDb = reader.GetDecimalOrNull(0);
                    }
                }
                ///////////////////////////

                if (!exchangeRateDb.HasValue)
                {
                    using var client = new HttpClient();

                    int unixTimestamp = (int)(date - new DateTime(1970, 1, 1).ToLocalTime()).TotalSeconds;

                    client.BaseAddress = new Uri("https://min-api.cryptocompare.com");
                    string? fiat_currency = SettingFiatCurrency;
                    string? api_key = SettingCoinDeskDataApiKey;
                    var response = client.GetAsync($"/data/v2/histoday?limit=1&fsym=" + xCurrency + "&tsym=" + fiat_currency + "&toTs=" + unixTimestamp + "&api_key={" + api_key + "}").Result;
                    var result = response.Content.ReadAsStringAsync().Result;
                    var json = JObject.Parse(result);

                    string? api_response_result = json["Response"]?.ToString();
                    string? api_response_message = json["Message"]?.ToString();

                    if (string.IsNullOrEmpty(api_key) || api_response_result != "Success")
                    {
                        string? error_message = null;
                        if (string.IsNullOrEmpty(api_key))
                        {
                            error_message = "CoinDesk Data API call failed: API key is not set";
                        }
                        else
                        {
                            error_message = "CoinDesk Data API call failed: " + api_response_message;
                        }
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            MessageBoxResult result = CustomMessageBox.Show(error_message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        });
                        throw new Exception(error_message);
                    }

                    exchangeRateApi = decimal.Parse(json["Data"]?["Data"]?[1]?["close"]?.ToString() ?? "0.00");
                }

                ///////////////////////////
                if (exchangeRateApi > 0)
                {
                    using var commandInsert = connection.CreateCommand();

                    commandInsert.CommandText = @"INSERT INTO TB_CONVERT_X_TO_FIAT_A (
                                                      CURRENCY, 
                                                      AMOUNT, 
                                                      DATE, 
                                                      FIAT_CURRENCY, 
                                                      EXCHANGE_RATE
                                                  )
                                                  VALUES (
                                                      @CURRENCY, 
                                                      @AMOUNT, 
                                                      @DATE, 
                                                      @FIAT_CURRENCY, 
                                                      printf('%.10f', @EXCHANGE_RATE)
                                                  )";

                    commandInsert.Parameters.AddWithValue("@CURRENCY", xCurrency);
                    commandInsert.Parameters.AddWithValue("@AMOUNT", xAmount.ToString());
                    commandInsert.Parameters.AddWithValue("@DATE", formattedDate);
                    commandInsert.Parameters.AddWithValue("@FIAT_CURRENCY", fiatCurrency);
                    commandInsert.Parameters.AddWithValue("@EXCHANGE_RATE", exchangeRateApi);

                    commandInsert.ExecuteNonQuery();
                }
                ///////////////////////////
                decimal fiatAmount = 0;
                string source = "";

                if (exchangeRateDb.HasValue)
                {
                    fiatAmount = (decimal)(xAmount * exchangeRateDb);
                    source = "DB";
                }
                else if (exchangeRateApi.HasValue)
                {
                    fiatAmount = (decimal)(xAmount * exchangeRateApi);
                    source = "API";
                }

                return (fiatAmount.ToString("0.0000000000", NumberFormatInfo.InvariantInfo), source);
            }
            catch (Exception ex)
            {
                throw new Exception("ConvertXToFiat failed", ex);
            }
        }

        public static decimal ConvertStringToDecimal(string input, decimal defaultValue = 0.0000000000m)
        {
            if (decimal.TryParse(input, out decimal tryParsedAmount))
            {
                return tryParsedAmount;
            }
            else
            {
                return defaultValue;
            }
        }

        public static string? NullIfEmpty(this string s)
        {
            return string.IsNullOrEmpty(s) ? null : s;
        }

        public static bool IsValidDateFormat(string date, string format)
        {
            // Try to parse the date using the specified format
            return DateTime.TryParseExact(date, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
        }

        public static string GetTodayAsIsoDate()
        {
            return DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        public static DateTime ConvertStringToIsoDate(string date)
        {
            return DateTime.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        public static DateTime ConvertStringToIsoDateTime(string datetime)
        {
            return DateTime.ParseExact(datetime, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        }
    }
}