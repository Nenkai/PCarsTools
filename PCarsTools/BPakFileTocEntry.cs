using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PCarsTools
{
    public class BPakFileTocEntry
    {
        public ulong UId { get; set; }
        public ulong Offset { get; set; }
        public uint PakSize { get; set; }
        public uint FileSize { get; set; }
        public ulong TimeStamp { get; set; }
        public PakFileCompressionType Compression { get; set; }
        public byte UnkFlag { get; set; }
        public uint CRC { get; set; }
        public char[] Extension { get; set; }
    }

    public enum PakFileCompressionType : byte
    {
        None,
        ZLib = 1,
        Mermaid = 3,
        Kraken = 4,
    }
}
