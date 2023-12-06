using System.Drawing;
using static anotherJpeg.Encoding;
#pragma warning disable CA1416 // Validate platform compatibility
namespace anotherJpeg
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string path = @"D:\convert2jpg.png";
            Bitmap image = new Bitmap(path);

            int width = image.Width;
            int height = image.Height;

            /* ==== Colour Space Conversion (RGB → YCbCr) ===== */
            byte[,] Y = new byte[height, width];
            byte[,] Cb = new byte[height, width];
            byte[,] Cr = new byte[height, width];
            ColourSpaceConversion(image, Y, Cb, Cr);

            /* ======= Chrominance Downsampling (4:2:0) ======= */
            int halfWidth = (width >> 1) + (width & 1);
            int halfHeight = (height >> 1) + (height & 1);
            byte[,] dsCb = new byte[halfHeight, halfWidth];
            byte[,] dsCr = new byte[halfHeight, halfWidth]; 
            ChrominanceDownsampling(height, width, Cb, Cr, dsCb, dsCr);

            /* ======= Discrete Cosine Transform (DST) ======== */
            /* =============== and Quantisation =============== */
            byte quality = 50;
            byte[,] qTableLum = TableData.GetQuantisationTable(quality, false);
            byte[,] qTableChrom = TableData.GetQuantisationTable(quality, true);

            short[,] quantisedY  = Quantise(DCT(Y), qTableLum);
            short[,] quantisedCb = Quantise(DCT(dsCb), qTableChrom);
            short[,] quantisedCr = Quantise(DCT(dsCr), qTableChrom);

            /* ==================== Zigzag ==================== */
            short[] zigzagY = Zigzag(quantisedY);
            short[] zigzagCb = Zigzag(quantisedCb);
            short[] zigzagCr = Zigzag(quantisedCr);

            /* ================ Run Length and ================ */
            /* =============== Huffman Encoding =============== */
            List<EncodedValue> lum = RunLengthEncoding(zigzagY);
            SetBits(lum, true);
            List<EncodedValue> chromBlue = RunLengthEncoding(zigzagCb);
            SetBits(chromBlue, true);
            List<EncodedValue> chromRed = RunLengthEncoding(zigzagCr);
            SetBits(chromRed, true);

            foreach (EncodedValue v in lum)
            {
                Console.WriteLine(v.RunLength + "\t" + v.Size + "\t" + v.Value + "\t" + v.PrefixBitString + "\t" + v.ValueBitString);
            }

            FileWriter.Write2File(qTableLum, qTableChrom, (short)height, (short)width, lum, chromBlue, chromRed);
        }
    }
}