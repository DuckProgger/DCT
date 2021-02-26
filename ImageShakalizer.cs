using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;

namespace DCT
{
    public class ImageShakalizer
    {
        public static Image Damage(Bitmap srcImage, int quality) {
            byte[,,] rgb = BitmapToByteRgb(srcImage);
            double[,,] ycbcr = ByteRgbToByteYCbCr(rgb);
            Process(ycbcr, quality);
            byte[,,] result_rgb = ByteYCbCrToByteRgb(ycbcr);
            Bitmap result_image = ByteRgbToBitmap(result_rgb);
            return result_image;
        }








        private static void Process(double[,,] ycbcr, int quantizator) {
            double[,] dct_matrix = CreateDCTMatrix(8.0);
            double[,] dct_matrix_transpose = TransposeMatrix(dct_matrix);
            double[,] q_matrix = CreateQuantMatrix(quantizator);

            int heigth = ycbcr.GetLength(1);
            int width = ycbcr.GetLength(2);

            // цикл по блокам 8х8
            for (int block_y = 0; block_y < heigth; block_y += 8) {
                for (int block_x = 0; block_x < width; block_x += 8) {
                    for (int dimension = 0; dimension <= 2; dimension++) {

                        double[,] block = GetBlock(ycbcr, dimension, block_y, block_x);
                        LinearRound(block);

                        double[,] t_matrix = MultipleMatrix(block, dct_matrix_transpose);
                        LinearRound(t_matrix);


                        LinearDivide(t_matrix, q_matrix);
                        LinearRound(t_matrix);

                        LinearRound(t_matrix);
                        LinearMultiple(t_matrix, q_matrix);

                        t_matrix = MultipleMatrix(t_matrix, dct_matrix);

                        LinearRound(t_matrix);
                        SetBlock(ycbcr, t_matrix, dimension, block_y, block_x);
                    }
                }
            }
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

        private static double[,,] ByteRgbToByteYCbCr(byte[,,] rgb) {
            int heigth = rgb.GetLength(1);
            int width = rgb.GetLength(2);
            double[,,] ycbcr = new double[3, heigth, width];
            for (int h = 0; h < heigth; h++) {
                for (int w = 0; w < width; w++) {
                    byte r = rgb[0, h, w];
                    byte g = rgb[1, h, w];
                    byte b = rgb[2, h, w];
                    double y = 0.299 * r + 0.587 * g + 0.114 * b;
                    ycbcr[0, h, w] = y;
                    ycbcr[1, h, w] = 0.564 * (b - y);
                    ycbcr[2, h, w] = 0.713 * (r - y);
                }
            }
            return ycbcr;
        }

        private static byte[,,] ByteYCbCrToByteRgb(double[,,] ycbcr) {
            int heigth = ycbcr.GetLength(1);
            int width = ycbcr.GetLength(2);
            byte[,,] rgb = new byte[3, heigth, width];
            for (int h = 0; h < heigth; h++) {
                for (int w = 0; w < width; w++) {
                    double y = ycbcr[0, h, w];
                    double cb = ycbcr[1, h, w];
                    double cr = ycbcr[2, h, w];
                    rgb[0, h, w] = ToByte(y + 1.402 * cr);
                    rgb[1, h, w] = ToByte(y - 0.344 * cb - 0.714 * cr);
                    rgb[2, h, w] = ToByte(y + 1.772 * cb);
                }
            }
            return rgb;
        }

        private static double[,] CreateDCTMatrix(double n) {
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

        private static double[,] CreateQuantMatrix(int q) {
            double[,] q_matrix = new double[8, 8];
            for (int i = 0; i < 8; i++) {
                for (int j = 0; j < 8; j++) {
                    q_matrix[i, j] = 1 + ((1 + i + j) * q);
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

        private static double[,] TransposeMatrix(double[,] a) {
            int ay = a.GetLength(0);
            int ax = a.GetLength(1);
            double[,] matrix = new double[ax, ay];
            for (int y = 0; y < ay; y++) {
                for (int x = 0; x < ax; x++) {
                    matrix[x, y] = a[y, x];
                }
            }
            return matrix;
        }

        private static double[,] MultipleMatrix(double[,] a, double[,] b) {
            int by = b.GetLength(0);
            int ax = a.GetLength(1);
            int bx = b.GetLength(1);

            Validate.IsTrue(ax == by);

            double[,] matrix = new double[by, bx];
            for (int y = 0; y < by; y++) {
                for (int x = 0; x < bx; x++) {
                    double value = 0.0;
                    for (int i = 0; i < ax; i++) {
                        value += a[y, i] * b[i, x];
                    }
                    matrix[y, x] = value;
                }
            }
            return matrix;
        }

        private static double[,] GetBlock(double[,,] matrix, int dimension, int offset_y, int offset_x) {
            double[,] block = new double[8, 8];
            for (int y = 0; y < 8; y++) {
                for (int x = 0; x < 8; x++) {
                    block[y, x] = matrix[dimension, y + offset_y, x + offset_x];
                }
            }
            return block;
        }

        private static void SetBlock(double[,,] matrix, double[,] block, int dimension, int offset_y, int offset_x) {
            for (int y = 0; y < 8; y++) {
                for (int x = 0; x < 8; x++) {
                    matrix[dimension, y + offset_y, x + offset_x] = block[y, x];
                }
            }
        }

        private static void LinearDivide(double[,] a, double[,] b) {
            int ay = a.GetLength(0);
            int ax = a.GetLength(1);
            for (int y = 0; y < ay; y++) {
                for (int x = 0; x < ax; x++) {
                    a[y, x] /= b[y, x];
                }
            }
        }

        private static void LinearMultiple(double[,] a, double[,] b) {
            int ay = a.GetLength(0);
            int ax = a.GetLength(1);
            for (int y = 0; y < ay; y++) {
                for (int x = 0; x < ax; x++) {
                    a[y, x] *= b[y, x];
                }
            }
        }


        private static void LinearRound(double[,] a) {
            int ay = a.GetLength(0);
            int ax = a.GetLength(1);
            for (int y = 0; y < ay; y++) {
                for (int x = 0; x < ax; x++) {
                    a[y, x] = Math.Round(a[y, x]);
                }
            }
        }




        private static string DEBUG_TOTEXT(double[,] a) {
            StringBuilder str = new StringBuilder();
            int ay = a.GetLength(0);
            int ax = a.GetLength(1);
            for (int y = 0; y < ay; y++) {
                for (int x = 0; x < ax; x++) {
                    str.Append(a[y, x] + "\t");
                }
                str.AppendLine();
            }
            return str.ToString();
        }
    }
}
