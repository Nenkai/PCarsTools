using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BFFPakExtractor
{
    public class BPakFileTocEntry
    {
        public ulong UId { get; set; }
        public ulong Offset { get; set; }
        public uint PakSize { get; set; }
        public uint FileSize { get; set; }
        public uint File;
        public uint PakFile { get; set; }
        public uint Flags { get; set; }
        public uint CRC { get; set; }
        public char[] Extension { get; set; }
    }
}
