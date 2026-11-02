using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace emo
{
    public partial class OrdersForm : Form
    {
        private int _userRole;

        public OrdersForm(int userRole)
        {
            InitializeComponent();
            _userRole = userRole;

            // Редактирование только для админа (роль 1)
            btnEditDate.Visible = (_userRole == 1);

            LoadOrders();
        }

        private void LoadOrders()
        {
            using (var conn = new SqlConnection(DBHelper.connectionString))
            {
                conn.Open();
                string q = @"SELECT o.id, o.[Дата заказа], 
                                    u.[Фамилия] + ' ' + u.[Имя] + ' ' + u.[Отчество] AS [ФИО клиента]
                             FROM Orders_import$ o
                             JOIN Users$ u ON o.id_user = u.id
                             ORDER BY o.[Дата заказа] DESC";

                using (var adapter = new SqlDataAdapter(q, conn))
                {
                    var dt = new DataTable();
                    adapter.Fill(dt);
                    dgvOrders.DataSource = dt;
                }
            }
        }

        private void btnDeleteOrder_Click(object sender, EventArgs e)
        {
            if (dgvOrders.SelectedRows.Count == 0) return;
            int orderId = Convert.ToInt32(dgvOrders.SelectedRows[0].Cells["id"].Value);

            if (MessageBox.Show("Удалить заказ?", "Подтверждение", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                using (var conn = new SqlConnection(DBHelper.connectionString))
                {
                    conn.Open();
                    new SqlCommand($"DELETE FROM Order_stock$ WHERE id_order = {orderId}", conn).ExecuteNonQuery();
                    new SqlCommand($"DELETE FROM Orders_import$ WHERE id = {orderId}", conn).ExecuteNonQuery();
                }
                LoadOrders();
            }
        }

        private void btnViewDetails_Click(object sender, EventArgs e)
        {
            if (dgvOrders.SelectedRows.Count == 0) return;
            int orderId = Convert.ToInt32(dgvOrders.SelectedRows[0].Cells["id"].Value);

            using (var conn = new SqlConnection(DBHelper.connectionString))
            {
                conn.Open();
                string q = @"SELECT p.[Наименование товара], s.[Размер], os.[Количество], p.[Цена] AS [Цена за ед.], 
                                    (os.[Количество] * p.[Цена]) AS [Сумма]
                             FROM Order_stock$ os
                             JOIN Stock_items$ si ON os.id_stock_items = si.id
                             JOIN Products$ p ON si.id_tovar = p.id
                             JOIN Sizes$ s ON si.id_sizes = s.id
                             WHERE os.id_order = @id";

                using (var cmd = new SqlCommand(q, conn))
                {
                    cmd.Parameters.AddWithValue("@id", orderId);
                    using (var adapter = new SqlDataAdapter(cmd))
                    {
                        var dt = new DataTable();
                        adapter.Fill(dt);

                        var detailsForm = new Form { Text = "Состав заказа", Size = new Size(700, 300), StartPosition = FormStartPosition.CenterParent };
                        var grid = new DataGridView { Dock = DockStyle.Fill, DataSource = dt, ReadOnly = true, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
                        detailsForm.Controls.Add(grid);
                        detailsForm.ShowDialog();
                    }
                }
            }
        }

        private void btnEditDate_Click(object sender, EventArgs e)
        {
            if (dgvOrders.SelectedRows.Count == 0) return;
            int orderId = Convert.ToInt32(dgvOrders.SelectedRows[0].Cells["id"].Value);
            DateTime currentDate = Convert.ToDateTime(dgvOrders.SelectedRows[0].Cells["Дата заказа"].Value);

            using (var editForm = new Form())
            {
                editForm.Text = "Изменить дату заказа";
                editForm.Size = new Size(300, 150);
                editForm.StartPosition = FormStartPosition.CenterParent;

                var dtp = new DateTimePicker { Value = currentDate, Location = new Point(20, 20), Width = 240 };
                var btnSave = new Button { Text = "Сохранить", Location = new Point(20, 60), Width = 240, BackColor = ColorTranslator.FromHtml("#70B2AF"), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };

                btnSave.Click += (s, ev) =>
                {
                    using (var conn = new SqlConnection(DBHelper.connectionString))
                    {
                        conn.Open();
                        new SqlCommand($"UPDATE Orders_import$ SET [Дата заказа] = '{dtp.Value:yyyy-MM-dd HH:mm:ss}' WHERE id = {orderId}", conn).ExecuteNonQuery();
                    }
                    editForm.Close();
                };

                editForm.Controls.Add(dtp);
                editForm.Controls.Add(btnSave);
                editForm.ShowDialog();
            }
            LoadOrders();
        }
    }
}
