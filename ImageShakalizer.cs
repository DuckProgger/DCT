using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace DCT
{
    public class ImageShakalizer
    {
        private readonly double[,,] originalYCbCrMatrix;
        private double[,,] yCbCrMatrix;
        private readonly int heigth;
        private readonly int width;
        private readonly double[,] tempBlock = new double[8, 8];


        public ImageShakalizer(Bitmap bitmap) {
            byte[,,] rgbMatrix = BitmapToByteRgb(bitmap);
            originalYCbCrMatrix = ByteRgbToByteYCbCr(rgbMatrix);
            heigth = originalYCbCrMatrix.GetLength(1);
            width = originalYCbCrMatrix.GetLength(2);
        }

        public Bitmap Damage(int quality) {
            yCbCrMatrix = CloneYCbCrMatrix();
            double[,] dct_matrix = CreateDCTMatrix(8.0);
            double[,] dct_matrix_transpose = TransposeMatrix(dct_matrix);
            double[,] q_matrix = CreateQuantMatrix(quality);

            // цикл по блокам 8х8
            for (int block_y = 0; block_y < heigth; block_y += 8) {
                for (int block_x = 0; block_x < width; block_x += 8) {
                    for (int dimension = 0; dimension <= 2; dimension++) {

                        MultipleMatrix(dimension, block_y, block_x, dct_matrix_transpose);

                        LinearDivide(dimension, block_y, block_x, q_matrix);

                        LinearRound(dimension, block_y, block_x);

                        LinearMultiple(dimension, block_y, block_x, q_matrix);

                        MultipleMatrix(dimension, block_y, block_x, dct_matrix);
                    }
                }
            }

            byte[,,] rgb = ByteYCbCrToByteRgb(yCbCrMatrix);
            return ByteRgbToBitmap(rgb);
        }


        private double[,,] CloneYCbCrMatrix() {
            double[,,] matrix = new double[
                originalYCbCrMatrix.GetLength(0),
                originalYCbCrMatrix.GetLength(1),
                originalYCbCrMatrix.GetLength(2)];
            Array.Copy(originalYCbCrMatrix, matrix, matrix.Length);
            return matrix;
        }

        private void MultipleMatrix(int dimension, int offset_y, int offset_x, double[,] b) {
            ReadTempBlock(dimension, offset_y, offset_x);
            for (int y = 0; y < 8; y++) {
                for (int x = 0; x < 8; x++) {
                    double value = 0.0;
                    for (int i = 0; i < 8; i++) {
                        value += tempBlock[y, i] * b[i, x];
                    }
                    yCbCrMatrix[dimension, y + offset_y, x + offset_x] = value;
                }
            }




            //int by = b.GetLength(0);
            //int ax = a.GetLength(1);
            //int bx = b.GetLength(1);

            //Validate.IsTrue(ax == by);

            //double[,] matrix = new double[by, bx];
            //for (int y = 0; y < by; y++) {
            //    for (int x = 0; x < bx; x++) {
            //        double value = 0.0;
            //        for (int i = 0; i < ax; i++) {
            //            value += a[y, i] * b[i, x];
            //        }
            //        matrix[y, x] = value;
            //    }
            //}
            //return matrix;


        }

        private void ReadTempBlock(int dimension, int offset_y, int offset_x) {
            for (int y = 0; y < 8; y++) {
                for (int x = 0; x < 8; x++) {
                    tempBlock[y, x] = yCbCrMatrix[dimension, y + offset_y, x + offset_x];
                }
            }
        }

        private void LinearDivide(int dimension, int offset_y, int offset_x, double[,] b) {
            for (int y = 0; y < 8; y++) {
                for (int x = 0; x < 8; x++) {
                    yCbCrMatrix[dimension, y + offset_y, x + offset_x] /= b[y, x];
                }
            }
        }

        private void LinearRound(int dimension, int offset_y, int offset_x) {
            for (int y = 0; y < 8; y++) {
                for (int x = 0; x < 8; x++) {
                    yCbCrMatrix[dimension, y + offset_y, x + offset_x] = Math.Round(yCbCrMatrix[dimension, y + offset_y, x + offset_x]);
                }
            }
        }

        private void LinearMultiple(int dimension, int offset_y, int offset_x, double[,] b) {
            for (int y = 0; y < 8; y++) {
                for (int x = 0; x < 8; x++) {
                    yCbCrMatrix[dimension, y + offset_y, x + offset_x] *= b[y, x];
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

        private static byte ToByte(double value) {
            if (value <= 0.0) {
                return 0;
            } else if (value >= 255.0) {
                return 255;
            } else {
                return (byte)Math.Round(value);
            }
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
            for (int y = 0; y < 8; y++) {
                for (int x = 0; x < 8; x++) {
                    q_matrix[y, x] = 1 + ((1 + y + x) * q);
                }
            }
            return q_matrix;
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
    }
}
