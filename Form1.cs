using System;
using System.Drawing;
using System.Windows.Forms;

namespace DCT
{
    public partial class Form1 : Form
    {
        private readonly Bitmap _sourceImage;
        Calc _calc;
        ImageShakalizer _imageShakalizer;


        public Form1()
        {
            InitializeComponent();
            _sourceImage = (Bitmap)Image.FromFile(@"2969267161300910242.bmp");

            _calc = new Calc();
            _calc.Initialization();

            //_imageShakalizer = new ImageShakalizer(_sourceImage);

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
                pictureBox1.Image = processImage(_sourceImage, quality);
                Text = quality.ToString();
            }
            catch (Exception ex)
            {
                Text = ex.Message;
            }
        }

        private Image processImage(Bitmap srcImage, int quality)
        {
            Bitmap image = (Bitmap)_calc.CompressImage(quality);
            return image;

            //return _imageShakalizer.Shakalize(quality);
        }
    }
}
