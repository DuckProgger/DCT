using System;
using System.Drawing;
using System.Diagnostics;

namespace DCT
{
    class Calc
    {
        private enum Parameter : int { Y, Cb, Cr }
        double[,] dct, transDCT;
        Image image;
        int width, height;

        public void Proc()
        {

            Color[,] pixelsRGB = GetRGBpixels(@"C:\test\test.bmp");

            YCbCr[,] pixelsYCbCr = ConvertRGBtoYCbCr(pixelsRGB);

            dct = GetDCT();
            transDCT = GetTransposedDCT(dct);

            YCbCr[,] compImage = GetCompressedPixelMatrix(pixelsYCbCr);

            Color[,] compPixelsRGB = ConvertYCbCrtoRGB(compImage);

            SaveCompressedImage(image, compPixelsRGB, @"C:\test\test2.bmp");
            ;
        }

        private Color[,] GetRGBpixels(string path)
        {
            image = Image.FromFile(path);
            width = image.Width;
            height = image.Height;
            Color[,] colors = new Color[width, height];
            if (image is Bitmap)
            {
                for (int i = 0; i < width; i++)
                {
                    for (int j = 0; j < height; j++)
                    {
                        colors[i, j] = ((Bitmap)image).GetPixel(i, j);
                    }
                }
            }
            return colors;
        }

        private YCbCr[,] ConvertRGBtoYCbCr(Color[,] pixelsRGB)
        {
            YCbCr[,] pixelsYCbCr = new YCbCr[width, height];
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    byte red = pixelsRGB[i, j].R;
                    byte green = pixelsRGB[i, j].G;
                    byte blue = pixelsRGB[i, j].B;

                    pixelsYCbCr[i, j].Y = (byte)(0.299 * red + 0.578 * green * blue);
                    pixelsYCbCr[i, j].Cb = (byte)(0.1678 * red - 0.3313 * green + 0.5 * blue);
                    pixelsYCbCr[i, j].Cr = (byte)(0.5 * red - 0.4187 * green + 0.0813 * blue);
                }
            }
            return pixelsYCbCr;
        }

        private Color[,] ConvertYCbCrtoRGB(YCbCr[,] pixelsYCbCr)
        {
            byte red, green, blue;

            Color[,] pixelsRGB = new Color[width, height];
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    byte Y = pixelsYCbCr[i, j].Y;
                    byte Cb = (byte)(pixelsYCbCr[i, j].Cb - 128);
                    byte Cr = (byte)(pixelsYCbCr[i, j].Cr - 128);

                    red = (byte)(Y + 1.402 * Cr);
                    green = (byte)(Y - 0.34414 * Cb - 0.71414 * Cr);
                    blue = (byte)(Y + 1.772 * Cb);

                    red = Math.Max((byte)0, Math.Min(red, (byte)255));
                    green = Math.Max((byte)0, Math.Min(green, (byte)255));
                    blue = Math.Max((byte)0, Math.Min(blue, (byte)255));

                    pixelsRGB[i, j] = Color.FromArgb(red, green, blue);
                }
            }
            return pixelsRGB;
        }

        private double[,] GetDCT()
        {
            int x = 8;
            double[,] dct = new double[x, x];
            for (int i = 0; i < x; i++)
            {
                for (int j = 0; j < x; j++)
                {
                    if (i == 0)
                    {
                        dct[i, j] = 0.35356;
                    }
                    else
                    {
                        dct[i, j] = 0.5 * Math.Cos((2 * j + 1) * i * 0.19635);
                    }
                }
            }

            ShowMatrix(dct);
            return dct;
        }

        private double[,] GetTransposedDCT(double[,] dct)
        {
            int x = dct.GetLength(0);
            double[,] transDCT = new double[x, x];

            for (int i = 0; i < x; i++)
            {
                for (int j = 0; j < x; j++)
                {
                    transDCT[i, j] = dct[j, i];
                }
            }
            ShowMatrix(transDCT);
            return transDCT;
        }

        private byte[,] MulMatrix(byte[,] matrix1, double[,] matrix2)
        {
            int x = matrix1.GetLength(0);
            byte[,] result = new byte[x, x];

            for (int i = 0; i < x; i++)
            {
                for (int j = 0; j < x; j++)
                {
                    result[i, j] = GetMatrixNode(GetRow(matrix1, i), GetColumn(matrix2, j));
                }
            }

            //ShowArray(result);
            return result;
        }

        private byte GetMatrixNode(byte[] row, double[] column)
        {
            byte matrixNode = 0;
            for (int i = 0; i < row.Length; i++)
            {
                matrixNode += (byte)(row[i] * column[i]);
            }

            return matrixNode;
        }

        private void ShowMatrix(double[,] matrix)
        {
            int x = matrix.GetLength(0);
            for (int i = 0; i < x; i++)
            {
                for (int j = 0; j < x; j++)
                {
                    Console.Write("{0, 6:0.###} ", matrix[i, j]);
                    if (j == x - 1)
                    {
                        Console.WriteLine();
                    }
                }
            }
            Console.WriteLine();
        }

        private static T[] GetRow<T>(T[,] matrix, int row)
        {
            if (matrix.GetLength(0) <= row)
                throw new ArgumentException();

            T[] row_res = new T[matrix.GetLength(1)];
            for (int i = 0; i < matrix.GetLength(1); i++)
            {
                row_res[i] = matrix[row, i];
            }

            return row_res;
        }

        private static T[] GetColumn<T>(T[,] matrix, int column)
        {
            if (matrix.GetLength(0) <= column)
                throw new ArgumentException();

            T[] column_res = new T[matrix.GetLength(1)];
            for (int i = 0; i < matrix.GetLength(1); i++)
            {
                column_res[i] = matrix[i, column];
            }

            return column_res;
        }

        private YCbCr[,] GetCompressedPixelMatrix(YCbCr[,] pixelsYCbCr)
        {
            YCbCr[,] compMatrix = new YCbCr[pixelsYCbCr.GetLength(0), pixelsYCbCr.GetLength(1)];
            YCbCr[,] pixelBlock, tempCompPixelBlock;
            int numberOfXblocks = width / 8;
            int numberOfYblocks = height / 8;

            for (int XblockCoord = 0; XblockCoord < numberOfXblocks; XblockCoord++)
            {
                for (int YblockCoord = 0; YblockCoord < numberOfYblocks; YblockCoord++)
                {
                    pixelBlock = GetBlock8x8(pixelsYCbCr, XblockCoord, YblockCoord);
                    tempCompPixelBlock = CompressBlock(pixelBlock);
                    WriteCompressedBlockToYCbCrMatrix(compMatrix, tempCompPixelBlock, XblockCoord, YblockCoord);
                }
            }

            return compMatrix;
        }

        private YCbCr[,] GetBlock8x8(YCbCr[,] pixels, int coorX, int coorY)
        {
            YCbCr[,] pixelBlock = new YCbCr[8, 8];

            int pixelBlockCoordX = 0;
            int pixelBlockCoordY = 0;

            for (int i = coorX * 8; i < (coorX + 1) * 8; i++, pixelBlockCoordX++, pixelBlockCoordY = 0)
            {
                for (int j = coorY * 8; j < (coorY + 1) * 8; j++, pixelBlockCoordY++)
                {
                    pixelBlock[pixelBlockCoordX, pixelBlockCoordY] = pixels[i, j];
                }
            }
            return pixelBlock;
        }

        private YCbCr[,] CompressBlock(YCbCr[,] pixelBlock)
        {
            Validate.IsTrue(pixelBlock.GetLength(0) == 8 || pixelBlock.GetLength(1) == 8, "Неверный размер блока");

            YCbCr[,] compPixelBlock = new YCbCr[8, 8];
            Array.Copy(pixelBlock, compPixelBlock, pixelBlock.Length);

            byte[,] tempParameterBlock = GetParameterBlockByYCbCrBlock(compPixelBlock, Parameter.Y);
            byte[,] tempMatrix = MulMatrix(tempParameterBlock, transDCT);
            tempMatrix = MulMatrix(tempMatrix, dct);
            WriteCompressedParameterBlockToYCbCrBlock(compPixelBlock, tempMatrix, Parameter.Y); 

            tempParameterBlock = GetParameterBlockByYCbCrBlock(compPixelBlock, Parameter.Cb);
            tempMatrix = MulMatrix(tempParameterBlock, transDCT);
            tempMatrix = MulMatrix(tempMatrix, dct);
            WriteCompressedParameterBlockToYCbCrBlock(compPixelBlock, tempMatrix, Parameter.Cb);

            tempParameterBlock = GetParameterBlockByYCbCrBlock(compPixelBlock, Parameter.Cr);
            tempMatrix = MulMatrix(tempParameterBlock, transDCT);
            tempMatrix = MulMatrix(tempMatrix, dct);
            WriteCompressedParameterBlockToYCbCrBlock(compPixelBlock, tempMatrix, Parameter.Cr);

            return compPixelBlock;
        }

        private byte[,] GetParameterBlockByYCbCrBlock(YCbCr[,] pixelBlock, Parameter par)
        {
            byte[,] parameterBlock = new byte[8, 8];

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

        private void WriteCompressedParameterBlockToYCbCrBlock(YCbCr[,] pixelBlock, byte[,] compParameterBlock, Parameter par)
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

            for (int x = blockCoorX * 8; x < (blockCoorX + 1) * 8; x++, pixelBlockCoordX++, pixelBlockCoordY = 0)
            {
                for (int y = blockCoorY * 8; y < (blockCoorY + 1) * 8; y++, pixelBlockCoordY++)
                {
                    pixels[x, y] = compBlock[pixelBlockCoordX, pixelBlockCoordY];
                }
            }
        }

        private void SaveCompressedImage(Image image, Color[,] pixelsRGB, string path)
        {
            if (image is Bitmap)
            {
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        ((Bitmap)image).SetPixel(x, y, pixelsRGB[x, y]);
                    }
                }
            }
            image.Save(path);
        }
    }

}

