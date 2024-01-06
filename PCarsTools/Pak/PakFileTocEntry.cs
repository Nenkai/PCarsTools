using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Syroot.BinaryData.Memory;

namespace PCarsTools.Pak
{
    public class PakFileTocEntry
    {
        public ulong mUid { get; set; }
        public ulong mDataPos { get; set; }
        public uint mSizeInPak { get; set; }
        public uint mOriginalSize { get; set; }
        public ulong mModifiedTime { get; set; }
        public PakFileCompressionType mFileType { get; set; }
        public byte mPad { get; set; }
        public uint mCRC { get; set; }
        public char[] mExt { get; set; } // extension

        public void Read(ref SpanReader sr)
        {
            mUid = sr.ReadUInt64();
            mDataPos = sr.ReadUInt64();
            mSizeInPak = sr.ReadUInt32();
            mOriginalSize = sr.ReadUInt32();
            mModifiedTime = sr.ReadUInt64();
            mFileType = (PakFileCompressionType)sr.ReadByte();
            mPad = sr.ReadByte();
            mCRC = sr.ReadUInt32();
            mExt = sr.ReadStringRaw(4).ToCharArray();
        }
    }

    public enum PakFileCompressionType : byte
    {
        None,
        ZLib = 1,
        LZX = 2, // Project Cars 1
        Mermaid = 3,
        Kraken = 4,
    }
}
