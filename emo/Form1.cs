using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace emo
{
    public partial class Form1 : Form
    {
        private readonly Color accent = ColorTranslator.FromHtml("#70B2AF");
        private readonly Color alertColor = ColorTranslator.FromHtml("#ff8080");

        internal string CurrentUserFIO = "";
        internal int CurrentUserId = 0;
        internal int CurrentUserRole = 0;

        public Form1()
        {
            InitializeComponent();
            LoadResources();

            // Подписываемся на событие загрузки формы
            this.Load += Form1_Load;

            // Адаптивность
            this.Resize += (s, e) => UpdateCardsWidth();

        }

        private void btnOrders_Click(object sender, EventArgs e)
        {
            using (var ordersForm = new OrdersForm(CurrentUserRole))
            {
                ordersForm.ShowDialog();
            }
        }

        // Этот метод вызовется, когда главное окно откроется
        private void Form1_Load(object sender, EventArgs e)
        {
            // Вызываем авторизацию
            using (var loginForm = new LoginForm())
            {
                if (loginForm.ShowDialog() == DialogResult.OK)
                {
                    CurrentUserFIO = loginForm.UserFIO;
                    CurrentUserId = loginForm.UserId;
                    CurrentUserRole = loginForm.UserRole;

                    // Выводим ФИО в шапку
                    if (labelUser != null)
                        labelUser.Text = CurrentUserFIO;

                    // Загружаем товары только после успешного входа
                    LoadProducts();
                }
                else
                {
                    // Если пользователь закрыл окно авторизации — закрываем программу
                    Application.Exit();
                }
            }
            if (CurrentUserRole == 1 || CurrentUserRole == 2)
            {
                btnOrders.Visible = true;
            }
        }

        private void LoadResources()
        {
            string resPath = Path.Combine(Application.StartupPath, "Resources");
            if (File.Exists(Path.Combine(resPath, "icon.ico"))) this.Icon = new Icon(Path.Combine(resPath, "icon.ico"));
        }

        private void LoadProducts()
        {
            try
            {
                panelContent.Controls.Clear();
                var products = DBHelper.LoadProducts();

                foreach (var p in products)
                {
                    Color bg = p.TotalCount <= 3 ? alertColor : Color.White;

                    var card = new Panel
                    {
                        BackColor = bg,
                        BorderStyle = BorderStyle.FixedSingle,
                        Height = 110,
                        Width = panelContent.ClientSize.Width - 25,
                        Margin = new Padding(0, 0, 0, 10),
                        Cursor = Cursors.Hand
                    };

                    // Клик по карточке
                    card.Click += (s, e) =>
                    {
                        using (var productForm = new ProductForm(p, CurrentUserId))
                        {
                            productForm.ShowDialog();
                        }
                        LoadProducts(); // Обновить список после заказа (если количество изменилось)
                    };

                    // 1. Картинка
                    var pic = new PictureBox
                    {
                        Size = new Size(80, 80),
                        Location = new Point(10, 15),
                        SizeMode = PictureBoxSizeMode.Zoom
                    };
                    string imgPath = Path.Combine(Application.StartupPath, "Resources", p.Image ?? "");
                    if (!File.Exists(imgPath)) imgPath = Path.Combine(Application.StartupPath, "Resources", "picture.png");
                    if (File.Exists(imgPath)) pic.Image = Image.FromFile(imgPath);
                    card.Controls.Add(pic);

                    // 2. Текст
                    card.Controls.Add(new Label { Text = $"{p.Manufacture} | {p.Name}", Font = new Font("Calibri", 12, FontStyle.Bold), Location = new Point(100, 10), AutoSize = true });
                    card.Controls.Add(new Label { Text = $"Категория: {p.Category}", Font = new Font("Calibri", 10), Location = new Point(100, 35), AutoSize = true });
                    card.Controls.Add(new Label { Text = $"Количество: {p.CountText}", Font = new Font("Calibri", 10), Location = new Point(100, 55), AutoSize = true });
                    card.Controls.Add(new Label { Text = $"Состав: {p.Composition}", Font = new Font("Calibri", 9), Location = new Point(100, 75), AutoSize = true, MaximumSize = new Size(450, 0) });

                    // 3. Цена
                    card.Controls.Add(new Label { Text = $"{p.Price} ₽", Font = new Font("Calibri", 14, FontStyle.Bold), ForeColor = accent, Location = new Point(580, 35), AutoSize = true });

                    panelContent.Controls.Add(card);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private void UpdateCardsWidth()
        {
            foreach (Control c in panelContent.Controls)
            {
                if (c is Panel)
                {
                    c.Width = panelContent.ClientSize.Width - 25;
                }
            }
        }
    }
}