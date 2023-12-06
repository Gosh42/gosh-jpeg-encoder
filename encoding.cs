using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#pragma warning disable CA1416 // Validate platform compatibility
namespace anotherJpeg
{
    internal class Encoding
    {
        public static void ColourSpaceConversion(Bitmap image, byte[,] Y, byte[,] Cb, byte[,] Cr)
        {
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    Color clr = image.GetPixel(x, y);
                    byte R = clr.R; byte G = clr.G; byte B = clr.B;

                    Y[y, x] = (byte)(0.299 * R + 0.587 * G + 0.114 * B);
                    Cb[y, x] = (byte)(-0.168736 * R - 0.331264 * G + 0.5 * B + 128);
                    Cr[y, x] = (byte)(0.5 * R - 0.418688 * G - 0.081312 * B + 128);
                }
            }
        }
        public static void ChrominanceDownsampling(int height, int width, byte[,] Cb, byte[,] Cr, byte[,] dsCb, byte[,] dsCr)
        {
            int halfHeightIndex = dsCb.GetLength(0) - 1;
            int halfWidthIndex = dsCb.GetLength(1) - 1;

            for (int y = 1; y < height - (height & 1); y++)
            {
                for (int x = 1; x < width - (width & 1); x++)
                {
                    dsCb[y >> 1, x >> 1] = (byte)((Cb[y - 1, x - 1] + Cb[y - 1, x] + Cb[y, x - 1] + Cb[y, x]) >> 2);
                    dsCr[y >> 1, x >> 1] = (byte)((Cr[y - 1, x - 1] + Cr[y - 1, x] + Cr[y, x - 1] + Cr[y, x]) >> 2);
                }
            }

            // если изначальное изображение имеет нечётные:
            if ((height & 1) == 1) //высоту
                for (int x = 1; x < width; x += 2)
                {
                    dsCb[halfHeightIndex, x >> 1] = (byte)((Cb[height - 1, x - 1] + Cb[height - 1, x]) >> 1);
                    dsCr[halfHeightIndex, x >> 1] = (byte)((Cr[height - 1, x - 1] + Cr[height - 1, x]) >> 1);
                }
            if ((width & 1) == 1) //ширину
                for (int y = 1; y < height; y += 2)
                {
                    dsCb[y >> 1, halfWidthIndex] = (byte)((Cb[y - 1, width - 1] + Cb[y, width - 1]) >> 1);
                    dsCr[y >> 1, halfWidthIndex] = (byte)((Cr[y - 1, width - 1] + Cr[y, width - 1]) >> 1);
                }
            if ((height & 1) == 1 && (width & 1) == 1) //высоту и ширину
            {
                dsCb[halfHeightIndex, halfWidthIndex] = Cb[height - 1, width - 1];
                dsCr[halfHeightIndex, halfWidthIndex] = Cr[height - 1, width - 1];
            }
        }
        public static short[,] DCT(byte[,] imageComponent)
        {
            int height = imageComponent.GetLength(0);
            int width = imageComponent.GetLength(1);

            int height8 = height;
            int width8 = width;

            if (height8 % 8 != 0) height8 += 8 - (height % 8);
            if (width8 % 8 != 0) width8 += 8 - (width % 8);

            // Дополнение размера и изображения до размеров, делящихся на 8
            // для разбиения на блоки по 8 на 8 пикселей,
            // а также "сдвиг" значений путём отнимания 128
            sbyte[,] shiftedValues = new sbyte[height8, width8];
            {
                for (int y = 0; y < height; y++)
                    for (int x = 0; x < width; x++)
                    {
                        shiftedValues[y, x] = (sbyte)(imageComponent[y, x] - 128);
                    }

                if (height8 > height)
                    for (int y = height; y < height8; y++)
                        for (int x = 0; x < width; x++)
                        {
                            shiftedValues[y, x] = shiftedValues[height - 1, x];
                        }

                if (width8 > width)
                    for (int y = 0; y < height; y++)
                        for (int x = width; x < width8; x++)
                        {
                            shiftedValues[y, x] = shiftedValues[y, width - 1];
                        }

                if (height8 > height && width8 > width)
                    for (int y = height; y < height8; y++)
                        for (int x = width; x < width8; x++)
                        {
                            shiftedValues[y, x] = shiftedValues[height - 1, width - 1];
                        }
            }
            // само дискретно косинусное преобразование
            double au, av;
            double oneDivBySqrt8 = 1 / Math.Sqrt(8);
            double sqrt0_25 = Math.Sqrt(0.25);
            short[,] dct = new short[height8, width8];

            for (int yOffset = 0; yOffset < height8; yOffset += 8)
                for (int xOffset = 0; xOffset < width8; xOffset += 8)
                    for (int v = 0; v < 8; v++)
                    {
                        if (v == 0)
                            av = oneDivBySqrt8;
                        else
                            av = sqrt0_25;

                        for (int u = 0; u < 8; u++)
                        {
                            if (u == 0)
                                au = oneDivBySqrt8;
                            else
                                au = sqrt0_25;

                            double temp = 0;

                            for (int x = 0; x < 8; x++)
                            {
                                for (int y = 0; y < 8; y++)
                                {   
                                    temp += shiftedValues[y + yOffset, x + xOffset] * Math.Cos((2 * x + 1) * u * Math.PI / 16)
                                        * Math.Cos((2 * y + 1) * v * Math.PI / 16);
                                }
                            }
                            temp *= au * av / 4;
                            dct[v + yOffset, u + xOffset] = (short)Math.Round(temp);
                        }
                    }

            return dct;
        }
        public static short[,] Quantise(short[,] dct, byte[,] quantisationMatrix)
        {
            for (int y = 0; y < dct.GetLength(0); y++)
                for (int x = 0; x < dct.GetLength(1); x++)
                    dct[y, x] /= quantisationMatrix[y % 8, x % 8];

            return dct;
        }
        public static short[] Zigzag(short[,] input)
        {
            int height = input.GetLength(0);
            int width = input.GetLength(1);

            short[] numbers = new short[height * width];
            int i = 0;
            for (int yOffset = 0; yOffset < height; yOffset += 8)
                for (int xOffset = 0; xOffset < width; xOffset += 8)
                {
                    int x = xOffset, y = yOffset;
                    // Обход до левого нижнего угла
                    while (true)
                    {
                        // Вправо один раз (x++)
                        numbers[i++] = input[y, x++];

                        do // Влево вниз (y++ x--)
                        {
                            numbers[i++] = input[y++, x--];
                        } while (x > xOffset);

                        if (y == yOffset + 7) // Выход при достижении нижнего края
                            break;

                        // Вниз один раз (y++)
                        numbers[i++] = input[y++, x];

                        do // Вправо вверх (y-- x++)
                        {
                            numbers[i++] = input[y--, x++];
                        } while (y > yOffset);
                    }

                    // Обход до правого нижнего угла
                    while (true)
                    {
                        // Вправо один раз (x++)
                        numbers[i++] = input[y, x++];

                        // Выход при достижении правого нижнего угла
                        if (x == xOffset + 7)
                            break;

                        // Вправо вверх (y-- x++)
                        do
                        {
                            numbers[i++] = input[y--, x++];
                        } while (x < xOffset + 7);


                        // Вниз один раз (y++)
                        numbers[i++] = input[y++, x];

                        // Влево вниз (y++ x--)
                        do
                        {
                            numbers[i++] = input[y++, x--];
                        } while (y < yOffset + 7);
                    }
                    numbers[i++] = input[y, x];
                }
            return numbers;
        }
        public static List<EncodedValue> RunLengthEncoding(short[] input)
        {
            List<EncodedValue> output = new List<EncodedValue>();
            
            short previousDC = 0;
            for (int offset = 0; offset < input.Length; offset += 64)
            {
                int lastNonZeroIndex = -1;
                for (int i = offset; i < offset + 64; i++)
                    if (input[i] != 0) 
                        lastNonZeroIndex = i;

                byte runLength = 0;

                for (int i = offset; i < offset + 64; i++)
                {
                    short num = input[i];
                    if (i == offset) // DC
                    {
                        short temp = num;
                        num = (short)(num - previousDC);
                        previousDC = temp;

                        byte size = (byte)(Math.Log(Math.Abs(num), 2) + 1);

                        output.Add(new EncodedValue(-1, size, num));
                    }
                    else // AC
                    {
                        if (i > lastNonZeroIndex)
                        {
                            output.Add(new EncodedValue(0, 0, 0));
                            break;
                        }
                        else if (num == 0 && runLength < 15)
                            runLength++;
                        else
                        {
                            byte size = (byte)(Math.Log(Math.Abs(num), 2) + 1);
                            output.Add(new EncodedValue(runLength, size, num));
                            runLength = 0;
                        }
                    }
                }
            }

            return output;
        }
        public static void SetBits(List<EncodedValue> input, bool isLuminance)
        {
            string[] categoryCodesDC = new string[0];
            string[,] prefixCodesAC = new string[0,0];
            if (isLuminance)
            {
                categoryCodesDC = TableData.categoryCodes_LumDC;
                prefixCodesAC = TableData.prefixCodes_LumAC;
            }/*
            else
            {
                categoryCodesDC = TableData.categoryCodes_ChromDC;
                prefixCodesAC = TableData.prefixCodes_ChromAC;
            }*/
            foreach (EncodedValue code in input)
            {
                short value = code.Value;

                if (value < 0)
                    value = (short)~(-value);

                string valueBitStr = Convert.ToString(value, 2)[^code.Size..];

                if (code.RunLength == -1)
                {
                    code.PrefixBitString = categoryCodesDC[code.Size];
                    code.ValueBitString = valueBitStr;
                } 
                else
                {
                    code.PrefixBitString = prefixCodesAC[code.RunLength, code.Size];
                    code.ValueBitString = valueBitStr;
                }

            }
        }
    }
}
