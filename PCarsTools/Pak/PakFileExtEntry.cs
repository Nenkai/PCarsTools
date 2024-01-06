using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Syroot.BinaryData.Memory;

namespace PCarsTools.Pak
{
    public class PakFileExtEntry
    {
        public ulong mNameOffset { get; set; }
        public ulong mModifiedTime { get; set; }

        public string Path { get; set; }

        public void Read(ref SpanReader sr)
        {
            mNameOffset = sr.ReadUInt64();
            mModifiedTime = sr.ReadUInt64();
        }

        public override string ToString()
           => $"{Path} (0x{mNameOffset:X8})";

    }
}
