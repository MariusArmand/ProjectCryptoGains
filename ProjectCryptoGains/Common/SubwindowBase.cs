using System.ComponentModel;
using System.Windows;

namespace ProjectCryptoGains.Common
{
    public class SubwindowBase : Window
    {
        protected UIElement? TitleBarElement { get; set; }

        public SubwindowBase()
        {
            Opacity = 0; // Start invisible to prevent white window flash

            Closing += Subwindow_Closing;
            // Hook up after XAML is loaded
            Loaded += SubwindowBase_Loaded;

            IsVisibleChanged += SubwindowBase_IsVisibleChanged;
        }

        private void SubwindowBase_Loaded(object sender, RoutedEventArgs e)
        {
            Opacity = 1;

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

        protected virtual void SubwindowBase_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
        }
    }
}