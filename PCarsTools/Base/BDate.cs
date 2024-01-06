using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PCarsTools
{
    public struct BDate
    {
        public uint Day { get; set; }
        public uint Month { get; set; }
        public uint Year { get; set; }

        public BDate(uint date)
        {
            Day = (date & 0b11111); // 5 bits
            Month = ((date >> 5) & 0b1111); // 4 bits
            Year = ((date >> 9) & 0b11111111_11111111); // 16 bits
        }
    }
}
