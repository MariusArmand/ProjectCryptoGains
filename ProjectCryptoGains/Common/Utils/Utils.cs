using FirebirdSql.Data.FirebirdClient;
using Newtonsoft.Json.Linq;
using System;
using System.Data.Common;
using System.Globalization;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using static ProjectCryptoGains.Common.Utils.DatabaseUtils;
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

        public static void ConsoleLog(TextBox txtLog, string logText)
        {
            // Get the current time in the specified format
            string timestamp = DateTime.Now.ToString("HH:mm:ss");

            // Remove trailing period from logText
            string trimmedLogText = logText.TrimEnd('.');

            // Append text
            if (string.IsNullOrWhiteSpace(txtLog.Text))
            {
                txtLog.AppendText($"{timestamp} {trimmedLogText}");
            }
            else
            {
                txtLog.AppendText($"\n{timestamp} {trimmedLogText}");
            }

            // Scroll the TextBox to the bottom
            txtLog.ScrollToEnd();
        }

        public static (decimal fiatAmount, string source) ConvertXToFiat(string xAsset, DateTime date, FbConnection connection)
        {
            try
            {
                string fiatCurrency = SettingFiatCurrency;

                decimal? exchangeRateApi = null;
                decimal? exchangeRateDb = null;

                using DbCommand selectCommand = connection.CreateCommand();
                selectCommand.CommandText = $@"SELECT EXCHANGE_RATE
                                               FROM TB_EXCHANGE_RATES
                                               WHERE ""DATE"" = @DATE
                                                  AND ASSET = @ASSET
                                                  AND FIAT_CURRENCY = @FIAT_CURRENCY";

                // Add parameters
                AddParameterWithValue(selectCommand, "@DATE", date);
                AddParameterWithValue(selectCommand, "@ASSET", xAsset);
                AddParameterWithValue(selectCommand, "@FIAT_CURRENCY", fiatCurrency);

                using (DbDataReader reader = selectCommand.ExecuteReader())
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
                    string fiat_currency = SettingFiatCurrency;
                    string? api_key = SettingCoinDeskDataApiKey;
                    var response = client.GetAsync($"/data/v2/histoday?limit=1&fsym={xAsset}&tsym={fiat_currency}&toTs={unixTimestamp}&api_key={api_key}").Result;
                    var result = response.Content.ReadAsStringAsync().Result;
                    var json = JObject.Parse(result);

                    string? api_response_result = json["Response"]?.ToString();
                    string? api_response_message = json["Message"]?.ToString();

                    if (string.IsNullOrEmpty(api_key) || api_response_result != "Success")
                    {
                        string? error_message = null;
                        if (string.IsNullOrEmpty(api_key))
                        {
                            error_message = "CoinDesk Data API call failed: API key is not set.";
                        }
                        else
                        {
                            error_message = $"CoinDesk Data API call failed: {api_response_message}";
                        }
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            CustomMessageBox.Show(error_message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        });
                        throw new Exception(error_message);
                    }

                    exchangeRateApi = decimal.Parse(json["Data"]?["Data"]?[1]?["close"]?.ToString() ?? "0.00");
                }

                ///////////////////////////
                if (exchangeRateApi > 0)
                {
                    using DbCommand insertCommand = connection.CreateCommand();
                    insertCommand.CommandText = @"INSERT INTO TB_EXCHANGE_RATES (
                                                      ""DATE"",
                                                      ASSET,                                                      
                                                      FIAT_CURRENCY,
                                                      EXCHANGE_RATE
                                                  )
                                                  VALUES (
                                                      @DATE,
                                                      @ASSET,
                                                      @FIAT_CURRENCY,
                                                      ROUND(@EXCHANGE_RATE, 10)
                                                  )";

                    AddParameterWithValue(insertCommand, "@ASSET", xAsset);
                    AddParameterWithValue(insertCommand, "@DATE", date);
                    AddParameterWithValue(insertCommand, "@FIAT_CURRENCY", fiatCurrency);
                    AddParameterWithValue(insertCommand, "@EXCHANGE_RATE", exchangeRateApi);

                    insertCommand.ExecuteNonQuery();
                }
                ///////////////////////////
                decimal fiatAmount = 0;
                string source = "";

                if (exchangeRateDb.HasValue)
                {
                    fiatAmount = (decimal)(exchangeRateDb);
                    source = "DB";
                }
                else if (exchangeRateApi.HasValue)
                {
                    fiatAmount = (decimal)(exchangeRateApi);
                    source = "API";
                }

                return (fiatAmount, source);
            }
            catch (Exception ex)
            {
                throw new Exception("ConvertXToFiat failed", ex);
            }
        }

        public static decimal ConvertStringToDecimal(string input, decimal defaultValue = 0.0000000000m)
        {
            if (decimal.TryParse(input, CultureInfo.InvariantCulture, out decimal tryParsedAmount))
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
    }
}