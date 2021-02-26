using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;

namespace DCT
{
    public class ImageShakalizer
    {
        private readonly float[,,] ycbcr_matrix_original;
        private readonly int heigth;
        private readonly int width;
        private readonly int blocks_count;

        private float[,,] ycbcr_matrix;
        private float[,] dct_matrix;
        private float[,] dct_matrix_transpose;
        private float[,] q_matrix;

        public ImageShakalizer(Bitmap bitmap) {
            byte[,,] rgbMatrix = BitmapToByteRgb(bitmap);
            ycbcr_matrix_original = ByteRgbToByteYCbCr(rgbMatrix);
            heigth = ycbcr_matrix_original.GetLength(1);
            width = ycbcr_matrix_original.GetLength(2);
            blocks_count = ycbcr_matrix_original.Length / 64;
        }

        public Bitmap Shakalize(int quality) {
            ycbcr_matrix = CloneYCbCrMatrix();
            dct_matrix = CreateDCTMatrix(8f);
            dct_matrix_transpose = TransposeMatrix(dct_matrix);
            q_matrix = CreateQuantMatrix(quality);

            List<Task> tasks = new List<Task>(blocks_count);

            // цикл по блокам 8х8
            for (int dimension = 0; dimension <= 2; dimension++) {
                for (int block_y = 0; block_y < heigth; block_y += 8) {
                    for (int block_x = 0; block_x < width; block_x += 8) {

                        int current_block_y = block_y;
                        int current_block_x = block_x;
                        int current_dimension = dimension;
                        Action prepareBlockAction = () => PrepareBlock(current_dimension, current_block_y, current_block_x);

                        if (blocks_count <= 1000) {
                            // если картинка небольшая, код будет выполняться синхронно
                            prepareBlockAction.Invoke();
                        } else {
                            // в случае большой картинки обработка каждого блока будет выполнена в отдельной задаче
                            Task task = Task.Run(prepareBlockAction);
                            tasks.Add(task);
                        }
                    }
                }
            }

            // ожидание, когда все асинхронные задачи по обработке блоков будут выполнены
            Task.WaitAll(tasks.ToArray());

            byte[,,] rgb = ByteYCbCrToByteRgb(ycbcr_matrix);
            return ByteRgbToBitmap(rgb);
        }


        private float[,,] CloneYCbCrMatrix() {
            float[,,] matrix = new float[
                ycbcr_matrix_original.GetLength(0),
                ycbcr_matrix_original.GetLength(1),
                ycbcr_matrix_original.GetLength(2)];
            Array.Copy(ycbcr_matrix_original, matrix, matrix.Length);
            return matrix;
        }

        private void PrepareBlock(int dimension, int offset_y, int offset_x) {
            float[,] temp_block = new float[8, 8];

            // кодирование
            MultipleBlock(dimension, offset_y, offset_x, dct_matrix_transpose, temp_block);

            LinearDivideBlock(dimension, offset_y, offset_x, q_matrix);

            LinearRoundBlock(dimension, offset_y, offset_x);

            // декодирование
            LinearMultipleBlock(dimension, offset_y, offset_x, q_matrix);

            MultipleBlock(dimension, offset_y, offset_x, dct_matrix, temp_block);
        }

        private void MultipleBlock(int dimension, int offset_y, int offset_x, float[,] matrix, float[,] temp_block) {
            ReadTempBlock(dimension, offset_y, offset_x, temp_block);
            for (int y = 0; y < 8; y++) {
                for (int x = 0; x < 8; x++) {
                    float value = 0f;
                    for (int i = 0; i < 8; i++) {
                        value += temp_block[y, i] * matrix[i, x];
                    }
                    ycbcr_matrix[dimension, y + offset_y, x + offset_x] = value;
                }
            }
        }

        private void ReadTempBlock(int dimension, int offset_y, int offset_x, float[,] tempBlock) {
            for (int y = 0; y < 8; y++) {
                for (int x = 0; x < 8; x++) {
                    tempBlock[y, x] = ycbcr_matrix[dimension, y + offset_y, x + offset_x];
                }
            }
        }

        private void LinearDivideBlock(int dimension, int offset_y, int offset_x, float[,] matrix) {
            for (int y = 0; y < 8; y++) {
                for (int x = 0; x < 8; x++) {
                    ycbcr_matrix[dimension, y + offset_y, x + offset_x] /= matrix[y, x];
                }
            }
        }

        private void LinearRoundBlock(int dimension, int offset_y, int offset_x) {
            for (int y = 0; y < 8; y++) {
                for (int x = 0; x < 8; x++) {
                    ycbcr_matrix[dimension, y + offset_y, x + offset_x] = (float)Math.Round(ycbcr_matrix[dimension, y + offset_y, x + offset_x]);
                }
            }
        }

        private void LinearMultipleBlock(int dimension, int offset_y, int offset_x, float[,] matrix) {
            for (int y = 0; y < 8; y++) {
                for (int x = 0; x < 8; x++) {
                    ycbcr_matrix[dimension, y + offset_y, x + offset_x] *= matrix[y, x];
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

        private static float[,,] ByteRgbToByteYCbCr(byte[,,] rgb) {
            int heigth = rgb.GetLength(1);
            int width = rgb.GetLength(2);
            float[,,] ycbcr = new float[3, heigth, width];
            for (int h = 0; h < heigth; h++) {
                for (int w = 0; w < width; w++) {
                    byte r = rgb[0, h, w];
                    byte g = rgb[1, h, w];
                    byte b = rgb[2, h, w];
                    float y = 0.299f * r + 0.587f * g + 0.114f * b;
                    ycbcr[0, h, w] = y;
                    ycbcr[1, h, w] = 0.564f * (b - y);
                    ycbcr[2, h, w] = 0.713f * (r - y);
                }
            }
            return ycbcr;
        }

        private static byte[,,] ByteYCbCrToByteRgb(float[,,] ycbcr) {
            int heigth = ycbcr.GetLength(1);
            int width = ycbcr.GetLength(2);
            byte[,,] rgb = new byte[3, heigth, width];
            for (int h = 0; h < heigth; h++) {
                for (int w = 0; w < width; w++) {
                    float y = ycbcr[0, h, w];
                    float cb = ycbcr[1, h, w];
                    float cr = ycbcr[2, h, w];
                    rgb[0, h, w] = ToByte(y + 1.402f * cr);
                    rgb[1, h, w] = ToByte(y - 0.344f * cb - 0.714f * cr);
                    rgb[2, h, w] = ToByte(y + 1.772f * cb);
                }
            }
            return rgb;
        }

        private static byte ToByte(float value) {
            if (value <= 0.0) {
                return 0;
            } else if (value >= 255.0) {
                return 255;
            } else {
                return (byte)Math.Round(value);
            }
        }

        private static float[,] CreateDCTMatrix(float n) {
            float[,] dct_matrix = new float[8, 8];
            for (int i = 0; i < 8; i++) {
                for (int j = 0; j < 8; j++) {
                    if (i == 0) {
                        dct_matrix[i, j] = (float)(1f / Math.Sqrt(n));
                    } else {
                        dct_matrix[i, j] = (float)(Math.Sqrt(2f / n) * Math.Cos((2f * j + 1f) * i * Math.PI / (2f * n)));
                    }
                }
            }
            return dct_matrix;
        }

        private static float[,] CreateQuantMatrix(int q) {
            float[,] q_matrix = new float[8, 8];
            for (int y = 0; y < 8; y++) {
                for (int x = 0; x < 8; x++) {
                    q_matrix[y, x] = 1 + ((1 + y + x) * q);
                }
            }
            return q_matrix;
        }

        private static float[,] TransposeMatrix(float[,] a) {
            int ay = a.GetLength(0);
            int ax = a.GetLength(1);
            float[,] matrix = new float[ax, ay];
            for (int y = 0; y < ay; y++) {
                for (int x = 0; x < ax; x++) {
                    matrix[x, y] = a[y, x];
                }
            }
            return matrix;
        }
    }
}
