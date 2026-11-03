namespace emo
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Panel panelHeader;
        private System.Windows.Forms.PictureBox pictureBoxLogo;
        private System.Windows.Forms.Label labelTitle;
        private System.Windows.Forms.Label labelUser;
        private System.Windows.Forms.Button btnOrders;
        private System.Windows.Forms.Button btnCart;
        private System.Windows.Forms.Button btnLogout;
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
            this.btnOrders = new System.Windows.Forms.Button();
            this.btnCart = new System.Windows.Forms.Button();
            this.btnLogout = new System.Windows.Forms.Button();
            this.panelContent = new System.Windows.Forms.FlowLayoutPanel();
            this.panelHeader.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxLogo)).BeginInit();
            this.SuspendLayout();

            // panelHeader
            this.panelHeader.BackColor = System.Drawing.ColorTranslator.FromHtml("#D2F6E7");
            this.panelHeader.Controls.Add(this.btnLogout);
            this.panelHeader.Controls.Add(this.btnCart);
            this.panelHeader.Controls.Add(this.btnOrders);
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
            this.labelUser.Font = new System.Drawing.Font("Calibri", 11F, System.Drawing.FontStyle.Bold);
            this.labelUser.Location = new System.Drawing.Point(350, 40);
            this.labelUser.MaximumSize = new System.Drawing.Size(180, 0);
            this.labelUser.Text = "";

            // btnCart
            this.btnCart.BackColor = System.Drawing.ColorTranslator.FromHtml("#70B2AF");
            this.btnCart.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCart.ForeColor = System.Drawing.Color.White;
            this.btnCart.Font = new System.Drawing.Font("Calibri", 10F, System.Drawing.FontStyle.Bold);
            this.btnCart.Location = new System.Drawing.Point(540, 35);
            this.btnCart.Size = new System.Drawing.Size(100, 30);
            this.btnCart.Text = "Корзина (0)";
            this.btnCart.UseVisualStyleBackColor = false;
            this.btnCart.Click += new System.EventHandler(this.btnCart_Click);

            // btnOrders
            this.btnOrders.BackColor = System.Drawing.ColorTranslator.FromHtml("#70B2AF");
            this.btnOrders.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnOrders.ForeColor = System.Drawing.Color.White;
            this.btnOrders.Font = new System.Drawing.Font("Calibri", 10F, System.Drawing.FontStyle.Bold);
            this.btnOrders.Location = new System.Drawing.Point(650, 35);
            this.btnOrders.Size = new System.Drawing.Size(80, 30);
            this.btnOrders.Text = "Заказы";
            this.btnOrders.UseVisualStyleBackColor = false;
            this.btnOrders.Visible = false;
            this.btnOrders.Click += new System.EventHandler(this.btnOrders_Click);

            // btnLogout
            this.btnLogout.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnLogout.BackColor = System.Drawing.ColorTranslator.FromHtml("#ff8080");
            this.btnLogout.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLogout.ForeColor = System.Drawing.Color.White;
            this.btnLogout.Font = new System.Drawing.Font("Calibri", 10F, System.Drawing.FontStyle.Bold);
            this.btnLogout.Location = new System.Drawing.Point(740, 35);
            this.btnLogout.Size = new System.Drawing.Size(50, 30);
            this.btnLogout.Text = "Выйти";
            this.btnLogout.UseVisualStyleBackColor = false;
            this.btnLogout.Click += new System.EventHandler(this.btnLogout_Click);

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