using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BFFPakExtractor.Xml
{
    public struct BCollection
    {
        public uint Hash;
        public int Index;
        public BCollectionFlags Flags;
        public int Pad;

        public bool IsAttribute
            => Flags.HasFlag(BCollectionFlags.Attribute);
    }

    [Flags]
    public enum BCollectionFlags : uint
    {
        None,
        Attribute = 0x01,
    }
}
