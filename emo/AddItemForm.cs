using System;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace emo
{
    public partial class AddItemForm : Form
    {
        private int _orderId;

        public AddItemForm(int orderId)
        {
            InitializeComponent();
            _orderId = orderId;
            LoadProducts();
        }

        private void LoadProducts()
        {
            using (var conn = new SqlConnection(DBHelper.connectionString))
            {
                conn.Open();
                string q = "SELECT id, [Наименование товара] FROM Products$ ORDER BY [Наименование товара]";
                using (var cmd = new SqlCommand(q, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        // Используем ComboBoxItem из ProductForm.cs
                        cmbProduct.Items.Add(new ComboBoxItem
                        {
                            Text = reader["Наименование товара"].ToString(),
                            Value = Convert.ToInt32(reader["id"])
                        });
                    }
                }
            }
            if (cmbProduct.Items.Count > 0) cmbProduct.SelectedIndex = 0;
        }

        private void cmbProduct_SelectedIndexChanged(object sender, EventArgs e)
        {
            cmbSize.Items.Clear();
            if (cmbProduct.SelectedItem == null) return;

            int productId = ((ComboBoxItem)cmbProduct.SelectedItem).Value;

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
                    cmd.Parameters.AddWithValue("@pid", productId);
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
            if (cmbSize.Items.Count > 0) cmbSize.SelectedIndex = 0;
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (cmbSize.SelectedItem == null) { MessageBox.Show("Выберите размер."); return; }

            var item = (ComboBoxItem)cmbSize.SelectedItem;
            int count = (int)numCount.Value;

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
                    string q = "INSERT INTO Order_stock$ (id_order, id_stock_items, [Количество]) VALUES (@order, @stock, @count)";
                    using (var cmd = new SqlCommand(q, conn))
                    {
                        cmd.Parameters.AddWithValue("@order", _orderId);
                        cmd.Parameters.AddWithValue("@stock", item.Value);
                        cmd.Parameters.AddWithValue("@count", count);
                        cmd.ExecuteNonQuery();
                    }

                    string qUpdate = "UPDATE Stock_items$ SET count = count - @count WHERE id = @stock";
                    using (var cmd = new SqlCommand(qUpdate, conn))
                    {
                        cmd.Parameters.AddWithValue("@count", count);
                        cmd.Parameters.AddWithValue("@stock", item.Value);
                        cmd.ExecuteNonQuery();
                    }
                }
                MessageBox.Show("Товар добавлен в заказ!", "Успех");
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}"); }
        }
    }
}