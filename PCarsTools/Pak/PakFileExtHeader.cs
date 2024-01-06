using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using PCarsTools.Encryption;

using Syroot.BinaryData;

namespace PCarsTools
{
    public class PakFileExtHeader
    {
        public uint mID { get; set; }
        public uint mInfoSize { get; set; }
        public string mConfigName { get; set; }
        public string mTargetRoot { get; set; }
        public string mPlatformName { get; set; }

        public void Read(BinaryStream bs)
        {
            mID = bs.ReadUInt32();
            mInfoSize = bs.ReadUInt32();
            mConfigName = bs.ReadString(0x100).TrimEnd('\0');
            mTargetRoot = bs.ReadString(0x100).TrimEnd('\0');
            mPlatformName = bs.ReadString(0x100).TrimEnd('\0');
        }

        public static uint GetSize()
        {
            return 0x308;
        }
    }
}
