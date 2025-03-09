using ProjectCryptoGains.Common;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using static ProjectCryptoGains.Common.Utils.SettingUtils;
using static ProjectCryptoGains.Common.Utils.Utils;

namespace ProjectCryptoGains
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : SubwindowBase
    {
        private readonly MainWindow _mainWindow;

        public SettingsWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            TitleBarElement = TitleBar;

            _mainWindow = mainWindow;

            // Populate ComboBox with options
            cmbFiatCurrency.ItemsSource = new List<string> { "EUR", "USD" };

            Bind();
        }

        private void BlockUI()
        {
            btnSave.IsEnabled = false;
            Cursor = Cursors.Wait;
        }

        private void UnblockUI()
        {
            btnSave.IsEnabled = true;
            Cursor = Cursors.Arrow;
        }

        private void Bind()
        {
            cmbFiatCurrency.Text = SettingFiatCurrency;

            txtRewardsTaxPercentage.Text = SettingRewardsTaxPercentage.ToString();

            txtCoinDeskDataApiKey.Text = SettingCoinDeskDataApiKey;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            ConsoleLog(_mainWindow.txtLog, $"[Settings] Saving settings");

            BlockUI();

            try
            {

                string? lastError = null;

                try
                {
                    SettingFiatCurrency = cmbFiatCurrency.SelectedItem as string ?? "NONE";

                    if (decimal.TryParse(txtRewardsTaxPercentage.Text, out decimal tryParsedAmount))
                    {
                        SettingRewardsTaxPercentage = tryParsedAmount;
                    }
                    else
                    {
                        txtRewardsTaxPercentage.Text = "0";
                        SettingRewardsTaxPercentage = 0m;
                    }

                    SettingCoinDeskDataApiKey = txtCoinDeskDataApiKey.Text;

                    string message = "Settings have been saved.";
                    ConsoleLog(_mainWindow.txtLog, $"[Settings] {message}");
                    MessageBoxResult result = CustomMessageBox.Show(message, "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (InvalidOperationException ex)
                {
                    lastError = ex.Message;
                    if (ex.InnerException != null)
                    {
                        lastError += Environment.NewLine + ex.InnerException.Message;
                    }

                    ConsoleLog(_mainWindow.txtLog, $"[Settings] {lastError}");
                    MessageBoxResult result = CustomMessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                if (lastError == null)
                {
                    ConsoleLog(_mainWindow.txtLog, $"[Settings] Saving successful");
                }
                else
                {
                    ConsoleLog(_mainWindow.txtLog, $"[Settings] Saving unsuccessful");
                }

                Bind();
            }
            finally
            {
                UnblockUI();
            }
        }
    }
}