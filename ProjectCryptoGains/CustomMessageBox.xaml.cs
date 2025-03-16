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
        public string btnOkContent { get; set; }
        public string btnCancelContent { get; set; }
        public Visibility btnCancelVisibility { get; set; }

        public CustomMessageBox(string message, string caption, MessageBoxButton buttons, MessageBoxImage icon, TextAlignment textAlignment = TextAlignment.Center)
        {
            InitializeComponent();

            // Set owner of Custom MessageBox
            Owner = Application.Current.MainWindow;

            // Capture drag on titlebar
            TitleBar.MouseLeftButtonDown += (sender, e) => DragMove();

            DataContext = this;

            Message = message;
            TitleBarCaption = caption;
            Title = caption;

            ImageSource = GetIconUri(icon);

            txtMessage.TextAlignment = textAlignment;

            // Handle buttons
            switch (buttons)
            {
                case MessageBoxButton.OKCancel:
                    btnOkContent = "OK";
                    btnCancelContent = "Cancel";
                    btnCancelVisibility = Visibility.Visible;
                    break;

                case MessageBoxButton.YesNo:
                    btnOkContent = "Yes";
                    btnCancelContent = "No";
                    btnCancelVisibility = Visibility.Visible;
                    break;

                default:
                    btnOkContent = "OK";
                    btnCancelContent = "Cancel";
                    btnCancelVisibility = Visibility.Collapsed;
                    break;
            }
        }

        private ImageSource GetIconUri(MessageBoxImage icon)
        {
            string iconPath;

            switch (icon)
            {
                case MessageBoxImage.Question:
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

            // After the dialog closes, ensure the owner regains focus
            if (customMessageBox.Owner != null)
            {
                customMessageBox.Owner.Activate();
            }

            // Return appropriate MessageBoxResult based on button type
            switch (buttons)
            {
                case MessageBoxButton.OKCancel:
                    return customMessageBox.DialogResult == true ? MessageBoxResult.OK : MessageBoxResult.Cancel;

                case MessageBoxButton.YesNo:
                    return customMessageBox.DialogResult == true ? MessageBoxResult.Yes : MessageBoxResult.No;

                default:
                    return customMessageBox.DialogResult == true ? MessageBoxResult.OK : MessageBoxResult.Cancel;
            }
        }

        private void SetDialogResultAndClose(bool result)
        {
            DialogResult = result;
            Close();
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResultAndClose(true);
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResultAndClose(false);
        }
    }
}
