using System;
using System.Drawing;
using System.Windows.Forms;

namespace DCT
{

    public partial class Form1 : Form
    {
        Calc calc;

        public Form1()
        {
            InitializeComponent();
            calc = new Calc();
            calc.Initialization();

            trackBar1_Scroll(this, EventArgs.Empty);
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            int quality = trackBar1.Value;

            Text = "Обработка...";
            Application.DoEvents();

            try
            {
                pictureBox1.Image = null;
                pictureBox1.Image = processImage(quality);
                Text = quality.ToString();
            }
            catch (Exception ex)
            {
                Text = ex.Message;
            }
        }



        private Bitmap processImage(int quality)
        {
            Bitmap image = (Bitmap)calc.CompressImage(quality);

            return image;
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }
    }
}
