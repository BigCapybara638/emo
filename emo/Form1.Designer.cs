namespace emo
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Panel panelHeader;
        private System.Windows.Forms.FlowLayoutPanel flowHeader;
        private System.Windows.Forms.PictureBox pictureBoxLogo;
        private System.Windows.Forms.Label labelTitle;
        private System.Windows.Forms.Label labelUser;
        private System.Windows.Forms.Button btnCart;
        private System.Windows.Forms.Button btnOrders;
        private System.Windows.Forms.Button btnLogout;
        private System.Windows.Forms.Panel panelFilters;
        private System.Windows.Forms.TextBox txtSearch;
        private System.Windows.Forms.ComboBox cmbSort;
        private System.Windows.Forms.ComboBox cmbCategory;
        private System.Windows.Forms.FlowLayoutPanel panelContent;
        private System.Windows.Forms.ToolTip toolTip1;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.panelHeader = new System.Windows.Forms.Panel();
            this.flowHeader = new System.Windows.Forms.FlowLayoutPanel();
            this.pictureBoxLogo = new System.Windows.Forms.PictureBox();
            this.labelTitle = new System.Windows.Forms.Label();
            this.labelUser = new System.Windows.Forms.Label();
            this.btnCart = new System.Windows.Forms.Button();
            this.btnOrders = new System.Windows.Forms.Button();
            this.btnLogout = new System.Windows.Forms.Button();
            this.panelFilters = new System.Windows.Forms.Panel();
            this.txtSearch = new System.Windows.Forms.TextBox();
            this.cmbSort = new System.Windows.Forms.ComboBox();
            this.cmbCategory = new System.Windows.Forms.ComboBox();
            this.panelContent = new System.Windows.Forms.FlowLayoutPanel();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.panelHeader.SuspendLayout();
            this.flowHeader.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxLogo)).BeginInit();
            this.SuspendLayout();

            // panelHeader
            this.panelHeader.BackColor = System.Drawing.ColorTranslator.FromHtml("#D2F6E7");
            this.panelHeader.Controls.Add(this.flowHeader);
            this.panelHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelHeader.Size = new System.Drawing.Size(800, 120);

            // flowHeader
            this.flowHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowHeader.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.flowHeader.WrapContents = false;
            this.flowHeader.Padding = new System.Windows.Forms.Padding(10, 20, 10, 10);
            this.flowHeader.Controls.Add(this.pictureBoxLogo);
            this.flowHeader.Controls.Add(this.labelTitle);
            this.flowHeader.Controls.Add(this.labelUser);
            this.flowHeader.Controls.Add(this.btnCart);
            this.flowHeader.Controls.Add(this.btnOrders);
            this.flowHeader.Controls.Add(this.btnLogout);

            // pictureBoxLogo
            this.pictureBoxLogo.Size = new System.Drawing.Size(100, 60);
            this.pictureBoxLogo.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBoxLogo.Margin = new System.Windows.Forms.Padding(10, 10, 10, 10);

            // labelTitle
            this.labelTitle.AutoSize = true;
            this.labelTitle.Font = new System.Drawing.Font("Calibri", 20F, System.Drawing.FontStyle.Bold);
            this.labelTitle.ForeColor = System.Drawing.ColorTranslator.FromHtml("#70B2AF");
            this.labelTitle.Text = "Список товаров";
            this.labelTitle.Margin = new System.Windows.Forms.Padding(10, 10, 10, 10);

            // labelUser
            this.labelUser.AutoSize = true;
            this.labelUser.Font = new System.Drawing.Font("Calibri", 11F, System.Drawing.FontStyle.Bold);
            this.labelUser.MaximumSize = new System.Drawing.Size(150, 0);
            this.labelUser.Margin = new System.Windows.Forms.Padding(20, 20, 20, 10);

            // btnCart
            this.btnCart.BackColor = System.Drawing.ColorTranslator.FromHtml("#70B2AF");
            this.btnCart.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCart.ForeColor = System.Drawing.Color.White;
            this.btnCart.Font = new System.Drawing.Font("Calibri", 10F, System.Drawing.FontStyle.Bold);
            this.btnCart.Size = new System.Drawing.Size(90, 30);
            this.btnCart.Text = "Корзина";
            this.btnCart.UseVisualStyleBackColor = false;
            this.btnCart.Margin = new System.Windows.Forms.Padding(20, 15, 10, 10);
            this.btnCart.Click += new System.EventHandler(this.btnCart_Click);

            // btnOrders
            this.btnOrders.BackColor = System.Drawing.ColorTranslator.FromHtml("#70B2AF");
            this.btnOrders.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnOrders.ForeColor = System.Drawing.Color.White;
            this.btnOrders.Font = new System.Drawing.Font("Calibri", 10F, System.Drawing.FontStyle.Bold);
            this.btnOrders.Size = new System.Drawing.Size(80, 30);
            this.btnOrders.Text = "Заказы";
            this.btnOrders.UseVisualStyleBackColor = false;
            this.btnOrders.Visible = false;
            this.btnOrders.Margin = new System.Windows.Forms.Padding(10, 15, 10, 10);
            this.btnOrders.Click += new System.EventHandler(this.btnOrders_Click);

            // btnLogout
            this.btnLogout.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnLogout.BackColor = System.Drawing.ColorTranslator.FromHtml("#ff8080");
            this.btnLogout.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLogout.ForeColor = System.Drawing.Color.White;
            this.btnLogout.Font = new System.Drawing.Font("Calibri", 10F, System.Drawing.FontStyle.Bold);
            this.btnLogout.Size = new System.Drawing.Size(80, 30);
            this.btnLogout.Text = "Выйти";
            this.btnLogout.UseVisualStyleBackColor = false;
            this.btnLogout.Margin = new System.Windows.Forms.Padding(10, 15, 10, 10);
            this.btnLogout.Click += new System.EventHandler(this.btnLogout_Click);

            // panelFilters
            this.panelFilters.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelFilters.Height = 50;
            this.panelFilters.BackColor = System.Drawing.Color.White;
            this.panelFilters.Padding = new System.Windows.Forms.Padding(10);
            this.panelFilters.Controls.Add(this.txtSearch);
            this.panelFilters.Controls.Add(this.cmbSort);
            this.panelFilters.Controls.Add(this.cmbCategory);

            // txtSearch
            this.txtSearch.Location = new System.Drawing.Point(10, 10);
            this.txtSearch.Size = new System.Drawing.Size(250, 25);
            this.txtSearch.Text = "Поиск...";
            this.txtSearch.ForeColor = System.Drawing.Color.Gray;
            this.txtSearch.Enter += (s, e) => { if (txtSearch.Text == "Поиск...") { txtSearch.Text = ""; txtSearch.ForeColor = System.Drawing.Color.Black; } };
            this.txtSearch.Leave += (s, e) => { if (string.IsNullOrWhiteSpace(txtSearch.Text)) { txtSearch.Text = "Поиск..."; txtSearch.ForeColor = System.Drawing.Color.Gray; } };
            this.txtSearch.TextChanged += new System.EventHandler(this.FilterChanged);

            // cmbSort
            this.cmbSort.Location = new System.Drawing.Point(270, 10);
            this.cmbSort.Size = new System.Drawing.Size(180, 25);
            this.cmbSort.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbSort.Items.AddRange(new object[] { "Цена: по возрастанию", "Цена: по убыванию" });
            this.cmbSort.SelectedIndex = 0;
            this.cmbSort.SelectedIndexChanged += new System.EventHandler(this.FilterChanged);

            // cmbCategory
            this.cmbCategory.Location = new System.Drawing.Point(460, 10);
            this.cmbCategory.Size = new System.Drawing.Size(180, 25);
            this.cmbCategory.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbCategory.SelectedIndexChanged += new System.EventHandler(this.FilterChanged);

            // panelContent
            this.panelContent.AutoScroll = true;
            this.panelContent.BackColor = System.Drawing.Color.White;
            this.panelContent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelContent.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.panelContent.WrapContents = false;
            this.panelContent.Padding = new System.Windows.Forms.Padding(10, 20, 10, 10);

            // ПОДСКАЗКИ
            this.toolTip1.SetToolTip(this.btnCart, "Перейти в корзину");
            this.toolTip1.SetToolTip(this.btnOrders, "Список заказов");
            this.toolTip1.SetToolTip(this.btnLogout, "Выйти из аккаунта");

            // Form1
            this.ClientSize = new System.Drawing.Size(800, 600);
            this.MinimumSize = new System.Drawing.Size(800, 500);
            this.Controls.Add(this.panelContent);
            this.Controls.Add(this.panelFilters);
            this.Controls.Add(this.panelHeader);
            this.Font = new System.Drawing.Font("Calibri", 10F);
            this.Text = "Чудо Обувь — Каталог";
            this.panelHeader.ResumeLayout(false);
            this.flowHeader.ResumeLayout(false);
            this.flowHeader.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxLogo)).EndInit();
            this.ResumeLayout(false);
        }
    }
}