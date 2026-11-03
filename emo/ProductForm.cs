using System;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using emo;

namespace emo
{
    public partial class ProductForm : Form
    {
        private int _productId;
        private Product _product;

        public ProductForm(Product product, int userId)
        {
            InitializeComponent();
            _product = product;
            _productId = product.Id;
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
            if (cmbSize.Items.Count > 0) { cmbSize.SelectedIndex = 0; numCount.Maximum = 1000; }
            else { MessageBox.Show("Нет товара в наличии."); btnAddToOrder.Enabled = false; }
        }

        private void btnAddToOrder_Click(object sender, EventArgs e)
        {
            if (cmbSize.SelectedItem == null) return;
            var item = (ComboBoxItem)cmbSize.SelectedItem;
            int count = (int)numCount.Value;

            if (count > item.MaxCount)
            {
                MessageBox.Show($"На складе только {item.MaxCount} пар. Уменьшите количество.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Cart.Add(new CartItem
            {
                StockItemId = item.Value,
                ProductName = _product.Name,
                SizeName = item.Text.Split(' ')[0],
                Count = count,
                Price = _product.Price
            });

            MessageBox.Show($"Товар добавлен в корзину!\nПозиций в корзине: {Cart.Items.Count}", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.Close();
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