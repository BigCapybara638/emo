namespace emo
{
    partial class ProductForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.PictureBox picProduct;
        private System.Windows.Forms.Label lblName;
        private System.Windows.Forms.Label lblPrice;
        private System.Windows.Forms.Label lblDescription;
        private System.Windows.Forms.ComboBox cmbSize;
        private System.Windows.Forms.NumericUpDown numCount;
        private System.Windows.Forms.Button btnAddToOrder;
        private System.Windows.Forms.Label lblSize;
        private System.Windows.Forms.Label lblCount;
        private System.Windows.Forms.ToolTip toolTip1; // <--- ОБЪЯВЛЕНИЕ

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.picProduct = new System.Windows.Forms.PictureBox();
            this.lblName = new System.Windows.Forms.Label();
            this.lblPrice = new System.Windows.Forms.Label();
            this.lblDescription = new System.Windows.Forms.Label();
            this.cmbSize = new System.Windows.Forms.ComboBox();
            this.numCount = new System.Windows.Forms.NumericUpDown();
            this.btnAddToOrder = new System.Windows.Forms.Button();
            this.lblSize = new System.Windows.Forms.Label();
            this.lblCount = new System.Windows.Forms.Label();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components); // <--- СОЗДАНИЕ
            ((System.ComponentModel.ISupportInitialize)(this.picProduct)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numCount)).BeginInit();
            this.SuspendLayout();

            // picProduct
            this.picProduct.Location = new System.Drawing.Point(20, 20);
            this.picProduct.Size = new System.Drawing.Size(150, 150);
            this.picProduct.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;

            // lblName
            this.lblName.AutoSize = true;
            this.lblName.Font = new System.Drawing.Font("Calibri", 14F, System.Drawing.FontStyle.Bold);
            this.lblName.Location = new System.Drawing.Point(190, 20);
            this.lblName.MaximumSize = new System.Drawing.Size(300, 0);

            // lblPrice
            this.lblPrice.AutoSize = true;
            this.lblPrice.Font = new System.Drawing.Font("Calibri", 16F, System.Drawing.FontStyle.Bold);
            this.lblPrice.ForeColor = System.Drawing.ColorTranslator.FromHtml("#70B2AF");
            this.lblPrice.Location = new System.Drawing.Point(190, 60);

            // lblDescription
            this.lblDescription.AutoSize = true;
            this.lblDescription.Font = new System.Drawing.Font("Calibri", 10F);
            this.lblDescription.Location = new System.Drawing.Point(190, 100);
            this.lblDescription.MaximumSize = new System.Drawing.Size(300, 0);

            // lblSize
            this.lblSize.AutoSize = true;
            this.lblSize.Location = new System.Drawing.Point(20, 190);
            this.lblSize.Text = "Размер:";

            // cmbSize
            this.cmbSize.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbSize.Location = new System.Drawing.Point(80, 187);
            this.cmbSize.Size = new System.Drawing.Size(100, 25);

            // lblCount
            this.lblCount.AutoSize = true;
            this.lblCount.Location = new System.Drawing.Point(200, 190);
            this.lblCount.Text = "Количество:";

            // numCount
            this.numCount.Location = new System.Drawing.Point(290, 187);
            this.numCount.Minimum = 1;
            this.numCount.Maximum = 10;
            this.numCount.Value = 1;
            this.numCount.Size = new System.Drawing.Size(60, 25);

            // btnAddToOrder
            this.btnAddToOrder.BackColor = System.Drawing.ColorTranslator.FromHtml("#70B2AF");
            this.btnAddToOrder.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAddToOrder.Font = new System.Drawing.Font("Calibri", 12F, System.Drawing.FontStyle.Bold);
            this.btnAddToOrder.ForeColor = System.Drawing.Color.White;
            this.btnAddToOrder.Location = new System.Drawing.Point(20, 240);
            this.btnAddToOrder.Size = new System.Drawing.Size(480, 40);
            this.btnAddToOrder.Text = "Добавить в заказ";
            this.btnAddToOrder.UseVisualStyleBackColor = false;
            this.btnAddToOrder.Click += new System.EventHandler(this.btnAddToOrder_Click);

            // ПОДСКАЗКИ (после того, как все элементы созданы)
            this.toolTip1.SetToolTip(this.btnAddToOrder, "Добавить выбранный товар в корзину");
            this.toolTip1.SetToolTip(this.cmbSize, "Выберите доступный размер");
            this.toolTip1.SetToolTip(this.numCount, "Укажите количество пар (не больше остатка на складе)");

            // ProductForm
            this.ClientSize = new System.Drawing.Size(520, 300);
            this.Controls.Add(this.picProduct);
            this.Controls.Add(this.lblName);
            this.Controls.Add(this.lblPrice);
            this.Controls.Add(this.lblDescription);
            this.Controls.Add(this.lblSize);
            this.Controls.Add(this.cmbSize);
            this.Controls.Add(this.lblCount);
            this.Controls.Add(this.numCount);
            this.Controls.Add(this.btnAddToOrder);
            this.Font = new System.Drawing.Font("Calibri", 10F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Оформление заказа";
            ((System.ComponentModel.ISupportInitialize)(this.picProduct)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numCount)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}