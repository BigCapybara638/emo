using System.Windows;

namespace emo;

public partial class LoginWindow : Window
{
    public string UserFIO { get; private set; } = "";
    public int UserRole { get; private set; }
    public int UserId { get; private set; }

    public LoginWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => Login.Focus();
    }

    private void GuestSignIn(object sender, RoutedEventArgs e)
    {
        UserFIO = "Гость";
        DialogResult = true;
    }

    private void SignIn(object sender, RoutedEventArgs e)
    {
        string login = Login.Text.Trim();
        if (string.IsNullOrEmpty(login))
        {
            MessageBox.Show("Введите логин!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var user = DBHelper.GetUserByLogin(login);
        if (user != null)
        {
            UserFIO = $"{user.LastName} {user.FirstName} {user.MiddleName}";
            UserRole = user.RoleId;
            UserId = user.Id;
            DialogResult = true;
        }
        else MessageBox.Show("Пользователь с таким логином не найден!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
