using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PCarsTools
{
    public class BPakInfo
    {
        public uint AssetCount { get; set; }
        public uint PakSize { get; set; }
        public uint PakOffset { get; set; }
        public string PakName { get; set; }
    }
}
