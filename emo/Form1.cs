using emo;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace emo
{
    public partial class Form1 : Form
    {
        private readonly Color bgMain = Color.White;
        private readonly Color bgSecondary = ColorTranslator.FromHtml("#D2F6E7");
        private readonly Color accent = ColorTranslator.FromHtml("#70B2AF");
        private readonly Color alertColor = ColorTranslator.FromHtml("#ff8080");

        public Form1()
        {
            InitializeComponent();
            LoadLogo();
            LoadProducts();
        }

        private void LoadLogo()
        {
            string logoPath = Path.Combine(Application.StartupPath, "Resources", "logo.png");
            if (File.Exists(logoPath))
            {
                pictureBoxLogo.Image = Image.FromFile(logoPath);
            }
        }

        private void LoadProducts()
        {
            try
            {
                var products = DBHelper.LoadProducts();
                panelContent.Controls.Clear();

                int yPos = 10;

                foreach (var p in products)
                {
                    Color cardBg = p.TotalCount <= 3 ? alertColor : bgMain;

                    var card = new Panel
                    {
                        Location = new Point(10, yPos),
                        Size = new Size(750, 100),
                        BackColor = cardBg,
                        BorderStyle = BorderStyle.FixedSingle
                    };

                    string imgPath = Path.Combine(Application.StartupPath, "Resources", p.Image ?? "");
                    if (!File.Exists(imgPath))
                        imgPath = Path.Combine(Application.StartupPath, "Resources", "picture.png");

                    if (File.Exists(imgPath))
                    {
                        var pic = new PictureBox
                        {
                            Image = Image.FromFile(imgPath),
                            Size = new Size(80, 80),
                            Location = new Point(10, 10),
                            SizeMode = PictureBoxSizeMode.Zoom
                        };
                        card.Controls.Add(pic);
                    }

                    var lblTitle = new Label
                    {
                        Text = $"{p.Manufacture} | {p.Name}",
                        Font = new Font("Calibri", 12, FontStyle.Bold),
                        Location = new Point(100, 10),
                        AutoSize = true
                    };
                    card.Controls.Add(lblTitle);

                    var lblCategory = new Label
                    {
                        Text = $"Категория: {p.Category}",
                        Location = new Point(100, 35),
                        AutoSize = true
                    };
                    card.Controls.Add(lblCategory);

                    var lblCount = new Label
                    {
                        Text = $"Количество: {p.CountText}",
                        Location = new Point(100, 55),
                        AutoSize = true
                    };
                    card.Controls.Add(lblCount);

                    var lblComposition = new Label
                    {
                        Text = $"Состав: {p.Composition}",
                        Font = new Font("Calibri", 9),
                        Location = new Point(100, 75),
                        AutoSize = true,
                        MaximumSize = new Size(500, 0)
                    };
                    card.Controls.Add(lblComposition);

                    var lblPrice = new Label
                    {
                        Text = $"{p.Price} ₽",
                        Font = new Font("Calibri", 14, FontStyle.Bold),
                        Location = new Point(650, 30),
                        AutoSize = true
                    };
                    card.Controls.Add(lblPrice);

                    panelContent.Controls.Add(card);
                    yPos += 110;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}