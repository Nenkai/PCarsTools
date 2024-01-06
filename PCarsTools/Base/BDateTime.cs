using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PCarsTools.Base
{
    public struct BDateTime
    {
        public ulong Milli { get; set; }
        public ulong Sec { get; set; }
        public ulong Min { get; set; }
        public ulong Hour { get; set; }
        public ulong Day { get; set; }
        public ulong Month { get; set; }
        public ulong Year { get; set; }

        public BDateTime(ulong date)
        {
            Milli = date & 0b11_11111111; // 10 bits
            Sec = date >> 10 & 0b11_1111; // 6 bits
            Min = date >> 16 & 0b11_1111; // 6 bits
            Hour = date >> 22 & 0b1_1111; // 5 bits
            Day = date >> 27 & 0b1_1111; // 5 bits
            Month = date >> 32 & 0b1111; // 4 bits
            Year = date >> 36 & 0b11111111_11111111; // 16 bits
        }
    }
}
