using System;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using static emo.DBHelper;

namespace emo
{
    public partial class ProductForm : Form
    {
        private int _productId, _userId;
        private Product _product;

        public ProductForm(Product product, int userId)
        {
            InitializeComponent();
            _product = product;
            _productId = product.Id;
            _userId = userId;

            LoadProductData();
            LoadSizes();
        }

        private void LoadProductData()
        {
            lblName.Text = _product.Name;
            lblPrice.Text = $"{_product.Price} ₽";
            lblDescription.Text = $"Производство: {_product.Manufacture}\nСостав: {_product.Composition}";

            string imgPath = Path.Combine(Application.StartupPath, "Resources", _product.Image ?? "");
            if (!File.Exists(imgPath)) imgPath = Path.Combine(Application.StartupPath, "Resources", "picture.png");
            if (File.Exists(imgPath)) picProduct.Image = Image.FromFile(imgPath);
        }

        private void LoadSizes()
        {
            using (var conn = new SqlConnection(DBHelper.connectionString))
            {
                conn.Open();
                string q = @"SELECT si.id, s.[Размер], si.count 
                             FROM Stock_items$ si
                             JOIN Sizes$ s ON si.id_sizes = s.id
                             WHERE si.id_tovar = @pid AND si.count > 0
                             ORDER BY s.[Размер]";
                using (var cmd = new SqlCommand(q, conn))
                {
                    cmd.Parameters.AddWithValue("@pid", _productId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            cmbSize.Items.Add(new ComboBoxItem
                            {
                                Text = $"{reader["Размер"]} (осталось {reader["count"]})",
                                Value = Convert.ToInt32(reader["id"]),
                                MaxCount = Convert.ToInt32(reader["count"])
                            });
                        }
                    }
                }
            }

            if (cmbSize.Items.Count > 0)
            {
                cmbSize.SelectedIndex = 0;
                numCount.Maximum = 1000; // Разрешаем вводить много, чтобы потом выдать ошибку
            }
            else
            {
                MessageBox.Show("Нет товара в наличии.");
                btnAddToOrder.Enabled = false;
            }
        }

        private void btnAddToOrder_Click(object sender, EventArgs e)
        {
            if (cmbSize.SelectedItem == null) return;

            var item = (ComboBoxItem)cmbSize.SelectedItem;
            int count = (int)numCount.Value;

            // Проверка на превышение остатка
            if (count > item.MaxCount)
            {
                MessageBox.Show($"На складе только {item.MaxCount} пар. Уменьшите количество.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (var conn = new SqlConnection(DBHelper.connectionString))
                {
                    conn.Open();

                    // 1. Создаем заказ
                    new SqlCommand($"INSERT INTO Orders_import$ ([Дата заказа], id_user) VALUES ('{DateTime.Now:yyyy-MM-dd HH:mm:ss}', {_userId})", conn).ExecuteNonQuery();

                    // 2. Получаем ID заказа и добавляем товар
                    int orderId = Convert.ToInt32(new SqlCommand("SELECT MAX(id) FROM Orders_import$", conn).ExecuteScalar());
                    new SqlCommand($"INSERT INTO Order_stock$ (id_order, id_stock_items, [Количество]) VALUES ({orderId}, {item.Value}, {count})", conn).ExecuteNonQuery();

                    // 3. Уменьшаем остаток
                    new SqlCommand($"UPDATE Stock_items$ SET count = count - {count} WHERE id = {item.Value}", conn).ExecuteNonQuery();
                }
                MessageBox.Show("Заказ оформлен!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }
    }

    public class ComboBoxItem
    {
        public string Text { get; set; }
        public int Value { get; set; }
        public int MaxCount { get; set; }
        public override string ToString() => Text;
    }
}