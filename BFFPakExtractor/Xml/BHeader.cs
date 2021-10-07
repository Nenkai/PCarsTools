using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BFFPakExtractor.Xml
{
    public struct BHeader
    {
        public uint mElementCount;
        public uint mAttributeCount;
        public uint mCollectionCount;
        public uint mNumberCount;
        public uint mStringCount;
        public uint mBoolCount;
        public int RootNodeIndex;
    }
}
