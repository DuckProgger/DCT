using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace DCT
{
    public class ImageShakalizer
    {
        public static Image Damage(Bitmap srcImage, int quality) {
            byte[,,] rgb = BitmapToByteRgbQ(srcImage);
            byte[,,] ycbcr = ByteRgbToByteYCbCr(rgb);




            byte[,,] result_rgb = ByteYCbCrToByteRgb(ycbcr);
            Bitmap result_image = ByteRgbToBitmap(result_rgb);

            return result_image;
        }






        public static unsafe byte[,,] BitmapToByteRgbQ(Bitmap bmp) {
            //https://habr.com/ru/post/196578/
            int width = bmp.Width,
                height = bmp.Height;
            byte[,,] rgb = new byte[3, height, width];
            BitmapData bd = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly,
                PixelFormat.Format24bppRgb);
            try {
                byte* curpos;
                fixed (byte* _res = rgb) {
                    byte* _r = _res, _g = _res + width * height, _b = _res + 2 * width * height;
                    for (int h = 0; h < height; h++) {
                        curpos = ((byte*)bd.Scan0) + h * bd.Stride;
                        for (int w = 0; w < width; w++) {
                            *_b = *(curpos++); ++_b;
                            *_g = *(curpos++); ++_g;
                            *_r = *(curpos++); ++_r;
                        }
                    }
                }
            } finally {
                bmp.UnlockBits(bd);
            }
            return rgb;
        }

        public static byte[,,] ByteRgbToByteYCbCr(byte[,,] rgb) {
            int heigth = rgb.GetLength(1);
            int width = rgb.GetLength(2);
            byte[,,] ycbcr = new byte[3, heigth, width];
            for (int h = 0; h < heigth; h++) {
                for (int w = 0; w < width; w++) {
                    byte r = rgb[0, h, w];
                    byte g = rgb[1, h, w];
                    byte b = rgb[2, h, w];

                    double y = 0.0 + (0.299 * r) + (0.587 * g) + (0.114 * b);
                    double cb = 128.0 - (0.168736 * r) - (0.331264 * g) + (0.5 * b);
                    double cr = 128.0 + (0.5 * r) - (0.418688 * g) - (0.081312 * b);

                    ycbcr[0, h, w] = y >= byte.MaxValue ? byte.MaxValue : (byte)Math.Round(y);
                    ycbcr[1, h, w] = cb >= byte.MaxValue ? byte.MaxValue : (byte)Math.Round(cb);
                    ycbcr[2, h, w] = cr >= byte.MaxValue ? byte.MaxValue : (byte)Math.Round(cr);
                }
            }
            return ycbcr;
        }

        public static byte[,,] ByteYCbCrToByteRgb(byte[,,] ycbcr) {
            int heigth = ycbcr.GetLength(1);
            int width = ycbcr.GetLength(2);
            byte[,,] rgb = new byte[3, heigth, width];
            for (int h = 0; h < heigth; h++) {
                for (int w = 0; w < width; w++) {
                    byte y = ycbcr[0, h, w];
                    byte cb = ycbcr[1, h, w];
                    byte cr = ycbcr[2, h, w];

                    double r = y + 1.402 * (cr - 128.0);
                    double g = y - 0.34414 * (cb - 128.0) - 0.71414 * (cr - 128.0);
                    double b = y + 1.772 * (cb - 128.0);

                    rgb[0, h, w] = r >= byte.MaxValue ? byte.MaxValue : (byte)Math.Round(r);
                    rgb[1, h, w] = g >= byte.MaxValue ? byte.MaxValue : (byte)Math.Round(g);
                    rgb[2, h, w] = b >= byte.MaxValue ? byte.MaxValue : (byte)Math.Round(b);
                }
            }
            return rgb;
        }

        public static unsafe Bitmap ByteRgbToBitmap(byte[,,] rgb) {
            return null;
        }






    }
}
