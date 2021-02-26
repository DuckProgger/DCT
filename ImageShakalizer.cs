using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace DCT
{
    public class ImageShakalizer
    {
        public static Image Damage(Bitmap srcImage, int quality) {
            byte[,,] rgb = BitmapToByteRgb(srcImage);
            byte[,,] ycbcr = ByteRgbToByteYCbCr(rgb);
            Quantization(ycbcr, quality);
            byte[,,] result_rgb = ByteYCbCrToByteRgb(ycbcr);
            Bitmap result_image = ByteRgbToBitmap(result_rgb);

            result_image.Save("ddd.bmp", ImageFormat.Png);
            return result_image;

        }








        private static unsafe byte[,,] BitmapToByteRgb(Bitmap bmp) {
            //https://habr.com/ru/post/196578/
            int width = bmp.Width,
                height = bmp.Height;
            byte[,,] rgb = new byte[3, height, width];
            BitmapData bd = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            try {
                byte* curpos;
                fixed (byte* _rgb = rgb) {
                    byte* _r = _rgb, _g = _rgb + width * height, _b = _rgb + 2 * width * height;
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

        private static unsafe Bitmap ByteRgbToBitmap(byte[,,] rgb) {
            int width = rgb.GetLength(2),
               height = rgb.GetLength(1);
            Bitmap bmp = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            BitmapData bd = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
            try {
                byte* curpos;
                fixed (byte* _rgb = rgb) {
                    byte* _r = _rgb, _g = _rgb + width * height, _b = _rgb + 2 * width * height;
                    for (int h = 0; h < height; h++) {
                        curpos = ((byte*)bd.Scan0) + h * bd.Stride;
                        for (int w = 0; w < width; w++) {
                            *(curpos++) = *_b; ++_b;
                            *(curpos++) = *_g; ++_g;
                            *(curpos++) = *_r; ++_r;
                        }
                    }
                }
            } finally {
                bmp.UnlockBits(bd);
            }
            return bmp;
        }

        private static byte[,,] ByteRgbToByteYCbCr(byte[,,] rgb) {
            int heigth = rgb.GetLength(1);
            int width = rgb.GetLength(2);
            byte[,,] ycbcr = new byte[3, heigth, width];
            for (int h = 0; h < heigth; h++) {
                for (int w = 0; w < width; w++) {
                    byte r = rgb[0, h, w];
                    byte g = rgb[1, h, w];
                    byte b = rgb[2, h, w];
                    ycbcr[0, h, w] = ToByte((0.299 * r) + (0.587 * g) + (0.114 * b));
                    ycbcr[1, h, w] = ToByte(128.0 - (0.168736 * r) - (0.331264 * g) + (0.5 * b));
                    ycbcr[2, h, w] = ToByte(128.0 + (0.5 * r) - (0.418688 * g) - (0.081312 * b));
                }
            }
            return ycbcr;
        }

        private static byte[,,] ByteYCbCrToByteRgb(byte[,,] ycbcr) {
            int heigth = ycbcr.GetLength(1);
            int width = ycbcr.GetLength(2);
            byte[,,] rgb = new byte[3, heigth, width];
            for (int h = 0; h < heigth; h++) {
                for (int w = 0; w < width; w++) {
                    byte y = ycbcr[0, h, w];
                    byte cb = ycbcr[1, h, w];
                    byte cr = ycbcr[2, h, w];
                    rgb[0, h, w] = ToByte(y + 1.402 * (cr - 128.0));
                    rgb[1, h, w] = ToByte(y - 0.34414 * (cb - 128.0) - 0.71414 * (cr - 128.0));
                    rgb[2, h, w] = ToByte(y + 1.772 * (cb - 128.0));
                }
            }
            return rgb;
        }

        private static void Quantization(byte[,,] ycbcr, int quantizator) {
            double[,] dct_matrix = GetDCTMatrix(8.0);
            byte[,] q_matrix = GetQuantMatrix(quantizator);

            double[,] t_matrix_cb = new double[8, 8];
            double[,] t_matrix_cr = new double[8, 8];

            int heigth = ycbcr.GetLength(1);
            int width = ycbcr.GetLength(2);

            // цикл по блокам 8х8
            for (int block_h = 0; block_h < heigth; block_h += 8) {
                int block_h_end = block_h + 8;
                for (int block_w = 0; block_w < width; block_w += 8) {
                    int block_w_end = block_w + 8;

                    // цикл по пикселям внутри блока 8х8
                    for (int h = block_h; h < block_h_end; h++) {
                        for (int w = block_w; w < block_w_end; w++) {

                            // используется смещение для матрицы квантования
                            int offset_h = h - block_h;
                            int offset_w = w - block_w;

                            //byte cb = ycbcr[1, h, w];
                            //byte cr = ycbcr[2, h, w];

                            //t_matrix_cb[offset_h, offset_w] = cb * dct_matrix[offset_w, offset_h];
                            //t_matrix_cr[offset_h, offset_w] = cr * dct_matrix[offset_w, offset_h];

                            //t_matrix_cb[offset_h, offset_w] *= dct_matrix[offset_h, offset_w];
                            //t_matrix_cr[offset_h, offset_w] *= dct_matrix[offset_h, offset_w];

                            //t_matrix_cb[offset_h, offset_w] *= q_matrix[offset_h, offset_w];
                            //t_matrix_cr[offset_h, offset_w] *= q_matrix[offset_h, offset_w];

                            //ycbcr[1, h, w] = GetByte(t_matrix_cb[offset_h, offset_w]);
                            //ycbcr[2, h, w] = GetByte(t_matrix_cr[offset_h, offset_w]);


                            ycbcr[1, h, w] = 0;
                            ycbcr[2, h, w] = 0;
                        }
                    }



                    { }


                }
            }
        }

        private static double[,] GetDCTMatrix(double n) {
            double[,] dct_matrix = new double[8, 8];
            for (int i = 0; i < 8; i++) {
                for (int j = 0; j < 8; j++) {
                    if (i == 0) {
                        dct_matrix[i, j] = 1.0 / Math.Sqrt(n);
                    } else {
                        dct_matrix[i, j] = Math.Sqrt(2.0 / n) * Math.Cos((2.0 * j + 1.0) * i * Math.PI / (2.0 * n));
                    }
                }
            }
            return dct_matrix;
        }

        private static byte[,] GetQuantMatrix(int q) {
            byte[,] q_matrix = new byte[8, 8];
            for (int i = 0; i < 8; i++) {
                for (int j = 0; j < 8; j++) {
                    q_matrix[i, j] = (byte)(1 + ((1 + i + j) * q));
                }
            }
            return q_matrix;
        }



        private static byte ToByte(double value) {
            if (value <= 0.0) {
                return 0;
            } else if (value >= 255.0) {
                return 255;
            } else {
                return (byte)Math.Round(value);
            }
        }
    }
}
