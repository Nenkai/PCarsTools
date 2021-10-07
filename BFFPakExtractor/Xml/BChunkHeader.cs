using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BFFPakExtractor.Xml
{
    public struct BChunkHeader
    {
        public uint Id;
        public uint Size;
        public uint Offset;
        public int Pad;
    }
}
