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
            if (idObj == null || idObj == DBNull.Value) { MessageBox.Show("Нет ID заказа."); return; }
            int orderId = Convert.ToInt32(idObj);

            var detailsForm = new Form
            {
                Text = $"Состав заказа №{orderId}",
                Size = new Size(800, 450),
                MinimumSize = new Size(800, 450),
                StartPosition = FormStartPosition.CenterParent,
                BackColor = Color.White
            };

            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };

            var panelBottom = new Panel { Dock = DockStyle.Bottom, Height = 50 };

            var btnRemoveItem = new Button
            {
                Text = "Удалить выбранную позицию",
                Location = new Point(10, 10),
                Size = new Size(220, 30),
                BackColor = ColorTranslator.FromHtml("#ff8080"),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Calibri", 10, FontStyle.Bold)
            };

            Action loadDetails = () =>
            {
                try
                {
                    using (var conn = new SqlConnection(DBHelper.connectionString))
                    {
                        conn.Open();
                        string q = @"SELECT p.[Наименование товара], s.[Размер], os.[Количество], 
                                    p.[Цена] AS [Цена за ед.], 
                                    (os.[Количество] * p.[Цена]) AS [Сумма],
                                    os.id_stock_items AS [StockItemId]
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
                                grid.DataSource = dt;

                                if (grid.Columns.Contains("StockItemId"))
                                    grid.Columns["StockItemId"].Visible = false;
                            }
                        }
                    }
                }
                catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}"); }
            };

            loadDetails();

            btnRemoveItem.Click += (s, ev) =>
            {
                if (grid.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Выберите товар для удаления.", "Внимание");
                    return;
                }

                if (MessageBox.Show("Удалить эту позицию из заказа? Товар вернется на склад.", "Подтверждение", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    try
                    {
                        var row = grid.SelectedRows[0];
                        int stockItemId = Convert.ToInt32(row.Cells["StockItemId"].Value);
                        int count = Convert.ToInt32(row.Cells["Количество"].Value);

                        using (var conn = new SqlConnection(DBHelper.connectionString))
                        {
                            conn.Open();

                            // 1. Возвращаем на склад
                            new SqlCommand($"UPDATE Stock_items$ SET count = count + {count} WHERE id = {stockItemId}", conn).ExecuteNonQuery();

                            // 2. Удаляем позицию из заказа
                            new SqlCommand($"DELETE FROM Order_stock$ WHERE id_order = {orderId} AND id_stock_items = {stockItemId}", conn).ExecuteNonQuery();

                            // 3. ПРОВЕРКА: остались ли ещё товары в заказе?
                            int remaining = Convert.ToInt32(new SqlCommand($"SELECT COUNT(*) FROM Order_stock$ WHERE id_order = {orderId}", conn).ExecuteScalar());

                            if (remaining == 0)
                            {
                                // Если товаров нет — удаляем сам заказ
                                new SqlCommand($"DELETE FROM Orders_import$ WHERE id = {orderId}", conn).ExecuteNonQuery();

                                MessageBox.Show("Все позиции удалены. Заказ полностью удалён.", "Заказ удалён");
                                detailsForm.Close();
                                LoadOrders(); // Обновляем список заказов в главной форме
                                return;
                            }
                        }

                        // Если заказ ещё есть, просто обновляем таблицу
                        loadDetails();
                        LoadOrders();
                        MessageBox.Show("Позиция удалена, товар возвращен на склад.", "Успех");
                    }
                    catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}"); }
                }
            };

            panelBottom.Controls.Add(btnRemoveItem);
            detailsForm.Controls.Add(grid);
            detailsForm.Controls.Add(panelBottom);
            detailsForm.ShowDialog();
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