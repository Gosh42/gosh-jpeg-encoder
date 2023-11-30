using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jpeg
{
    internal class JPGFileWrite
    {
        /* ================= Header Data ================== */
        static readonly byte[] SOI = { 0xFF, 0xD8 }; // Start of Image
        static readonly byte[] APP0 = { 0xFF, 0xE0 };
        static readonly byte[] headerLength = { 0x00, 0x10 }; // Длина заголовка. В данном случае 0x10 = 16 байт, включая саму длину
        static readonly byte[] JFIFIdentifier = { 0x4A, 0x46, 0x49, 0x46, 0x00 }; // Идентификатор - "JFIF" в ASCII
        static readonly byte[] JFIFVersion = { 0x01, 0x02 }; // Версия JFIF: 1.02
        static readonly byte densityType = 0x00; // 0x00 - плотность пикселей - т.е. "pixels per cm"/"pixels per inch".
                                          // В данном случае без каких-либо единиц плотности
        static readonly byte[] xDensity = { 0x00, 0x01 }; // Плотность по x
        static readonly byte[] yDensity = { 0x00, 0x01 }; // Плотность по y
        static readonly byte xThumbnail = 0x00; // Ширина превью
        static readonly byte yThumbnail = 0x00; // Высота превью. Ширина/высота по нулям, т.к. превью не будет
        // Если бы тут было превью, то оно заняло бы (3 * ширина * высота) байт - закодированы каждый цветовой канал


        /* ============= Quantisation Tables ============== */
        static readonly byte[] DQT = { 0xFF, 0xDB }; // Define Quantisation Tables
        static readonly byte[] DQTLength = { 0x00, 2+1+64/*+1+64*/ };
        static readonly byte quantisationTableInfoY = 0x00; // 8 бит, ID = 0
        static byte[] quantisationTableY = new byte[64];
        static readonly byte quantisationTableInfoС = 0x01; // 8 бит, ID = 1
        static byte[] quantisationTableС = new byte[64];


        // Тут был бы маркер DRI и его информация, но я его использовать не буду


        /* =============== Start of Frame ================= */
        static readonly byte[] SOF = { 0xFF, 0xC0 }; // Start of Frame
        static byte[] SOFLength = new byte[2];
        static byte precision = 0x08; // Бит на цветовой канал - всегда 8
        static byte[] frameHeight = new byte[2];
        static byte[] frameWidth = new byte[2];
        static byte channelAmount = 0x01;//3; // 3 - для YCbCr

        // Y
        static byte channelID_Y = 0x01;
        static byte samplingFactorY = 0x22;
        static byte quantisationTableID_Y = 0x00;
        // Cb
        static byte channelID_Cb = 0x02;
        static byte samplingFactorCb = 0x11;
        static byte quantisationTableID_Cb = 0x01;
        // Cr
        static byte channelID_Cr = 0x03;
        static byte samplingFactorCr = 0x11;
        static byte quantisationTableID_Cr = 0x01;


        /* ============ Define Huffman Tables ============= */
        static readonly byte[] DHT = { 0xFF, 0xC4 };
        static byte[] DHTLength = new byte[2];
        static readonly byte DHTInfoDC_Y = 0x00; // DC, ID: 0
        static byte[] DHTCodeAmountDC_Y = new byte[16];
        static List<byte> DHTCodesDC_Y = new List<byte>();

        // тут короче резня, потом допишу


        /* ================ Start of Scan ================= */
        static byte[] SOS = { 0xFF, 0xDA }; // Start of Scan
        static byte[] SOSLength = new byte[2];
        static byte SOSchannelAmount = 0x01; // кол-во компонентов

        static readonly byte SOSChannelID_Y = 0x00;

        static readonly byte EndOfSelection = 0x3F;
        static readonly byte StartOfSelection = 0x00;
        static readonly byte SuccessiveApproximation = 0x00;

        static readonly byte[] EOI = { 0xFF, 0xD9 }; // End of Image

        /* =============== The Thing :TM: ================= */
        public static void WriteToFile(
            byte[,] qTableY, byte[,] qTableC,
            short height, short width,
            byte[] byteCountsDC_Y, List<byte> tableValuesDC_Y, List<string> asd
            )
        {
            List<byte> byteList = new List<byte>();

            // Заголовок
            {
                byteList.AddRange(SOI);
                byteList.AddRange(APP0);
                byteList.AddRange(headerLength);
                byteList.AddRange(JFIFIdentifier);
                byteList.AddRange(JFIFVersion);
                byteList.Add(densityType);
                byteList.AddRange(xDensity);
                byteList.AddRange(yDensity);
                byteList.Add(xThumbnail);
                byteList.Add(yThumbnail);
            }
            // Таблицы квантования
            {
                for (int y = 0; y < 8; y++)
                    for (int x = 0; x < 8; x++)
                    {
                        quantisationTableY[y * 8 + x] = qTableY[y, x];
                        quantisationTableС[y * 8 + x] = qTableC[y, x];
                    }
                byteList.AddRange(DQT);
                byteList.AddRange(DQTLength);
                byteList.Add(quantisationTableInfoY);
                byteList.AddRange(quantisationTableY);
                //byteList.Add(quantisationTableInfoС);
                //byteList.AddRange(quantisationTableС);
            }

            // Define Huffman Tables
            {
                List<string> strList = new List<string>();
                foreach (string s in asd)
                {
                    strList.Add("0000" + Convert.ToString(s.Length & 0x0F, 2).PadLeft(4, '0') + s);
                }

                string str = string.Join("", strList);
                str.PadRight(str.Length + (8 - str.Length % 8), '1');

                Console.WriteLine(str.Length);

                for (int i = 0; i < str.Length / 8; i++)
                {
                    DHTCodesDC_Y.Add(Convert.ToByte(str.Substring(8 * i, 8), 2));
                }

                short DHTLengthCalculation = (short)(DHT.Length + 1 + byteCountsDC_Y.Length + tableValuesDC_Y.Count);
                DHTCodeAmountDC_Y = byteCountsDC_Y;

                DHTLength[0] = (byte)((DHTLengthCalculation >> 8) & 0xff);
                DHTLength[1] = (byte)(DHTLengthCalculation & 0xff);

                Console.WriteLine(" _____ " + String.Join(", ", tableValuesDC_Y));
                Console.WriteLine(byteCountsDC_Y.Length);

                byteList.AddRange(DHT);
                byteList.AddRange(DHTLength);
                byteList.Add(DHTInfoDC_Y);
                byteList.AddRange(byteCountsDC_Y);
                byteList.AddRange(tableValuesDC_Y);
        }

            // Start of Frame
            {
                short SOFLengthCalculation = (short)(SOFLength.Length + 1/*precision*/ + frameHeight.Length +
                    frameWidth.Length + 1/*channelAmount*/ + 3);//9 /*channel data*/);
                SOFLength[0] = (byte)((SOFLengthCalculation >> 8) & 0xFF);
                SOFLength[1] = (byte)(SOFLengthCalculation & 0xFF);

                frameHeight[0] = (byte)((height >> 8) & 0xFF);
                frameHeight[1] = (byte)(height & 0xFF);
                frameWidth[0] = (byte)((width >> 8) & 0xFF);
                frameWidth[1] = (byte)(width & 0xFF);

                byteList.AddRange(SOF);
                byteList.AddRange(SOFLength);
                byteList.Add(precision);
                byteList.AddRange(frameHeight);
                byteList.AddRange(frameWidth);
                byteList.Add(channelAmount);
                byteList.Add(channelID_Y);
                byteList.Add(samplingFactorY);
                byteList.Add(quantisationTableID_Y);
                /*byteList.Add(channelID_Cb);
                byteList.Add(samplingFactorCb);
                byteList.Add(quantisationTableID_Cb);
                byteList.Add(channelID_Cr);
                byteList.Add(samplingFactorCr);
                byteList.Add(quantisationTableID_Cr);*/
            }
            // Start of Scan
            {
                short SOSLengthCalculation = (short)(SOSLength.Length + 5);

                byteList.AddRange(SOS);
                byteList.AddRange(SOSLength);
                byteList.Add(SOSchannelAmount);
                byteList.Add(SOSChannelID_Y);
                byteList.Add(StartOfSelection);
                byteList.Add(EndOfSelection);
                byteList.Add(SuccessiveApproximation);
                byteList.AddRange(DHTCodesDC_Y);

                byteList.AddRange(EOI);
            }
            File.WriteAllBytes("funny_jpeg.jpg", byteList.ToArray());
        }

    }
}
