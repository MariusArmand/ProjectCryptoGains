using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace ProjectCryptoGains.Common.Utils
{
    public static class WindowUtils
    {
        public static void FileDialog(TextBox txtFileName)
        {
            // Create OpenFileDialog
            Microsoft.Win32.OpenFileDialog openFileDlg = new()
            {
                // Set filter for file extension and default file extension
                DefaultExt = ".csv",
                Filter = "CSV Files (*.csv)|*.csv"
            };

            // Launch OpenFileDialog by calling ShowDialog method
            bool? result = openFileDlg.ShowDialog();

            // Get the selected file name and display in a TextBox.
            if (result == true)
            {
                // Store the filename in the textbox and global
                txtFileName.Text = openFileDlg.FileName;
            }
        }

        public static void ShowAndFocusSubWindow(Window window, Window ownerWindow)
        {
            ShowAndFocusWindow(window);
            window.Owner = ownerWindow;
        }

        public static void ShowAndFocusWindow(Window window)
        {
            window.Show();
            window.WindowState = WindowState.Normal;
            window.Activate();
            window.Focus();
        }

        public static void OpenHelp(string filename)
        {
            string helpfilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "help", filename);

            if (File.Exists(helpfilePath))
            {
                try
                {
                    // Opens the file with the default application for .html files, which is usually the default web browser
                    Process.Start(new ProcessStartInfo(helpfilePath) { UseShellExecute = true });
                }
                catch (System.ComponentModel.Win32Exception ex)
                {
                    CustomMessageBox.Show("The help file could not be opened." + Environment.NewLine + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                CustomMessageBox.Show("The help file does not exist.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}