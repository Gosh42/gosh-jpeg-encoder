namespace anotherJpeg
{
    internal class MarkerData
    {
        /* ================= Header Data ================== */
        public static readonly byte[] SOI = { 0xFF, 0xD8 }; // Start of Image
        public static readonly byte[] APP0 = { 0xFF, 0xE0 };
        public static readonly byte[] headerLength = { 0x00, 0x10 }; // Длина заголовка. В данном случае 0x10 = 16 байт, включая саму длину
        public static readonly byte[] JFIFIdentifier = { 0x4A, 0x46, 0x49, 0x46, 0x00 }; // Идентификатор - "JFIF" в ASCII
        public static readonly byte[] JFIFVersion = { 0x01, 0x02 }; // Версия JFIF: 1.02
        public static readonly byte densityType = 0x00; // 0x00 - плотность пикселей - т.е. "pixels per cm"/"pixels per inch".
                                                 // В данном случае без каких-либо единиц плотности
        public static readonly byte[] xDensity = { 0x00, 0x01 }; // Плотность по x
        public static readonly byte[] yDensity = { 0x00, 0x01 }; // Плотность по y
        public static readonly byte xThumbnail = 0x00; // Ширина превью
        public static readonly byte yThumbnail = 0x00; // Высота превью. Ширина/высота по нулям, т.к. превью не будет
        // Если бы тут было превью, то оно заняло бы (3 * ширина * высота) байт - закодированы каждый цветовой канал


        /* ============= Quantisation Tables ============== */
        public static readonly byte[] DQT = { 0xFF, 0xDB }; // Define Quantisation Tables
        public static readonly byte[] DQTLength = { 0x00, 2 + 1 + 64/*+1+64*/ };
        public static readonly byte qTableInfoLum = 0x00; // 8 бит, ID = 0
        // 64 байт - матрица квантования для яркости
        public static readonly byte qTableInfoСhrom = 0x01; // 8 бит, ID = 1
        // 64 байт - матрица квантования для цветности


        // Тут был бы маркер DRI и его информация, но я его использовать не буду


        /* =============== Start of Frame ================= */
        public static readonly byte[] SOF = { 0xFF, 0xC0 }; // Start of Frame
        public static byte[] SOFLength = new byte[2];
        public static byte precision = 0x08; // Бит на цветовой канал - всегда 8
        public static byte[] frameHeight = new byte[2];
        public static byte[] frameWidth = new byte[2];
        public static byte channelAmount = 0x01;//3; // 3 - для YCbCr

        // Y
        public static byte channelID_Y = 0x01;
        public static byte samplingFactorY = 0x11;
        public static byte quantisationTableID_Y = 0x00;
        // Cb
        public static byte channelID_Cb = 0x02;
        public static byte samplingFactorCb = 0x11;
        public static byte quantisationTableID_Cb = 0x01;
        // Cr
        public static byte channelID_Cr = 0x03;
        public static byte samplingFactorCr = 0x11;
        public static byte quantisationTableID_Cr = 0x01;


        /* ============ Define Huffman Tables ============= */
        public static readonly byte[] DHT = { 0xFF, 0xC4 };
        public static byte[] DHTLength = new byte[2];
        public static readonly byte DHTInfo_LumDC = 0x00; // DC, ID: 0
        public static readonly byte DHTInfo_LumAC = 0x10; // AC, ID: 0
                                                   //static byte[] DHTCodeAmountDC_Y = new byte[16];
                                                   //static List<byte> DHTCodesDC_Y = new List<byte>();


        // тут короче резня, потом допишу


        /* ================ Start of Scan ================= */
        public static byte[] SOS = { 0xFF, 0xDA }; // Start of Scan
        public static byte[] SOSLength = new byte[2];
        public static byte SOSchannelAmount = 0x01; // кол-во компонентов
        
        public static readonly byte SOSChannelID_Y = 0x00;
        
        public static readonly byte EndOfSelection = 0x3F;
        public static readonly byte StartOfSelection = 0x00;
        public static readonly byte SuccessiveApproximation = 0x00;
        
        public static readonly byte[] EOI = { 0xFF, 0xD9 }; // End of Image
    }
}
