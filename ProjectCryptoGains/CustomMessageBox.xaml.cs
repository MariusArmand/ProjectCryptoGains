using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ProjectCryptoGains
{
    public partial class CustomMessageBox : Window
    {
        public string Message { get; set; }
        public string TitleBarCaption { get; set; }
        public ImageSource ImageSource { get; set; }
        public Visibility CancelButtonVisibility { get; set; }

        public CustomMessageBox(string message, string caption, MessageBoxButton buttons, MessageBoxImage icon, TextAlignment textAlignment = TextAlignment.Center)
        {
            InitializeComponent();

            // Capture drag on titlebar
            TitleBar.MouseLeftButtonDown += (sender, e) => DragMove();

            DataContext = this;

            Message = message;
            TitleBarCaption = caption;
            Title = caption;

            ImageSource = GetIconUri(icon);

            txtMessage.TextAlignment = textAlignment;

            // Set visibility of Cancel button
            if (buttons == MessageBoxButton.OKCancel)
            {
                CancelButtonVisibility = Visibility.Visible;
            }
            else
            {
                CancelButtonVisibility = Visibility.Collapsed;
            }
        }

        private ImageSource GetIconUri(MessageBoxImage icon)
        {
            string iconPath;

            switch (icon)
            {
                case MessageBoxImage.Warning:
                    iconPath = "pack://application:,,,/ProjectCryptoGains;component/Resources/warning.png";
                    break;
                case MessageBoxImage.Error:
                    iconPath = "pack://application:,,,/ProjectCryptoGains;component/Resources/error.png";
                    break;
                case MessageBoxImage.Information:
                    iconPath = "pack://application:,,,/ProjectCryptoGains;component/Resources/information.png";
                    break;
                default:
                    iconPath = "pack://application:,,,/ProjectCryptoGains;component/Resources/information.png";
                    break;
            }

            return new BitmapImage(new Uri(iconPath));
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            SystemCommands.CloseWindow(this);
        }

        public static MessageBoxResult Show(string message, string caption, MessageBoxButton buttons, MessageBoxImage icon, TextAlignment textAlignment = TextAlignment.Center)
        {
            var customMessageBox = new CustomMessageBox(message, caption, buttons, icon, textAlignment);
            customMessageBox.ShowDialog();

            if (customMessageBox.DialogResult == true)
            {
                return MessageBoxResult.OK;
            }
            else
            {
                return MessageBoxResult.Cancel;
            }
        }

        private void SetDialogResultAndClose(bool result)
        {
            DialogResult = result;
            Close();
        }

        private void OnOKClick(object sender, RoutedEventArgs e)
        {
            SetDialogResultAndClose(true);
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            SetDialogResultAndClose(false);
        }
    }
}
