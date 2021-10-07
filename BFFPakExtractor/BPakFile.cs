using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Syroot.BinaryData;
using Syroot.BinaryData.Core;
using Syroot.BinaryData.Memory;

using BFFPakExtractor.Encryption;
using BFFPakExtractor.Config;

namespace BFFPakExtractor
{
    public class BPakFile
    {
        public const string TagId = "PAK ";

        public BVersion Version { get; set; }
        public string Name { get; set; }
        public bool BigEndian { get; set; }
        public eEncryptionType EncryptionType { get; set; }

        public List<BPakFileTocEntry> Entries { get; set; }

        public static BPakFile FromStream(Stream stream, int index = 0)
        {
            var pak = new BPakFile();

            using var bs = new BinaryStream(stream, leaveOpen: true);
            int mID = bs.ReadInt32();
            pak.Version = new BVersion(bs.ReadUInt32());
            int assetCount = bs.ReadInt32();
            bs.Position += 12;
            pak.Name = bs.ReadString(0x100).TrimEnd('\0');

            uint pakFileTocEntrySize = bs.ReadUInt32();
            uint crc = bs.ReadUInt32();
            uint extInfoSize = bs.ReadUInt32();
            bs.Position += 8;
            pak.BigEndian = bs.ReadBoolean(BooleanCoding.Byte);
            pak.EncryptionType = (eEncryptionType)bs.Read1Byte();
            bs.Position += 2;

            var pakTocBuffer = bs.ReadBytes((int)pakFileTocEntrySize);

            if (pak.EncryptionType != eEncryptionType.None)
            {
                int keyIndex = BConfig.Instance.GetPatternIdx(pak.Name);
                BPakFileEncryption.DecryptData(pak.EncryptionType, pakTocBuffer, pakTocBuffer.Length, keyIndex);
            }

            pak.Entries = new List<BPakFileTocEntry>(assetCount);
            SpanReader sr = new SpanReader(pakTocBuffer);
            for (int i = 0; i < assetCount; i++)
            {
                sr.Position = (i * 0x2A);
                var pakFileToCEntry = new BPakFileTocEntry();
                pakFileToCEntry.UId = sr.ReadUInt64();
                pakFileToCEntry.Offset = sr.ReadUInt64();
                pakFileToCEntry.PakSize = sr.ReadUInt32();
                pakFileToCEntry.FileSize = sr.ReadUInt32();
                pakFileToCEntry.File = sr.ReadUInt32();
                pakFileToCEntry.PakFile = sr.ReadUInt32();
                pakFileToCEntry.Flags = sr.ReadUInt16();
                pakFileToCEntry.CRC = sr.ReadUInt32();
                pakFileToCEntry.Extension = sr.ReadStringRaw(4).ToCharArray();
                pak.Entries.Add(pakFileToCEntry);
            }

            bs.Position += 0x308;
            var extTocBuffer = bs.ReadBytes((int)extInfoSize);

            /* TODO - Custom
            if (pak.EncryptionType != eEncryptionType.None)
            {
                int keyIndex = BConfig.Instance.GetPatternIdx(pak.Name);
                BPakFileEncryption.DecryptData(pak.EncryptionType, extTocBuffer, extTocBuffer.Length, keyIndex);
            }*/

            return pak;
        }
    }
}
