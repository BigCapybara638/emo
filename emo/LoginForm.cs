using System;
using System.Windows.Forms;

namespace emo
{
    public partial class LoginForm : Form
    {
        public string UserFIO { get; private set; } = "";
        public int UserRole { get; private set; } = 0;
        public int UserId { get; private set; } = 0;

        public LoginForm()
        {
            InitializeComponent();
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            string login = txtLogin.Text.Trim();
            if (string.IsNullOrEmpty(login))
            {
                MessageBox.Show("Введите логин!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Ищем пользователя в БД
            var user = DBHelper.GetUserByLogin(login);
            if (user != null)
            {
                UserFIO = $"{user.LastName} {user.FirstName} {user.MiddleName}";
                UserRole = user.RoleId;
                UserId = user.Id;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show("Пользователь с таким логином не найден!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}