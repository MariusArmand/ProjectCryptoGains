using System;
using System.Windows;
using System.Windows.Input;
using static ProjectCryptoGains.Utility;

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
            _mainWindow = mainWindow;

            Bind();
        }

        private void Bind()
        {
            txtFiatCurrency.Text = SettingFiatCurrency;
            txtCryptoCompareApiKey.Text = SettingCryptoCompareApiKey;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            ConsoleLog(_mainWindow.txtLog, $"[Settings] Saving settings");

            btnSave.IsEnabled = false;
            this.Cursor = Cursors.Wait;

            string? lastError = null;

            try
            {
                SettingFiatCurrency = txtFiatCurrency.Text;
                SettingCryptoCompareApiKey = txtCryptoCompareApiKey.Text;

                string message = "Settings have been saved";
                ConsoleLog(_mainWindow.txtLog, $"[Settings] {message}");
                MessageBox.Show(message, "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (InvalidOperationException ex)
            {
                lastError = ex.Message;
                if (ex.InnerException != null)
                {
                    lastError += Environment.NewLine + ex.InnerException.Message;
                }

                ConsoleLog(_mainWindow.txtLog, $"[Settings] {lastError}");
                MessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            if (lastError == null)
            {
                ConsoleLog(_mainWindow.txtLog, $"[Settings] Saving successful");
            }
            else
            {
                ConsoleLog(_mainWindow.txtLog, $"[Settings] Saving unsuccessful");
            }

            btnSave.IsEnabled = true;
            this.Cursor = Cursors.Arrow;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Visibility = Visibility.Hidden;
        }
    }
}