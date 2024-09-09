using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;

namespace RateListener
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        }
    }
}
