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
        private int _productId;
        private int _userId;
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
            cmbSize.Items.Clear();
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
                            int count = Convert.ToInt32(reader["count"]);
                            cmbSize.Items.Add(new ComboBoxItem
                            {
                                Text = $"{reader["Размер"]} (осталось {count})",
                                Value = Convert.ToInt32(reader["id"]),
                                MaxCount = count
                            });
                        }
                    }
                }
            }

            if (cmbSize.Items.Count > 0)
            {
                cmbSize.SelectedIndex = 0;
                numCount.Maximum = 1000; // Разрешаем вводить много, чтобы выдать ошибку
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

                    // 1. Создаем заказ (дата и пользователь)
                    string qOrder = @"INSERT INTO Orders_import$ ([Дата заказа], id_user) 
                                      VALUES (@date, @user)";
                    using (var cmd = new SqlCommand(qOrder, conn))
                    {
                        cmd.Parameters.AddWithValue("@date", DateTime.Now);
                        cmd.Parameters.AddWithValue("@user", _userId);
                        cmd.ExecuteNonQuery();
                    }

                    // 2. Получаем ID последнего заказа (даже если IDENTITY не настроен)
                    int orderId = 0;
                    string qGetId = "SELECT MAX(id) FROM Orders_import$";
                    using (var cmd = new SqlCommand(qGetId, conn))
                    {
                        object result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                            orderId = Convert.ToInt32(result);
                    }

                    if (orderId == 0)
                    {
                        MessageBox.Show("Не удалось создать заказ (таблица Orders пуста или ошибка ID).");
                        return;
                    }

                    // 3. Добавляем товар в заказ
                    string qItem = @"INSERT INTO Order_stock$ (id_order, id_stock_items, [Количество]) 
                                     VALUES (@order, @stock, @count)";
                    using (var cmd = new SqlCommand(qItem, conn))
                    {
                        cmd.Parameters.AddWithValue("@order", orderId);
                        cmd.Parameters.AddWithValue("@stock", item.Value);
                        cmd.Parameters.AddWithValue("@count", count);
                        cmd.ExecuteNonQuery();
                    }

                    // 4. Уменьшаем остаток на складе
                    string qUpdate = @"UPDATE Stock_items$ SET count = count - @count WHERE id = @stock";
                    using (var cmd = new SqlCommand(qUpdate, conn))
                    {
                        cmd.Parameters.AddWithValue("@count", count);
                        cmd.Parameters.AddWithValue("@stock", item.Value);
                        cmd.ExecuteNonQuery();
                    }
                }
                MessageBox.Show("Заказ успешно оформлен!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при оформлении: {ex.Message}\n\n{ex.StackTrace}");
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