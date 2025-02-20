﻿using System;
using System.Collections.Generic;
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
            // Capture drag on titlebar
            this.TitleBar.MouseLeftButtonDown += (sender, e) => this.DragMove();

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

            btnSave.IsEnabled = false;
            this.Cursor = Cursors.Wait;

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

            btnSave.IsEnabled = true;
            this.Cursor = Cursors.Arrow;

            Bind();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Visibility = Visibility.Hidden;
        }
    }
}