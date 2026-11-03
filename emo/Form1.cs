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
            this.Resize += (s, e) => UpdateCardsWidth();
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
                        Height = 110,
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
                }
                btnCart.Text = $"Корзина ({Cart.Items.Count})";
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
                cartForm.Size = new Size(600, 400);
                cartForm.StartPosition = FormStartPosition.CenterParent;

                var grid = new DataGridView
                {
                    Dock = DockStyle.Top,
                    Height = 250,
                    ReadOnly = true,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                    AllowUserToAddRows = false
                };

                var dt = new System.Data.DataTable();
                dt.Columns.Add("Товар");
                dt.Columns.Add("Размер");
                dt.Columns.Add("Кол-во");
                dt.Columns.Add("Цена");
                dt.Columns.Add("Сумма");

                foreach (var item in Cart.Items)
                    dt.Rows.Add(item.ProductName, item.SizeName, item.Count, item.Price, item.Count * item.Price);

                grid.DataSource = dt;

                var btnCheckout = new Button
                {
                    Text = "Оформить заказ",
                    Dock = DockStyle.Bottom,
                    Height = 50,
                    BackColor = ColorTranslator.FromHtml("#70B2AF"),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Calibri", 12, FontStyle.Bold)
                };

                btnCheckout.Click += (s, ev) =>
                {
                    try
                    {
                        using (var conn = new SqlConnection(DBHelper.connectionString))
                        {
                            conn.Open();
                            new SqlCommand($"INSERT INTO Orders_import$ ([Дата заказа], id_user) VALUES ('{DateTime.Now:yyyy-MM-dd HH:mm:ss}', {CurrentUserId})", conn).ExecuteNonQuery();
                            int orderId = Convert.ToInt32(new SqlCommand("SELECT MAX(id) FROM Orders_import$", conn).ExecuteScalar());

                            foreach (var item in Cart.Items)
                            {
                                new SqlCommand($"INSERT INTO Order_stock$ (id_order, id_stock_items, [Количество]) VALUES ({orderId}, {item.StockItemId}, {item.Count})", conn).ExecuteNonQuery();
                                new SqlCommand($"UPDATE Stock_items$ SET count = count - {item.Count} WHERE id = {item.StockItemId}", conn).ExecuteNonQuery();
                            }
                        }
                        MessageBox.Show("Заказ успешно оформлен!", "Успех");
                        Cart.Clear();
                        cartForm.Close();
                        LoadProducts();
                        btnCart.Text = "Корзина (0)";
                    }
                    catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}"); }
                };

                cartForm.Controls.Add(grid);
                cartForm.Controls.Add(btnCheckout);
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