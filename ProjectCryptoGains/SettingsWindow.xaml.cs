using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using static ProjectCryptoGains.Common.Utility;

namespace ProjectCryptoGains
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private readonly MainWindow _mainWindow;

        public SettingsWindow(MainWindow mainWindow)
        {
            InitializeComponent();

            // Capture drag on titlebar
            TitleBar.MouseLeftButtonDown += (sender, e) => DragMove();

            _mainWindow = mainWindow;

            // Populate ComboBox with options
            cmbFiatCurrency.ItemsSource = new List<string> { "EUR", "USD" };

            Bind();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            SystemCommands.MinimizeWindow(this);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            SystemCommands.CloseWindow(this);
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
            // Set the selected item based on the current setting
            cmbFiatCurrency.Text = SettingFiatCurrency;

            txtRewardsTaxPercentage.Text = SettingRewardsTaxPercentage.ToString();

            txtCoinDeskDataApiKey.Text = SettingCoinDeskDataApiKey;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            ConsoleLog(_mainWindow.txtLog, $"[Settings] Saving settings");

            BlockUI();

            string? lastError = null;

            try
            {
                SettingFiatCurrency = cmbFiatCurrency.SelectedItem as string;

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

                string message = "Settings have been saved";
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

            UnblockUI();

            Bind();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Visibility = Visibility.Hidden;
        }
    }
}