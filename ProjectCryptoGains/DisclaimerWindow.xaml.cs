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
        }

        private void OnAgreeClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        // Static method to show the MessageBox and handle the result
        public static new bool? Show()
        {
            DisclaimerWindow messageBox = new();
            return messageBox.ShowDialog();
        }
    }
}