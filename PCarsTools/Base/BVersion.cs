using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PCarsTools.Base
{
    public struct BVersion
    {
        public byte Major { get; set; }
        public byte Minor { get; set; }
        public short Interim { get; set; }
        public short Auto { get; set; }

        public BVersion(uint version)
        {
            Major = (byte)(version >> 28); // 4 bits
            Minor = (byte)(version >> 22 & 0b11_1111); // 6 bits
            Interim = (short)(version >> 11 & 0b111_1111_1111); // 11 bits
            Auto = (short)(version & 0x111_1111_1111); // 11 bits
        }
    }
}
