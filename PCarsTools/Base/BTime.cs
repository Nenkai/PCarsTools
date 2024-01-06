using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PCarsTools
{
    public struct BTime
    {
        public uint Milli { get; set; }
        public uint Sec { get; set; }
        public uint Min { get; set; }
        public uint Hour { get; set; }

        public BTime(uint time)
        {
            Milli = (time & 011_11111111); // 10 bits
            Sec = ((time >> 10) & 0b111111); // 6 bits
            Min = ((time >> 16) & 0b111111); // 6 bits
            Hour = ((time >> 21) & 0b11111); // 5 bits
        }
    }
}
