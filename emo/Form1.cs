using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using emo;

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
            this.Load += Form1_Load;
            this.Shown += Form1_Shown;
            this.Resize += (s, e) => UpdateCardsWidth();
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            // Принудительно обновляем ширину карточек после того, как окно отрисовалось
            UpdateCardsWidth();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            using (var loginForm = new LoginForm())
            {
                if (loginForm.ShowDialog() == DialogResult.OK)
                {
                    CurrentUserFIO = loginForm.UserFIO;
                    CurrentUserId = loginForm.UserId;
                    CurrentUserRole = loginForm.UserRole;

                    labelUser.Text = CurrentUserFIO;
                    btnOrders.Visible = (CurrentUserRole == 1 || CurrentUserRole == 2);

                    LoadProducts();
                }
                else Application.Exit();
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
                        Height = 140,
                        Width = panelContent.ClientSize.Width - 25,
                        Margin = new Padding(0, 0, 0, 10),
                        Cursor = Cursors.Hand
                    };

                    card.Click += (s, e) =>
                    {
                        using (var productForm = new ProductForm(p, CurrentUserId))
                            productForm.ShowDialog();
                    };
                   

                    var pic = new PictureBox { Size = new Size(80, 80), Location = new Point(10, 15), SizeMode = PictureBoxSizeMode.Zoom };
                    string imgPath = Path.Combine(Application.StartupPath, "Resources", p.Image ?? "");
                    if (!File.Exists(imgPath)) imgPath = Path.Combine(Application.StartupPath, "Resources", "picture.png");
                    if (File.Exists(imgPath)) pic.Image = Image.FromFile(imgPath);
                    card.Controls.Add(pic);

                    card.Controls.Add(new Label { Text = $"{p.Manufacture} | {p.Name}", Font = new Font("Calibri", 12, FontStyle.Bold), Location = new Point(100, 10), AutoSize = true });
                    card.Controls.Add(new Label { Text = $"Категория: {p.Category}", Font = new Font("Calibri", 10), Location = new Point(100, 35), AutoSize = true });
                    card.Controls.Add(new Label { Text = $"Количество: {p.CountText}", Font = new Font("Calibri", 10), Location = new Point(100, 55), AutoSize = true });
                    card.Controls.Add(new Label { Text = $"Состав: {p.Composition}", Font = new Font("Calibri", 9), Location = new Point(100, 75), AutoSize = true, MaximumSize = new Size(400, 0) });
                    card.Controls.Add(new Label { Text = $"{p.Price} ₽", Font = new Font("Calibri", 14, FontStyle.Bold), Location = new Point(560, 35), AutoSize = true });

                    panelContent.Controls.Add(card);

                    Action clickAction = () =>
                    {
                        // Временная блокировка, чтобы не было двойного клика
                        if (this.Tag != null && this.Tag.ToString() == "blocked") return;
                        this.Tag = "blocked";

                        try
                        {
                            using (var productForm = new ProductForm(p, CurrentUserId))
                            {
                                productForm.ShowDialog(); // Открываем форму, ждем закрытия
                            }
                            LoadProducts(); // Обновляем список после закрытия
                        }
                        finally
                        {
                            this.Tag = null; // Снимаем блокировку
                        }
                    };

                }
                btnCart.Text = $"Корзина";
            }
            catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}"); }
        }

        private void UpdateCardsWidth()
        {
            foreach (Control c in panelContent.Controls)
                if (c is Panel) c.Width = panelContent.ClientSize.Width - 25;
        }

        private void btnOrders_Click(object sender, EventArgs e)
        {
            using (var ordersForm = new OrdersForm(CurrentUserRole))
                ordersForm.ShowDialog();
        }

        private void btnLogout_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Вы уверены, что хотите выйти?", "Выход", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                this.Hide();
                using (var loginForm = new LoginForm())
                {
                    if (loginForm.ShowDialog() == DialogResult.OK)
                    {
                        CurrentUserFIO = loginForm.UserFIO;
                        CurrentUserId = loginForm.UserId;
                        CurrentUserRole = loginForm.UserRole;
                        labelUser.Text = CurrentUserFIO;
                        btnOrders.Visible = (CurrentUserRole == 1 || CurrentUserRole == 2);
                        LoadProducts();
                        this.Show();
                    }
                    else Application.Exit();
                }
            }
        }

        private void btnCart_Click(object sender, EventArgs e)
        {
            if (Cart.Items.Count == 0)
            {
                MessageBox.Show("Корзина пуста.", "Корзина");
                return;
            }

            using (var cartForm = new Form())
            {
                cartForm.Text = "Корзина";
                cartForm.Size = new Size(700, 450); // Немного увеличили размер для кнопок
                cartForm.MinimumSize = new Size(600, 350);
                cartForm.StartPosition = FormStartPosition.CenterParent;

                // Таблица для отображения товаров
                var grid = new DataGridView
                {
                    Dock = DockStyle.Top,
                    Height = 250,
                    ReadOnly = true,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                    AllowUserToAddRows = false,
                    SelectionMode = DataGridViewSelectionMode.FullRowSelect // Выделение целой строки
                };

                // Панель для кнопок внизу
                var panelBottom = new Panel { Dock = DockStyle.Bottom, Height = 60 };

                // Кнопка "Удалить выбранное"
                var btnRemove = new Button
                {
                    Text = "Удалить выбранное",
                    Location = new Point(20, 10),
                    Size = new Size(150, 40),
                    BackColor = ColorTranslator.FromHtml("#ff8080"), // Красный цвет
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Calibri", 10, FontStyle.Bold)
                };

                // Кнопка "Оформить заказ"
                var btnCheckout = new Button
                {
                    Text = "Оформить заказ",
                    Location = new Point(200, 10),
                    Size = new Size(180, 40),
                    BackColor = ColorTranslator.FromHtml("#70B2AF"), // Бирюзовый цвет
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Calibri", 10, FontStyle.Bold)
                };

                // Функция для обновления таблицы (чтобы не дублировать код)
                Action refreshGrid = () =>
                {
                    var dt = new System.Data.DataTable();
                    dt.Columns.Add("Товар");
                    dt.Columns.Add("Размер");
                    dt.Columns.Add("Кол-во");
                    dt.Columns.Add("Цена");
                    dt.Columns.Add("Сумма");

                    foreach (var item in Cart.Items)
                        dt.Rows.Add(item.ProductName, item.SizeName, item.Count, item.Price, item.Count * item.Price);

                    grid.DataSource = dt;
                };

                // Первичное заполнение таблицы
                refreshGrid();

                // Логика удаления
                btnRemove.Click += (s, ev) =>
                {
                    if (grid.SelectedRows.Count == 0)
                    {
                        MessageBox.Show("Выберите строку для удаления.", "Внимание");
                        return;
                    }
                    // Получаем индекс выбранной строки
                    int index = grid.SelectedRows[0].Index;
                    if (index >= 0 && index < Cart.Items.Count)
                    {
                        Cart.Items.RemoveAt(index); // Удаляем из списка
                        refreshGrid();              // Обновляем таблицу
                    }
                };

                // Логика оформления (как было, но с обновлением)
                btnCheckout.Click += (s, ev) =>
                {
                    if (Cart.Items.Count == 0)
                    {
                        MessageBox.Show("Корзина пуста.", "Внимание");
                        return;
                    }

                    try
                    {
                        using (var conn = new SqlConnection(DBHelper.connectionString))
                        {
                            conn.Open();
                            // Создаем один заказ
                            new SqlCommand($"INSERT INTO Orders_import$ ([Дата заказа], id_user) VALUES ('{DateTime.Now:yyyy-MM-dd HH:mm:ss}', {CurrentUserId})", conn).ExecuteNonQuery();
                            int orderId = Convert.ToInt32(new SqlCommand("SELECT MAX(id) FROM Orders_import$", conn).ExecuteScalar());

                            // Добавляем все позиции
                            foreach (var item in Cart.Items)
                            {
                                new SqlCommand($"INSERT INTO Order_stock$ (id_order, id_stock_items, [Количество]) VALUES ({orderId}, {item.StockItemId}, {item.Count})", conn).ExecuteNonQuery();
                                // Уменьшаем остатки
                                new SqlCommand($"UPDATE Stock_items$ SET count = count - {item.Count} WHERE id = {item.StockItemId}", conn).ExecuteNonQuery();
                            }
                        }
                        MessageBox.Show("Заказ успешно оформлен!", "Успех");
                        Cart.Clear();
                        cartForm.Close();
                        LoadProducts(); // Обновить витрину
                    }
                    catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}"); }
                };

                // Добавляем кнопки на панель
                panelBottom.Controls.Add(btnRemove);
                panelBottom.Controls.Add(btnCheckout);

                // Добавляем элементы на форму
                cartForm.Controls.Add(grid);
                cartForm.Controls.Add(panelBottom);

                cartForm.ShowDialog();
            }
        }
    }

    public static class Cart
    {
        public static List<CartItem> Items = new List<CartItem>();
        public static void Add(CartItem item) => Items.Add(item);
        public static void Clear() => Items.Clear();
    }

    public class CartItem
    {
        public int StockItemId { get; set; }
        public string ProductName { get; set; }
        public string SizeName { get; set; }
        public int Count { get; set; }
        public decimal Price { get; set; }
    }
}