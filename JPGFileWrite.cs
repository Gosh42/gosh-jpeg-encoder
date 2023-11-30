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
        static readonly byte DHTInfo_LuminanceDC = 0x00; // DC, ID: 0
        //static byte[] DHTCodeAmountDC_Y = new byte[16];
        //static List<byte> DHTCodesDC_Y = new List<byte>();

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
            short imageHeight, short imageWidth,
            byte[] byteCounts_LuminanceDC, List<byte> tableValues_LuminanceDC, List<string> HuffmanCodes_LuminanceDC
            )
        {
            List<byte> data = new List<byte>();

            // Заголовок
            AddHeaderData(data);

            // Таблицы квантования
            AddQuantisationTableData(data, qTableY, qTableC);

            // Define Huffman Tables
            AddHuffmanTableDefinitionData(data, byteCounts_LuminanceDC, tableValues_LuminanceDC);

            // Start of Frame
            AddStartOfFrameData(data, imageHeight, imageWidth);

            // Start of Scan
            AddStartOfScanData(data);
            AddSOSHuffmanData(data, HuffmanCodes_LuminanceDC);

            // End of Image
            data.AddRange(EOI);

            File.WriteAllBytes("funny_jpeg.jpg", data.ToArray());
        }


        static void AddHeaderData(List<byte> data)
        {
            data.AddRange(SOI);
            data.AddRange(APP0);
            data.AddRange(headerLength);
            data.AddRange(JFIFIdentifier);
            data.AddRange(JFIFVersion);
            data.Add(densityType);
            data.AddRange(xDensity);
            data.AddRange(yDensity);
            data.Add(xThumbnail);
            data.Add(yThumbnail);
        }
        static void AddQuantisationTableData(List<byte> data, byte[,] qTableLuminance, byte[,] qTableChrominance)
        {
            for (int y = 0; y < 8; y++)
                for (int x = 0; x < 8; x++)
                {
                    quantisationTableY[y * 8 + x] = qTableLuminance[y, x];
                    quantisationTableС[y * 8 + x] = qTableChrominance[y, x];
                }

            data.AddRange(DQT);
            data.AddRange(DQTLength);
            data.Add(quantisationTableInfoY);
            data.AddRange(quantisationTableY);
            //data.Add(quantisationTableInfoС);
            //data.AddRange(quantisationTableС);
        }
        static void AddHuffmanTableDefinitionData(List<byte> data, byte[] byteCounts_LuminanceDC, List<byte> tableValuesDC_Y)
        {
            short DHTLengthCalculation = (short)(
                DHT.Length + 
                1 + /*DHTInfo_LuminanceDC*/
                16 + /*byteCounts_LuminanceDC.Length*/
                tableValuesDC_Y.Count
                );

            DHTLength = Short2ByteArray(DHTLengthCalculation);

            data.AddRange(DHT);
            data.AddRange(DHTLength);
            data.Add(DHTInfo_LuminanceDC);
            data.AddRange(byteCounts_LuminanceDC);
            data.AddRange(tableValuesDC_Y);
        }
        static void AddStartOfFrameData(List<byte> data, short imageHeight, short imageWidth)
        {
            short SOFLengthCalculation = (short)(
                SOFLength.Length +
                1 + // precision
                frameHeight.Length +
                frameWidth.Length +
                1 + // channelAmount
                3 //9 // channel data
                );

            SOFLength = Short2ByteArray(SOFLengthCalculation);

            frameHeight = Short2ByteArray(imageHeight);
            frameWidth = Short2ByteArray(imageWidth);

            data.AddRange(SOF);
            data.AddRange(SOFLength);
            data.Add(precision);
            data.AddRange(frameHeight);
            data.AddRange(frameWidth);
            data.Add(channelAmount);
            data.Add(channelID_Y);
            data.Add(samplingFactorY);
            data.Add(quantisationTableID_Y);
          /*data.Add(channelID_Cb);
            data.Add(samplingFactorCb);
            data.Add(quantisationTableID_Cb);
            data.Add(channelID_Cr);
            data.Add(samplingFactorCr);
            data.Add(quantisationTableID_Cr);*/
        }
        static void AddStartOfScanData(List<byte> data)
        {
            short SOSLengthCalculation = (short)(SOSLength.Length + 5);

            SOSLength = Short2ByteArray(SOSLengthCalculation);

            data.AddRange(SOS);
            data.AddRange(SOSLength);
            data.Add(SOSchannelAmount);
            data.Add(SOSChannelID_Y);
            data.Add(StartOfSelection);
            data.Add(EndOfSelection);
            data.Add(SuccessiveApproximation);
            
        }
        static void AddSOSHuffmanData(List<byte> data, List<string> strHuffmanCodes_LuminanceDC)
        {
            List<byte> HuffmanCodes_LuminanceDC = HuffmanCodeStrings2ByteList_DC(strHuffmanCodes_LuminanceDC);

            data.AddRange(HuffmanCodes_LuminanceDC);
        }

        static List<byte> HuffmanCodeStrings2ByteList_DC(List<string> HuffmanCodes_DC)
        {
            string str = "";
            foreach (string s in HuffmanCodes_DC)
                str += "0000" + Convert.ToString(s.Length & 0x0F, 2).PadLeft(4, '0') + s;

            str.PadRight(str.Length + (8 - str.Length % 8), '1');

            List<byte> codeList = new List<byte>();

            for (int i = 0; i < str.Length / 8; i++)
            {
                codeList.Add(Convert.ToByte(str.Substring(8 * i, 8), 2));
            }

            return codeList;
        }
        static byte[] Short2ByteArray(short input)
        {
            byte[] output =
            {
                (byte)(input >> 8),
                (byte)input
            };

            return output;
        }
    }
}
