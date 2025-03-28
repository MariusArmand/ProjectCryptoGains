﻿using ProjectCryptoGains.Common;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using static ProjectCryptoGains.Common.Utils.SettingsUtils;
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

        protected override void SubwindowBase_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.IsVisible)
            {
                ReadSettings();
            }
        }

        private void ReadSettings()
        {
            cmbFiatCurrency.Text = SettingFiatCurrency;

            txtRewardsTaxPercentage.Text = SettingRewardsTaxPercentage.ToString();

            txtCoinDeskDataApiKey.Text = SettingCoinDeskDataApiKey;

            txtPrintoutTitlePrefix.Text = SettingPrintoutTitlePrefix;
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
            ReadSettings();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            ConsoleLog(_mainWindow.txtLog, $"[Settings] Saving settings");

            BlockUI();

            try
            {
                string? lastError = null;

                try
                {
                    // Save fiat currency setting
                    SettingFiatCurrency = cmbFiatCurrency.SelectedItem as string ?? "NONE";

                    // Save rewards tax percentage setting
                    if (decimal.TryParse(txtRewardsTaxPercentage.Text, out decimal tryParsedAmount))
                    {
                        SettingRewardsTaxPercentage = tryParsedAmount;
                    }
                    else
                    {
                        txtRewardsTaxPercentage.Text = "0";
                        SettingRewardsTaxPercentage = 0m;
                    }

                    // Save CoinDesk Data API key setting
                    SettingCoinDeskDataApiKey = txtCoinDeskDataApiKey.Text;

                    // Save printout title prefix setting
                    SettingPrintoutTitlePrefix = txtPrintoutTitlePrefix.Text;

                    string message = "Settings have been saved.";
                    ConsoleLog(_mainWindow.txtLog, $"[Settings] {message}");
                    CustomMessageBox.Show(message, "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (InvalidOperationException ex)
                {
                    lastError = ex.Message;
                    if (ex.InnerException != null)
                    {
                        lastError += Environment.NewLine + ex.InnerException.Message;
                    }

                    ConsoleLog(_mainWindow.txtLog, $"[Settings] {lastError}");
                    CustomMessageBox.Show(lastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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