using System.Globalization;
using System.Windows;

namespace emo;

public partial class App : Application
{
    public App()
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("ru-RU");
        DispatcherUnhandledException += (_, e) =>
        {
            MessageBox.Show(e.Exception.Message, "Ошибка — проверьте данные и повторите действие", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        };
    }
}
