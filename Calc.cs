﻿using System;
using System.Drawing;
using System.Diagnostics;

namespace DCT
{
    class Calc
    {
        private enum Parameter : int { Y, Cb, Cr }
        private float[,] dct, transDCT;
        private Image image;
        private int width, height;
        private int CompressPercent { get; set; }
        private int firstByteToDelete;
        private int firstByteToDeleteX, firstByteToDeleteY;
        private YCbCr[,] pixelsYCbCr;

        public void Initialization()
        {
            Color[,] pixelsRGB = GetRGBpixels(@"C:\test\test.bmp");

            pixelsYCbCr = ConvertRGBtoYCbCr(pixelsRGB);
        }

        public Image CompressImage(int quality)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            YCbCr[,] compImage = GetCompressedPixelMatrix(pixelsYCbCr, quality);

            stopWatch.Stop();
            TimeSpan ts = stopWatch.Elapsed;
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
            Console.WriteLine("Время выполнения программы: " + elapsedTime);

            stopWatch.Restart();

            Color[,] compPixelsRGB = ConvertYCbCrtoRGB(compImage);

            stopWatch.Stop();
            ts = stopWatch.Elapsed;
            elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
            Console.WriteLine("Время выполнения программы: " + elapsedTime);


            stopWatch.Restart();

            Image img = SaveCompressedImage(image, compPixelsRGB);

            stopWatch.Stop();
            ts = stopWatch.Elapsed;
            elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
            Console.WriteLine("Время выполнения программы: " + elapsedTime);

            return img;
        }

        private Color[,] GetRGBpixels(string path)
        {
            image = Image.FromFile(path);
            width = image.Width;
            height = image.Height;
            Color[,] colors = new Color[width, height];
            if (image is Bitmap bitmap)
            {
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        colors[x, y] = bitmap.GetPixel(x, y);
                    }
                }
            }
            return colors;
        }

        private YCbCr[,] ConvertRGBtoYCbCr(Color[,] pixelsRGB)
        {
            YCbCr[,] pixelsYCbCr = new YCbCr[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    byte red = pixelsRGB[x, y].R;
                    byte green = pixelsRGB[x, y].G;
                    byte blue = pixelsRGB[x, y].B;

                    pixelsYCbCr[x, y].Y = Limit(0.299f * red + 0.587f * green + 0.114f * blue, 16, 235);
                    pixelsYCbCr[x, y].Cb = Limit(128 - 0.1678736f * red - 0.331264f * green + 0.5f * blue, 16, 240);
                    pixelsYCbCr[x, y].Cr = Limit(128 + 0.5f * red - 0.418688f * green + 0.081312f * blue, 16, 240);
                }
            }
            return pixelsYCbCr;
        }

        private Color[,] ConvertYCbCrtoRGB(YCbCr[,] pixelsYCbCr)
        {
            Color[,] pixelsRGB = new Color[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float Y = pixelsYCbCr[x, y].Y;
                    float Cb = pixelsYCbCr[x, y].Cb - 128;
                    float Cr = pixelsYCbCr[x, y].Cr - 128;

                    float red = Y + 1.4022f * Cr;
                    float green = Y - 0.3456f * Cb - 0.7145f * Cr;
                    float blue = Y + 1.771f * Cb;

                    red = Limit(red, 0, 255);
                    green = Limit(green, 0, 255);
                    blue = Limit(blue, 0, 255);

                    pixelsRGB[x, y] = Color.FromArgb((byte)red, (byte)green, (byte)blue);
                }
            }
            return pixelsRGB;
        }

        private float Limit(float value, float lower, float upper)
        {
            return Math.Max(lower, Math.Min(value, upper));
        }

        private float[,] GetDCT()
        {
            float[,] dct = new float[8, 8];
            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    if (x == 0)
                    {
                        dct[x, y] = 0.35356f;
                    }
                    else
                    {
                        dct[x, y] = (float)(0.5f * Math.Cos((2 * y + 1) * x * 0.19635f));
                    }
                }
            }

            return dct;
        }

        private float[,] MulMatrix(float[,] matrix1, float[,] matrix2)
        {
            int size = matrix1.GetLength(0);
            float[,] result = new float[size, size];

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    result[x, y] = GetMatrixNode(GetRow(matrix1, x), GetColumn(matrix2, y));
                }
            }

            return result;
        }

        private float GetMatrixNode(float[] row, float[] column)
        {
            float matrixNode = 0;
            for (int i = 0; i < row.Length; i++)
            {
                matrixNode += row[i] * column[i];
            }

            return matrixNode;
        }

        private T[,] GetTransposedMatrix<T>(T[,] matrix)
        {
            int size = matrix.GetLength(0);
            T[,] tranMatrix = new T[size, size];

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    tranMatrix[x, y] = matrix[y, x];
                }
            }
            return tranMatrix;
        }

        private static T[] GetRow<T>(T[,] matrix, int row)
        {
            if (matrix.GetLength(0) <= row)
                throw new ArgumentException();

            T[] rowRes = new T[matrix.GetLength(1)];
            for (int i = 0; i < matrix.GetLength(1); i++)
            {
                rowRes[i] = matrix[row, i];
            }

            return rowRes;
        }

        private static T[] GetColumn<T>(T[,] matrix, int column)
        {
            if (matrix.GetLength(0) <= column)
                throw new ArgumentException();

            T[] columnRes = new T[matrix.GetLength(1)];
            for (int i = 0; i < matrix.GetLength(1); i++)
            {
                columnRes[i] = matrix[i, column];
            }

            return columnRes;
        }

        private YCbCr[,] GetCompressedPixelMatrix(YCbCr[,] pixelsYCbCr, int quality)
        {
            YCbCr[,] compMatrix = new YCbCr[width, height];
            YCbCr[,] pixelBlock, compPixelBlock;
            int numberOfXblocks = width / 8;
            int numberOfYblocks = height / 8;

            CompressPercent = quality;
            Validate.IsTrue(CompressPercent >= 0 && CompressPercent <= 100, "Недопустимое значение.");
            firstByteToDelete = (int)Limit((float)((100 - CompressPercent) * 0.64), 0, 100);
            firstByteToDeleteX = firstByteToDelete / 8;
            firstByteToDeleteY = firstByteToDelete % 8;

            dct = GetDCT();
            transDCT = GetTransposedMatrix(dct);

            for (int XblockCoord = 0; XblockCoord < numberOfXblocks; XblockCoord++)
            {
                for (int YblockCoord = 0; YblockCoord < numberOfYblocks; YblockCoord++)
                {
                    pixelBlock = GetBlock8x8(pixelsYCbCr, XblockCoord, YblockCoord);
                    compPixelBlock = CompressBlock(pixelBlock);
                    WriteCompressedBlockToYCbCrMatrix(compMatrix, compPixelBlock, XblockCoord, YblockCoord);
                }
            }

            return compMatrix;
        }

        private YCbCr[,] GetBlock8x8(YCbCr[,] pixels, int blockCoordX, int blockCoordY)
        {
            YCbCr[,] pixelBlock = new YCbCr[8, 8];

            int pixelBlockCoordX = 0;
            int pixelBlockCoordY = 0;
            int offsetX = blockCoordX * 8;
            int offsetY = blockCoordY * 8;

            for (int x = offsetX; x < offsetX + 8; x++, pixelBlockCoordX++, pixelBlockCoordY = 0)
            {
                for (int y = offsetY; y < offsetY + 8; y++, pixelBlockCoordY++)
                {
                    pixelBlock[pixelBlockCoordX, pixelBlockCoordY] = pixels[x, y];
                }
            }
            return pixelBlock;
        }

        private YCbCr[,] CompressBlock(YCbCr[,] pixelBlock)
        {
            YCbCr[,] compPixelBlock = new YCbCr[8, 8];
            Array.Copy(pixelBlock, compPixelBlock, pixelBlock.Length);
            float[,] tempParameterBlock, tempMatrix;

            tempParameterBlock = GetParameterBlockByYCbCrBlock(compPixelBlock, Parameter.Y);
            tempMatrix = MulMatrix(tempParameterBlock, transDCT);
            tempMatrix = RemoveExcess(ref tempMatrix);
            tempMatrix = MulMatrix(tempMatrix, dct);
            WriteCompressedParameterBlockToYCbCrBlock(compPixelBlock, tempMatrix, Parameter.Y);

            tempParameterBlock = GetParameterBlockByYCbCrBlock(compPixelBlock, Parameter.Cb);
            tempMatrix = MulMatrix(tempParameterBlock, transDCT);
            tempMatrix = RemoveExcess(ref tempMatrix);
            tempMatrix = MulMatrix(tempMatrix, dct);
            WriteCompressedParameterBlockToYCbCrBlock(compPixelBlock, tempMatrix, Parameter.Cb);

            tempParameterBlock = GetParameterBlockByYCbCrBlock(compPixelBlock, Parameter.Cr);
            tempMatrix = MulMatrix(tempParameterBlock, transDCT);
            tempMatrix = RemoveExcess(ref tempMatrix);
            tempMatrix = MulMatrix(tempMatrix, dct);
            WriteCompressedParameterBlockToYCbCrBlock(compPixelBlock, tempMatrix, Parameter.Cr);

            return compPixelBlock;
        }

        private float[,] RemoveExcess(ref float[,] matrix)
        {
            int x = firstByteToDeleteX;
            int y = firstByteToDeleteY;

            for (; x < 8; x++, y = 0)
            {
                for (; y < 8; y++)
                {
                    matrix[y, x] = 0;
                }
            }
            return matrix;
        }

        private float[,] GetParameterBlockByYCbCrBlock(YCbCr[,] pixelBlock, Parameter par)
        {
            float[,] parameterBlock = new float[8, 8];

            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    switch (par)
                    {
                        case Parameter.Y:
                            parameterBlock[x, y] = pixelBlock[x, y].Y;
                            break;
                        case Parameter.Cb:
                            parameterBlock[x, y] = pixelBlock[x, y].Cb;
                            break;
                        case Parameter.Cr:
                            parameterBlock[x, y] = pixelBlock[x, y].Cr;
                            break;
                    }
                }
            }

            return parameterBlock;

        }

        private void WriteCompressedParameterBlockToYCbCrBlock(YCbCr[,] pixelBlock, float[,] compParameterBlock, Parameter par)
        {
            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    switch (par)
                    {
                        case Parameter.Y:
                            pixelBlock[x, y].Y = compParameterBlock[x, y];
                            break;
                        case Parameter.Cb:
                            pixelBlock[x, y].Cb = compParameterBlock[x, y];
                            break;
                        case Parameter.Cr:
                            pixelBlock[x, y].Cr = compParameterBlock[x, y];
                            break;
                    }
                }
            }
        }

        private void WriteCompressedBlockToYCbCrMatrix(YCbCr[,] pixels, YCbCr[,] compBlock, int blockCoorX, int blockCoorY)
        {
            int pixelBlockCoordX = 0;
            int pixelBlockCoordY = 0;
            int offsetX = blockCoorX * 8;
            int offsetY = blockCoorY * 8;

            for (int x = offsetX; x < offsetX + 8; x++, pixelBlockCoordX++, pixelBlockCoordY = 0)
            {
                for (int y = offsetY; y < offsetY + 8; y++, pixelBlockCoordY++)
                {
                    pixels[x, y] = compBlock[pixelBlockCoordX, pixelBlockCoordY];
                }
            }
        }
       
        private Image SaveCompressedImage(Image image, Color[,] pixelsRGB)
        {
            if (image is Bitmap bitmap)
            {
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        bitmap.SetPixel(x, y, pixelsRGB[x, y]);
                    }
                }
            }
            return image;
        }
    }

}

