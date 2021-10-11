using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PCarsTools
{
    public class BExtendedFileInfoEntry
    {
        public long Offset { get; set; }
        public long TimeStamp { get; set; }

        public string Path { get; set; }

        public override string ToString()
           => $"{Path} (0x{Offset:X8})";

    }
}
