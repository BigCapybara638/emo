namespace emo
{
    partial class AddItemForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.ComboBox cmbProduct;
        private System.Windows.Forms.ComboBox cmbSize;
        private System.Windows.Forms.NumericUpDown numCount;
        private System.Windows.Forms.Button btnAdd;
        private System.Windows.Forms.Label lblProduct;
        private System.Windows.Forms.Label lblSize;
        private System.Windows.Forms.Label lblCount;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.cmbProduct = new System.Windows.Forms.ComboBox();
            this.cmbSize = new System.Windows.Forms.ComboBox();
            this.numCount = new System.Windows.Forms.NumericUpDown();
            this.btnAdd = new System.Windows.Forms.Button();
            this.lblProduct = new System.Windows.Forms.Label();
            this.lblSize = new System.Windows.Forms.Label();
            this.lblCount = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.numCount)).BeginInit();
            this.SuspendLayout();

            // lblProduct
            this.lblProduct.AutoSize = true;
            this.lblProduct.Location = new System.Drawing.Point(20, 20);
            this.lblProduct.Text = "Товар:";

            // cmbProduct
            this.cmbProduct.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbProduct.Location = new System.Drawing.Point(100, 17);
            this.cmbProduct.Size = new System.Drawing.Size(300, 25);
            this.cmbProduct.SelectedIndexChanged += new System.EventHandler(this.cmbProduct_SelectedIndexChanged);

            // lblSize
            this.lblSize.AutoSize = true;
            this.lblSize.Location = new System.Drawing.Point(20, 60);
            this.lblSize.Text = "Размер:";

            // cmbSize
            this.cmbSize.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbSize.Location = new System.Drawing.Point(100, 57);
            this.cmbSize.Size = new System.Drawing.Size(150, 25);

            // lblCount
            this.lblCount.AutoSize = true;
            this.lblCount.Location = new System.Drawing.Point(20, 100);
            this.lblCount.Text = "Количество:";

            // numCount
            this.numCount.Location = new System.Drawing.Point(100, 97);
            this.numCount.Minimum = 1;
            this.numCount.Maximum = 1000;
            this.numCount.Value = 1;
            this.numCount.Size = new System.Drawing.Size(60, 25);

            // btnAdd
            this.btnAdd.BackColor = System.Drawing.ColorTranslator.FromHtml("#70B2AF");
            this.btnAdd.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAdd.ForeColor = System.Drawing.Color.White;
            this.btnAdd.Font = new System.Drawing.Font("Calibri", 12F, System.Drawing.FontStyle.Bold);
            this.btnAdd.Location = new System.Drawing.Point(100, 140);
            this.btnAdd.Size = new System.Drawing.Size(300, 40);
            this.btnAdd.Text = "Добавить в заказ";
            this.btnAdd.UseVisualStyleBackColor = false;
            this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);

            // AddItemForm
            this.ClientSize = new System.Drawing.Size(450, 200);
            this.Controls.Add(this.lblProduct);
            this.Controls.Add(this.cmbProduct);
            this.Controls.Add(this.lblSize);
            this.Controls.Add(this.cmbSize);
            this.Controls.Add(this.lblCount);
            this.Controls.Add(this.numCount);
            this.Controls.Add(this.btnAdd);
            this.Font = new System.Drawing.Font("Calibri", 10F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Добавить товар в заказ";
            ((System.ComponentModel.ISupportInitialize)(this.numCount)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}