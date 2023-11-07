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

            //PrintMatrix(Cb); CreateComparisonImage(image, Y, Cb, Cr);

            /* ======= Chrominance Downsampling (4:2:0) ======= */

            int halfWidth  = (width  >> 1) + (width  & 1);
            int halfHeight = (height >> 1) + (height & 1);

            byte[,] dsCb = new byte[halfHeight, halfWidth];
            byte[,] dsCr = new byte[halfHeight, halfWidth];

            ChrominanceDownsampling(height, width, Cb, Cr, dsCb, dsCr);
            
            //PrintMatrix(dsCb); CreateDownsampledImage(dsCb, dsCr);


            /* ======= Discrete Cosine Transform (DST) ======== */

            DiscreteCosineTransform(halfHeight, halfWidth, dsCr);



            //quantization

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

                    Console.Write(input[y, x] + " ");
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
        static void DiscreteCosineTransform(int height, int width, byte[,] imageComponent)
        {
            int width8 = width + 8 - (width % 8);
            int height8 = height + 8 - (height % 8);
            sbyte[,] shiftedValues = new sbyte[height8, width8];

            Console.WriteLine(width + "x" + height);
            Console.WriteLine(width8 + "x" + height8);

            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    shiftedValues[y, x] = (sbyte)(imageComponent[y, x] - 128);
                }

            if(height8 > height)
                for(int y = height; y < height8; y++)
                    for(int x = 0; x < width; x++)
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


            PrintMatrix(shiftedValues);
        }
    }
}