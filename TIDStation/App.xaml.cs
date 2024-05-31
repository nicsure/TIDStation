using System.Configuration;
using System.Data;
using System.Globalization;
using System.Windows;

namespace TIDStation
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            CultureInfo customCulture = new("en-US");
            CultureInfo.DefaultThreadCurrentCulture = customCulture;
            CultureInfo.DefaultThreadCurrentUICulture = customCulture;
            Thread.CurrentThread.CurrentCulture = customCulture;
            base.OnStartup(e);
        }
    }

}
