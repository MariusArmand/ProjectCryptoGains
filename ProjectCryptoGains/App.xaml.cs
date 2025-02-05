using System.Globalization;
using System.Threading;
using System.Windows;

namespace ProjectCryptoGains
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Set the culture to en-US
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

            base.OnStartup(e);
        }
    }
}