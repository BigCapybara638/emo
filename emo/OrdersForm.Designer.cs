namespace emo
{
    partial class OrdersForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.DataGridView dgvOrders;
        private System.Windows.Forms.Button btnViewDetails;
        private System.Windows.Forms.Button btnDeleteOrder;
        private System.Windows.Forms.Button btnEditDate;
        private System.Windows.Forms.Button btnAddItem;
        private System.Windows.Forms.ToolTip toolTip1;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.dgvOrders = new System.Windows.Forms.DataGridView();
            this.btnViewDetails = new System.Windows.Forms.Button();
            this.btnDeleteOrder = new System.Windows.Forms.Button();
            this.btnEditDate = new System.Windows.Forms.Button();
            this.btnAddItem = new System.Windows.Forms.Button();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.dgvOrders)).BeginInit();
            this.SuspendLayout();

            // dgvOrders (сначала создан, потом настроен)
            this.dgvOrders.AllowUserToAddRows = false;
            this.dgvOrders.AllowUserToDeleteRows = false;
            this.dgvOrders.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvOrders.BackgroundColor = System.Drawing.Color.White;
            this.dgvOrders.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvOrders.Location = new System.Drawing.Point(20, 20);
            this.dgvOrders.ReadOnly = true;
            this.dgvOrders.Size = new System.Drawing.Size(740, 400);
            this.dgvOrders.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;

            // btnViewDetails
            this.btnViewDetails.BackColor = System.Drawing.ColorTranslator.FromHtml("#70B2AF");
            this.btnViewDetails.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnViewDetails.ForeColor = System.Drawing.Color.White;
            this.btnViewDetails.Location = new System.Drawing.Point(20, 440);
            this.btnViewDetails.Size = new System.Drawing.Size(150, 40);
            this.btnViewDetails.Text = "Состав заказа";
            this.btnViewDetails.Click += new System.EventHandler(this.btnViewDetails_Click);

            // btnDeleteOrder
            this.btnDeleteOrder.BackColor = System.Drawing.ColorTranslator.FromHtml("#ff8080");
            this.btnDeleteOrder.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDeleteOrder.ForeColor = System.Drawing.Color.White;
            this.btnDeleteOrder.Location = new System.Drawing.Point(180, 440);
            this.btnDeleteOrder.Size = new System.Drawing.Size(150, 40);
            this.btnDeleteOrder.Text = "Удалить заказ";
            this.btnDeleteOrder.Click += new System.EventHandler(this.btnDeleteOrder_Click);

            // btnEditDate
            this.btnEditDate.BackColor = System.Drawing.Color.Gray;
            this.btnEditDate.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnEditDate.ForeColor = System.Drawing.Color.White;
            this.btnEditDate.Location = new System.Drawing.Point(340, 440);
            this.btnEditDate.Size = new System.Drawing.Size(150, 40);
            this.btnEditDate.Text = "Изменить дату";
            this.btnEditDate.Click += new System.EventHandler(this.btnEditDate_Click);

            // btnAddItem
            this.btnAddItem.BackColor = System.Drawing.ColorTranslator.FromHtml("#70B2AF");
            this.btnAddItem.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAddItem.ForeColor = System.Drawing.Color.White;
            this.btnAddItem.Location = new System.Drawing.Point(500, 440);
            this.btnAddItem.Size = new System.Drawing.Size(180, 40);
            this.btnAddItem.Text = "Добавить товар в заказ";
            this.btnAddItem.Click += new System.EventHandler(this.btnAddItem_Click);

            // Подсказки
            this.toolTip1.SetToolTip(this.btnViewDetails, "Просмотреть состав заказа");
            this.toolTip1.SetToolTip(this.btnDeleteOrder, "Удалить заказ (товары вернутся на склад)");
            this.toolTip1.SetToolTip(this.btnEditDate, "Изменить дату заказа (только для администратора)");
            this.toolTip1.SetToolTip(this.btnAddItem, "Добавить товар в выбранный заказ");

            // OrdersForm
            this.ClientSize = new System.Drawing.Size(800, 500);
            this.MinimumSize = new System.Drawing.Size(800, 500);
            this.Controls.Add(this.dgvOrders);
            this.Controls.Add(this.btnViewDetails);
            this.Controls.Add(this.btnDeleteOrder);
            this.Controls.Add(this.btnEditDate);
            this.Controls.Add(this.btnAddItem);
            this.Font = new System.Drawing.Font("Calibri", 10F);
            this.Text = "Список заказов";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            ((System.ComponentModel.ISupportInitialize)(this.dgvOrders)).EndInit();
            this.ResumeLayout(false);
        }
    }
}