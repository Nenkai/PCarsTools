using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PCarsTools.Base;
using PCarsTools.Encryption;

using Syroot.BinaryData;

namespace PCarsTools.Pak
{
    public class PakFileHeader
    {
        public uint mID { get; set; }
        public BVersion mVersion { get; set; }
        public uint mFileCount { get; set; }
        public ulong mDataOffset { get; set; }
        public uint mSectorSize { get; set; }
        public string mFileName { get; set; }
        public uint mTocSize { get; set; }
        public uint mCRCSize { get; set; }
        public uint mExtInfoSize { get; set; }
        public uint mSectionInfoPos { get; set; }
        public uint mSectionInfoSize { get; set; }
        public ePakFlags mFlags { get; set; }
        public eEncryptionType mEncryption { get; set; }

        public void Read(BinaryStream bs)
        {
            mID = bs.ReadUInt32();
            mVersion = new BVersion(bs.ReadUInt32());
            mFileCount = bs.ReadUInt32();
            mDataOffset = bs.ReadUInt64();
            mSectorSize = bs.ReadUInt32();
            mFileName = bs.ReadString(0x100).TrimEnd('\0');
            mTocSize = bs.ReadUInt32();
            mCRCSize = bs.ReadUInt32();
            mExtInfoSize = bs.ReadUInt32();
            mSectionInfoPos = bs.ReadUInt32();
            mSectionInfoSize = bs.ReadUInt32();
            mFlags = (ePakFlags)bs.Read1Byte();
            mEncryption = (eEncryptionType)bs.Read1Byte();
            bs.Position += 2; // mPad2
        }
    }
}
