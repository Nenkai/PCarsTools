using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BFFPakExtractor.Xml
{
    public struct BAttribute
    {
        public uint AttributeHash;
        public AttributeType ValueType;

        /// <summary>
        /// Index for numbers and bools, offsets for strings
        /// </summary>
        public int Value;
        public uint ValueVectorCount;
        public uint NextInCollection;

        public bool IsType(AttributeType attributeType)
            => GetType() == attributeType;

        public AttributeType GetType()
            => (AttributeType)((uint)ValueType & 3);
    }

    public enum AttributeType : uint
    {
        Number,
        Boolean,
        String
    }
}
