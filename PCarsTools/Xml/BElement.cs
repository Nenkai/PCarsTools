using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PCarsTools.Xml
{
    public struct BElement
    {
        public uint ElementHash;
        public int AttributeIndex;
        public uint AttributeCount;
        public uint ChildCount;
        public int FirstChild;
        public int NextSiblingIndex;
        public int NextInCollection;
    }
}
