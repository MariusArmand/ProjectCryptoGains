using System.ComponentModel;
using System.Windows;

namespace ProjectCryptoGains.Common
{
    public class SubwindowBase : Window
    {
        protected UIElement? TitleBarElement { get; set; }

        public SubwindowBase()
        {
            Closing += Subwindow_Closing;
            // Hook up after XAML is loaded
            Loaded += SubwindowBase_Loaded;
        }

        private void SubwindowBase_Loaded(object sender, RoutedEventArgs e)
        {
            if (TitleBarElement != null)
            {
                TitleBarElement.MouseLeftButtonDown += (s, args) => DragMove();
            }
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