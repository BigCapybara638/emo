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
            btnEditDate.Visible = (_userRole == 1); // Редактирование только админу
            LoadOrders();
        }

        private void LoadOrders()
        {
            try
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
            catch (Exception ex) { MessageBox.Show($"Ошибка загрузки заказов: {ex.Message}"); }
        }

        private void btnDeleteOrder_Click(object sender, EventArgs e)
        {
            if (dgvOrders.SelectedRows.Count == 0) return;
            object idObj = dgvOrders.SelectedRows[0].Cells["id"].Value;
            if (idObj == null || idObj == DBNull.Value) return;
            int orderId = Convert.ToInt32(idObj);

            if (MessageBox.Show("Удалить заказ? Товары вернутся на склад.", "Подтверждение", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                try
                {
                    using (var conn = new SqlConnection(DBHelper.connectionString))
                    {
                        conn.Open();
                        // 1. Возвращаем товары на склад
                        string qReturn = @"UPDATE si SET si.count = si.count + os.[Количество]
                                           FROM Stock_items$ si
                                           JOIN Order_stock$ os ON si.id = os.id_stock_items
                                           WHERE os.id_order = @id";
                        using (var cmd = new SqlCommand(qReturn, conn))
                        {
                            cmd.Parameters.AddWithValue("@id", orderId);
                            cmd.ExecuteNonQuery();
                        }
                        // 2. Удаляем состав и заказ
                        new SqlCommand($"DELETE FROM Order_stock$ WHERE id_order = {orderId}", conn).ExecuteNonQuery();
                        new SqlCommand($"DELETE FROM Orders_import$ WHERE id = {orderId}", conn).ExecuteNonQuery();
                    }
                    LoadOrders();
                }
                catch (Exception ex) { MessageBox.Show($"Ошибка при удалении: {ex.Message}"); }
            }
        }

        private void btnViewDetails_Click(object sender, EventArgs e)
        {
            if (dgvOrders.SelectedRows.Count == 0) return;
            object idObj = dgvOrders.SelectedRows[0].Cells["id"].Value;
            if (idObj == null || idObj == DBNull.Value) return;
            int orderId = Convert.ToInt32(idObj);

            try
            {
                using (var conn = new SqlConnection(DBHelper.connectionString))
                {
                    conn.Open();
                    string q = @"SELECT p.[Наименование товара], s.[Размер], os.[Количество], 
                                        p.[Цена] AS [Цена за ед.], 
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
                            if (dt.Rows.Count == 0) { MessageBox.Show("В заказе нет товаров."); return; }

                            var detailsForm = new Form { Text = $"Состав заказа №{orderId}", Size = new Size(700, 300), StartPosition = FormStartPosition.CenterParent };
                            var grid = new DataGridView { Dock = DockStyle.Fill, DataSource = dt, ReadOnly = true, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, AllowUserToAddRows = false };
                            detailsForm.Controls.Add(grid);
                            detailsForm.ShowDialog();
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}"); }
        }

        private void btnEditDate_Click(object sender, EventArgs e)
        {
            if (dgvOrders.SelectedRows.Count == 0) return;
            object idObj = dgvOrders.SelectedRows[0].Cells["id"].Value;
            if (idObj == null || idObj == DBNull.Value) return;
            int orderId = Convert.ToInt32(idObj);

            object dateObj = dgvOrders.SelectedRows[0].Cells["Дата заказа"].Value;
            DateTime currentDate = (dateObj == null || dateObj == DBNull.Value) ? DateTime.Now : Convert.ToDateTime(dateObj);

            using (var editForm = new Form())
            {
                editForm.Text = "Изменить дату заказа";
                editForm.Size = new Size(300, 150);
                editForm.StartPosition = FormStartPosition.CenterParent;

                var dtp = new DateTimePicker { Value = currentDate, Location = new Point(20, 20), Width = 240 };
                var btnSave = new Button { Text = "Сохранить", Location = new Point(20, 60), Width = 240, BackColor = ColorTranslator.FromHtml("#70B2AF"), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };

                btnSave.Click += (s, ev) =>
                {
                    try
                    {
                        using (var conn = new SqlConnection(DBHelper.connectionString))
                        {
                            conn.Open();
                            new SqlCommand($"UPDATE Orders_import$ SET [Дата заказа] = '{dtp.Value:yyyy-MM-dd HH:mm:ss}' WHERE id = {orderId}", conn).ExecuteNonQuery();
                        }
                        editForm.Close();
                        LoadOrders();
                    }
                    catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}"); }
                };

                editForm.Controls.Add(dtp);
                editForm.Controls.Add(btnSave);
                editForm.ShowDialog();
            }
        }

        private void btnAddItem_Click(object sender, EventArgs e)
        {
            if (dgvOrders.SelectedRows.Count == 0)
            {
                MessageBox.Show("Выберите заказ, в который нужно добавить товар.", "Внимание");
                return;
            }

            object idObj = dgvOrders.SelectedRows[0].Cells["id"].Value;
            if (idObj == null || idObj == DBNull.Value)
            {
                MessageBox.Show("У выбранного заказа отсутствует ID.", "Ошибка");
                return;
            }
            int orderId = Convert.ToInt32(idObj);

            using (var addForm = new AddItemForm(orderId))
            {
                if (addForm.ShowDialog() == DialogResult.OK)
                {
                    LoadOrders(); // Обновляем список заказов
                }
            }
        }
    }
    }