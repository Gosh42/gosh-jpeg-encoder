using System.Drawing;

namespace jpeg
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string path = @"D:\convert2jpg.png";
            Bitmap image = new Bitmap(path);

            int width  = image.Width;
            int height = image.Height;


            /* ==== Colour Space Conversion (RGB → YCbCr) ===== */

            byte[,] Y  = new byte[height, width];
            byte[,] Cb = new byte[height, width];
            byte[,] Cr = new byte[height, width];
            ColourSpaceConversion(image, Y, Cb, Cr);

            //PrintMatrix(Cb);
            CreateComparisonImage(image, Y, Cb, Cr);

            /* ======= Chrominance Downsampling (4:2:0) ======= */

            int halfWidth  = (width  >> 1) + (width  & 1);
            int halfHeight = (height >> 1) + (height & 1);

            byte[,] dsCb = new byte[halfHeight, halfWidth];
            byte[,] dsCr = new byte[halfHeight, halfWidth];

            ChrominanceDownsampling(height, width, Cb, Cr, dsCb, dsCr);

            //PrintMatrix(dsCb);
            CreateDownsampledImage(dsCb, dsCr);


            /* ======= Discrete Cosine Transform (DST) ======== */
            /* =============== and Quantisation =============== */

            byte quality = 50;
            short[,] luminanceQuantisationMatrix = GenerateQuantisationMatrix(quality, false);
            short[,] chrominanceQuantisationMatrix = GenerateQuantisationMatrix(quality, true);

            short[,] quantisedY = DiscreteCosineTransform(height, width, Y);
            short[,] quantisedCb = DiscreteCosineTransform(halfHeight, halfWidth, dsCb);
            short[,] quantisedCr = DiscreteCosineTransform(halfHeight, halfWidth, dsCr);

            //PrintMatrix(quantisedCb);

            Quantise(quantisedY, luminanceQuantisationMatrix);
            Quantise(quantisedCb, chrominanceQuantisationMatrix);
            Quantise(quantisedCr, chrominanceQuantisationMatrix);

            PrintMatrix(quantisedCb);

            short[,] test =
            {
                { 0,  1,  5,  6, 14, 15, 27, 28},
                { 2,  4,  7, 13, 16, 26, 29, 42},
                { 3,  8, 12, 17, 25, 30, 41, 43},
                { 9, 11, 18, 24, 31, 40, 44, 53},
                {10, 19, 23, 32, 39, 45, 52, 54},
                {20, 22, 33, 38, 46, 51, 55, 60},
                {21, 34, 37, 47, 50, 56, 59, 61},
                {35, 36, 48, 49, 57, 58, 62, 63}
            };

            //PrintMatrix(test);
            Zigzag(quantisedCb);

            //run length and huffman encoding
        }

        // Temporary helper functions
        static void PrintMatrix(byte[,] input)
        {
            for (int y = 0; y < input.GetLength(0); y++)
            {
                for (int x = 0; x < input.GetLength(1); x++)
                {
                    Console.Write(input[y, x] + "\t");
                }
                Console.WriteLine();
            }
            Console.WriteLine();
        }
        static void PrintMatrix(sbyte[,] input)
        {
            for (int y = 0; y < input.GetLength(0); y++)
            {
                for (int x = 0; x < input.GetLength(1); x++)
                {
                    Console.Write(input[y, x] + "  ");
                }
                Console.WriteLine();
            }
            Console.WriteLine();
        }
        static void PrintMatrix(short[,] input)
        {
            for (int y = 0; y < input.GetLength(0); y++)
            {
                for (int x = 0; x < input.GetLength(1); x++)
                {
                    Console.Write(input[y, x] + "  ");
                }
                Console.WriteLine();
            }
            Console.WriteLine();
        }
        static void CreateComparisonImage(Bitmap image, byte[,] Y, byte[,] Cb, byte[,] Cr)
        {
            int height = image.Height;
            int width = image.Width;

            Bitmap newImage = new Bitmap(image.Width * 3, image.Height * 2);
            //R
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    newImage.SetPixel(x, y, Color.FromArgb(image.GetPixel(x, y).R, 0, 0));
            //G
            for (int y = 0; y < height; y++)
                for (int x = width; x < width * 2; x++)
                    newImage.SetPixel(x, y, Color.FromArgb(0, image.GetPixel(x - width, y).G, 0));
            //B
            for (int y = 0; y < height; y++)
                for (int x = width * 2; x < width * 3; x++)
                    newImage.SetPixel(x, y, Color.FromArgb(0, 0, image.GetPixel(x - width * 2, y).B));
            //Y
            for (int y = height; y < height * 2; y++)
                for (int x = 0; x < width; x++)
                    newImage.SetPixel(x, y, Color.FromArgb(Y[y - height, x], Y[y - height, x], Y[y - height, x]));
            //Cb
            for (int y = height; y < height * 2; y++)
                for (int x = width; x < width * 2; x++)
                    newImage.SetPixel(x, y, Color.FromArgb(
                        (byte)128, 
                        (byte)(128 - 0.344136 * (Cb[y - height, x - width] - 128)),
                        (byte)(128 + 1.772 * (Cb[y - height, x - width] - 128))
                        ));
            //Cr
            for (int y = height; y < height * 2; y++)
                for (int x = width * 2; x < width * 3; x++)
                    newImage.SetPixel(x, y, Color.FromArgb(
                        (byte)(128 + 1.402 * (Cr[y - height, x - width * 2] - 128)),
                        (byte)(128 - 0.714136 * (Cr[y - height, x - width * 2] - 128)), 
                        (byte)(128)
                        ));

            newImage.Save(@"clr.png");
        }
        static void CreateDownsampledImage(byte[,] dsCb, byte[,] dsCr)
        {
            int height = dsCb.GetLength(0);
            int width = dsCb.GetLength(1);
            Bitmap newImage = new Bitmap(width << 1, height);
            //Cb
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    newImage.SetPixel(x, y, Color.FromArgb(
                        (byte)128,
                        (byte)(128 - 0.344136 * (dsCb[y, x] - 128)),
                        (byte)(128 + 1.772 * (dsCb[y, x] - 128))
                        ));
            //Cr
            for (int y = 0; y < height; y++)
                for (int x = width; x < width * 2; x++)
                    newImage.SetPixel(x, y, Color.FromArgb(
                        (byte)(128 + 1.402 * (dsCr[y, x - width] - 128)),
                        (byte)(128 - 0.714136 * (dsCr[y, x - width] - 128)),
                        (byte)(128)
                        ));

            newImage.Save(@"downsampled.png");
        }

        // Actual steps
        static void ColourSpaceConversion(Bitmap image, byte[,] Y, byte[,] Cb, byte[,] Cr)
        {
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    Color clr = image.GetPixel(x, y);
                    byte R = clr.R; byte G = clr.G; byte B = clr.B;

                     Y[y, x] = (byte)( 0.299    * R + 0.587    * G + 0.114    * B);
                    Cb[y, x] = (byte)(-0.168736 * R - 0.331264 * G + 0.5      * B + 128);
                    Cr[y, x] = (byte)( 0.5      * R - 0.418688 * G - 0.081312 * B + 128);
                }
            }
        }
        static void ChrominanceDownsampling(int height, int width, byte[,] Cb, byte[,] Cr, byte[,] dsCb, byte[,] dsCr)
        {
            int halfHeightIndex = dsCb.GetLength(0) - 1;
            int halfWidthIndex  = dsCb.GetLength(1) - 1;

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
        static short[,] DiscreteCosineTransform(int height, int width, byte[,] imageComponent)
        {
            int width8 = width;
            int height8 = height; 

            if (width8 % 8 != 0) width8 += 8 - (width % 8);
            if (height8 % 8 != 0) height8 += 8 - (height % 8);

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

            double au, av;
            double oneDivBySqrt2 = 1 / Math.Sqrt(2);
            short[,] dct = new short[height8, width8];

            for (int yOffset = 0; yOffset < height8; yOffset += 8)
                for (int xOffset = 0; xOffset < width8; xOffset += 8)
                    for(int v = 0; v < 8; v++)
                    {
                        if (v == 0)
                            av = oneDivBySqrt2;
                        else
                            av = 1;

                        for (int u = 0; u < 8; u++)
                        {
                            if (u == 0)
                                au = oneDivBySqrt2;
                            else
                                au = 1;

                            double temp = 0;
                    
                            for (int x = 0; x < 8; x++)
                            {
                                for(int y = 0; y < 8; y++)
                                {   //TODO: перенести вычисления этих косинусов в отдельный цикл чтобы оно кучу раз заново не вычислялось
                                    temp += shiftedValues[y + yOffset, x + xOffset] * Math.Cos((2*x+1)*u*Math.PI/16)
                                        * Math.Cos((2*y+1)*v*Math.PI/16);
                                }
                            }
                            temp *= au * av / 4;
                            dct[v + yOffset, u + xOffset] = (short)Math.Round(temp);
                        }
                    }

            return dct;
        }
        static short[,] GenerateQuantisationMatrix(byte quality, bool isForChrominance)
        {
            short[,] luminanceMatrix =
            {
                {16,    11,    10,    16,    24,    40,    51,    61},
                {12,    12,    14,    19,    26,    58,    60,    55},
                {14,    13,    16,    24,    40,    57,    69,    56},
                {14,    17,    22,    29,    51,    87,    80,    62},
                {18,    22,    37,    56,    68,   109,   103,    77},
                {24,    35,    55,    64,    81,   104,   113,    92},
                {49,    64,    78,    87,   103,   121,   120,   101},
                {72,    92,    95,    98,   112,   100,   103,    99}
            };
            short[,] chrominanceMatrix =
            {
                {17,    18,    24,    47,    99,    99,    99,     99},
                {18,    21,    26,    66,    99,    99,    99,     99},
                {24,    26,    56,    99,    99,    99,    99,     99},
                {47,    66,    99,    99,    99,    99,    99,     99},
                {99,    99,    99,    99,    99,    99,    99,     99},
                {99,    99,    99,    99,    99,    99,    99,     99},
                {99,    99,    99,    99,    99,    99,    99,     99},
                {99,    99,    99,    99,    99,    99,    99,     99}
            };

            short[,] matrix;
            if (isForChrominance)
                matrix = chrominanceMatrix;
            else
                matrix = luminanceMatrix;

            if (quality == 50)
                return matrix;

            int S;
            if (quality < 50)
                S = 5000 / quality;
            else
                S = 200 - 2 * quality;

            for (int y = 0; y < 8; y++)
                for (int x = 0; x < 8; x++)
                {
                    matrix[y, x] = (short)((S * matrix[y, x] + 50) / 100);
                    if (matrix[y, x] == 0)
                        matrix[y, x] = 1;
                }

            return matrix;
        }
        static void Quantise(short[,] dct, short[,] quantisationMatrix)
        {
            for (int y = 0; y < dct.GetLength(0); y++)
                for (int x = 0; x < dct.GetLength(1); x++)
                    dct[y, x] /= quantisationMatrix[y % 8, x % 8];
        }
        static void Zigzag(short[,] input)
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

            Console.WriteLine(String.Join(", ", numbers));
        }
    }
}