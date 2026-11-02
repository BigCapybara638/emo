namespace emo
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Panel panelHeader;
        private System.Windows.Forms.PictureBox pictureBoxLogo;
        private System.Windows.Forms.Label labelTitle;
        private System.Windows.Forms.Label labelUser;
        private System.Windows.Forms.Button btnOrders; // <--- НОВАЯ КНОПКА
        private System.Windows.Forms.FlowLayoutPanel panelContent;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.panelHeader = new System.Windows.Forms.Panel();
            this.pictureBoxLogo = new System.Windows.Forms.PictureBox();
            this.labelTitle = new System.Windows.Forms.Label();
            this.labelUser = new System.Windows.Forms.Label();
            this.btnOrders = new System.Windows.Forms.Button(); // <---
            this.panelContent = new System.Windows.Forms.FlowLayoutPanel();
            this.panelHeader.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxLogo)).BeginInit();
            this.SuspendLayout();

            // panelHeader
            this.panelHeader.BackColor = System.Drawing.ColorTranslator.FromHtml("#D2F6E7");
            this.panelHeader.Controls.Add(this.btnOrders); // <--- ДОБАВЛЯЕМ КНОПКУ
            this.panelHeader.Controls.Add(this.labelUser);
            this.panelHeader.Controls.Add(this.pictureBoxLogo);
            this.panelHeader.Controls.Add(this.labelTitle);
            this.panelHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelHeader.Size = new System.Drawing.Size(800, 100);

            // pictureBoxLogo
            this.pictureBoxLogo.Location = new System.Drawing.Point(20, 15);
            this.pictureBoxLogo.Size = new System.Drawing.Size(120, 70);
            this.pictureBoxLogo.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;

            // labelTitle
            this.labelTitle.AutoSize = true;
            this.labelTitle.Font = new System.Drawing.Font("Calibri", 20F, System.Drawing.FontStyle.Bold);
            this.labelTitle.ForeColor = System.Drawing.ColorTranslator.FromHtml("#70B2AF");
            this.labelTitle.Location = new System.Drawing.Point(160, 35);
            this.labelTitle.Text = "Список товаров";

            // labelUser
            this.labelUser.AutoSize = true;
            this.labelUser.Font = new System.Drawing.Font("Calibri", 12F, System.Drawing.FontStyle.Bold);
            this.labelUser.Location = new System.Drawing.Point(600, 40);
            this.labelUser.Text = "";

            // btnOrders
            this.btnOrders.BackColor = System.Drawing.ColorTranslator.FromHtml("#70B2AF");
            this.btnOrders.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnOrders.ForeColor = System.Drawing.Color.White;
            this.btnOrders.Font = new System.Drawing.Font("Calibri", 12F, System.Drawing.FontStyle.Bold);
            this.btnOrders.Location = new System.Drawing.Point(450, 35);
            this.btnOrders.Size = new System.Drawing.Size(120, 35);
            this.btnOrders.Text = "Заказы";
            this.btnOrders.UseVisualStyleBackColor = false;
            this.btnOrders.Visible = false; // Скрыта по умолчанию
            this.btnOrders.Click += new System.EventHandler(this.btnOrders_Click); 

            // panelContent
            this.panelContent.AutoScroll = true;
            this.panelContent.BackColor = System.Drawing.Color.White;
            this.panelContent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelContent.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.panelContent.WrapContents = false;
            this.panelContent.Padding = new System.Windows.Forms.Padding(10);

            // Form1
            this.ClientSize = new System.Drawing.Size(800, 600);
            this.Controls.Add(this.panelContent);
            this.Controls.Add(this.panelHeader);
            this.Font = new System.Drawing.Font("Calibri", 10F);
            this.Text = "Чудо Обувь — Каталог";
            this.panelHeader.ResumeLayout(false);
            this.panelHeader.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxLogo)).EndInit();
            this.ResumeLayout(false);
        }
    }
}