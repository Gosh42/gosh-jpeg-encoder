using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace anotherJpeg
{
    internal class EncodedValue
    {
        public short RunLength { get; set; } // предшествующие нули. -1 для компонентов DC
        public byte Size { get; set; } // размер в битах
        public short Value { get; set; } // значение
        public string? PrefixBitString {  get; set; }
        public string? ValueBitString { get; set; }
        
        public EncodedValue(short runLength, byte size, short value)
        {
            RunLength = runLength;
            Size = size;
            Value = value;
        }
    }
}
