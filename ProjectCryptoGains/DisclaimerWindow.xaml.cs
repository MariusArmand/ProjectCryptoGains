using System.Windows;

namespace ProjectCryptoGains
{
    /// <summary>
    /// Interaction logic for DisclaimerWindow.xaml
    /// </summary>
    public partial class DisclaimerWindow : Window
    {
        public DisclaimerWindow()
        {
            InitializeComponent();

            // Capture drag on titlebar
            TitleBar.MouseLeftButtonDown += (sender, e) => DragMove();
        }

        private void OnAgreeClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        // Static method to show the MessageBox and handle the result
        public static new bool? Show()
        {
            DisclaimerWindow messageBox = new();
            return messageBox.ShowDialog();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            SystemCommands.CloseWindow(this);
        }
    }
}