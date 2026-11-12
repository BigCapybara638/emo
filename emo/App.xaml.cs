using System.Globalization;
using System.Windows;

namespace emo;

public partial class App : Application
{
    private void Start(object sender, StartupEventArgs e) => SignIn();

    internal void SignIn()
    {
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        var login = new LoginWindow();
        MainWindow = login;
        if (login.ShowDialog() != true)
        {
            Shutdown();
            return;
        }
        var catalog = new MainWindow();
        MainWindow = catalog;
        catalog.StartSession(login);
        ShutdownMode = ShutdownMode.OnMainWindowClose;
        catalog.Show();
    }

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
