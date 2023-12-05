﻿namespace anotherJpeg
{
    internal class TableData
    {
        static readonly byte[,] qTableLum = {
                {16,    11,    10,    16,    24,    40,    51,    61},
                {12,    12,    14,    19,    26,    58,    60,    55},
                {14,    13,    16,    24,    40,    57,    69,    56},
                {14,    17,    22,    29,    51,    87,    80,    62},
                {18,    22,    37,    56,    68,   109,   103,    77},
                {24,    35,    55,    64,    81,   104,   113,    92},
                {49,    64,    78,    87,   103,   121,   120,   101},
                {72,    92,    95,    98,   112,   100,   103,    99}
            };
        static readonly byte[,] qTableChrom = {
                {17,    18,    24,    47,    99,    99,    99,     99},
                {18,    21,    26,    66,    99,    99,    99,     99},
                {24,    26,    56,    99,    99,    99,    99,     99},
                {47,    66,    99,    99,    99,    99,    99,     99},
                {99,    99,    99,    99,    99,    99,    99,     99},
                {99,    99,    99,    99,    99,    99,    99,     99},
                {99,    99,    99,    99,    99,    99,    99,     99},
                {99,    99,    99,    99,    99,    99,    99,     99}
            };
        static readonly byte[][] huffmanTable_LumDC = {
/*  1 */    new byte[0],
/*  2 */    new byte[] { 0 },
/*  3 */    new byte[] { 1, 2, 3, 4, 5 },
/*  4 */    new byte[] { 6 },
/*  5 */    new byte[] { 7 },
/*  6 */    new byte[] { 8 },
/*  7 */    new byte[] { 9 },
/*  8 */    new byte[] { 10 },
/*  9 */    new byte[] { 11 },
/* 10 */    new byte[0],
/* 11 */    new byte[0],
/* 12 */    new byte[0],
/* 13 */    new byte[0],
/* 14 */    new byte[0],
/* 15 */    new byte[0],
/* 16 */    new byte[0]
            };
        static readonly byte[][] huffmanTable_LumAC = {
/*  1 */     new byte[0],
/*  2 */     new byte[] { 1, 2 },
/*  3 */     new byte[] { 3 },
/*  4 */     new byte[] { 0, 4, 17 },
/*  5 */     new byte[] { 5, 18, 33 },
/*  6 */     new byte[] { 49, 65 },
/*  7 */     new byte[] { 6, 19, 81, 97 },
/*  8 */     new byte[] { 7, 34, 113 },
/*  9 */     new byte[] { 20, 50, 129, 145, 161 },
/* 10 */     new byte[] { 8, 35, 66, 177, 193 },
/* 11 */     new byte[] { 21, 82, 209, 240 },
/* 12 */     new byte[] { 36, 51, 98, 114 },
/* 13 */     new byte[0],
/* 14 */     new byte[0],
/* 15 */     new byte[] { 130 },
/* 16 */     new byte[] { 9, 10, 22, 23, 24, 25, 26, 37, 38, 39, 40, 41, 42, 52, 53, 54, 55, 56, 57, 58, 67, 68, 69, 70, 71, 72, 73, 74,
                83, 84, 85, 86, 87, 88, 89, 90, 99, 100, 101, 102, 103, 104, 105, 106, 115, 116, 117, 118, 119, 120, 121, 122, 131, 132,
                133, 134, 135, 136, 137, 138, 146, 147, 148, 149, 150, 151, 152, 153, 154, 162, 163, 164, 165, 166, 167, 168, 169, 170,
                178, 179, 180, 181, 182, 183, 184, 185, 186, 194, 195, 196, 197, 198, 199, 200, 201, 202, 210, 211, 212, 213, 214, 215,
                216, 217, 218, 225, 226, 227, 228, 229, 230, 231, 232, 233, 234, 241, 242, 243, 244, 245, 246, 247, 248, 249, 250 }
             };
        public static byte[,] GetQuantisationTable(int quality, bool isLuminance)
        {

            byte[,] matrix;
            if (isLuminance)
                matrix = qTableLum;
            else
                matrix = qTableChrom;

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
                    matrix[y, x] = (byte)((S * matrix[y, x] + 50) / 100);
                    if (matrix[y, x] == 0)
                        matrix[y, x] = 1;
                }

            return matrix;
        }
        public static byte[] GetHuffmanLengths(bool isLuminance, bool isDC)
        {
            byte[][] table = new byte[0][];
            if (isLuminance && isDC)
                table = huffmanTable_LumDC;
            else if (isLuminance && !isDC)
                table = huffmanTable_LumAC;
            /*else if (!isLuminance && isDC)
                  table = HuffmanTable_ChromDC;
              else
                  table = HuffmanTable_ChromAC;*/

            byte[] bytes = new byte[16];

            for (int i = 0; i < 16; i++)
                bytes[i] = (byte)table[i].Length;


            return bytes;
        }
        public static List<byte> GetHuffmanValues(bool isLuminance, bool isDC)
        {
            byte[][] table = new byte[0][];
            if (isLuminance && isDC)
                table = huffmanTable_LumDC;
            else if (isLuminance && !isDC)
                table = huffmanTable_LumAC;
            /*else if (!isLuminance && isDC)
                  table = HuffmanTable_ChromDC;
              else
                  table = HuffmanTable_ChromAC;*/

            List<byte> bytes = new List<byte>();

            for (int i = 0; i < table.Length; i++)
            {
                for (int j = 0; j < table[i].Length; j++)
                {
                    bytes.Add(table[i][j]);
                }
            }

            return bytes;
        }
    }
}
