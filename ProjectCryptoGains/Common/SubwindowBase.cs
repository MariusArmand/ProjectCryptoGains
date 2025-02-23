using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace ProjectCryptoGains.Common
{
    public class SubwindowBase : Window
    {
        public SubwindowBase()
        {
            Closing += Subwindow_Closing;
        }

        protected virtual void Minimize_Click(object sender, RoutedEventArgs e)
        {
            SystemCommands.MinimizeWindow(this);
        }

        protected virtual void Resize_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                SystemCommands.RestoreWindow(this);
            }
            else
            {
                SystemCommands.MaximizeWindow(this);
            }
        }

        protected virtual void Close_Click(object sender, RoutedEventArgs e)
        {
            SystemCommands.CloseWindow(this);
        }

        protected virtual void Subwindow_Closing(object? sender, CancelEventArgs e)
        {
            sender = this;
            e.Cancel = true;
            Visibility = Visibility.Hidden;
        }
    }
}