using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static anotherJpeg.MarkerData;
namespace anotherJpeg
{
    internal class FileWriter
    {
        public static void Write2File(byte[,] qTableLum, byte[,] qTableChrom, 
            short imageHeight, short imageWidth, List<EncodedValue> codesLum, 
            List<EncodedValue> codesCb, List<EncodedValue> codesCr)
        {
            List<byte> data = new List<byte>();
            AddHeaderData(data);

            AddQuantisationTableData(data, qTableLum, qTableChrom);
            AddStartOfFrameData(data, imageHeight, imageWidth);

            AddHuffmanTableDefinitionData(data, DHTInfo_LumDC, DHTInfo_LumAC,
                TableData.GetHuffmanLengths(true, true),  TableData.GetHuffmanValues(true, true),
                TableData.GetHuffmanLengths(true, false), TableData.GetHuffmanValues(true, false));
            // AddHuffmanTableDefinitionData() для цветности

            AddStartOfScanData(data);
            AddSOSEncodedValueData(data, codesLum);
            AddSOSEncodedValueData(data, codesCb);
            AddSOSEncodedValueData(data, codesCr);

            data.AddRange(EOI); // End of Image

            File.WriteAllBytes("funny_jpeg.jpg", data.ToArray());
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
        static void AddQuantisationTableData(List<byte> data, byte[,] qTableLum, byte[,] qTableChrom)
        {
            byte[] qTableArrayLum = new byte[64];
            byte[] qTableArrayChrom = new byte[64];
            for (int y = 0; y < 8; y++)
                for (int x = 0; x < 8; x++)
                {
                    qTableArrayLum[y * 8 + x] = qTableLum[y, x];
                    qTableArrayChrom[y * 8 + x] = qTableChrom[y, x];
                }

            data.AddRange(DQT);
            data.AddRange(DQTLength);
            data.Add(qTableInfoLum);
            data.AddRange(qTableArrayLum);
            data.Add(qTableInfoСhrom);
            data.AddRange(qTableArrayChrom);
        }
        static void AddStartOfFrameData(List<byte> data, short imageHeight, short imageWidth)
        {
            short SOFLengthCalculation = (short)(
                SOFLength.Length +
                1 + // precision
                frameHeight.Length +
                frameWidth.Length +
                1 + // channelAmount
                9 // channel data
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

            data.Add(channelID_Cb);
            data.Add(samplingFactorCb);
            data.Add(quantisationTableID_Cb);

            data.Add(channelID_Cr);
            data.Add(samplingFactorCr);
            data.Add(quantisationTableID_Cr);
        }
        static void AddHuffmanTableDefinitionData(List<byte> data, byte infoDC, byte infoAC,
            byte[] byteCounts_DC, List<byte> tableValues_DC, byte[] byteCounts_AC, List<byte> tableValues_AC)
        {
            short DHTLengthCalculation = (short)(
                DHT.Length +
                1 + /*DHTInfo_DC*/
                16 + /*byteCounts_DC.Length*/
                tableValues_DC.Count +
                1 + /*DHTInfo_AC*/
                16 + /*byteCounts_AC.Length*/
                tableValues_AC.Count
                );


            DHTLength = Short2ByteArray(DHTLengthCalculation);

            data.AddRange(DHT);
            data.AddRange(DHTLength);

            data.Add(infoDC);
            data.AddRange(byteCounts_DC);
            data.AddRange(tableValues_DC);

            data.Add(infoAC);
            data.AddRange(byteCounts_AC);
            data.AddRange(tableValues_AC);
        }
        static void AddStartOfScanData(List<byte> data)
        {
            short SOSLengthCalculation = (short)(SOSLength.Length + 1 + 4 + 4 + 4);

            SOSLength = Short2ByteArray(SOSLengthCalculation);

            data.AddRange(SOS);
            data.AddRange(SOSLength);

            data.Add(SOSchannelAmount);

            data.Add(SOSChannelID_Y);
            data.Add(StartOfSelection);
            data.Add(EndOfSelection);
            data.Add(SuccessiveApproximation);

            data.Add(SOSChannelID_Y);
            data.Add(StartOfSelection);
            data.Add(EndOfSelection);
            data.Add(SuccessiveApproximation);

            data.Add(SOSChannelID_Y);
            data.Add(StartOfSelection);
            data.Add(EndOfSelection);
            data.Add(SuccessiveApproximation);
        }
        static void AddSOSEncodedValueData(List<byte> data, List<EncodedValue> codes)
        {
            string str = "";
            foreach (EncodedValue code in codes)
            {
                str += code.PrefixBitString + /*(code.Value > 0 ? '0' : '1') +*/ code.ValueBitString;
            }
            if (str.Length % 8 != 0) str.PadRight(str.Length + 8 - (str.Length % 8), '1');

            for (int i = 0; i < str.Length / 8; i++)
                data.Add(Convert.ToByte(str.Substring(i * 8, 8), 2));
        }
    }
}
